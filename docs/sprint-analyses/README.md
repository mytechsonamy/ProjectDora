# Sprint Analyses

> This folder contains sprint-by-sprint analysis reports, decisions, and DoR YAML files.

## Purpose

1. **Sprint-level**: Provides context for BA Agent + Developer (DoR YAML files accumulate here)
2. **Post-sprint**: Records retrospective insights and architectural decisions
3. **End-of-project**: Source material for user manual and technical documentation

## Folder Structure

```
docs/sprint-analyses/
├── README.md                 ← This file
├── S00-setup/
│   ├── analysis.md           ← Sprint analysis report
│   └── decisions.md          ← Decisions made during sprint
├── S01-admin-panel/
│   ├── analysis.md
│   ├── decisions.md
│   └── dor/                  ← Sprint DoR YAML files
│       ├── US-101.yaml
│       └── US-102.yaml
├── S02-content-modeling/
│   ├── analysis.md
│   ├── decisions.md
│   └── dor/
...
└── S13-uat/
    ├── analysis.md
    └── decisions.md
```

## Sprint Analysis Report Template

Each sprint's `analysis.md` should follow this format:

```markdown
# Sprint SXX — [Sprint Name]

## Kapsam (Scope)
- Spec items: 4.1.X.X
- Stories: US-XXX, US-XXX

## Is Analizi Ozeti (Business Analysis Summary)
- Requirements extracted from Teknik Sartname
- Constraints and dependencies
- RBAC requirements

## Teknik Kararlar (Technical Decisions)
- Architectural choices (ADR reference if applicable)
- New interface/abstraction definitions
- Database changes

## Test Plani (Test Plan)
- New test case IDs
- Coverage target

## Sprint Sonucu (Sprint Outcome)
- Completed stories
- Carried-over stories
- Lessons learned (retrospective)

## Dokumantasyon Notlari (Documentation Notes)
> Information to include in end-of-project user manual and technical documentation
- User-facing feature descriptions
- API endpoint summary
- Configuration parameters
- Screen/flow descriptions
```

## Decisions Log Template

Each sprint's `decisions.md` should follow this format:

```markdown
# Sprint SXX — Decisions Log

## D-001: [Decision Title]
- **Date**: YYYY-MM-DD
- **Context**: Why this decision was needed
- **Decision**: What was decided
- **Consequences**: Impact on architecture, schedule, or scope
- **ADR**: ADR-XXX (if elevated to an ADR)

## D-002: ...
```

## DoR YAML Convention

DoR files in `dor/` folders follow the format defined in `.claude/ai-sdlc/definition-of-ready.md`.

File naming: `US-{story_id}.yaml`

## Cross-References

- **Definition of Ready**: [../../.claude/ai-sdlc/definition-of-ready.md](../../.claude/ai-sdlc/definition-of-ready.md)
- **Sprint Roadmap**: [../../.claude/ai-sdlc/sprint-roadmap.md](../../.claude/ai-sdlc/sprint-roadmap.md)
- **Test Cases**: [../../.claude/testing/test-cases.md](../../.claude/testing/test-cases.md)
