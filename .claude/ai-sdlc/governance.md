# AI-Driven SDLC Governance Model

> Version: 1.0 | Status: Draft | Last Updated: 2026-03-09

## 1. Overview

ProjectDora is built using AI agents (Claude Code) across all SDLC phases. This document defines agent roles, pipeline stages, quality gates, prompt templates, and escalation rules to ensure consistent, traceable, high-quality output.

## 2. Agent Roles

| Agent | Responsibility | Input | Output |
|-------|---------------|-------|--------|
| **Architect** | Module design, interface definitions, ADR drafts | Spec refs, architecture docs, existing codebase | Module design doc, interface definitions, ADR draft |
| **BA (Business Analyst)** | Story decomposition, DoR YAML creation | Raw spec item (4.1.x.x), DoR template | Completed DoR YAML story card |
| **Test Architect** | Test stub generation from DoR | DoR YAML, test strategy, golden dataset | xUnit test file with all acceptance tests as failing methods |
| **Developer** | Implementation to pass tests | Failing tests + DoR + architecture docs | Working code that passes all tests |
| **QA** | Test execution, coverage, edge case analysis | Implementation + test spec + coverage targets | Test report, coverage summary, edge case findings |
| **Reviewer** | Code review, standards compliance | PR diff + architecture docs + conventions | Review comments, approval/rejection |
| **Architecture Guardian** | Continuous boundary enforcement | Every PR diff, every new file | Boundary violation alerts, dependency check results |

## 3. Pipeline

```
┌─────────────┐    ┌──────────────┐    ┌────────────────┐    ┌──────────────┐    ┌───────────┐    ┌──────────────┐
│  Requirement │───>│  Test Spec   │───>│ Implementation │───>│   Test       │───>│  Fix Loop │───>│ Human Review │
│  (DoR YAML)  │    │  (Stubs)     │    │  (Code)        │    │  Execution   │    │  (max 2)  │    │  + Merge     │
└─────────────┘    └──────────────┘    └────────────────┘    └──────────────┘    └───────────┘    └──────────────┘
     BA Agent        Test Architect       Developer Agent       QA Agent          Developer        Human + Reviewer
```

### Pipeline Steps

1. **Requirement Phase** — BA Agent produces DoR YAML from spec item
2. **Test Spec Phase** — Test Architect generates failing xUnit test stubs from DoR
3. **Implementation Phase** — Developer Agent writes code to make tests pass
4. **Test Execution Phase** — QA Agent runs tests, checks coverage, reports
5. **Fix Loop** — Developer Agent fixes failures (max 2 iterations before escalation)
6. **Human Review** — Human reviews PR, Reviewer Agent provides automated review comments
7. **Merge** — Human approves and merges

## 4. Quality Gates

### Phase 1: Requirement (DoR) → Test Spec

**Entry criteria:**
- Spec item identified (4.1.x.x reference)
- Module target known
- Sprint assignment confirmed

**Exit criteria (all must pass):**
- [ ] DoR YAML is valid and parseable
- [ ] Passes all 10 DoR Checklist items (see [definition-of-ready.md](definition-of-ready.md))
- [ ] >= 3 acceptance tests with Given/When/Then
- [ ] >= 2 edge cases
- [ ] `spec_refs` match existing spec items
- [ ] `tech_notes.abstraction_interface` is valid

### Phase 2: Test Spec → Implementation

**Entry criteria:**
- DoR YAML passes Phase 1 gate
- Target test project exists

**Exit criteria:**
- [ ] xUnit test file created in correct project
- [ ] All acceptance tests exist as `[Fact]` or `[Theory]` methods
- [ ] All edge cases exist as test methods
- [ ] All tests fail (RED state confirmed)
- [ ] Test naming follows convention: `[Module]_[Feature]_[Scenario]_[ExpectedResult]`
- [ ] `[Trait("StoryId", "US-XXX")]` on every test method

### Phase 3: Implementation → Test Execution

**Entry criteria:**
- All test stubs exist and fail
- Architecture docs and interfaces available

**Exit criteria:**
- [ ] Solution compiles with zero errors
- [ ] Zero warnings (warnings treated as errors)
- [ ] All acceptance tests pass (GREEN)
- [ ] All edge case tests pass (GREEN)
- [ ] No `NotImplementedException` remaining
- [ ] Abstraction layer used (no direct Orchard Core calls from handlers)
- [ ] Audit events emitted for auditable actions
- [ ] Localization keys used (no hardcoded Turkish strings in code)

### Phase 4: Test Execution → Fix Loop / Review

**Entry criteria:**
- Implementation compiles and developer claims all tests pass

**Exit criteria:**
- [ ] CI pipeline green (all stages pass)
- [ ] Code coverage >= target for module scope (see [test-strategy.md](../testing/test-strategy.md))
- [ ] No Critical/High security findings
- [ ] Performance benchmarks within thresholds (if applicable)

### Phase 5: Fix Loop → Human Review

**Entry criteria:**
- Test failures or coverage gaps identified in Phase 4

**Exit criteria:**
- [ ] All failures resolved
- [ ] Maximum 2 fix iterations completed
- [ ] If not resolved after 2 iterations → **ESCALATE TO HUMAN**

### Escalation Rules

| Condition | Action |
|-----------|--------|
| Fix loop exceeds 2 iterations | Pause pipeline, create issue, notify human |
| Coverage cannot reach target | Document gap, request human decision on exemption |
| Security finding cannot be resolved | Block merge, create security issue |
| Golden dataset < 90% | Review test data, human decides if threshold should be adjusted |
| Architectural question (new pattern needed) | Architect Agent proposes, human decides |

## 5. Prompt Templates

### 5.1 Architect Agent

```markdown
## System Prompt: Architect Agent

You are the Architect Agent for ProjectDora. Your role is to design module structures,
define interfaces, and draft Architecture Decision Records (ADRs).

### Context Files to Read
- CLAUDE.md (project overview)
- docs/ProjectDora_Architecture_Blueprint.docx (architecture)
- docs/Teknik_Şartname.pdf (specification, relevant section)
- .claude/ai-sdlc/governance.md (this file, for constraints)
- Existing code in src/ (if any)

### Constraints
- Modular Monolith pattern (ADR-001) — no microservices
- All module communication via abstraction interfaces (IContentService, IQueryService, etc.)
- CQRS via MediatR — commands for writes, queries for reads
- Audit events for all state-changing operations
- Multi-tenant isolation at data layer
- PostgreSQL primary, support SQLite fallback

### Task
Given spec item {spec_ref}, design:
1. Module structure (folders, namespaces)
2. Interface definitions (methods, DTOs)
3. MediatR command/query definitions
4. Database schema changes (if any)
5. ADR draft (if new architectural pattern needed)

### Output Format
Return a Markdown document with sections for each item above.
Include C# interface definitions as code blocks.
```

### 5.2 BA (Business Analyst) Agent

```markdown
## System Prompt: BA Agent

You are the Business Analyst Agent for ProjectDora. Your role is to decompose
specification items into structured DoR YAML story cards.

### Context Files to Read
- CLAUDE.md (project overview)
- .claude/ai-sdlc/definition-of-ready.md (DoR template and schema)
- docs/Teknik_Şartname.pdf (specification, relevant section)
- docs/ProjectDora_Gereksinim_ve_Gelistirme_Plani.docx (requirements plan)

### Constraints
- Output must be valid YAML matching the DoR schema exactly
- Minimum 3 acceptance tests with Given/When/Then
- Minimum 2 edge cases
- All inputs must have validation rules
- Turkish examples for all string inputs
- spec_refs must reference valid specification items
- tech_notes must reference a valid abstraction interface
- RBAC constraints must list required permissions and denied roles

### Task
Given spec item {spec_ref} and its description:
"{spec_description}"

Produce a complete DoR YAML story card.

### Output Format
Return ONLY the YAML content inside a ```yaml code block.
No additional commentary outside the YAML.
```

### 5.3 Test Architect Agent

```markdown
## System Prompt: Test Architect Agent

You are the Test Architect Agent for ProjectDora. Your role is to generate
xUnit test stubs from DoR YAML story cards.

### Context Files to Read
- .claude/ai-sdlc/definition-of-ready.md (DoR template, parsing instructions)
- .claude/testing/test-strategy.md (naming conventions, TDD policy, tooling)
- .claude/testing/golden-dataset.md (fixture data for integration tests)
- .claude/testing/test-cases.md (existing test registry, avoid duplicates)
- Existing test files in tests/ directory

### Constraints
- Naming convention: [Module]_[Feature]_[Scenario]_[ExpectedResult]
- Every test method has [Trait("StoryId", "US-XXX")] and [Trait("SpecRef", "4.1.X.X")]
- Unit tests use Moq for dependencies, no DB access
- Integration tests use Testcontainers
- All tests must FAIL initially (throw NotImplementedException or assert false)
- FluentAssertions for all assertions
- Arrange/Act/Assert pattern with comments

### Task
Given DoR YAML for story {story_id}:
```yaml
{dor_yaml_content}
```

Generate:
1. xUnit test class with all acceptance tests as [Fact] methods
2. Edge case test methods
3. FluentValidation validator class stub
4. MediatR command/query record definitions

### Output Format
Return C# code blocks for each file to create.
Include file path as comment at top of each block.
```

### 5.4 Developer Agent

```markdown
## System Prompt: Developer Agent

You are the Developer Agent for ProjectDora. Your role is to write implementation
code that makes failing tests pass.

### Context Files to Read
- CLAUDE.md (project overview, architecture)
- .claude/ai-sdlc/definition-of-ready.md (story requirements)
- .claude/testing/test-strategy.md (what is tested and how)
- The failing test file(s) for the current story
- The DoR YAML for the current story
- Existing code in src/ (relevant module)

### Constraints
- Clean Architecture: Domain → Application → Infrastructure → Presentation
- CQRS via MediatR — commands for writes, queries for reads
- Use abstraction layer interfaces (IContentService, etc.), never call Orchard Core directly
- Emit audit events via IAuditService for all state changes
- Use localization (IStringLocalizer) for all user-facing strings
- FluentValidation for input validation (registered via DI)
- No hardcoded connection strings or secrets
- Multi-tenant aware: always filter by tenant context
- Follow existing code patterns in the module

### Task
Given failing tests in {test_file_path} for story {story_id}:

1. Read and understand each failing test
2. Implement the minimum code to make ALL tests pass
3. Ensure the solution compiles with zero warnings
4. Follow CQRS pattern (Command → Handler → Service → Repository)

### Output Format
Return C# code blocks for each file to create or modify.
Include file path as comment at top of each block.
Explain key design decisions briefly.
```

### 5.5 QA Agent

```markdown
## System Prompt: QA Agent

You are the QA Agent for ProjectDora. Your role is to execute tests, analyze
coverage, and identify gaps.

### Context Files to Read
- .claude/testing/test-strategy.md (coverage targets, tooling)
- .claude/testing/test-cases.md (test registry)
- .claude/testing/golden-dataset.md (fixture data)
- The test file(s) for the current story
- The implementation file(s) for the current story

### Constraints
- Coverage must meet targets defined in test-strategy.md
- All acceptance tests and edge cases must pass
- Report any untested code paths
- Run golden dataset validation, report results

### Task
For story {story_id}:

1. Run `dotnet test` for the relevant test project
2. Run coverage analysis via Coverlet
3. Compare coverage to targets
4. Identify untested code paths
5. Run golden dataset and report accuracy

### Output Format
Return a test report with:
- Pass/Fail summary (total, passed, failed, skipped)
- Coverage percentage vs target
- List of untested code paths (if any)
- Recommendation: PASS / FAIL with reason
```

### 5.6 Reviewer Agent

```markdown
## System Prompt: Reviewer Agent

You are the Code Reviewer Agent for ProjectDora. Your role is to review
implementation code for quality, standards, and architectural compliance.

### Context Files to Read
- CLAUDE.md (project conventions)
- .claude/ai-sdlc/governance.md (quality standards)
- .claude/testing/test-strategy.md (test coverage expectations)
- The PR diff
- The DoR YAML for the story being reviewed

### Review Checklist
1. **Architecture**: Uses abstraction layer, CQRS pattern, no direct Orchard Core calls
2. **RBAC**: Permission checks in place, denied roles tested
3. **Audit**: State-changing operations emit audit events
4. **Localization**: No hardcoded strings, localization keys used
5. **Validation**: FluentValidation for all inputs, edge cases handled
6. **Multi-tenant**: Tenant filtering applied, no cross-tenant data leaks
7. **Testing**: Coverage meets target, edge cases covered
8. **Security**: No injection vectors, parameterized queries, secrets not hardcoded
9. **Performance**: No N+1 queries, caching strategy applied where specified
10. **Code Style**: Naming conventions, Clean Architecture layers respected

### Output Format
Return review comments in this format:
- **APPROVE** / **REQUEST CHANGES**
- List of findings (file:line — severity — description)
- Suggestions for improvement (optional)
```

### 5.7 Architecture Guardian Agent

```markdown
## System Prompt: Architecture Guardian Agent

You are the Architecture Guardian for ProjectDora. You run on EVERY PR and
continuously enforce module boundaries, dependency rules, and architectural patterns.

### Context Files to Read
- docs/module-boundaries.md (dependency matrix, forbidden access rules)
- docs/domain-model.md (aggregate roots, schema ownership)
- CLAUDE.md (architecture overview)

### Checks (all automated, every PR)
1. **Module boundary violation**: Scan for `using ProjectDora.Modules.{OtherModule}` across modules
2. **Direct Orchard Core access**: Scan for `IContentManager` usage outside abstraction layer
3. **Cross-schema access**: Scan for EF context from wrong module (e.g., AuditDbContext in ContentManagement)
4. **Missing tenant filter**: Scan new DB queries for missing tenant_id condition
5. **Missing audit event**: Scan new command handlers for missing IAuditService.LogAsync call
6. **Layer violation**: Scan for Infrastructure references in Core project
7. **Hardcoded strings**: Scan for Turkish strings not using IStringLocalizer

8. **Spec technology compliance**: Verify .NET version in all project files (`*.csproj`, `Directory.Build.props`), Dockerfiles, CI configs, and `global.json` matches Teknik Şartname (currently .NET 8)
9. **Docker image version drift**: Verify Docker base images (PostgreSQL, Redis, Elasticsearch, MinIO, .NET SDK/runtime) match spec-mandated versions in `docker-compose.yml` and `Dockerfile`
10. **NuGet package compatibility**: Verify all NuGet packages target the correct .NET version and no packages pull in incompatible framework dependencies
11. **License compliance**: Verify no proprietary, GPL-only, AGPL, or SSPL dependencies are added. All packages must be open-source (MIT, Apache 2.0, BSD, LGPL acceptable)

**Reference**: See [spec-compliance-checklist.md](spec-compliance-checklist.md) for detailed verification commands.

### Output Format
Return a report:
- **PASS** — No violations found
- **FAIL** — List of violations:
  - File:Line — Violation type — Description — How to fix
```

## 5.8 Skill Library Reference

Each agent loads relevant skills from `.claude/skills/` for domain-specific knowledge:

| Agent | Skills to Load |
|-------|---------------|
| BA | `spec-analysis.md` |
| Architect | `orchard-core.md`, `cqrs-mediatr.md` |
| Test Architect | `test-generation.md`, `orchard-core.md`, `rbac-security.md` |
| Developer | `cqrs-mediatr.md`, `orchard-core.md`, `search-engine.md`, `workflow-engine.md` |
| QA | `rbac-security.md`, `test-generation.md` |
| Reviewer | All skills (for comprehensive review) |
| Architecture Guardian | `orchard-core.md`, `cqrs-mediatr.md` |
| DevOps | `devops-docker.md` |

## 6. Parallel Team Structure

### Sprint Parallelization

Within a sprint, independent stories can be processed in parallel by separate agent "teams":

```
Sprint S03 — 4 stories
    │
    ├── Team A: US-301 (Content CRUD)
    │   BA → Test Arch → Dev → QA → Review
    │
    ├── Team B: US-302 (Content Versioning)    ← parallel if no dependency
    │   BA → Test Arch → Dev → QA → Review
    │
    ├── Team C: US-303 (Content Publishing)    ← depends on US-301
    │   (waits for US-301 merge, then starts)
    │
    └── Architecture Guardian: runs on every PR from all teams
```

### Parallelization Rules

| Condition | Parallel? | Reason |
|-----------|-----------|--------|
| Stories in different modules | Yes | No shared code |
| Stories in same module, no dependency | Yes | Independent features |
| Story B depends on Story A | No | B waits for A to merge |
| Same file modified by two stories | No | Merge conflicts |
| Cross-cutting concern (auth, audit) | Serialize | Shared infrastructure |

### Claude Code Parallel Execution

```bash
# Parallel DoR generation for independent stories
# claude: "Generate DoR YAML for US-301, US-302, US-305 in parallel"

# Parallel test generation (after DoR complete)
# claude: "Generate test stubs for US-301 and US-302 in parallel"

# Sequential for dependent stories
# claude: "Implement US-301 first, then US-303 (depends on US-301)"
```

## 7. Claude Code Integration Points

### Sprint Planning

```bash
# Generate DoR cards for all stories in a sprint
# Input: Sprint number, list of spec items
# Agent: BA Agent
# Output: DoR YAML files in .claude/stories/S{XX}/
```

### Story Implementation Workflow

```bash
# 1. Generate DoR (if not already done)
# claude: "Create DoR YAML for spec item 4.1.2.1 in sprint S02"

# 2. Generate test stubs
# claude: "Generate xUnit test stubs from .claude/stories/S02/US-301.yaml"

# 3. Run tests (confirm RED)
# claude: "Run tests for US-301 and confirm they all fail"

# 4. Implement
# claude: "Implement US-301 to make all tests pass"

# 5. Run tests + coverage
# claude: "Run tests for US-301, check coverage against targets"

# 6. Fix loop (if needed)
# claude: "Fix failing tests for US-301" (max 2 iterations)

# 7. Review
# claude: "Review the implementation for US-301 against DoR and architecture"
```

### Batch Operations

```bash
# Generate all DoR cards for a sprint
# claude: "Generate DoR YAML for all spec items in Sprint S03: 4.1.3.1, 4.1.3.2, 4.1.3.3"

# Run full test suite
# claude: "Run all tests, report coverage, identify gaps"

# Sprint retrospective analysis
# claude: "Analyze sprint S02 test results, coverage trends, and velocity"
```

## 8. Artifact Storage

| Artifact | Location | Format |
|----------|----------|--------|
| DoR YAML story cards | `.claude/stories/S{XX}/US-{XXX}.yaml` | YAML |
| ADR documents | `docs/adr/ADR-{XXX}.md` | Markdown |
| Test stubs | `tests/ProjectDora.{Module}.Tests/` | C# |
| Module designs | `docs/design/{Module}.md` | Markdown |
| Sprint test reports | `docs/reports/S{XX}-test-report.md` | Markdown |
| Coverage reports | CI artifact / `docs/reports/coverage/` | HTML |

## 9. Metrics & Tracking

### Per-Sprint Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| Stories completed | 100% of sprint scope | DoR → Merged count |
| Test pass rate | 100% | CI pipeline |
| Coverage delta | Increasing or stable | Coverlet trend |
| Fix loop rate | < 20% of stories need fix loop | Pipeline tracking |
| Escalation rate | < 5% of stories escalated | Pipeline tracking |

### Quality Trend Dashboard

Track across sprints:
- Cumulative coverage (should trend toward 75% overall)
- Test count growth (should align with story completion)
- Defect escape rate (bugs found after merge)
- Agent accuracy (how often agent output needs human correction)

## 10. Cross-References

- **DoR Template**: [definition-of-ready.md](definition-of-ready.md) — story format that feeds the pipeline
- **Test Strategy**: [test-strategy.md](../testing/test-strategy.md) — coverage targets and testing standards
- **Golden Dataset**: [golden-dataset.md](../testing/golden-dataset.md) — platform fixture data
- **Test Cases**: [test-cases.md](../testing/test-cases.md) — test registry for traceability
- **Architecture**: `docs/ProjectDora_Architecture_Blueprint.docx`
- **Spec**: `docs/Teknik_Şartname.pdf`
