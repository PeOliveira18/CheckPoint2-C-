using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExpenseHub.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExpenseHub.Api.Auth;

internal sealed partial class IdentitySeeder
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SeedAdminOptions _adminOptions;
    private readonly ILogger<IdentitySeeder> _logger;

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
