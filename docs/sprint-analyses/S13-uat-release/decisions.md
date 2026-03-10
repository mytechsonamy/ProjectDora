# Sprint S13 — Key Decisions

## D-001: UAT tests validate module registration, not feature behavior
**Karar**: S13 UAT tests (`Uat/ModuleRegistrationTests.cs`) verify structural completeness: 10 modules registered, 50–100 total permissions, Administrator stereotype in all modules, unique menu positions, no empty permission sets.
**Neden**: Feature behavior is validated per-sprint by unit/integration tests. UAT validates the system as a whole — that all pieces are wired together correctly.
**Etki**: `AllProviders()` helper instantiates all 10 `IPermissionProvider` implementations directly (no DI container). Fast, deterministic.

## D-002: TestLocalizer — static mock helper
**Karar**: `internal static class TestLocalizer` with `static For<T>()` method returns a Moq mock `IStringLocalizer<T>` that echoes strings unchanged.
**Neden**: Navigation providers require `IStringLocalizer<T>` injection. For UAT tests, actual translation is irrelevant — only menu structure matters.
**Etki**: All navigation provider tests use `TestLocalizer.For<T>()` (not `new TestLocalizer().For<T>()`). CA1822 (static method) satisfied.

## D-003: CA2012 — ValueTask awaiting in tests
**Karar**: When calling `INavigationProvider.BuildNavigationAsync()` in sync test context, check `task.IsCompleted` first; only call `task.AsTask().GetAwaiter().GetResult()` if not already completed.
**Neden**: Direct `.GetAwaiter().GetResult()` on `ValueTask` triggers CA2012 analyzer error. The `IsCompleted` check pattern is the correct way to synchronously consume a `ValueTask`.
**Etki**: Pattern applied in `Uat_Modules_AdminMenuItemsHaveUniquePositions` test.

## D-004: Story numbering — S13 = US-1301 through US-1306
**Karar**: UAT stories numbered US-1301–US-1306, not re-using any prior sprint number range.
**Neden**: Global uniqueness of story IDs enables traceability from test `[Trait("StoryId", "US-XXXX")]` back to sprint analysis DoR YAML.
**Etki**: 6 UAT stories covering: module registration, RBAC completeness, menu uniqueness, permission security, integration smoke, release readiness.
