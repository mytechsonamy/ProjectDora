using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using OrchardCore.Security;
using OrchardCore.Security.Permissions;
using ProjectDora.Core.Abstractions;

namespace ProjectDora.UserManagement.Services;

public sealed class OrchardRoleService : ProjectDora.Core.Abstractions.IRoleService
{
    private const string PermissionClaimType = "Permission";

    private readonly RoleManager<IRole> _roleManager;
    private readonly IEnumerable<IPermissionProvider> _permissionProviders;

    public OrchardRoleService(
        RoleManager<IRole> roleManager,
        IEnumerable<IPermissionProvider> permissionProviders)
    {
        _roleManager = roleManager;
        _permissionProviders = permissionProviders;
    }

    public async Task<RoleDto> CreateAsync(CreateRoleCommand command)
    {
        var role = new Role { RoleName = command.Name, RoleDescription = command.Description ?? string.Empty };

        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create role: {errors}");
        }

        if (command.Permissions is { Count: > 0 })
        {
            foreach (var perm in command.Permissions)
            {
                await _roleManager.AddClaimAsync(role, new Claim(PermissionClaimType, perm));
            }
        }

        return await MapToDtoAsync(role);
    }

    public async Task<RoleDto?> GetAsync(string roleName)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role is null)
        {
            return null;
        }

        return await MapToDtoAsync(role);
    }

    public async Task<IReadOnlyList<RoleDto>> ListAsync()
    {
        var roles = _roleManager.Roles.ToList();
        var dtos = new List<RoleDto>();
        foreach (var role in roles)
        {
            dtos.Add(await MapToDtoAsync(role));
        }

        return dtos;
    }

    public async Task<RoleDto> UpdateAsync(string roleName, UpdateRoleCommand command)
    {
        var role = await _roleManager.FindByNameAsync(roleName)
            ?? throw new KeyNotFoundException($"Role '{roleName}' not found.");

        if (role is Role orchardRole && command.Description is not null)
        {
            orchardRole.RoleDescription = command.Description;
            await _roleManager.UpdateAsync(orchardRole);
        }

        if (command.Permissions is not null)
        {
            var existingClaims = await _roleManager.GetClaimsAsync(role);
            foreach (var claim in existingClaims.Where(c => c.Type == PermissionClaimType))
            {
                await _roleManager.RemoveClaimAsync(role, claim);
            }

            foreach (var perm in command.Permissions)
            {
                await _roleManager.AddClaimAsync(role, new Claim(PermissionClaimType, perm));
            }
        }

        return await MapToDtoAsync(role);
    }

    public async Task DeleteAsync(string roleName)
    {
        var role = await _roleManager.FindByNameAsync(roleName)
            ?? throw new KeyNotFoundException($"Role '{roleName}' not found.");

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to delete role: {errors}");
        }
    }

    public Task AssignRolesToUserAsync(string userId, IEnumerable<string> roleNames)
    {
        // Handled via UserManager in OrchardUserService
        return Task.CompletedTask;
    }

    public Task RevokeRolesFromUserAsync(string userId, IEnumerable<string> roleNames)
    {
        // Handled via UserManager in OrchardUserService
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<PermissionDto>> ListPermissionsAsync()
    {
        var allPermissions = new List<PermissionDto>();

        foreach (var provider in _permissionProviders)
        {
            var permissions = await provider.GetPermissionsAsync();
            foreach (var perm in permissions)
            {
                allPermissions.Add(new PermissionDto(
                    perm.Name,
                    perm.Description ?? string.Empty,
                    perm.Category ?? "General",
                    perm.IsSecurityCritical));
            }
        }

        return allPermissions;
    }

    public Task GenerateContentTypePermissionsAsync(string contentTypeName)
    {
        // Orchard Core's ContentTypePermissions module handles this automatically
        return Task.CompletedTask;
    }

    public Task GenerateQueryPermissionAsync(string queryName)
    {
        // Permission name convention: Query.Execute.{queryName}
        return Task.CompletedTask;
    }

    private async Task<RoleDto> MapToDtoAsync(IRole role)
    {
        var claims = await _roleManager.GetClaimsAsync(role);
        var permissions = claims
            .Where(c => c.Type == PermissionClaimType)
            .Select(c => c.Value)
            .ToList();

        return new RoleDto(
            role.RoleName,
            role is Role r ? r.RoleDescription : null,
            permissions);
    }
}
