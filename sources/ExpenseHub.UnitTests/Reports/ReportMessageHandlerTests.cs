using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Core.Domain;
using ExpenseHub.Core.Messaging;
using ExpenseHub.Core.Reports;
using ExpenseHub.UnitTests.Support;
using ExpenseHub.Worker.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Reports;

/// <summary>
/// CP3-I03 — consumo da mensagem pelo worker: confirmação só após salvar, duplicata sem efeito e falhas.
/// </summary>
[TestClass]
public sealed class ReportMessageHandlerTests
{
    private FakeReportRepository _repository = null!;
    private FixedTimeProvider _clock = null!;
    private ReportMessageHandler _handler = null!;

    /// <summary>Cria um handler novo com os reembolsos do exemplo da apostila.</summary>
    [TestInitialize]
    public void Setup()
    {
        _repository = new FakeReportRepository();
        _clock = new FixedTimeProvider();
        _handler = new ReportMessageHandler(new ReportProcessor(_repository, _clock), NullLogger<ReportMessageHandler>.Instance);
        ReportScenario.ApostilaExpenses().ForEach(_repository.SeedExpense);
    }

    /// <summary>Mensagem de solicitação Pending: conclui, salva e só então confirma (ack).</summary>
    [TestMethod]
    public async Task PendingReportIsSavedBeforeAck()
    {
        ReportRequest report = Seed();

        MessageDisposition disposition = await Handle(report.Id);

        Assert.AreEqual(MessageDisposition.Ack, disposition);
        ReportRequest stored = _repository.Stored(report.Id);
        Assert.AreEqual(ReportStatus.Completed, stored.Status);
        Assert.IsNotNull(stored.ResultJson);
    }

    /// <summary>A mesma mensagem de novo (reentrega ou republicação) não gera novo resultado nem nova gravação.</summary>
    [TestMethod]
    public async Task DuplicateMessageHasNoNewEffect()
    {
        ReportRequest report = Seed();
        await Handle(report.Id);
        ReportRequest first = _repository.Stored(report.Id);
        int saves = _repository.SaveCount;
        _clock.Advance(TimeSpan.FromHours(2));

        MessageDisposition disposition = await Handle(report.Id, redelivered: true);

        Assert.AreEqual(MessageDisposition.Ack, disposition);
        ReportRequest second = _repository.Stored(report.Id);
        Assert.AreEqual(first.ResultJson, second.ResultJson);
        Assert.AreEqual(first.CompletedAtUtc, second.CompletedAtUtc);
        Assert.AreEqual(first.ConcurrencyStamp, second.ConcurrencyStamp);
        Assert.AreEqual(saves, _repository.SaveCount);
        Assert.AreEqual(1, _repository.ExpenseQueryCount);
    }

    /// <summary>A proteção contra duplicata está no registro persistido: um handler novo (worker reiniciado) também a respeita.</summary>
    [TestMethod]
    public async Task DuplicateProtectionSurvivesWorkerRestart()
    {
        ReportRequest report = Seed();
        await Handle(report.Id);
        int saves = _repository.SaveCount;
        ReportMessageHandler restarted = new(new ReportProcessor(_repository, _clock), NullLogger<ReportMessageHandler>.Instance);

        MessageDisposition disposition = await restarted.HandleAsync(Body(report.Id), redelivered: false, CancellationToken.None);

        Assert.AreEqual(MessageDisposition.Ack, disposition);
        Assert.AreEqual(saves, _repository.SaveCount);
    }

    /// <summary>Banco indisponível é falha transitória: devolve à fila e não marca como concluído.</summary>
    [TestMethod]
    public async Task TransientFailureRequeuesWithoutCompleting()
    {
        ReportRequest report = Seed();
        _repository.ThrowOnSave = true;

        MessageDisposition disposition = await Handle(report.Id);

        _repository.ThrowOnSave = false;
        Assert.AreEqual(MessageDisposition.Requeue, disposition);
        Assert.AreEqual(ReportStatus.Pending, _repository.Stored(report.Id).Status);
    }

    /// <summary>Conflito de concorrência devolve à fila; a próxima entrega conclui normalmente.</summary>
    [TestMethod]
    public async Task ConflictRequeuesAndNextDeliveryCompletes()
    {
        ReportRequest report = Seed();
        _repository.FailNextSave = true;

        Assert.AreEqual(MessageDisposition.Requeue, await Handle(report.Id));
        Assert.AreEqual(MessageDisposition.Ack, await Handle(report.Id, redelivered: true));
        Assert.AreEqual(ReportStatus.Completed, _repository.Stored(report.Id).Status);
    }

    /// <summary>Limite de tentativas atingido vira Failed e é confirmado, sem reencaminhamento infinito.</summary>
    [TestMethod]
    public async Task MaxAttemptsMarksFailedAndAcks()
    {
        ReportRequest report = TestData.Report(TestData.AnaId, ReportScope.Mine, ReportScenario.From, ReportScenario.To);
        report.Status = ReportStatus.Processing;
        report.Attempts = ReportProcessor.MaxAttempts;
        _repository.Seed(report);

        MessageDisposition disposition = await Handle(report.Id, redelivered: true);

        Assert.AreEqual(MessageDisposition.Ack, disposition);
        ReportRequest stored = _repository.Stored(report.Id);
        Assert.AreEqual(ReportStatus.Failed, stored.Status);
        Assert.AreEqual(ReportProcessor.MaxAttemptsCode, stored.ErrorCode);
        Assert.IsNull(stored.ResultJson);
        Assert.AreEqual(0, _repository.ExpenseQueryCount);
    }

    /// <summary>Falha definitiva (período inválido registrado) é confirmada e não volta para a fila.</summary>
    [TestMethod]
    public async Task DefinitiveFailureIsAcked()
    {
        ReportRequest report = TestData.Report(TestData.AnaId, ReportScope.Mine, ReportScenario.To, ReportScenario.From);
        _repository.Seed(report);

        Assert.AreEqual(MessageDisposition.Ack, await Handle(report.Id));
        Assert.AreEqual(ReportStatus.Failed, _repository.Stored(report.Id).Status);
    }

    /// <summary>Solicitação inexistente é descartada (ack) sem gravar nada.</summary>
    [TestMethod]
    public async Task UnknownReportIsAckedWithoutEffect()
    {
        Assert.AreEqual(MessageDisposition.Ack, await Handle(Guid.NewGuid()));
        Assert.AreEqual(0, _repository.SaveCount);
    }

    /// <summary>Mensagem sem reportId válido é descartada sem consultar o banco.</summary>
    /// <param name="payload">Corpo recebido da fila.</param>
    [TestMethod]
    [DataRow("não é json")]
    [DataRow("{}")]
    [DataRow("{\"reportId\":\"00000000-0000-0000-0000-000000000000\"}")]
    public async Task InvalidMessageIsAckedWithoutEffect(string payload)
    {
        MessageDisposition disposition =
            await _handler.HandleAsync(Encoding.UTF8.GetBytes(payload), redelivered: false, CancellationToken.None);

        Assert.AreEqual(MessageDisposition.Ack, disposition);
        Assert.AreEqual(0, _repository.SaveCount);
    }

    private static ReadOnlyMemory<byte> Body(Guid reportId) => new ReportMessage(reportId).ToBytes();

    private Task<MessageDisposition> Handle(Guid reportId, bool redelivered = false) =>
        _handler.HandleAsync(Body(reportId), redelivered, CancellationToken.None);

    private ReportRequest Seed()
    {
        ReportRequest report = TestData.Report(TestData.AnaId, ReportScope.Mine, ReportScenario.From, ReportScenario.To);
        _repository.Seed(report);
        return report;
    }
}
