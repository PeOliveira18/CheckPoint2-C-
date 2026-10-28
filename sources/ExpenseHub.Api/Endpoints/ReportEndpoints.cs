using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Application;
using ExpenseHub.Api.Contracts;
using ExpenseHub.Api.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ExpenseHub.Api.Endpoints;

internal static class ReportEndpoints
{
    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder reports = app.MapGroup("/api/reports")
            .RequireAuthorization()
            .WithTags("Reports");

        reports.MapPost("/", CreateAsync)
            .WithName("CreateReport");

        reports.MapGet("/", ListAsync)
            .WithName("ListReports");

        reports.MapGet("/{id:guid}", GetAsync)
            .WithName("GetReport");

        return app;
    }

    private static async Task<IResult> CreateAsync(
        CreateReportRequest request,
        ClaimsPrincipal principal,
        ReportService service,
        CancellationToken cancellationToken)
    {
        ServiceResult<ReportSubmission> result = await service.RequestAsync(
            UserContext.FromPrincipal(principal),
            new ReportInput(request.FromDate, request.ToDate, request.Scope),
            cancellationToken);
        return result.ToHttpResult(ToAcceptedOrUnavailable);
    }

    private static async Task<IResult> ListAsync(
        ClaimsPrincipal principal,
        ReportService service,
        CancellationToken cancellationToken)
    {
        ServiceResult<IReadOnlyList<ReportResponse>> result =
            await service.ListAsync(UserContext.FromPrincipal(principal), cancellationToken);
        return result.ToHttpResult(TypedResults.Ok);
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        ClaimsPrincipal principal,
        ReportService service,
        CancellationToken cancellationToken)
    {
        ServiceResult<ReportResponse> result =
            await service.GetAsync(UserContext.FromPrincipal(principal), id, cancellationToken);
        return result.ToHttpResult(TypedResults.Ok);
    }

    private static IResult ToAcceptedOrUnavailable(ReportSubmission submission)
    {
        ReportResponse report = submission.Report;
        if (submission.Published)
        {
            return TypedResults.Accepted($"/api/reports/{report.Id}", report);
        }

        return TypedResults.Problem(
            title: "Publicação não confirmada.",
            detail: "A solicitação foi registrada, mas o broker não confirmou a publicação. Consulte o andamento pelo id.",
            statusCode: StatusCodes.Status503ServiceUnavailable,
            extensions: new Dictionary<string, object?> { ["id"] = report.Id });
    }
}
