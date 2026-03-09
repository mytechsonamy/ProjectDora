# Release Management

> Version: 1.0 | Status: Draft | Last Updated: 2026-03-09

## 1. Versioning

### Semantic Versioning (SemVer)

```
MAJOR.MINOR.PATCH

MAJOR — Breaking API changes
MINOR — New features, backward-compatible
PATCH — Bug fixes, backward-compatible
```

### Version Examples

| Version | Trigger |
|---------|---------|
| `1.0.0` | First production release (after S13 UAT) |
| `1.1.0` | New feature added post-release |
| `1.0.1` | Bug fix / hotfix |
| `2.0.0` | Breaking API change |

### Pre-release Tags

```
1.0.0-alpha.1   — Early development (S0-S5)
1.0.0-beta.1    — Feature complete (S10-S11)
1.0.0-rc.1      — Release candidate (S12-S13)
1.0.0            — Production release
```

## 2. Branch Strategy

```
main ──────────────────────────────────────────── production
  │                                         ▲
  │                                         │ merge (PR)
  │                              release/v1.0.0
  │                                   ▲
  │                                   │ branch from develop
  develop ─────────────────────────────────── integration
    │         ▲         ▲         ▲
    │         │         │         │
  feature/  feature/  feature/  bugfix/
  US-301    US-302    US-303    fix-xxx
```

### Branch Naming

| Type | Pattern | Example |
|------|---------|---------|
| Feature | `feature/US-{id}-{description}` | `feature/US-301-content-crud` |
| Bugfix | `bugfix/{description}` | `bugfix/fix-tenant-filter` |
| Hotfix | `hotfix/{description}` | `hotfix/fix-auth-bypass` |
| Release | `release/v{version}` | `release/v1.0.0` |

## 3. Release Process

### 3.1 Release Checklist

```
PRE-RELEASE
  [ ] All sprint DoD criteria met
  [ ] All P0/P1 bugs resolved
  [ ] Full test suite passes on develop
  [ ] Security scan completed (no critical/high)
  [ ] Performance baseline met

RELEASE BRANCH
  [ ] Create release/vX.Y.Z from develop
  [ ] Bump version in Directory.Build.props
  [ ] Update CHANGELOG.md
  [ ] Run full regression test suite
  [ ] Fix any release-blocking issues (commit to release branch)

STAGING
  [ ] Deploy to staging environment
  [ ] Run smoke tests on staging
  [ ] Run UAT scenarios
  [ ] Verify multi-tenant isolation
  [ ] Verify health check endpoints
  [ ] Performance test on staging

PRODUCTION
  [ ] Database backup taken
  [ ] Migration scripts reviewed and tested
  [ ] Merge release branch → main (PR, requires approval)
  [ ] Tag: git tag -a vX.Y.Z -m "Release vX.Y.Z"
  [ ] Deploy to production
  [ ] Run smoke tests on production
  [ ] Verify health checks
  [ ] Monitor error rates for 1 hour
  [ ] Merge main → develop (sync back)

POST-RELEASE
  [ ] Close release milestone
  [ ] Archive release branch
  [ ] Notify stakeholders
  [ ] Update documentation site (if applicable)
```

### 3.2 Deployment Commands

```bash
# Build production image
docker compose -f docker/docker-compose.prod.yml build

# Deploy
docker compose -f docker/docker-compose.prod.yml up -d --remove-orphans

# Verify
curl http://localhost:5000/health/ready

# Check version
curl http://localhost:5000/api/version
```

## 4. Hotfix Workflow

```
main ─────────────── hotfix/fix-xxx ─────── main
                         │                    │
                         │ fix applied        │ merge back
                         │                    ▼
develop ──────────────────────────────────── develop
```

### Hotfix Steps

1. **Identify**: Critical bug in production
2. **Branch**: `git checkout -b hotfix/fix-xxx main`
3. **Fix**: Apply minimal fix, add test
4. **Test**: Run affected test suite
5. **Review**: PR to main (expedited review)
6. **Deploy**: Follow abbreviated release process
7. **Merge back**: Merge main → develop to sync fix
8. **Tag**: `git tag -a vX.Y.Z -m "Hotfix: description"`

### Hotfix Criteria

Only these qualify as hotfixes (everything else waits for next release):

| Qualifies | Does Not Qualify |
|-----------|-----------------|
| Security vulnerability | UI cosmetic issue |
| Data corruption/loss | Performance optimization |
| Complete feature outage | Missing feature |
| Authentication bypass | Minor validation bug |
| Tenant data leak | Logging improvement |

## 5. Rollback Procedure

### 5.1 Application Rollback

```bash
# Stop current version
docker compose -f docker/docker-compose.prod.yml down

# Deploy previous version
git checkout v{previous-version}
docker compose -f docker/docker-compose.prod.yml build
docker compose -f docker/docker-compose.prod.yml up -d

# Verify
curl http://localhost:5000/health/ready
```

### 5.2 Database Rollback

```bash
# If migration was applied, revert it
dotnet ef database update {PreviousMigrationName} \
  --project src/ProjectDora.Modules/ProjectDora.AuditTrail \
  --context AuditDbContext

# If data corruption, restore from backup
pg_restore -h localhost -U projectdora -d projectdora -c backup_YYYYMMDD.dump
```

### 5.3 Rollback Decision Matrix

| Scenario | Action |
|----------|--------|
| Bug found within 1 hour of deploy | Rollback immediately |
| Bug found after 1 hour, data safe | Hotfix forward |
| Data corruption detected | Rollback + restore from backup |
| Performance degradation only | Monitor, hotfix if critical |

## 6. Release Notes Template

```markdown
# Release vX.Y.Z — [Release Name]

**Date**: YYYY-MM-DD
**Sprints**: SXX — SYY

## New Features
- **[Module]**: Description of feature (US-XXX)

## Improvements
- **[Module]**: Description of improvement

## Bug Fixes
- **[Module]**: Description of fix (#issue-number)

## Breaking Changes
- **[Module]**: Description of breaking change and migration path

## API Changes
- `POST /api/v1/new-endpoint` — New endpoint for X
- `PUT /api/v1/existing/{id}` — Added `newField` parameter

## Database Migrations
- `YYYYMMDD_MigrationName` — Description

## Known Issues
- [#issue] Description

## Upgrade Instructions
1. Take database backup
2. Apply migrations: `dotnet ef database update`
3. Deploy new version
4. Verify: `curl /health/ready`
```

## 7. Cross-References

- **Definition of Done**: [../.claude/ai-sdlc/definition-of-done.md](../.claude/ai-sdlc/definition-of-done.md) — release DoD criteria
- **Sprint Roadmap**: [../.claude/ai-sdlc/sprint-roadmap.md](../.claude/ai-sdlc/sprint-roadmap.md) — sprint plan
- **Runbook**: [runbook.md](runbook.md) — operational deployment procedures
- **Migration Strategy**: [migration-strategy.md](migration-strategy.md) — database migration process
