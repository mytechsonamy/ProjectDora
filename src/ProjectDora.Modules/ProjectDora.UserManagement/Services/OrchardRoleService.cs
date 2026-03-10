using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using OrchardCore.Entities;
using OrchardCore.Security;
using OrchardCore.Security.Permissions;
using OrchardCore.Settings;
using OrchardCore.Users;
using ProjectDora.Core.Abstractions;

namespace ProjectDora.UserManagement.Services;

/// <summary>
/// Dynamically generated permissions persisted via ISiteService.
/// </summary>
internal sealed class GeneratedPermissionsData
{
    public List<string> PermissionNames { get; set; } = new();
}

public sealed class OrchardRoleService : ProjectDora.Core.Abstractions.IRoleService
{
    private const string PermissionClaimType = "Permission";

    private readonly RoleManager<IRole> _roleManager;
    private readonly IEnumerable<IPermissionProvider> _permissionProviders;
    private readonly UserManager<IUser> _userManager;
    private readonly ISiteService _siteService;

    public OrchardRoleService(
        RoleManager<IRole> roleManager,
        IEnumerable<IPermissionProvider> permissionProviders,
        UserManager<IUser> userManager,
        ISiteService siteService)
    {
        _roleManager = roleManager;
        _permissionProviders = permissionProviders;
        _userManager = userManager;
        _siteService = siteService;
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

    public async Task AssignRolesToUserAsync(string userId, IEnumerable<string> roleNames)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User '{userId}' not found.");

        await _userManager.AddToRolesAsync(user, roleNames);
    }

    public async Task RevokeRolesFromUserAsync(string userId, IEnumerable<string> roleNames)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User '{userId}' not found.");

        await _userManager.RemoveFromRolesAsync(user, roleNames);
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

        // Include dynamically generated permissions persisted via ISiteService
        var site = await _siteService.LoadSiteSettingsAsync();
        var generated = site.As<GeneratedPermissionsData>();
        if (generated is not null)
        {
            foreach (var name in generated.PermissionNames)
            {
                if (!allPermissions.Any(p => p.Name == name))
                {
                    allPermissions.Add(new PermissionDto(name, string.Empty, "Generated", false));
                }
            }
        }

        return allPermissions;
    }

    public async Task GenerateContentTypePermissionsAsync(string contentTypeName)
    {
        var names = GetContentTypePermissionNames(contentTypeName);
        await PersistGeneratedPermissionsAsync(names);
    }

    public async Task GenerateQueryPermissionAsync(string queryName)
    {
        await PersistGeneratedPermissionsAsync(new[] { $"Query.Execute.{queryName}" });
    }

    private static string[] GetContentTypePermissionNames(string contentTypeName) =>
        new[]
        {
            $"View_{contentTypeName}",
            $"Preview_{contentTypeName}",
            $"Publish_{contentTypeName}",
            $"Edit_{contentTypeName}",
            $"Delete_{contentTypeName}",
            $"ViewOwn_{contentTypeName}",
            $"EditOwn_{contentTypeName}",
            $"DeleteOwn_{contentTypeName}",
        };

    private async Task PersistGeneratedPermissionsAsync(IEnumerable<string> names)
    {
        var site = await _siteService.LoadSiteSettingsAsync();
        site.Alter<GeneratedPermissionsData>(d =>
            d.PermissionNames = d.PermissionNames.Union(names).ToList());
        await _siteService.UpdateSiteSettingsAsync(site);
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
