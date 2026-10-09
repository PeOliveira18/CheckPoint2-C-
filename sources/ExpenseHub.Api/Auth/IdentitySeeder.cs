using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExpenseHub.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExpenseHub.Api.Auth;

/// <summary>
/// Seed idempotente: garante as roles da aplicação e uma única conta Admin inicial.
/// </summary>
internal sealed partial class IdentitySeeder
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SeedAdminOptions _adminOptions;
    private readonly ILogger<IdentitySeeder> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IdentitySeeder"/> class.
    /// </summary>
    /// <param name="roleManager">Gerenciador de roles.</param>
    /// <param name="userManager">Gerenciador de usuários.</param>
    /// <param name="adminOptions">Dados da conta Admin inicial.</param>
    /// <param name="logger">Logger.</param>
    public IdentitySeeder(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IOptions<SeedAdminOptions> adminOptions,
        ILogger<IdentitySeeder> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _adminOptions = adminOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Cria as roles ausentes e, se ainda não existir nenhum Admin, a conta Admin configurada.
    /// Pode ser executado várias vezes sem duplicar dados.
    /// </summary>
    /// <returns>Tarefa assíncrona.</returns>
    public async Task SeedAsync()
    {
        foreach (string role in RoleNames.All)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                EnsureSucceeded(await _roleManager.CreateAsync(new IdentityRole(role)), $"criar a role {role}");
            }
        }

        IList<ApplicationUser> admins = await _userManager.GetUsersInRoleAsync(RoleNames.Admin);
        if (admins.Count > 0)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_adminOptions.Email) || string.IsNullOrWhiteSpace(_adminOptions.Password))
        {
            LogAdminNotConfigured(_logger);
            return;
        }

        ApplicationUser? admin = await _userManager.FindByEmailAsync(_adminOptions.Email);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = _adminOptions.Email,
                Email = _adminOptions.Email,
                EmailConfirmed = true,
            };
            EnsureSucceeded(await _userManager.CreateAsync(admin, _adminOptions.Password), "criar a conta Admin");
        }

        EnsureSucceeded(await _userManager.AddToRoleAsync(admin, RoleNames.Admin), "atribuir a role Admin");
        LogAdminCreated(_logger, _adminOptions.Email);
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (!result.Succeeded)
        {
            string errors = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Falha ao {operation}: {errors}");
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Nenhum Admin existe e SeedAdmin:Email/SeedAdmin:Password não estão configurados; a conta Admin não foi criada.")]
    private static partial void LogAdminNotConfigured(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Conta Admin inicial {Email} criada.")]
    private static partial void LogAdminCreated(ILogger logger, string email);
}
