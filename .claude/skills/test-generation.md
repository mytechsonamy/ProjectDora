# Skill: Test Generation

> Target agents: Test Architect, QA

## 1. Generation Pipeline

```
DoR YAML
    │
    ├─ acceptance_tests[] ──→ [Fact] methods (unit + integration)
    ├─ edge_cases[]        ──→ [Fact] methods (edge case tests)
    ├─ inputs[]            ──→ FluentValidation validator tests
    ├─ constraints.rbac    ──→ Security/authorization tests
    └─ tech_notes          ──→ Audit event tests
```

## 2. Test File Structure

```csharp
// File: tests/ProjectDora.{Module}.Tests/{Feature}Tests.cs

namespace ProjectDora.Modules.ContentManagement.Tests;

[Collection("ContentManagement")]
public class CreateContentItemTests : IClassFixture<ProjectDoraWebApplicationFactory>
{
    private readonly ProjectDoraWebApplicationFactory _factory;

    public CreateContentItemTests(ProjectDoraWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // === Acceptance Tests (from DoR) ===

    [Fact]
    [Trait("Category", "AcceptanceTest")]
    [Trait("StoryId", "US-301")]
    [Trait("SpecRef", "4.1.3.1")]
    public async Task ContentManagement_CreateItem_WithValidData_ReturnsCreated()
    {
        // Arrange — Editor role, content type exists
        // Act — POST /api/v1/content/DestekProgrami
        // Assert — 201 Created, contentItemId returned
        throw new NotImplementedException("Generated from US-301 / AT-001");
    }

    // === Edge Case Tests (from DoR) ===

    [Fact]
    [Trait("Category", "EdgeCase")]
    [Trait("StoryId", "US-301")]
    public async Task ContentManagement_CreateItem_TurkishCharsInTitle_PreservedCorrectly()
    {
        // Arrange — title with ş, ç, ğ, ı, ö, ü
        // Act — create and retrieve
        // Assert — characters preserved
        throw new NotImplementedException("Generated from US-301 / EC-001");
    }

    // === Validator Tests (from DoR inputs) ===

    [Theory]
    [InlineData("", false)]           // empty = invalid
    [InlineData("Valid Title", true)]  // normal = valid
    [InlineData(null, false)]          // null = invalid
    [Trait("Category", "Validation")]
    [Trait("StoryId", "US-301")]
    public async Task ContentManagement_CreateValidator_DisplayText_ValidatesCorrectly(
        string? displayText, bool expectedValid)
    {
        var validator = new CreateContentItemCommandValidator(
            Mock.Of<IStringLocalizer<CreateContentItemCommandValidator>>());
        var command = new CreateContentItemCommand("Duyuru", displayText!, null, false);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().Be(expectedValid);
    }

    // === Security Tests (from DoR constraints.rbac) ===

    [Fact]
    [Trait("Category", "Security")]
    [Trait("StoryId", "US-301")]
    public async Task ContentManagement_CreateItem_AsViewer_ReturnsForbidden()
    {
        // Arrange — Viewer role (denied)
        // Act — attempt create
        // Assert — 403 Forbidden
        throw new NotImplementedException("Generated from US-301 / AT-003");
    }

    // === Audit Tests (from DoR tech_notes.audit_events) ===

    [Fact]
    [Trait("Category", "Audit")]
    [Trait("StoryId", "US-301")]
    public async Task ContentManagement_CreateItem_EmitsContentItemCreatedAuditEvent()
    {
        // Arrange — mock IAuditService
        // Act — create content
        // Assert — ContentItemCreated event emitted
        throw new NotImplementedException("Generated from US-301 / audit");
    }
}
```

## 3. Naming Convention

```
{Module}_{Feature}_{Scenario}_{ExpectedResult}
```

### Mapping from DoR

| DoR Field | Maps To |
|-----------|---------|
| `module` → last segment | `{Module}` |
| `action` verb | `{Feature}` |
| `acceptance_test.scenario` | `{Scenario}` |
| `acceptance_test.then` | `{ExpectedResult}` |

### Examples

```
ContentManagement_CreateItem_WithValidData_ReturnsCreated
ContentManagement_CreateItem_WithEmptyTitle_ThrowsValidationException
ContentManagement_CreateItem_AsViewer_ReturnsForbidden
ContentManagement_CreateItem_TurkishCharsInTitle_PreservedCorrectly
QueryEngine_ExecuteLucene_WithTurkishStemming_ReturnsMatchingResults
```

## 4. Test Categories

### Unit Tests (Moq, no DB)

```csharp
// Handler test — mock all dependencies
var mockService = new Mock<IContentService>();
var handler = new CreateContentItemCommandHandler(mockService.Object, ...);
var result = await handler.Handle(command, CancellationToken.None);
```

### Integration Tests (Testcontainers, real DB)

```csharp
// Full stack test — real DB, real HTTP
public class ContentIntegrationTests : IClassFixture<ProjectDoraWebApplicationFactory>
{
    [Fact]
    public async Task ContentAPI_CreateAndRetrieve_RoundtripWorks()
    {
        var client = _factory.CreateAuthenticatedClient("editor-001", "Editor");

        // Create
        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/content/Duyuru",
            new { displayText = "Test Duyuru", published = false });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<ContentItemDto>();

        // Retrieve
        var getResponse = await client.GetAsync(
            $"/api/v1/content/Duyuru/{created.ContentItemId}");
        var retrieved = await getResponse.Content.ReadFromJsonAsync<ContentItemDto>();

        retrieved.DisplayText.Should().Be("Test Duyuru");
        retrieved.Status.Should().Be("Draft");
    }
}
```

## 5. Assertion Patterns

### FluentAssertions

```csharp
// Basic
result.Should().NotBeNull();
result.ContentItemId.Should().NotBeEmpty();
result.Version.Should().Be(1);

// Collections
results.Items.Should().HaveCount(10);
results.Items.Should().AllSatisfy(i => i.TenantId.Should().Be("default"));
results.Items.Should().BeInDescendingOrder(i => i.CreatedUtc);

// Exceptions
var act = () => handler.Handle(invalidCommand, CancellationToken.None);
await act.Should().ThrowAsync<ValidationException>()
    .WithMessage("*DisplayText*");

// HTTP
response.StatusCode.Should().Be(HttpStatusCode.Created);
response.Headers.Location.Should().NotBeNull();

// Approximate numeric assertion
confidence.Should().BeGreaterOrEqualTo(0.7);
```

## 6. Test Data Helpers

```csharp
public static class TestDataBuilder
{
    public static CreateContentItemCommand ValidContentCommand(
        string contentType = "Duyuru",
        string title = "KOBİ Teknoloji Geliştirme Desteği")
    {
        return new CreateContentItemCommand(
            ContentType: contentType,
            DisplayText: title,
            Body: "<p>Destek programı detayları...</p>",
            Published: false);
    }

    public static string TurkishText(int length = 100)
    {
        // Generate Turkish text with special characters
        var chars = "abcçdefgğhıijklmnoöprsştuüvyz ABCÇDEFGĞHIİJKLMNOÖPRSŞTUÜVYZ";
        return new string(Enumerable.Range(0, length)
            .Select(_ => chars[Random.Shared.Next(chars.Length)])
            .ToArray());
    }
}
```

## 7. Generation Checklist

Before submitting generated tests:

- [ ] All `[Fact]` / `[Theory]` methods have `[Trait("StoryId", "US-XXX")]`
- [ ] All methods have `[Trait("SpecRef", "4.1.X.X")]`
- [ ] All methods follow naming convention
- [ ] All methods throw `NotImplementedException` (RED state)
- [ ] Unit tests use Moq (no real DB)
- [ ] Integration tests use `ProjectDoraWebApplicationFactory`
- [ ] Security tests cover denied roles from DoR
- [ ] Audit tests verify event emission
- [ ] Turkish character test included if string inputs exist
- [ ] At least 1 Theory with multiple InlineData for validators
