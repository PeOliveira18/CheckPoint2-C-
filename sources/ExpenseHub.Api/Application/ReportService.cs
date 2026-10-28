using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Contracts;
using ExpenseHub.Core.Domain;
using ExpenseHub.Core.Reports;
using Microsoft.Extensions.Logging;

namespace ExpenseHub.Api.Application;

internal sealed class ReportService
{
    private readonly IReportRepository _repository;
    private readonly IReportPublisher _publisher;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ReportService> _logger;

    public ReportService(
        IReportRepository repository,
        IReportPublisher publisher,
        TimeProvider timeProvider,
        ILogger<ReportService> logger)
    {
        _repository = repository;
        _publisher = publisher;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<ServiceResult<ReportSubmission>> RequestAsync(UserContext user, ReportInput input, CancellationToken cancellationToken)
    {
        Dictionary<string, string[]> errors = ReportRules.ValidateRequest(input.FromDate, input.ToDate, input.Scope);
        if (errors.Count > 0)
        {
            return ServiceResult<ReportSubmission>.Invalid(errors);
        }

        ReportScope scope = input.Scope.GetValueOrDefault();
        if (!ReportAccessPolicy.CanRequest(user, scope))
        {
            return ServiceResult<ReportSubmission>.Failure(
                ServiceError.Forbidden,
                scope == ReportScope.Global ? "Somente Auditor solicita relatório Global." : "Somente Employee solicita relatório Mine.");
        }

        ReportRequest report = new()
        {
            Id = Guid.NewGuid(),
            RequesterId = user.UserId,
            Scope = scope,
            FromDate = input.FromDate.GetValueOrDefault(),
            ToDate = input.ToDate.GetValueOrDefault(),
            Status = ReportStatus.Pending,
            CreatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            ConcurrencyStamp = Guid.NewGuid(),
        };

        _repository.Add(report);
        if (!await _repository.SaveChangesAsync(cancellationToken))
        {
            return ServiceResult<ReportSubmission>.Failure(ServiceError.Conflict, "Não foi possível registrar a solicitação.");
        }

        bool published = await _publisher.PublishAsync(report.Id, cancellationToken);
        if (!published)
        {
            _logger.LogWarning("Relatório {ReportId} registrado, mas a publicação não foi confirmada pelo broker.", report.Id);
        }

        return ServiceResult<ReportSubmission>.Success(new ReportSubmission(ReportResponse.From(report), published));
    }

    public async Task<ServiceResult<IReadOnlyList<ReportResponse>>> ListAsync(UserContext user, CancellationToken cancellationToken)
    {
        IReadOnlyList<ReportRequest> reports = await _repository.ListOwnAsync(user.UserId, cancellationToken);
        List<ReportResponse> response = reports.Select(ReportResponse.From).ToList();
        return ServiceResult<IReadOnlyList<ReportResponse>>.Success(response);
    }

    public async Task<ServiceResult<ReportResponse>> GetAsync(UserContext user, Guid id, CancellationToken cancellationToken)
    {
        ReportRequest? report = await _repository.FindOwnAsync(id, user.UserId, cancellationToken);
        return report is null
            ? ServiceResult<ReportResponse>.Failure(ServiceError.NotFound, "Relatório não encontrado.")
            : ServiceResult<ReportResponse>.Success(ReportResponse.From(report));
    }
}
