using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Application;
using ExpenseHub.Api.Contracts;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ExpenseHub.Api.Endpoints;

/// <summary>
/// Endpoints de reembolsos. O atributo de role é a primeira barreira; ownership e estado são decididos no serviço.
/// </summary>
internal static class ExpenseEndpoints
{
    /// <summary>
    /// Mapeia as rotas <c>/api/expenses</c>.
    /// </summary>
    /// <param name="app">Construtor de rotas.</param>
    /// <returns>O mesmo construtor, para encadeamento.</returns>
    public static IEndpointRouteBuilder MapExpenseEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder expenses = app.MapGroup("/api/expenses")
            .RequireAuthorization()
            .WithTags("Expenses");

        expenses.MapPost("/", CreateAsync)
            .RequireAuthorization(policy => policy.RequireRole(RoleNames.Employee))
            .WithName("CreateExpense");

        expenses.MapPut("/{id:guid}", UpdateAsync)
            .RequireAuthorization(policy => policy.RequireRole(RoleNames.Employee))
            .WithName("UpdateExpense");

        expenses.MapGet("/", ListAsync)
            .RequireAuthorization(ReadPolicy)
            .WithName("ListExpenses");

        expenses.MapGet("/{id:guid}", GetAsync)
            .RequireAuthorization(ReadPolicy)
            .WithName("GetExpense");

        expenses.MapPost("/{id:guid}/submit", SubmitAsync)
            .RequireAuthorization(policy => policy.RequireRole(RoleNames.Employee))
            .WithName("SubmitExpense");

        return app;
    }

    /// <summary>
    /// Roles com acesso de leitura; o escopo de cada uma é aplicado no serviço. Admin não está incluído.
    /// </summary>
    private static void ReadPolicy(AuthorizationPolicyBuilder policy) =>
        policy.RequireRole(RoleNames.Employee, RoleNames.Approver, RoleNames.Finance, RoleNames.Auditor);

    private static async Task<IResult> ListAsync(
        ClaimsPrincipal principal,
        ExpenseService service,
        CancellationToken cancellationToken)
    {
        ServiceResult<IReadOnlyList<ExpenseResponse>> result =
            await service.ListAsync(UserContext.FromPrincipal(principal), cancellationToken);
        return result.ToHttpResult(TypedResults.Ok);
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        ClaimsPrincipal principal,
        ExpenseService service,
        CancellationToken cancellationToken)
    {
        ServiceResult<ExpenseResponse> result =
            await service.GetAsync(UserContext.FromPrincipal(principal), id, cancellationToken);
        return result.ToHttpResult(TypedResults.Ok);
    }

    private static async Task<IResult> SubmitAsync(
        Guid id,
        ClaimsPrincipal principal,
        ExpenseService service,
        CancellationToken cancellationToken)
    {
        ServiceResult<ExpenseResponse> result =
            await service.SubmitAsync(UserContext.FromPrincipal(principal), id, cancellationToken);
        return result.ToHttpResult(TypedResults.Ok);
    }

    private static async Task<IResult> CreateAsync(
        ExpenseRequest request,
        ClaimsPrincipal principal,
        ExpenseService service,
        CancellationToken cancellationToken)
    {
        ServiceResult<ExpenseResponse> result =
            await service.CreateAsync(UserContext.FromPrincipal(principal), ToInput(request), cancellationToken);
        return result.ToHttpResult(expense => TypedResults.Created($"/api/expenses/{expense.Id}", expense));
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        ExpenseRequest request,
        ClaimsPrincipal principal,
        ExpenseService service,
        CancellationToken cancellationToken)
    {
        ServiceResult<ExpenseResponse> result =
            await service.UpdateAsync(UserContext.FromPrincipal(principal), id, ToInput(request), cancellationToken);
        return result.ToHttpResult(TypedResults.Ok);
    }

    private static ExpenseInput ToInput(ExpenseRequest request) =>
        new(request.Description, request.Amount, request.ExpenseDate ?? DateOnly.MinValue, request.CategoryId);
}
