# Skill: CQRS + MediatR Pattern

> Target agents: Developer, Test Architect, Architect

## 1. Pattern Overview

```
API Controller
    │
    ├── Command (write) ──→ CommandHandler ──→ Service (abstraction) ──→ DB
    │                           │
    │                     Validator (FluentValidation)
    │                     AuditBehavior (cross-cutting)
    │                     AuthBehavior (cross-cutting)
    │
    └── Query (read)   ──→ QueryHandler ──→ Service (abstraction) ──→ DB/Cache
```

**Rule**: Commands modify state. Queries never modify state. Never mix.

## 2. Command Pattern

### Command Record

```csharp
// File: src/ProjectDora.Core/Commands/CreateContentItemCommand.cs
namespace ProjectDora.Core.Commands;

public record CreateContentItemCommand(
    string ContentType,
    string DisplayText,
    string? Body,
    bool Published
) : IRequest<ContentItemDto>;
```

### Command Handler

```csharp
// File: src/ProjectDora.Modules/ContentManagement/Handlers/CreateContentItemCommandHandler.cs
namespace ProjectDora.Modules.ContentManagement.Handlers;

public class CreateContentItemCommandHandler
    : IRequestHandler<CreateContentItemCommand, ContentItemDto>
{
    private readonly IContentService _contentService;
    private readonly IAuditService _auditService;
    private readonly IStringLocalizer<CreateContentItemCommandHandler> S;

    public CreateContentItemCommandHandler(
        IContentService contentService,
        IAuditService auditService,
        IStringLocalizer<CreateContentItemCommandHandler> localizer)
    {
        _contentService = contentService;
        _auditService = auditService;
        S = localizer;
    }

    public async Task<ContentItemDto> Handle(
        CreateContentItemCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _contentService.CreateAsync(
            request.ContentType,
            request);

        await _auditService.LogAsync(new AuditEvent
        {
            Action = "ContentItemCreated",
            EntityType = request.ContentType,
            EntityId = result.ContentItemId
        });

        return result;
    }
}
```

### Command Validator

```csharp
// File: src/ProjectDora.Modules/ContentManagement/Validators/CreateContentItemCommandValidator.cs
namespace ProjectDora.Modules.ContentManagement.Validators;

public class CreateContentItemCommandValidator
    : AbstractValidator<CreateContentItemCommand>
{
    public CreateContentItemCommandValidator(
        IStringLocalizer<CreateContentItemCommandValidator> S)
    {
        RuleFor(x => x.ContentType)
            .NotEmpty()
            .WithMessage(S["Content.Validation.ContentTypeRequired"])
            .MaximumLength(100)
            .Matches(@"^[A-Za-z][A-Za-z0-9]*$")
            .WithMessage(S["Content.Validation.InvalidContentType"]);

        RuleFor(x => x.DisplayText)
            .NotEmpty()
            .WithMessage(S["Content.Validation.DisplayTextRequired"])
            .MaximumLength(500);

        RuleFor(x => x.Body)
            .MaximumLength(50000)
            .When(x => x.Body != null);
    }
}
```

## 3. Query Pattern

### Query Record

```csharp
// File: src/ProjectDora.Core/Queries/GetContentItemQuery.cs
namespace ProjectDora.Core.Queries;

public record GetContentItemQuery(
    string ContentItemId,
    int? Version = null
) : IRequest<ContentItemDto?>;
```

### Query Handler

```csharp
// File: src/ProjectDora.Modules/ContentManagement/Handlers/GetContentItemQueryHandler.cs
namespace ProjectDora.Modules.ContentManagement.Handlers;

public class GetContentItemQueryHandler
    : IRequestHandler<GetContentItemQuery, ContentItemDto?>
{
    private readonly IContentService _contentService;
    private readonly ICacheService _cacheService;

    public GetContentItemQueryHandler(
        IContentService contentService,
        ICacheService cacheService)
    {
        _contentService = contentService;
        _cacheService = cacheService;
    }

    public async Task<ContentItemDto?> Handle(
        GetContentItemQuery request,
        CancellationToken cancellationToken)
    {
        // Try cache first (only for published, latest version)
        if (request.Version == null)
        {
            var cached = await _cacheService.GetAsync<ContentItemDto>(
                $"content:{request.ContentItemId}");
            if (cached != null) return cached;
        }

        var result = await _contentService.GetAsync(
            request.ContentItemId,
            request.Version);

        // Cache published content
        if (result?.Status == "Published" && request.Version == null)
        {
            await _cacheService.SetAsync(
                $"content:{request.ContentItemId}",
                result,
                TimeSpan.FromMinutes(5));
        }

        return result;
    }
}
```

## 4. Pipeline Behaviors (Cross-Cutting)

### Registration Order

```csharp
// File: src/ProjectDora.Web/Startup.cs (or module Startup)
services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<CreateContentItemCommand>();

    // Pipeline behaviors execute in order:
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TenantBehavior<,>));
});
```

### Validation Behavior

```csharp
// Runs FluentValidation validators before handler
public class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var results = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = results
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Any())
                throw new ValidationException(failures);
        }

        return await next();
    }
}
```

### Authorization Behavior

```csharp
// Checks permissions before handler
public class AuthorizationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IAuthService _authService;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Check if request has [RequirePermission] attribute
        var attr = typeof(TRequest).GetCustomAttribute<RequirePermissionAttribute>();
        if (attr != null)
        {
            var authorized = await _authService.HasPermissionAsync(attr.Permission);
            if (!authorized)
                throw new ForbiddenException($"Missing permission: {attr.Permission}");
        }

        return await next();
    }
}
```

### Tenant Behavior

```csharp
// Injects tenant context into every request
public class TenantBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ITenantService _tenantService;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Set tenant context (used by services for data filtering)
        var tenantId = _tenantService.GetCurrentTenantId();
        if (string.IsNullOrEmpty(tenantId))
            throw new UnauthorizedException("Tenant context required");

        return await next();
    }
}
```

## 5. Controller → MediatR Integration

```csharp
// File: src/ProjectDora.Modules/Integration/Controllers/ContentController.cs
[ApiController]
[Route("api/v1/content")]
[Authorize]
public class ContentController : ControllerBase
{
    private readonly IMediator _mediator;

    public ContentController(IMediator mediator) => _mediator = mediator;

    [HttpPost("{contentType}")]
    [ProducesResponseType(typeof(ContentItemDto), 201)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> Create(
        string contentType,
        [FromBody] CreateContentItemRequest request)
    {
        var command = new CreateContentItemCommand(
            contentType,
            request.DisplayText,
            request.Body,
            request.Published);

        var result = await _mediator.Send(command);

        return CreatedAtAction(
            nameof(Get),
            new { contentType, contentItemId = result.ContentItemId },
            result);
    }

    [HttpGet("{contentType}/{contentItemId}")]
    public async Task<IActionResult> Get(
        string contentType,
        string contentItemId,
        [FromQuery] int? version = null)
    {
        var result = await _mediator.Send(
            new GetContentItemQuery(contentItemId, version));

        return result != null ? Ok(result) : NotFound();
    }
}
```

## 6. File Organization

```
src/ProjectDora.Core/
├── Commands/
│   ├── CreateContentItemCommand.cs
│   ├── UpdateContentItemCommand.cs
│   └── PublishContentItemCommand.cs
├── Queries/
│   ├── GetContentItemQuery.cs
│   └── ListContentItemsQuery.cs
├── DTOs/
│   ├── ContentItemDto.cs
│   └── PagedResult.cs
├── Interfaces/
│   ├── IContentService.cs
│   └── IAuditService.cs
└── Behaviors/
    ├── ValidationBehavior.cs
    ├── AuthorizationBehavior.cs
    └── TenantBehavior.cs

src/ProjectDora.Modules/ContentManagement/
├── Handlers/
│   ├── CreateContentItemCommandHandler.cs
│   ├── UpdateContentItemCommandHandler.cs
│   └── GetContentItemQueryHandler.cs
├── Validators/
│   ├── CreateContentItemCommandValidator.cs
│   └── UpdateContentItemCommandValidator.cs
└── Services/
    └── ContentService.cs  (implements IContentService)
```

## 7. Testing Pattern

```csharp
// Unit test for handler
[Fact]
[Trait("StoryId", "US-301")]
public async Task ContentManagement_CreateItem_WithValidData_ReturnsContentItemDto()
{
    // Arrange
    var mockContentService = new Mock<IContentService>();
    var mockAuditService = new Mock<IAuditService>();
    mockContentService
        .Setup(s => s.CreateAsync(It.IsAny<string>(), It.IsAny<CreateContentItemCommand>()))
        .ReturnsAsync(new ContentItemDto { ContentItemId = "test-id", Version = 1 });

    var handler = new CreateContentItemCommandHandler(
        mockContentService.Object,
        mockAuditService.Object,
        Mock.Of<IStringLocalizer<CreateContentItemCommandHandler>>());

    var command = new CreateContentItemCommand("DestekProgrami", "Test Title", null, false);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.ContentItemId.Should().Be("test-id");
    result.Version.Should().Be(1);
    mockAuditService.Verify(a => a.LogAsync(It.Is<AuditEvent>(
        e => e.Action == "ContentItemCreated")), Times.Once);
}

// Unit test for validator
[Fact]
public async Task ContentManagement_CreateValidator_EmptyDisplayText_Fails()
{
    var validator = new CreateContentItemCommandValidator(
        Mock.Of<IStringLocalizer<CreateContentItemCommandValidator>>());

    var command = new CreateContentItemCommand("DestekProgrami", "", null, false);

    var result = await validator.ValidateAsync(command);

    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => e.PropertyName == "DisplayText");
}
```

## 8. Anti-Patterns

| Anti-Pattern | Why Wrong | Correct |
|-------------|-----------|---------|
| Business logic in Controller | Controllers should only map HTTP → MediatR | Put logic in Handler |
| Handler calls another Handler | Creates hidden coupling | Extract shared logic to Service |
| Query modifies state | Violates CQRS | Queries are read-only |
| Validator in Handler | Missing pipeline behavior | Use `ValidationBehavior<,>` |
| Direct DB access in Handler | Bypasses abstraction | Use `IContentService` etc. |
| `IRequest<Unit>` for fire-and-forget | Loses error tracking | Use `IRequest<Result>` pattern |
