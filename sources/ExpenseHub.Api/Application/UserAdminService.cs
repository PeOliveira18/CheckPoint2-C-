using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Contracts;
using ExpenseHub.Core.Data;
using ExpenseHub.Core.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ExpenseHub.Api.Application;

internal sealed class UserAdminService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ExpenseHubDbContext _dbContext;

    public UserAdminService(UserManager<ApplicationUser> userManager, ExpenseHubDbContext dbContext)
    {
        _userManager = userManager;
        _dbContext = dbContext;
    }

    public async Task<ServiceResult<UserResponse>> RegisterAsync(string email, string password)
    {
        ApplicationUser user = new() { UserName = email, Email = email };
        IdentityResult result = await _userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            return ServiceResult<UserResponse>.Success(new UserResponse(user.Id, email, []));
        }

        if (result.Errors.Any(error => error.Code is "DuplicateUserName" or "DuplicateEmail"))
        {
            return ServiceResult<UserResponse>.Failure(ServiceError.Conflict, "Já existe um usuário com este e-mail.");
        }

        return ServiceResult<UserResponse>.Invalid(result.Errors
            .GroupBy(error => error.Code.StartsWith("Password", StringComparison.Ordinal) ? "password" : "email")
            .ToDictionary(group => group.Key, group => group.Select(error => error.Description).ToArray()));
    }

    public async Task<IReadOnlyList<UserResponse>> ListUsersAsync(CancellationToken cancellationToken)
    {
        var users = await _dbContext.Users
            .AsNoTracking()
            .OrderBy(user => user.Email)
            .Select(user => new { user.Id, user.Email })
            .ToListAsync(cancellationToken);

        var userRoles = await (
                from userRole in _dbContext.UserRoles
                join role in _dbContext.Roles on userRole.RoleId equals role.Id
                select new { userRole.UserId, role.Name })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        ILookup<string, string> rolesByUser = userRoles.ToLookup(item => item.UserId, item => item.Name ?? string.Empty);
        return users
            .Select(user => new UserResponse(
                user.Id,
                user.Email ?? string.Empty,
                rolesByUser[user.Id].Order(StringComparer.Ordinal).ToList()))
            .ToList();
    }

    public async Task<ServiceResult<UserResponse>> UpdateRolesAsync(
        string targetUserId,
        string actingUserId,
        IEnumerable<string> requestedRoles,
        CancellationToken cancellationToken)
    {
        ServiceResult<IReadOnlyList<string>> validation = RoleChangeRules.Normalize(requestedRoles, targetUserId, actingUserId);
        if (!validation.Succeeded)
        {
            return validation.ValidationErrors is null
                ? ServiceResult<UserResponse>.Failure(validation.Error.Value, validation.Message ?? string.Empty)
                : ServiceResult<UserResponse>.Invalid(validation.ValidationErrors);
        }

        ApplicationUser? user = await _userManager.FindByIdAsync(targetUserId);
        if (user is null)
        {
            return ServiceResult<UserResponse>.Failure(ServiceError.NotFound, "Usuário não encontrado.");
        }

        IReadOnlyList<string> desired = validation.Value;
        IList<string> current = await _userManager.GetRolesAsync(user);
        string[] toRemove = current.Except(desired, StringComparer.Ordinal).ToArray();
        string[] toAdd = desired.Except(current, StringComparer.Ordinal).ToArray();

        await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        if (toRemove.Length > 0)
        {
            EnsureSucceeded(await _userManager.RemoveFromRolesAsync(user, toRemove));
        }

        if (toAdd.Length > 0)
        {
            EnsureSucceeded(await _userManager.AddToRolesAsync(user, toAdd));
        }

        EnsureSucceeded(await _userManager.UpdateSecurityStampAsync(user));
        await transaction.CommitAsync(cancellationToken);

        return ServiceResult<UserResponse>.Success(new UserResponse(
            user.Id,
            user.Email ?? string.Empty,
            desired.Order(StringComparer.Ordinal).ToList()));
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(error => error.Description)));
        }
    }
}
