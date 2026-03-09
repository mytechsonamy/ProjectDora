# Skill: RBAC & Security

> Target agents: Developer, QA, Test Architect

## 1. Permission Model

```
Tenant
 └── Role
      └── Permission[]
           ├── Module-level: "ContentModeling.Manage"
           ├── Action-level: "ContentModeling.Create"
           └── Type-level:   "ContentModeling.Edit.DestekProgrami"
```

### Permission Naming Convention

```
{Module}.{Action}                    — Module-wide permission
{Module}.{Action}.{ContentType}      — Content-type-specific permission
```

### Standard Permissions per Module

| Module | Permissions |
|--------|-----------|
| ContentModeling | `.Manage`, `.Create`, `.Edit`, `.Delete`, `.View` |
| ContentManagement | `.Create`, `.EditOwn`, `.EditAll`, `.Publish`, `.Delete`, `.ViewDraft`, `.ViewPublished` |
| QueryEngine | `.Manage`, `.Execute`, `.Create`, `.Delete` |
| UserRolePermission | `.ManageUsers`, `.ManageRoles`, `.AssignRoles` |
| Workflow | `.Manage`, `.Execute`, `.View` |
| AuditTrail | `.View`, `.Export` |

## 2. Role → Permission Matrix

| Permission | SuperAdmin | TenantAdmin | Editor | Author | Analyst | Denetci | SEO | WFAdmin | Viewer |
|-----------|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| Content.Create | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Content.EditAll | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Content.EditOwn | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Content.Publish | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Content.Delete | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Content.ViewPublished | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Content.ViewDraft | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Query.Execute | ✅ | ✅ | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ |
| User.Manage | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Role.Manage | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Workflow.Manage | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ |
| Audit.View | ✅ | ✅ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ |

## 3. Authorization Implementation

### MediatR Authorization Behavior

```csharp
// Attribute on commands/queries
[RequirePermission("ContentManagement.Create")]
public record CreateContentItemCommand(...) : IRequest<ContentItemDto>;

[RequirePermission("ContentManagement.Publish")]
public record PublishContentItemCommand(string ContentItemId) : IRequest;

// Owner check for EditOwn
[RequirePermission("ContentManagement.EditOwn")]
[RequireOwnership]  // Additional check: user must own the resource
public record UpdateContentItemCommand(...) : IRequest<ContentItemDto>;
```

### Ownership Check

```csharp
public class OwnershipBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, ...)
    {
        var ownershipAttr = typeof(TRequest)
            .GetCustomAttribute<RequireOwnershipAttribute>();

        if (ownershipAttr != null)
        {
            var currentUserId = _authService.GetCurrentUserId();
            var resourceOwnerId = await GetResourceOwner(request);

            // If user doesn't own resource, check for EditAll permission
            if (resourceOwnerId != currentUserId)
            {
                var hasEditAll = await _authService.HasPermissionAsync(
                    "ContentManagement.EditAll");
                if (!hasEditAll)
                    throw new ForbiddenException("Cannot edit others' content");
            }
        }

        return await next();
    }
}
```

## 4. Tenant Isolation

### Every Query Must Filter by Tenant

```csharp
// EF Core global query filter
public class AuditDbContext : DbContext
{
    private readonly string _tenantId;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Global filter — CANNOT be bypassed accidentally
        builder.Entity<AuditLog>()
            .HasQueryFilter(e => e.TenantId == _tenantId);
    }
}
```

### Tenant Context Extraction

```csharp
public class TenantMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Priority: Header → Host → Default
        var tenantId = context.Request.Headers["X-Tenant-Id"].FirstOrDefault()
            ?? ResolveTenantFromHost(context.Request.Host)
            ?? "default";

        context.Items["TenantId"] = tenantId;
        await _next(context);
    }
}
```

## 5. Security Testing Patterns

### Privilege Escalation Test

```csharp
[Fact]
[Trait("Category", "Security")]
public async Task Security_ViewerRole_CannotCreateContent()
{
    // Arrange — authenticate as Viewer
    var client = _factory.CreateAuthenticatedClient("viewer-001", "Viewer");

    // Act
    var response = await client.PostAsJsonAsync(
        "/api/v1/content/Duyuru",
        new { displayText = "Test", published = false });

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
}
```

### IDOR Test

```csharp
[Fact]
[Trait("Category", "Security")]
public async Task Security_UserA_CannotAccessUserBProfile()
{
    // Arrange
    var clientA = _factory.CreateAuthenticatedClient("editor-001", "Editor");
    var userBId = "user-editor-002";

    // Act — try to access another user's profile
    var response = await clientA.GetAsync($"/api/v1/users/{userBId}");

    // Assert — should be forbidden (not 200)
    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
}
```

### Tenant Isolation Test

```csharp
[Fact]
[Trait("Category", "Security")]
public async Task Security_TenantA_CannotSeeTenantBData()
{
    // Arrange — create content in tenant-b
    var clientB = _factory.CreateAuthenticatedClient(
        "editor-001", "Editor", tenantId: "test-tenant-b");
    await clientB.PostAsJsonAsync("/api/v1/content/Duyuru",
        new { displayText = "Tenant B Secret" });

    // Act — query from tenant-a
    var clientA = _factory.CreateAuthenticatedClient(
        "editor-001", "Editor", tenantId: "default");
    var response = await clientA.GetAsync("/api/v1/content/Duyuru?search=Secret");

    // Assert — must not find tenant-b data
    var result = await response.Content.ReadFromJsonAsync<PagedResult>();
    result.Items.Should().NotContain(i => i.DisplayText.Contains("Tenant B"));
}
```

### JWT Tampering Test

```csharp
[Fact]
[Trait("Category", "Security")]
public async Task Security_TamperedJWT_IsRejected()
{
    // Arrange — get valid token, modify payload
    var validToken = await GetValidToken("viewer-001");
    var tamperedToken = TamperWithRole(validToken, "SuperAdmin");

    var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", tamperedToken);

    // Act
    var response = await client.GetAsync("/api/v1/users");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
}
```

## 6. OWASP Checklist for Every Endpoint

| Check | Implementation |
|-------|---------------|
| Authentication | `[Authorize]` attribute on controller/action |
| Authorization | `[RequirePermission]` on MediatR command/query |
| Input validation | FluentValidation in pipeline behavior |
| Output encoding | JSON serialization (auto-escaped) |
| CSRF | Bearer token (not cookies) for API |
| Rate limiting | Middleware per endpoint category |
| Logging | Audit events for state changes |
| Error handling | RFC 7807, no stack traces in production |

## 7. Anti-Patterns

| Anti-Pattern | Correct |
|-------------|---------|
| Check permission in controller | Check in MediatR AuthorizationBehavior |
| Hardcode role names in code | Use permission-based checks, not role-based |
| Return 404 for unauthorized | Return 403 Forbidden (don't hide resource existence for auth'd users) |
| Trust client-provided tenant ID | Validate tenant ID against user's tenant claim |
| Missing tenant filter on new query | Use EF Core global query filters |
| `[AllowAnonymous]` without justification | Document why endpoint is public |
