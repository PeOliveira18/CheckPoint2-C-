using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Application;
using ExpenseHub.Api.Contracts;
using ExpenseHub.Api.Domain;
using ExpenseHub.UnitTests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Expenses;

/// <summary>
/// I08 — histórico completo do fluxo e visibilidade da consulta.
/// </summary>
[TestClass]
public sealed class ExpenseHistoryTests
{
    private FakeExpenseRepository _repository = null!;
    private FixedTimeProvider _clock = null!;
    private ExpenseService _service = null!;

    /// <summary>Cria um serviço novo para cada teste.</summary>
    [TestInitialize]
    public void Setup()
    {
        _repository = new FakeExpenseRepository();
        _clock = new FixedTimeProvider();
        _service = new ExpenseService(_repository, _clock);
    }

    /// <summary>Fluxo completo registra cada ação, com ator e estados, em ordem cronológica.</summary>
    [TestMethod]
    public async Task FullFlowRecordsEveryActionInOrder()
    {
        Guid id = await RunFullFlowAsync();

        ServiceResult<IReadOnlyList<ExpenseHistoryResponse>> result =
            await _service.GetHistoryAsync(TestData.Edu, id, CancellationToken.None);

        Assert.IsTrue(result.Succeeded);
        CollectionAssert.AreEqual(
            new[] { ExpenseAction.Created, ExpenseAction.Updated, ExpenseAction.Submitted, ExpenseAction.Approved, ExpenseAction.Paid },
            result.Value.Select(entry => entry.Action).ToArray());
        CollectionAssert.AreEqual(
            new[] { TestData.AnaId, TestData.AnaId, TestData.AnaId, TestData.CarlaId, TestData.DaniId },
            result.Value.Select(entry => entry.ActorId).ToArray());
        Assert.AreEqual(ExpenseStatus.Paid, result.Value[^1].ToStatus);
        Assert.IsNotNull(result.Value[1].Changes);
    }

    /// <summary>O histórico segue a visibilidade do reembolso: proprietário e Auditor veem; outro Employee e Approver (após aprovação) recebem 404.</summary>
    [TestMethod]
    public async Task HistoryFollowsExpenseVisibility()
    {
        Guid id = await RunFullFlowAsync();

        Assert.IsTrue((await _service.GetHistoryAsync(TestData.Ana, id, CancellationToken.None)).Succeeded);
        Assert.IsTrue((await _service.GetHistoryAsync(TestData.Dani, id, CancellationToken.None)).Succeeded);
        Assert.AreEqual(ServiceError.NotFound, (await _service.GetHistoryAsync(TestData.Bruno, id, CancellationToken.None)).Error);
        Assert.AreEqual(ServiceError.NotFound, (await _service.GetHistoryAsync(TestData.Carla, id, CancellationToken.None)).Error);
        Assert.AreEqual(ServiceError.Forbidden, (await _service.GetHistoryAsync(TestData.Fabio, id, CancellationToken.None)).Error);
    }

    /// <summary>Histórico de reembolso inexistente retorna 404.</summary>
    [TestMethod]
    public async Task HistoryOfUnknownExpenseReturnsNotFound()
    {
        ServiceResult<IReadOnlyList<ExpenseHistoryResponse>> result =
            await _service.GetHistoryAsync(TestData.Edu, Guid.NewGuid(), CancellationToken.None);

        Assert.AreEqual(ServiceError.NotFound, result.Error);
    }

    /// <summary>A reprovação fica no histórico com a justificativa e encerra o fluxo (não pode ser paga).</summary>
    [TestMethod]
    public async Task RejectionIsRecordedAndIsFinal()
    {
        ServiceResult<ExpenseResponse> created =
            await _service.CreateAsync(TestData.Ana, TestData.ValidInput(_clock.Today), CancellationToken.None);
        Guid id = created.Value!.Id;
        await _service.SubmitAsync(TestData.Ana, id, CancellationToken.None);
        await _service.RejectAsync(TestData.Carla, id, "Despesa fora da política de viagens.", CancellationToken.None);

        ServiceResult<ExpenseResponse> pay = await _service.PayAsync(TestData.Dani, id, CancellationToken.None);
        ServiceResult<IReadOnlyList<ExpenseHistoryResponse>> history =
            await _service.GetHistoryAsync(TestData.Edu, id, CancellationToken.None);

        Assert.AreEqual(ServiceError.Conflict, pay.Error);
        ExpenseHistoryResponse rejected = history.Value!.Single(entry => entry.Action == ExpenseAction.Rejected);
        Assert.AreEqual("Despesa fora da política de viagens.", rejected.Justification);
        Assert.AreEqual(ExpenseStatus.Rejected, rejected.ToStatus);
    }

    private async Task<Guid> RunFullFlowAsync()
    {
        ServiceResult<ExpenseResponse> created =
            await _service.CreateAsync(TestData.Ana, TestData.ValidInput(_clock.Today), CancellationToken.None);
        Guid id = created.Value!.Id;
        _clock.Advance(TimeSpan.FromMinutes(1));
        await _service.UpdateAsync(TestData.Ana, id, TestData.ValidInput(_clock.Today) with { Amount = 42m }, CancellationToken.None);
        _clock.Advance(TimeSpan.FromMinutes(1));
        await _service.SubmitAsync(TestData.Ana, id, CancellationToken.None);
        _clock.Advance(TimeSpan.FromMinutes(1));
        await _service.ApproveAsync(TestData.Carla, id, CancellationToken.None);
        _clock.Advance(TimeSpan.FromMinutes(1));
        await _service.PayAsync(TestData.Dani, id, CancellationToken.None);
        return id;
    }
}
