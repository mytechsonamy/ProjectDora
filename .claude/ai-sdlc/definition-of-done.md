# Definition of Done (DoD)

> Version: 1.0 | Last Updated: 2026-03-09

## 1. Story DoD

A user story is "Done" when ALL of the following are true:

### Code Complete
- [ ] All acceptance criteria from DoR YAML are implemented
- [ ] Code compiles with zero warnings (`--warnaserror`)
- [ ] No `TODO` or `HACK` comments left in production code
- [ ] Code follows project conventions (English code, C# naming standards)

### Tests Green
- [ ] All unit tests pass (`dotnet test --filter "Category=Unit"`)
- [ ] All integration tests pass (`dotnet test --filter "Category=Integration"`)
- [ ] New tests written for all acceptance criteria (minimum 3 per story)
- [ ] Edge case tests written (minimum 2 per story)
- [ ] Security tests written for RBAC constraints
- [ ] Test naming follows convention: `{Module}_{Feature}_{Scenario}_{ExpectedResult}`
- [ ] All tests have `[Trait("StoryId", "US-XXX")]` attribute

### Code Quality
- [ ] No new Roslyn analyzer warnings
- [ ] FluentValidation validators for all command inputs
- [ ] MediatR pipeline behaviors applied (auth, validation, audit)
- [ ] Multi-tenant isolation verified (tenant_id filter on all queries)

### Reviewed & Merged
- [ ] Pull request created with description
- [ ] Code review completed (at least 1 reviewer)
- [ ] All review comments addressed
- [ ] PR merged to develop branch
- [ ] No merge conflicts

### Documentation
- [ ] API endpoint documented in `api-contract.yaml` (if new endpoint)
- [ ] Audit events documented (if state-changing operation)
- [ ] Sprint analysis notes updated with user-facing feature description

---

## 2. Sprint DoD

A sprint is "Done" when ALL of the following are true:

### Stories Complete
- [ ] All committed stories meet Story DoD
- [ ] No critical or high-severity bugs open
- [ ] Regression test suite passes (all previous sprint tests still green)

### Quality Gates
- [ ] Unit test coverage >= 75% (line coverage)
- [ ] Integration test coverage >= 60%
- [ ] Zero security test failures
- [ ] No P0/P1 bugs remaining

### Demo Ready
- [ ] All features demonstrable in staging environment
- [ ] Test data (golden dataset) loaded and functional
- [ ] Health check endpoints return healthy

### Sprint Analysis Written
- [ ] `docs/sprint-analyses/SXX-{name}/analysis.md` completed
- [ ] `docs/sprint-analyses/SXX-{name}/decisions.md` completed
- [ ] DoR YAML files committed to `dor/` folder
- [ ] Documentation notes captured for end-of-project deliverables

### Spec Compliance
- [ ] Spec compliance checklist executed ([spec-compliance-checklist.md](spec-compliance-checklist.md))
- [ ] All technology versions match Teknik Şartname requirements
- [ ] No new proprietary dependencies introduced
- [ ] Docker images use spec-mandated base versions

### CI/CD
- [ ] CI pipeline green on develop branch
- [ ] Docker images build successfully
- [ ] `docker compose up` works with all services healthy

---

## 3. Release DoD

A release is "Done" when ALL of the following are true:

### Staging Validation
- [ ] Full test suite passes in staging environment
- [ ] Smoke tests pass on staging
- [ ] Multi-tenant isolation verified in staging
- [ ] Performance baseline met (p95 < 500ms for content API)

### Security
- [ ] OWASP security scan completed (no critical/high findings)
- [ ] All security test cases pass
- [ ] JWT token validation verified
- [ ] RBAC matrix validated against spec

### Performance
- [ ] Load test completed (target: 500 concurrent users)
- [ ] Database query performance acceptable (no N+1, no full table scans)
- [ ] Redis cache hit ratio > 80%
- [ ] Search response time < 200ms (p95)

### Documentation
- [ ] Release notes written
- [ ] API changelog documented
- [ ] Migration guide written (if breaking changes)
- [ ] Runbook updated with new operational procedures

### Deployment
- [ ] Database migrations tested and scripted (idempotent)
- [ ] Rollback plan documented and tested
- [ ] Monitoring dashboards configured
- [ ] Health check endpoints verified
- [ ] Backup taken before deployment

---

## 4. Cross-References

- **Definition of Ready**: [definition-of-ready.md](definition-of-ready.md) — story input criteria
- **Test Strategy**: [../testing/test-strategy.md](../testing/test-strategy.md) — coverage targets
- **Sprint Roadmap**: [sprint-roadmap.md](sprint-roadmap.md) — sprint plan
- **Release Management**: [../../docs/release-management.md](../../docs/release-management.md) — release process
