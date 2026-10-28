using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Application;
using ExpenseHub.Api.Auth;
using ExpenseHub.Api.Contracts;
using ExpenseHub.Api.Dtos;
using ExpenseHub.Core.Domain;
using ExpenseHub.Core.Reports;
using ExpenseHub.UnitTests.Support;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Reports;

/// <summary>
/// CP3-I02 — permissão por perfil e escopo e isolamento por solicitante.
/// </summary>
[TestClass]
public sealed class ReportIsolationTests
{
    private static readonly string[] _requestFields = ["FromDate", "ToDate", "Scope"];

    private FakeReportRepository _repository = null!;
    private FakeReportPublisher _publisher = null!;
    private FixedTimeProvider _clock = null!;
    private ReportService _service = null!;

    /// <summary>Cria um serviço novo com os reembolsos do exemplo da apostila.</summary>
    [TestInitialize]
    public void Setup()
    {
        _repository = new FakeReportRepository();
        _publisher = new FakeReportPublisher();
        _clock = new FixedTimeProvider();
        _service = new ReportService(_repository, _publisher, _clock, NullLogger<ReportService>.Instance);
        ReportScenario.ApostilaExpenses().ForEach(_repository.SeedExpense);
    }

    /// <summary>
    /// Matriz de solicitação: Mine exige Employee e Global exige Auditor; Admin, Approver e Finance não ganham
    /// privilégio implícito.
    /// </summary>
    /// <param name="roles">Roles do solicitante, separadas por vírgula (vazio = sem role).</param>
    /// <param name="scope">Escopo pedido.</param>
    /// <param name="allowed">Se a solicitação deve ser aceita.</param>
    [TestMethod]
    [DataRow("Employee", "Mine", true)]
    [DataRow("Employee", "Global", false)]
    [DataRow("Auditor", "Global", true)]
    [DataRow("Auditor", "Mine", false)]
    [DataRow("Employee,Auditor", "Mine", true)]
    [DataRow("Employee,Auditor", "Global", true)]
    [DataRow("Admin", "Mine", false)]
    [DataRow("Admin", "Global", false)]
    [DataRow("Approver", "Mine", false)]
    [DataRow("Finance", "Global", false)]
    [DataRow("", "Mine", false)]
    public async Task RequestFollowsRoleAndScopeMatrix(string roles, string scope, bool allowed)
    {
        UserContext user = TestData.User("solicitante", roles.Split(',', StringSplitOptions.RemoveEmptyEntries));

        ServiceResult<ReportSubmission> result = await Request(user, Enum.Parse<ReportScope>(scope));

        Assert.AreEqual(allowed, result.Succeeded);
        if (!allowed)
        {
            Assert.AreEqual(ServiceError.Forbidden, result.Error);
        }
    }

    /// <summary>Employee pedindo Global é negado sem criar registro nem publicar mensagem.</summary>
    [TestMethod]
    public async Task DeniedScopeCreatesNoWork()
    {
        ServiceResult<ReportSubmission> result = await Request(TestData.Ana, ReportScope.Global);

        Assert.AreEqual(ServiceError.Forbidden, result.Error);
        Assert.AreEqual(0, _repository.StoredCount);
        Assert.IsEmpty(_publisher.PublishedIds);
    }

    /// <summary>O cliente não informa o solicitante: o contrato de entrada só tem datas e escopo.</summary>
    [TestMethod]
    public void RequestContractHasNoOwnerField()
    {
        CollectionAssert.AreEquivalent(
            _requestFields,
            typeof(CreateReportRequest).GetProperties().Select(property => property.Name).ToList());
    }

    /// <summary>A rota de solicitação exige Employee ou Auditor; Admin sozinho fica de fora já na rota.</summary>
    [TestMethod]
    public void RequestPolicyAllowsOnlyEmployeeAndAuditor()
    {
        AuthorizationOptions options = new();
        Policies.Configure(options);

        AuthorizationPolicy? policy = options.GetPolicy(Policies.ReportRequester);

        Assert.IsNotNull(policy);
        RolesAuthorizationRequirement requirement = policy.Requirements.OfType<RolesAuthorizationRequirement>().Single();
        CollectionAssert.AreEquivalent(new[] { RoleNames.Employee, RoleNames.Auditor }, requirement.AllowedRoles.ToList());
    }

    /// <summary>Auditor não consulta relatório criado por outro usuário (404), nem aparece na listagem dele.</summary>
    [TestMethod]
    public async Task AuditorDoesNotSeeReportsCreatedByOthers()
    {
        ServiceResult<ReportSubmission> anas = await Request(TestData.Ana, ReportScope.Mine);
        ServiceResult<ReportSubmission> own = await Request(TestData.Edu, ReportScope.Global);

        ServiceResult<ReportResponse> detail = await _service.GetAsync(TestData.Edu, anas.Value!.Report.Id, CancellationToken.None);
        ServiceResult<IReadOnlyList<ReportResponse>> list = await _service.ListAsync(TestData.Edu, CancellationToken.None);

        Assert.AreEqual(ServiceError.NotFound, detail.Error);
        CollectionAssert.AreEqual(new[] { own.Value!.Report.Id }, list.Value!.Select(report => report.Id).ToList());
    }

    /// <summary>Admin sem roles funcionais não ganha acesso a relatórios alheios.</summary>
    [TestMethod]
    public async Task AdminDoesNotSeeReportsCreatedByOthers()
    {
        ServiceResult<ReportSubmission> anas = await Request(TestData.Ana, ReportScope.Mine);

        Assert.AreEqual(ServiceError.NotFound, (await _service.GetAsync(TestData.Fabio, anas.Value!.Report.Id, CancellationToken.None)).Error);
        Assert.IsEmpty((await _service.ListAsync(TestData.Fabio, CancellationToken.None)).Value!);
    }

    /// <summary>Mine processado pelo worker soma só o solicitante registrado, nunca outro funcionário.</summary>
    [TestMethod]
    public async Task MineProcessingUsesStoredRequesterOnly()
    {
        ServiceResult<ReportSubmission> brunos = await Request(TestData.Bruno, ReportScope.Mine);
        Guid id = brunos.Value!.Report.Id;

        await new ReportProcessor(_repository, _clock).ProcessAsync(id, CancellationToken.None);

        ReportResult result = ReportResultJson.Deserialize(_repository.Stored(id).ResultJson!);
        Assert.AreEqual(1, result.TotalCount);
        Assert.AreEqual(200.00m, result.TotalAmount);
    }

    /// <summary>O escopo registrado é preservado até o processamento: Global do Auditor soma todos os funcionários.</summary>
    [TestMethod]
    public async Task GlobalProcessingKeepsRegisteredScope()
    {
        ServiceResult<ReportSubmission> global = await Request(TestData.Edu, ReportScope.Global);
        Guid id = global.Value!.Report.Id;

        await new ReportProcessor(_repository, _clock).ProcessAsync(id, CancellationToken.None);

        ReportRequest stored = _repository.Stored(id);
        Assert.AreEqual(ReportScope.Global, stored.Scope);
        Assert.AreEqual(TestData.EduId, stored.RequesterId);
        Assert.AreEqual(375.00m, ReportResultJson.Deserialize(stored.ResultJson!).TotalAmount);
    }

    private Task<ServiceResult<ReportSubmission>> Request(UserContext user, ReportScope scope) =>
        _service.RequestAsync(user, new ReportInput(ReportScenario.From, ReportScenario.To, scope), CancellationToken.None);
}
