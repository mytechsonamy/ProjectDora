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
        // Full implementation requires YesSql ISession with UserIndex for paginated queries
        var result = new PagedResult<UserDto>(
            Array.Empty<UserDto>(),
            0,
            query.Page,
            query.PageSize);

        return Task.FromResult(result);
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
