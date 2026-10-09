using System;
using System.Threading.Tasks;
using ExpenseHub.Api.Data;
using ExpenseHub.Api.Domain;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace ExpenseHub.Api.Auth;

/// <summary>
/// Registro do Identity, da autenticação bearer (JWT) e da autorização.
/// </summary>
internal static class AuthSetup
{
    /// <summary>
    /// Registra Identity com persistência relacional, JWT bearer e autorização por roles.
    /// </summary>
    /// <param name="services">Coleção de serviços.</param>
    /// <param name="configuration">Configuração da aplicação.</param>
    /// <returns>A mesma coleção, para encadeamento.</returns>
    public static IServiceCollection AddExpenseHubAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Lockout.MaxFailedAccessAttempts = 5;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ExpenseHubDbContext>();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.Configure<SeedAdminOptions>(configuration.GetSection(SeedAdminOptions.SectionName));

        JwtOptions jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = TokenService.CreateSigningKey(jwt.SigningKey),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = ClaimNames.Subject,
                    RoleClaimType = ClaimNames.Role,
                };
                options.Events = new JwtBearerEvents { OnTokenValidated = ValidateSecurityStampAsync };
            });

        services.AddAuthorization();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<TokenService>();
        services.AddScoped<IdentitySeeder>();
        return services;
    }

    /// <summary>
    /// Rejeita tokens emitidos antes da última alteração de segurança do usuário (ex.: troca de roles),
    /// obrigando um novo login.
    /// </summary>
    /// <param name="context">Contexto do token validado.</param>
    /// <returns>Tarefa assíncrona.</returns>
    private static async Task ValidateSecurityStampAsync(TokenValidatedContext context)
    {
        string? userId = context.Principal?.FindFirst(ClaimNames.Subject)?.Value;
        string? tokenStamp = context.Principal?.FindFirst(ClaimNames.SecurityStamp)?.Value;
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tokenStamp))
        {
            context.Fail("Token sem identificação do usuário.");
            return;
        }

        UserManager<ApplicationUser> userManager =
            context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        ApplicationUser? user = await userManager.FindByIdAsync(userId);
        if (user is null || !string.Equals(user.SecurityStamp, tokenStamp, StringComparison.Ordinal))
        {
            context.Fail("Credencial expirada. Faça login novamente.");
        }
    }
}
