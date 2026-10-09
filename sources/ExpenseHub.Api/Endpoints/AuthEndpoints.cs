using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ExpenseHub.Api.Auth;
using ExpenseHub.Api.Contracts;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;

namespace ExpenseHub.Api.Endpoints;

/// <summary>
/// Endpoints de autenticação.
/// </summary>
internal static class AuthEndpoints
{
    /// <summary>
    /// Mapeia <c>POST /login</c> e <c>GET /api/me</c>.
    /// </summary>
    /// <param name="app">Construtor de rotas.</param>
    /// <returns>O mesmo construtor, para encadeamento.</returns>
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/login", LoginAsync)
            .AllowAnonymous()
            .WithName("Login")
            .WithTags("Auth");

        app.MapGet("/api/me", GetCurrentUser)
            .RequireAuthorization()
            .WithName("GetCurrentUser")
            .WithTags("Auth");

        return app;
    }

    private static async Task<Results<Ok<TokenResponse>, ProblemHttpResult>> LoginAsync(
        LoginRequest request,
        UserManager<ApplicationUser> userManager,
        TokenService tokenService)
    {
        ApplicationUser? user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || await userManager.IsLockedOutAsync(user))
        {
            return InvalidCredentials();
        }

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            await userManager.AccessFailedAsync(user);
            return InvalidCredentials();
        }

        await userManager.ResetAccessFailedCountAsync(user);
        IList<string> roles = await userManager.GetRolesAsync(user);
        return TypedResults.Ok(tokenService.CreateToken(user, roles));
    }

    private static Ok<CurrentUserResponse> GetCurrentUser(ClaimsPrincipal user)
    {
        List<string> roles = user.FindAll(ClaimNames.Role).Select(claim => claim.Value).ToList();
        return TypedResults.Ok(new CurrentUserResponse(
            user.FindFirstValue(ClaimNames.Subject) ?? string.Empty,
            user.FindFirstValue(ClaimNames.Email) ?? string.Empty,
            roles));
    }

    private static ProblemHttpResult InvalidCredentials() =>
        TypedResults.Problem(
            title: "Credenciais inválidas.",
            detail: "E-mail ou senha incorretos.",
            statusCode: StatusCodes.Status401Unauthorized);
}
