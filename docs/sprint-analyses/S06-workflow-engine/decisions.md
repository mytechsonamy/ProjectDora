# Sprint S06 — Decisions Log

## D-001: Leverage OrchardCore.Workflows as Foundation
- **Date**: 2026-03-09
- **Context**: Spec 4.1.7 requires a full workflow engine with visual designer, triggers, activities, and execution tracking. Building from scratch would take multiple sprints and duplicate mature open-source functionality.
- **Decision**: Use `OrchardCore.Workflows` module as the workflow engine foundation. This provides the visual designer (4.1.7.2), built-in triggers (4.1.7.3), and built-in activities (4.1.7.4) out of the box. We extend with custom activities and wrap everything behind `IWorkflowService` abstraction.
- **Consequences**: Faster delivery; dependent on Orchard Core's workflow model; visual designer is coupled to admin panel UI. Custom activities must follow Orchard Core's `Activity` base class pattern.
- **ADR**: ADR-001 (Modular Monolith on Orchard Core)

## D-002: IWorkflowService Abstraction Layer
- **Date**: 2026-03-09
- **Context**: Direct dependency on `OrchardCore.Workflows` would make the workflow engine non-portable and hard to test. Per ADR-001, all Orchard Core dependencies must be isolated behind abstraction interfaces.
- **Decision**: All workflow operations (CRUD, enable/disable, trigger, execution queries) go through `IWorkflowService` interface defined in `ProjectDora.Core`. The implementation in `ProjectDora.Modules.Workflows` delegates to Orchard Core's `IWorkflowManager` and `IWorkflowStore`.
- **Consequences**: Clean separation; unit tests can mock `IWorkflowService`; future migration away from Orchard Core workflows is possible without affecting consumers.
- **ADR**: ADR-001

## D-003: Custom Activity Development Pattern
- **Date**: 2026-03-09
- **Context**: Spec 4.1.7.5 requires activities to access related entities (content items, SQL queries) directly. Standard Orchard Core activities do not cover all ProjectDora-specific needs (e.g., running a saved query, accessing AI services).
- **Decision**: Create custom activities by inheriting `Activity` base class. Each custom activity: (1) receives dependencies via DI constructor, (2) uses `IContentService` / `IQueryService` for entity access (never direct DB), (3) exposes `WorkflowExpression<T>` properties for designer configuration, (4) returns minimum `Done` + `Failed` outcomes. Template in `.claude/skills/workflow-engine.md`.
- **Consequences**: Consistent pattern for all custom activities; testable via mocked services; Liquid template support for dynamic property values.
- **ADR**: None (module-level decision)

## D-004: Workflow Permission Model
- **Date**: 2026-03-09
- **Context**: Workflow management is a powerful capability that should be restricted to administrative roles. Need to define granular permissions that follow the project's RBAC conventions.
- **Decision**: Three permissions: `Workflow.Manage` (CRUD + enable/disable), `Workflow.Execute` (manual trigger), `Workflow.View` (read definitions + execution history). Only SuperAdmin, TenantAdmin, and WorkflowAdmin roles get these permissions. All other roles are denied.
- **Consequences**: Clear separation of concerns; WorkflowAdmin role can manage workflows without full admin access; Viewer/Editor/Author roles cannot accidentally trigger workflows.
- **ADR**: None

## D-005: Event-Driven Trigger Architecture
- **Date**: 2026-03-09
- **Context**: Spec 4.1.7.3 requires multiple trigger types: time-based, content events, HTTP events. Need a consistent event delivery mechanism.
- **Decision**: Content events flow through Orchard Core's event bus (`IContentHandler` pipeline). Timer events use Orchard Core's background task scheduler (no polling). HTTP triggers use dedicated webhook endpoints. Signal triggers use `IWorkflowService.TriggerAsync()` for programmatic invocation.
- **Consequences**: No custom event bus needed; relies on Orchard Core's proven event pipeline; timer accuracy depends on background service interval (configurable, default 1 minute).
- **ADR**: None

## D-006: Workflow Execution History Storage
- **Date**: 2026-03-09
- **Context**: Spec 4.1.7 implicitly requires execution tracking for monitoring and debugging. Need to decide where and how long to store execution data.
- **Decision**: Store workflow execution history in YesSql (orchard schema) via `WorkflowInstance` documents. Execution records include: workflow ID, trigger event, start/end timestamps, status (Running/Completed/Faulted), activity execution log, error details. Retention follows tenant-level audit retention policy.
- **Consequences**: Consistent with Orchard Core's storage model; queryable via YesSql indexes; no additional database schema needed. Large execution volumes may need periodic cleanup (handled by retention policy).
- **ADR**: None

## D-007: Workflow Definition Validation
- **Date**: 2026-03-09
- **Context**: Invalid workflow definitions (missing start activity, orphan transitions, circular references without exit) could cause runtime failures.
- **Decision**: Validate workflow definitions on create/update: (1) must have exactly one start activity, (2) no orphan transitions (source/destination must exist), (3) all activities must be reachable from start, (4) max execution step limit to prevent infinite loops (configurable, default 1000).
- **Consequences**: Prevents invalid workflows from being saved; validation errors returned as 400 responses; max step limit as safety net against infinite loops.
- **ADR**: None
