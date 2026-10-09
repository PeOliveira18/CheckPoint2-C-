using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Application;
using ExpenseHub.Api.Auth;
using ExpenseHub.Api.Contracts;
using ExpenseHub.Api.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace ExpenseHub.Api.Endpoints;

/// <summary>
/// Endpoints de cadastro e de administração de usuários.
/// </summary>
internal static class AdminEndpoints
{
    /// <summary>
    /// Mapeia <c>POST /register</c> (público) e as rotas <c>/api/admin/users</c> (somente Admin).
    /// </summary>
    /// <param name="app">Construtor de rotas.</param>
    /// <returns>O mesmo construtor, para encadeamento.</returns>
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/register", RegisterAsync)
            .AllowAnonymous()
            .WithName("Register")
            .WithTags("Auth");

        RouteGroupBuilder admin = app.MapGroup("/api/admin/users")
            .RequireAuthorization(Policies.Admin)
            .WithTags("Admin");

        admin.MapGet("/", ListUsersAsync).WithName("ListUsers");
        admin.MapPut("/{id}/roles", UpdateRolesAsync).WithName("UpdateUserRoles");

        return app;
    }

    private static async Task<IResult> RegisterAsync(RegisterRequest request, UserAdminService service)
    {
        ServiceResult<UserResponse> result = await service.RegisterAsync(request.Email, request.Password);
        return result.ToHttpResult(user => TypedResults.Created($"/api/admin/users/{user.Id}", user));
    }

    private static async Task<Ok<IReadOnlyList<UserResponse>>> ListUsersAsync(
        UserAdminService service,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.ListUsersAsync(cancellationToken));

    private static async Task<IResult> UpdateRolesAsync(
        string id,
        UpdateUserRolesRequest request,
        ClaimsPrincipal principal,
        UserAdminService service,
        CancellationToken cancellationToken)
    {
        string actingUserId = principal.FindFirstValue(ClaimNames.Subject) ?? string.Empty;
        ServiceResult<UserResponse> result = await service.UpdateRolesAsync(id, actingUserId, request.Roles, cancellationToken);
        return result.ToHttpResult(TypedResults.Ok);
    }
}
