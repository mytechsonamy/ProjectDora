# Definition of Ready (DoR) — AI-Agent Story Format

> Version: 1.0 | Status: Draft | Last Updated: 2026-03-09

## 1. Purpose

This document defines the structured YAML format for user stories that AI agents (Claude Code) can parse and use to generate test stubs, validation rules, MediatR DTOs, and implementation scaffolding. Every story must pass the DoR Checklist before entering a sprint.

## 2. YAML Story Schema

```yaml
# --- Required Fields ---
story_id: "US-XXX"                    # Unique identifier (US = User Story)
title: "Short descriptive title"       # Max 80 characters
module: "ProjectDora.Modules.XXX"      # Full module namespace
spec_refs:                             # Traceability to specification
  - "4.1.X.X"                         # At least one spec reference
sprint: "S01"                          # Target sprint (S00-S17)
priority: "P0|P1|P2|P3"               # P0=Blocker, P1=Critical, P2=Major, P3=Minor

# --- User Story Statement ---
role: "As a [role]"                    # Actor (maps to RBAC role)
action: "I want to [action]"          # Capability
benefit: "So that [benefit]"          # Business value

# --- Interface Contract ---
inputs:
  - name: "fieldName"                  # Parameter name (camelCase)
    type: "string|int|bool|DateTime|ContentItem|File|List<T>"
    required: true|false
    validation: "MaxLength(200), NotEmpty"  # FluentValidation rules
    example: "Yeni Destek Programı"    # Realistic Turkish example

outputs:
  - name: "fieldName"
    type: "string|int|ContentItem|PagedResult<T>"
    description: "What this output represents"

# --- Constraints ---
constraints:
  rbac:
    required_permissions:
      - "ContentModeling.Create"       # Orchard Core permission name
    denied_roles:
      - "Anonymous"                    # Roles that must NOT access this
  data_model:
    content_type: "DestekProgrami"     # Orchard Core content type (if applicable)
    parts:
      - "TitlePart"
      - "BodyPart"
      - "AuditTrailPart"
    fields: []                         # Custom fields
  api:
    endpoint: "POST /api/v1/content/{contentType}"
    request_content_type: "application/json"
    response_codes:
      - 201: "Created"
      - 400: "Validation error"
      - 403: "Forbidden"
      - 409: "Conflict (duplicate)"

# --- Acceptance Tests ---
acceptance_tests:                      # Minimum 3 required
  - id: "AT-001"
    scenario: "Short scenario name"
    given: "Precondition state"
    when: "Action performed"
    then: "Expected outcome"

# --- Edge Cases ---
edge_cases:                            # Minimum 2 required
  - id: "EC-001"
    scenario: "Edge case description"
    input: "Problematic input"
    expected: "How system handles it"

# --- Dependencies ---
dependencies:
  stories: ["US-XXX"]                  # Other stories this depends on
  modules: ["ProjectDora.Core"]        # Module dependencies
  external: []                         # External system dependencies

# --- Technical Notes ---
tech_notes:
  abstraction_interface: "IContentService"  # Which abstraction layer interface
  mediatr_commands:
    - "CreateContentCommand"
    - "CreateContentCommandHandler"
  mediatr_queries: []
  audit_events:
    - "ContentCreated"                 # Audit event names to emit
  localization_keys:
    - "ContentModeling.Created.Success"
  caching:
    strategy: "None|ReadThrough|WriteThrough"
    key_pattern: "content:{contentType}:{id}"
    ttl: "5m"
```

## 3. DoR Checklist

All items must pass before a story is considered "Ready" for sprint:

| # | Check | Validation |
|---|-------|-----------|
| 1 | YAML is complete and parseable | All required fields present, valid YAML syntax |
| 2 | >= 3 acceptance tests defined | `acceptance_tests` array length >= 3 |
| 3 | >= 2 edge cases defined | `edge_cases` array length >= 2 |
| 4 | `spec_refs` validated | Each ref matches pattern `4.1.\d+.\d+` or `4.2.\d+.\d+` |
| 5 | Dependencies listed | `dependencies.stories` and `dependencies.modules` populated |
| 6 | Constraints reference RBAC + data model | `constraints.rbac` and `constraints.data_model` non-empty |
| 7 | `tech_notes` references abstraction interface | `tech_notes.abstraction_interface` is one of: IContentService, IContentTypeService, IQueryService, IWorkflowService, IAuthService, IUserService, IRoleService, IAuditService |
| 8 | Inputs have validation rules | Every required input has `validation` field |
| 9 | API contract defined (if applicable) | `constraints.api` has endpoint, response codes |
| 10 | Priority assigned | `priority` is one of P0, P1, P2, P3 |

### Checklist Validation Script

```bash
# Validate a DoR YAML file
# Usage: dotnet test --filter "Category=DoRValidation"
# Or manually: parse YAML, check all 10 rules
```

## 4. Agent Parsing Instructions

### YAML → xUnit Test Stubs

For each `acceptance_test` entry, generate:

```csharp
[Fact]
[Trait("Category", "AcceptanceTest")]
[Trait("StoryId", "{story_id}")]
[Trait("SpecRef", "{spec_refs[0]}")]
public async Task {Module}_{Feature}_{Scenario}_{ExpectedResult}()
{
    // Arrange — {given}
    // TODO: Set up preconditions

    // Act — {when}
    // TODO: Execute action

    // Assert — {then}
    // TODO: Verify outcome
    throw new NotImplementedException("Generated from {story_id} / {acceptance_test.id}");
}
```

For each `edge_case` entry, generate:

```csharp
[Fact]
[Trait("Category", "EdgeCase")]
[Trait("StoryId", "{story_id}")]
public async Task {Module}_{Feature}_{EdgeScenario}_{ExpectedBehavior}()
{
    // Arrange — Edge case: {scenario}
    // Input: {input}

    // Act
    // TODO: Execute with edge case input

    // Assert — {expected}
    throw new NotImplementedException("Generated from {story_id} / {edge_case.id}");
}
```

### YAML → FluentValidation Rules

For each `input` with `validation` field:

```csharp
public class {Command}Validator : AbstractValidator<{Command}>
{
    public {Command}Validator()
    {
        // Generated from {story_id} inputs
        RuleFor(x => x.{Name})
            .{ValidationRule}()
            .WithMessage(S["{localization_key}"]);
    }
}
```

### YAML → MediatR DTOs

From `inputs` + `outputs` + `tech_notes.mediatr_commands`:

```csharp
// Command
public record {CommandName}(
    {foreach input: {input.type} {input.name}}
) : IRequest<{output.type}>;

// Handler stub
public class {CommandName}Handler : IRequestHandler<{CommandName}, {output.type}>
{
    private readonly {abstraction_interface} _service;
    private readonly IAuditService _audit;

    public async Task<{output.type}> Handle({CommandName} request, CancellationToken ct)
    {
        // TODO: Implement
        // Audit event: {audit_events[0]}
        throw new NotImplementedException();
    }
}
```

## 5. Worked Example 1: Content CRUD (US-301)

```yaml
story_id: "US-301"
title: "Create new content item with validation"
module: "ProjectDora.Modules.ContentModeling"
spec_refs:
  - "4.1.2.1"
  - "4.1.3.1"
sprint: "S02"
priority: "P1"

role: "As an Editor"
action: "I want to create a new content item of any defined content type"
benefit: "So that I can publish information for KOSGEB stakeholders"

inputs:
  - name: "contentType"
    type: "string"
    required: true
    validation: "NotEmpty, MaxLength(100), Matches('^[A-Za-z][A-Za-z0-9]*$')"
    example: "DestekProgrami"
  - name: "displayText"
    type: "string"
    required: true
    validation: "NotEmpty, MaxLength(500)"
    example: "KOBİ Teknoloji Geliştirme Desteği 2026"
  - name: "body"
    type: "string"
    required: false
    validation: "MaxLength(50000)"
    example: "Bu program kapsamında KOBİ'lere teknoloji geliştirme desteği sağlanmaktadır..."
  - name: "published"
    type: "bool"
    required: false
    validation: ""
    example: false

outputs:
  - name: "contentItemId"
    type: "string"
    description: "Unique ID of created content item"
  - name: "version"
    type: "int"
    description: "Initial version number (1)"
  - name: "status"
    type: "string"
    description: "Draft or Published"

constraints:
  rbac:
    required_permissions:
      - "ContentModeling.Create"
      - "ContentModeling.EditOwn"
    denied_roles:
      - "Anonymous"
      - "Viewer"
  data_model:
    content_type: "dynamic"
    parts:
      - "TitlePart"
      - "BodyPart"
      - "CommonPart"
      - "AuditTrailPart"
    fields: []
  api:
    endpoint: "POST /api/v1/content/{contentType}"
    request_content_type: "application/json"
    response_codes:
      - 201: "Created successfully"
      - 400: "Validation error (missing required fields, invalid content type)"
      - 403: "User lacks ContentModeling.Create permission"
      - 404: "Content type not found"

acceptance_tests:
  - id: "AT-001"
    scenario: "Create draft content item"
    given: "User has Editor role and content type 'DestekProgrami' exists"
    when: "User submits valid content item with published=false"
    then: "Content item is created with status Draft, version 1, and contentItemId returned"
  - id: "AT-002"
    scenario: "Create and publish content item"
    given: "User has Editor role with Publish permission"
    when: "User submits valid content item with published=true"
    then: "Content item is created with status Published and is visible via public API"
  - id: "AT-003"
    scenario: "Reject creation without permission"
    given: "User has Viewer role (no ContentModeling.Create permission)"
    when: "User attempts to create a content item"
    then: "API returns 403 Forbidden"
  - id: "AT-004"
    scenario: "Validate required fields"
    given: "User has Editor role"
    when: "User submits content item with empty displayText"
    then: "API returns 400 with validation error for displayText field"

edge_cases:
  - id: "EC-001"
    scenario: "Turkish special characters in displayText"
    input: "displayText = 'Şırnak İlçesi Küçük Ölçekli Girişimci Desteği'"
    expected: "Content created successfully, Turkish characters preserved in storage and retrieval"
  - id: "EC-002"
    scenario: "Content type name with invalid characters"
    input: "contentType = 'Destek-Programı'"
    expected: "API returns 400 — content type must match ^[A-Za-z][A-Za-z0-9]*$"
  - id: "EC-003"
    scenario: "Maximum length body content"
    input: "body = 50,000 character Turkish text"
    expected: "Content created successfully without truncation"

dependencies:
  stories: ["US-201", "US-202"]
  modules: ["ProjectDora.Core", "ProjectDora.Modules.ContentModeling"]
  external: []

tech_notes:
  abstraction_interface: "IContentService"
  mediatr_commands:
    - "CreateContentItemCommand"
    - "CreateContentItemCommandHandler"
  mediatr_queries: []
  audit_events:
    - "ContentItemCreated"
    - "ContentItemPublished"
  localization_keys:
    - "Content.Created.Success"
    - "Content.Validation.DisplayTextRequired"
    - "Content.Validation.InvalidContentType"
  caching:
    strategy: "None"
    key_pattern: ""
    ttl: ""
```

## 6. Worked Example 2: Lucene Query Execution (US-501)

```yaml
story_id: "US-501"
title: "Execute Lucene query with Turkish text support"
module: "ProjectDora.Modules.QueryEngine"
spec_refs:
  - "4.1.5.1"
sprint: "S04"
priority: "P1"

role: "As an Analyst"
action: "I want to execute full-text search queries with Turkish language support"
benefit: "So that I can find content items quickly using keyword search"

inputs:
  - name: "queryText"
    type: "string"
    required: true
    validation: "NotEmpty, MaxLength(1000)"
    example: "KOBİ destek programı"
  - name: "contentType"
    type: "string"
    required: false
    validation: "MaxLength(100)"
    example: "Duyuru"
  - name: "pageSize"
    type: "int"
    required: false
    validation: "InclusiveBetween(1, 100)"
    example: 20
  - name: "page"
    type: "int"
    required: false
    validation: "InclusiveBetween(1, 10000)"
    example: 1

outputs:
  - name: "items"
    type: "PagedResult<ContentItemDto>"
    description: "Matching content items with relevance score"
  - name: "totalCount"
    type: "int"
    description: "Total number of matching results"

constraints:
  rbac:
    required_permissions:
      - "QueryEngine.Execute"
    denied_roles:
      - "Anonymous"
  data_model:
    content_type: "N/A"
    parts: []
    fields: []
  api:
    endpoint: "GET /api/v1/queries/lucene"
    request_content_type: "application/json"
    response_codes:
      - 200: "Query executed successfully"
      - 400: "Invalid query syntax"
      - 403: "User lacks QueryEngine.Execute permission"

acceptance_tests:
  - id: "AT-001"
    scenario: "Simple keyword search"
    given: "Content items exist with Turkish text and user has Analyst role"
    when: "User submits Lucene query 'destek'"
    then: "System returns matching content items ordered by relevance"
  - id: "AT-002"
    scenario: "Turkish stemming search"
    given: "Content items with words 'destekler', 'destekleri', 'destek'"
    when: "User searches for 'destek'"
    then: "All variants are found via Turkish stemming"
  - id: "AT-003"
    scenario: "Reject query without permission"
    given: "User has Viewer role (no QueryEngine.Execute permission)"
    when: "User attempts to execute a query"
    then: "API returns 403 Forbidden"

edge_cases:
  - id: "EC-001"
    scenario: "Turkish special characters in query"
    input: "queryText = 'Şırnak İlçesi Küçük Ölçekli'"
    expected: "Search returns results, Turkish characters handled correctly"
  - id: "EC-002"
    scenario: "Empty search results"
    input: "queryText = 'nonexistent-keyword-xyz'"
    expected: "System returns empty PagedResult with totalCount=0"

dependencies:
  stories: []
  modules: ["ProjectDora.Core", "ProjectDora.Modules.QueryEngine"]
  external: []

tech_notes:
  abstraction_interface: "IQueryService"
  mediatr_commands: []
  mediatr_queries:
    - "ExecuteLuceneQuery"
    - "ExecuteLuceneQueryHandler"
  audit_events:
    - "QueryExecuted"
  localization_keys:
    - "Query.Executed.Success"
    - "Query.Validation.InvalidSyntax"
  caching:
    strategy: "ReadThrough"
    key_pattern: "query:lucene:{tenantId}:{queryHash}"
    ttl: "5m"
```

## 7. Cross-References

- **Test Strategy**: [test-strategy.md](../testing/test-strategy.md) — TDD policy and naming conventions for generated tests
- **Governance**: [governance.md](governance.md) — BA Agent uses this template, Test Architect generates from it
- **Test Cases**: [test-cases.md](../testing/test-cases.md) — acceptance tests flow into test case registry
- **Golden Dataset**: [golden-dataset.md](../testing/golden-dataset.md) — platform fixture data for tests
