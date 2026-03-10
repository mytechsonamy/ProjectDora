using Microsoft.AspNetCore.Identity;
using OrchardCore.Users;
using OrchardCore.Users.Models;
using ProjectDora.Core.Abstractions;

namespace ProjectDora.UserManagement.Services;

public sealed class OrchardUserService : IUserService
{
    private readonly UserManager<IUser> _userManager;

    public OrchardUserService(UserManager<IUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<UserDto> CreateAsync(CreateUserCommand command)
    {
        var user = new User
        {
            UserName = command.UserName,
            Email = command.Email,
            IsEnabled = true,
        };

        var result = await _userManager.CreateAsync(user, command.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create user: {errors}");
        }

        if (command.Roles is { Count: > 0 })
        {
            await _userManager.AddToRolesAsync(user, command.Roles);
        }

        return MapToDto(user);
    }

    public async Task<UserDto?> GetAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is not User orchardUser)
        {
            return null;
        }

        return MapToDto(orchardUser);
    }

    public Task<PagedResult<UserDto>> ListAsync(ListUsersQuery query)
    {
        IEnumerable<User> users = _userManager.Users.OfType<User>();

        if (query.Enabled.HasValue)
        {
            users = users.Where(u => u.IsEnabled == query.Enabled.Value);
        }

        if (!string.IsNullOrEmpty(query.SearchTerm))
        {
            users = users.Where(u =>
                (u.UserName?.Contains(query.SearchTerm, StringComparison.OrdinalIgnoreCase) == true) ||
                (u.Email?.Contains(query.SearchTerm, StringComparison.OrdinalIgnoreCase) == true));
        }

        if (!string.IsNullOrEmpty(query.Role))
        {
            users = users.Where(u =>
                u.RoleNames?.Contains(query.Role, StringComparer.OrdinalIgnoreCase) == true);
        }

        var list = users.ToList();
        var total = list.Count;
        var paged = list
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(MapToDto)
            .ToList();

        return Task.FromResult(new PagedResult<UserDto>(paged, total, query.Page, query.PageSize));
    }

    public async Task<UserDto> UpdateAsync(string userId, UpdateUserCommand command)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User '{userId}' not found.");

        if (!string.IsNullOrEmpty(command.Email) && user is User orchardUser)
        {
            await _userManager.SetEmailAsync(user, command.Email);
            return MapToDto(orchardUser);
        }

        if (user is User u)
        {
            return MapToDto(u);
        }

        throw new KeyNotFoundException($"User '{userId}' not found.");
    }

    public async Task EnableAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User '{userId}' not found.");

        if (user is User orchardUser)
        {
            orchardUser.IsEnabled = true;
            await _userManager.UpdateAsync(orchardUser);
        }
    }

    public async Task DisableAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User '{userId}' not found.");

        if (user is User orchardUser)
        {
            orchardUser.IsEnabled = false;
            await _userManager.UpdateAsync(orchardUser);
        }
    }

    public async Task DeleteAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User '{userId}' not found.");

        await _userManager.DeleteAsync(user);
    }

    private static UserDto MapToDto(User user)
    {
        var roles = user.RoleNames?.ToList() as IReadOnlyList<string> ?? Array.Empty<string>();
        return new UserDto(
            user.UserId,
            user.UserName,
            user.Email,
            null,
            user.IsEnabled,
            roles,
            DateTime.UtcNow,
            null);
    }
}
