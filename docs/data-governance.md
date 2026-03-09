# Data Governance & Classification

> Version: 1.0 | Status: Draft | Last Updated: 2026-03-09

## 1. Purpose

This document defines data classification, retention policies, encryption requirements, and KVKK/GDPR compliance measures for ProjectDora. All AI agents and developers must consult this document when handling user data, logs, or PII.

## 2. Regulatory Context

| Regulation | Scope | Key Requirements |
|------------|-------|-----------------|
| **KVKK** (6698 sayılı Kişisel Verilerin Korunması Kanunu) | Turkish data protection law | Explicit consent, data minimization, right to erasure, breach notification (72h) |
| **GDPR** (if EU users) | EU data protection | Same as KVKK + data portability, DPO requirement |
| **5070 Sayılı Elektronik İmza Kanunu** | Electronic signatures | Audit log integrity |

## 3. Data Classification

### 3.1 Classification Levels

| Level | Label | Definition | Handling |
|-------|-------|-----------|----------|
| **L1** | Public | Content published for public access | No restrictions on storage/display |
| **L2** | Internal | Operational data, non-public content | Access via RBAC, no external exposure |
| **L3** | Sensitive | Audit logs, system config, analytics | Encrypted at rest, RBAC + audit logging |
| **L4** | PII | Personal identifiable information | Encrypted at rest + in transit, consent required, right to erasure |
| **L5** | Restricted | Credentials, secrets, encryption keys | Never stored in code/logs, vault only |

### 3.2 Data Classification Matrix

| Data Type | Schema | Classification | Encryption at Rest | Encryption in Transit | Retention | Access Roles |
|-----------|--------|---------------|-------------------|---------------------|-----------|-------------|
| Published content | `orchard` | L1 — Public | No | TLS | Unlimited | All (incl. Anonymous) |
| Draft content | `orchard` | L2 — Internal | No | TLS | Unlimited | Editor, Author, Admin |
| Content versions | `orchard` | L2 — Internal | No | TLS | Unlimited | Editor, Admin |
| User account (username, roles) | `orchard` | L2 — Internal | No | TLS | Account lifetime + 1 year | Admin, Self |
| User email | `orchard` | L4 — PII | AES-256 | TLS | Account lifetime + 1 year | Admin, Self |
| User password hash | `orchard` | L5 — Restricted | bcrypt (self-encrypting) | TLS | Account lifetime | System only |
| Audit logs | `audit` | L3 — Sensitive | AES-256 | TLS | **2 years** | Denetci, Admin |
| Audit diffs | `audit` | L3 — Sensitive | AES-256 | TLS | **2 years** | Denetci, Admin |
| Generated reports | MinIO | L3 — Sensitive | AES-256 (MinIO SSE) | TLS | **1 year** | Owner, Admin |
| Uploaded media | MinIO | L1-L2 | No | TLS | Until deleted | Per content RBAC |
| Analytics data (destek, KOBİ) | `analytics` | L3 — Sensitive | AES-256 | TLS | **5 years** | Analyst, Admin |
| Search indexes | Elasticsearch | L2 — Internal | Disk encryption | TLS | Rebuilt from source | System |
| Redis cache | Redis | L2 — Internal | No (ephemeral) | TLS | TTL-based | System |
| Application logs (Serilog) | File/stdout | L2 — Internal | No | N/A | **90 days** | Admin, DevOps |
| Connection strings | Environment/Vault | L5 — Restricted | Vault encrypted | TLS | N/A | System only |
| API keys / tokens | Memory | L5 — Restricted | N/A (in memory) | TLS | Token TTL | System only |

### 3.3 Tenant Data Isolation

| Aspect | Implementation |
|--------|---------------|
| Database | Separate schema per tenant OR tenant_id filter on every query |
| Search index | Tenant-prefixed index names |
| File storage | Tenant-prefixed MinIO bucket/folder |
| Cache | Tenant-prefixed cache keys |
| Audit logs | tenant_id column, filtered in all queries |

**Invariant**: No API endpoint, query, or background job may return data from a tenant other than the requester's tenant. SuperAdmin is the only exception (explicit tenant context switch).

## 4. Retention Policies

### 4.1 Retention Schedule

| Data Category | Retention Period | After Expiry | Legal Basis |
|--------------|-----------------|-------------|-------------|
| Published content | Unlimited | N/A | Business need |
| Draft content | Unlimited | N/A | Business need |
| User accounts | Account lifetime + 1 year | Anonymize or delete | KVKK Art. 7 |
| Audit logs | 2 years | Archive or delete | KVKK Art. 12, compliance |
| Generated reports | 1 year | Delete files, keep metadata | Business need |
| Analytics data | 5 years | Archive | Regulatory (KOSGEB) |
| Application logs | 90 days | Rotate/delete | Operational |

### 4.2 Retention Enforcement

```
Background Job: RetentionCleanupJob
Schedule: Daily at 02:00 UTC
Process:
  1. For each tenant:
     a. Query expired records by retention policy
     b. For L4 (PII): Hard delete or anonymize
     c. For L3 (Sensitive): Archive to cold storage or delete
     d. For L2 (Internal): Soft delete or archive
     e. Log cleanup action to audit trail (meta-audit)
  2. Emit RetentionCleanupCompleted event
```

### 4.3 Right to Erasure (KVKK Art. 11 / GDPR Art. 17)

When a user requests data deletion:

| Step | Action | Scope |
|------|--------|-------|
| 1 | Verify identity | Authenticate requester |
| 2 | Disable account | Set `enabled = false` |
| 3 | Anonymize PII | Replace email, username with hash |
| 4 | Anonymize audit logs | Replace user_id with "DELETED_USER_{hash}" (keep logs for compliance) |
| 5 | Delete generated reports | Remove files from MinIO |
| 6 | Retain anonymized analytics | Keep aggregated data without PII |
| 7 | Log erasure | Create audit entry for the erasure itself |
| 8 | Confirm to user | Provide confirmation within 30 days (KVKK) |

## 5. Encryption Standards

### 5.1 At Rest

| Component | Method | Key Management |
|-----------|--------|---------------|
| PostgreSQL (L3, L4 columns) | pgcrypto AES-256 or application-level encryption | Key in environment variable / vault |
| MinIO | Server-Side Encryption (SSE-S3) | MinIO managed keys |
| Elasticsearch | Disk-level encryption (LUKS/dm-crypt) | OS-level keys |
| Redis | Not encrypted (ephemeral, no PII) | N/A |

### 5.2 In Transit

| Channel | Method |
|---------|--------|
| Client ↔ API | TLS 1.2+ (HTTPS) |
| API ↔ PostgreSQL | TLS (sslmode=require) |
| API ↔ Redis | TLS |
| API ↔ Elasticsearch | TLS |
| API ↔ MinIO | TLS |
| Inter-service (if any) | TLS |

### 5.3 Secrets Management

| Secret Type | Storage | Never Store In |
|-------------|---------|---------------|
| DB connection strings | Environment variables or Docker secrets | Code, config files, logs |
| API keys | Environment variables or Docker secrets | Code, config files, logs |
| JWT signing key | Environment variables or vault | Code, config files |
| MinIO access keys | Environment variables or Docker secrets | Code, config files |
| Encryption keys | Vault (HashiCorp or Docker secrets) | Code, config files, logs, DB |

## 6. PII Handling Rules for AI Agents

### 6.1 Do NOT

- Store PII in log messages (Serilog must use destructure with masking)
- Include real email/names in test fixtures (use `@kosgeb-test.gov.tr` domain)
- Return PII in error responses or stack traces
- Cache PII in Redis without TTL
- Include PII in search indexes unless explicitly required
- Log query results that may contain PII

### 6.2 DO

- Use `[PersonalData]` attribute on PII properties in EF Core models
- Mask PII in logs: `email: m***@kosgeb.gov.tr`
- Apply encryption to PII columns in database
- Include `tenant_id` filter in every query touching PII
- Implement `IDataProtectionProvider` for PII encryption/decryption
- Use parameterized queries (never string interpolation) for PII values

### 6.3 Golden Dataset Compliance

The golden test dataset ([golden-dataset.md](../.claude/testing/golden-dataset.md)) must:

- Use fake Turkish names (not real people): `Ahmet Yılmaz`, `Ayşe Demir`, etc.
- Use test email domain: `*@kosgeb-test.gov.tr`
- Use fake vergi_no (tax ID): `1234567890` format, clearly fake
- Use fake phone numbers: `+90 555 000 XXXX`
- Never contain real KOSGEB data

## 7. Breach Response Plan

| Step | Action | Timeline | Responsible |
|------|--------|----------|-------------|
| 1 | Detect breach (monitoring, alert) | Immediate | System |
| 2 | Contain breach (disable affected accounts, rotate keys) | < 1 hour | DevOps + Admin |
| 3 | Assess scope (what data, how many users, which tenants) | < 4 hours | Security team |
| 4 | Notify KVKK board (Kişisel Verileri Koruma Kurumu) | < 72 hours | Legal |
| 5 | Notify affected users | < 72 hours | Admin + Legal |
| 6 | Remediate (patch vulnerability, strengthen controls) | ASAP | Dev team |
| 7 | Post-mortem and documentation | < 1 week | All |

## 8. Cross-References

- **Domain Model**: [domain-model.md](domain-model.md) — entity definitions that map to classifications
- **Golden Dataset**: [../.claude/testing/golden-dataset.md](../.claude/testing/golden-dataset.md) — test data compliance
- **Test Strategy**: [../.claude/testing/test-strategy.md](../.claude/testing/test-strategy.md) — security test layer
- **Threat Model**: [threat-model.md](threat-model.md) — threats that data governance mitigates
- **Module Boundaries**: [module-boundaries.md](module-boundaries.md) — schema isolation per module
