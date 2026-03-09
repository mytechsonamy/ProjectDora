namespace ProjectDora.Core.Abstractions;

/// <summary>
/// User management operations — wraps Orchard Core's user management.
/// </summary>
public interface IUserService
{
    Task<UserDto> CreateAsync(CreateUserCommand command);
    Task<UserDto?> GetAsync(string userId);
    Task<PagedResult<UserDto>> ListAsync(ListUsersQuery query);
    Task<UserDto> UpdateAsync(string userId, UpdateUserCommand command);
    Task EnableAsync(string userId);
    Task DisableAsync(string userId);
    Task DeleteAsync(string userId);
}

public record UserDto(
    string UserId,
    string UserName,
    string Email,
    string? DisplayName,
    bool Enabled,
    IReadOnlyList<string> Roles,
    DateTime CreatedUtc,
    DateTime? LastLoginUtc);

public record CreateUserCommand(
    string UserName,
    string Email,
    string Password,
    string? DisplayName = null,
    IReadOnlyList<string>? Roles = null);

public record UpdateUserCommand(
    string? DisplayName = null,
    string? Email = null);

public record ListUsersQuery(
    int Page = 1,
    int PageSize = 20,
    string? Role = null,
    bool? Enabled = null,
    string? SearchTerm = null);
