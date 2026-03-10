# Threat Model

> Version: 1.0 | Status: Draft | Last Updated: 2026-03-09

## 1. Purpose

OWASP-aligned threat model for ProjectDora. Identifies threats, attack vectors, and mitigations across all modules. Referenced by security tests in [test-cases.md](../.claude/testing/test-cases.md).

## 2. Trust Boundaries

```
┌─────────────────────────────────────────────────────────────┐
│                    EXTERNAL (Untrusted)                       │
│  Anonymous Users, Public Internet, External API Consumers    │
└────────────────────────┬────────────────────────────────────┘
                         │ TLS (HTTPS)
                    ┌────▼────┐
                    │ WAF /   │ ← Trust Boundary 1
                    │ Reverse │
                    │ Proxy   │
                    └────┬────┘
                         │
┌────────────────────────▼────────────────────────────────────┐
│                    DMZ (Semi-Trusted)                         │
│  API Gateway, Rate Limiter, Auth Middleware                   │
└────────────────────────┬────────────────────────────────────┘
                         │ ← Trust Boundary 2
┌────────────────────────▼────────────────────────────────────┐
│                APPLICATION (Trusted)                          │
│  Module Services, MediatR Handlers, Business Logic           │
└────────────────────────┬────────────────────────────────────┘
                         │ ← Trust Boundary 3
┌────────────────────────▼────────────────────────────────────┐
│                    DATA (Most Trusted)                        │
│  PostgreSQL, Redis, Elasticsearch, MinIO                      │
└─────────────────────────────────────────────────────────────┘
```

## 3. Threat Catalog

### STRIDE Classification

| Category | Description |
|----------|-------------|
| **S** — Spoofing | Impersonating another user or system |
| **T** — Tampering | Modifying data without authorization |
| **R** — Repudiation | Denying an action was performed |
| **I** — Information Disclosure | Exposing data to unauthorized parties |
| **D** — Denial of Service | Making the system unavailable |
| **E** — Elevation of Privilege | Gaining unauthorized access |

## 4. Threat Details

### T-001: SQL Injection

| Field | Value |
|-------|-------|
| **STRIDE** | T, I, E |
| **Component** | QueryEngine, any DB query |
| **Attack Vector** | Malicious input in query parameters |
| **Impact** | Data exfiltration, data modification, auth bypass |
| **Likelihood** | High (public API surface) |
| **Risk** | Critical |

**Mitigations:**

| # | Mitigation | Implementation | Status |
|---|-----------|----------------|--------|
| 1 | Parameterized queries everywhere | All DB access via EF Core / YesSql (parameterized by default) | Planned |
| 2 | SQL query SELECT-only enforcement | SQL parser validates no DML/DDL/DCL | Planned |
| 3 | Input validation | FluentValidation on all API inputs | Planned |
| 5 | WAF SQL injection rules | Reverse proxy level filtering | Planned |

**Test Cases:** TC-QRY-U003, TC-SEC-S001

---

### T-002: Cross-Site Scripting (XSS)

| Field | Value |
|-------|-------|
| **STRIDE** | T, I |
| **Component** | Content body, Liquid templates |
| **Attack Vector** | Script injection via content fields, stored XSS via rich text |
| **Impact** | Session hijacking, data theft, defacement |
| **Likelihood** | High (CMS with user-generated content) |
| **Risk** | High |

**Mitigations:**

| # | Mitigation | Implementation |
|---|-----------|----------------|
| 1 | HTML sanitization on content save | HtmlSanitizer library, whitelist-based |
| 2 | Liquid template auto-escaping | Orchard Core default escaping |
| 3 | CSP header | `Content-Security-Policy: default-src 'self'` |
| 4 | HttpOnly cookies | Prevent JS access to auth cookies |

**Test Cases:** TC-CM-U015, TC-THM-U006, TC-SEC-S002

---

### T-003: Authentication Bypass

| Field | Value |
|-------|-------|
| **STRIDE** | S, E |
| **Component** | Auth middleware, OpenID Connect, JWT validation |
| **Attack Vector** | JWT manipulation, expired token reuse, direct URL access |
| **Impact** | Unauthorized access to all resources |
| **Likelihood** | Medium |
| **Risk** | Critical |

**Mitigations:**

| # | Mitigation | Implementation |
|---|-----------|----------------|
| 1 | JWT signature validation | RS256 with key rotation |
| 2 | Token expiry enforcement | Short-lived access tokens (15 min) |
| 3 | Refresh token rotation | One-time-use refresh tokens |
| 4 | Auth middleware on all endpoints | `[Authorize]` attribute default; `[AllowAnonymous]` explicit |
| 5 | Rate limiting on login | 10 attempts/min per IP |

**Test Cases:** TC-URP-S003, TC-URP-S005, TC-SEC-S004

---

### T-004: RBAC / Authorization Bypass

| Field | Value |
|-------|-------|
| **STRIDE** | E |
| **Component** | Permission checks, content-type-level RBAC |
| **Attack Vector** | IDOR (changing resource ID in URL), privilege escalation via API, missing permission check |
| **Impact** | Access to other users' data, admin functions |
| **Likelihood** | Medium |
| **Risk** | Critical |

**Mitigations:**

| # | Mitigation | Implementation |
|---|-----------|----------------|
| 1 | Permission check in every handler | MediatR pipeline behavior for authorization |
| 2 | IDOR prevention | Verify resource belongs to requester's tenant and user |
| 3 | Content-type-level RBAC | Check permission per content type, not just global |
| 4 | Deny by default | No permission = denied, never fail-open |
| 5 | Architecture tests | Ensure no handler skips auth behavior |

**Test Cases:** TC-URP-U005, TC-URP-U006, TC-URP-S001, TC-URP-S002, TC-URP-E003

---

### T-005: Tenant Data Leakage

| Field | Value |
|-------|-------|
| **STRIDE** | I |
| **Component** | All data access, queries, APIs |
| **Attack Vector** | Missing tenant_id filter, manipulating tenant header, cross-tenant search |
| **Impact** | Exposure of one tenant's data to another |
| **Likelihood** | Medium (modular monolith with shared DB) |
| **Risk** | Critical |

**Mitigations:**

| # | Mitigation | Implementation |
|---|-----------|----------------|
| 1 | Tenant filter in every query | Global query filter in EF Core; YesSql index filter |
| 2 | Tenant context middleware | Extract tenant from host/header early in pipeline |
| 3 | Integration tests | Dedicated cross-tenant test suite |
| 4 | Search index per tenant | Tenant-prefixed Elasticsearch indexes |

**Test Cases:** TC-CNT-I006, TC-URP-I006, TC-AUD-I006, TC-INF-I005, TC-QRY-I006

---

### T-006: Denial of Service

| Field | Value |
|-------|-------|
| **STRIDE** | D |
| **Component** | API endpoints, search queries |
| **Attack Vector** | Request flooding, expensive query abuse, large file upload, deep GraphQL |
| **Impact** | Platform unavailability |
| **Likelihood** | Medium |
| **Risk** | High |

**Mitigations:**

| # | Mitigation | Implementation |
|---|-----------|----------------|
| 1 | Rate limiting per user/IP | Configurable per endpoint category |
| 2 | Request size limits | Max body 10MB, max file 50MB |
| 3 | Query timeout | 30s for SQL, 10s for search |
| 4 | GraphQL depth limiting | Max depth 5, max complexity 1000 |
| 5 | Pagination required | Max page size 100, default 20 |
| 6 | Circuit breakers | Polly for all external dependencies |

**Test Cases:** TC-API-I004, TC-API-I006, TC-PERF-P007

---

### T-007: Insecure File Upload

| Field | Value |
|-------|-------|
| **STRIDE** | T, E |
| **Component** | Media upload, document processing |
| **Attack Vector** | Upload malicious file (web shell, path traversal), bypass file type check |
| **Impact** | Remote code execution, server compromise |
| **Likelihood** | Medium |
| **Risk** | Critical |

**Mitigations:**

| # | Mitigation | Implementation |
|---|-----------|----------------|
| 1 | File type whitelist | jpg, png, gif, pdf, docx, xlsx only |
| 2 | Magic byte validation | Validate file header, not just extension |
| 3 | Filename sanitization | Strip path components, generate UUID names |
| 4 | Separate storage | MinIO (S3), not local filesystem |
| 5 | No execution | MinIO serves files as downloads, no script execution |
| 6 | File size limit | 10MB media, 50MB documents |
| 7 | Antivirus scan | ClamAV scan on upload (optional) |

**Test Cases:** TC-ADM-U005, TC-ADM-U006, TC-SEC-S007

---

### T-008: Sensitive Data Exposure

| Field | Value |
|-------|-------|
| **STRIDE** | I |
| **Component** | API error responses, logs, stack traces |
| **Attack Vector** | Error messages revealing internal structure, PII in logs |
| **Impact** | Information leakage aiding further attacks |
| **Likelihood** | High (default .NET error responses are verbose) |
| **Risk** | Medium |

**Mitigations:**

| # | Mitigation | Implementation |
|---|-----------|----------------|
| 1 | Custom error handler | RFC 7807 Problem Details without stack traces |
| 2 | Serilog PII masking | `Destructure.ByTransforming<UserDto>()` |
| 3 | No secrets in logs | Connection strings, tokens never logged |
| 4 | Secure headers | Remove `Server`, `X-Powered-By` headers |
| 5 | HTTPS everywhere | TLS 1.2+ enforced |

**Test Cases:** TC-SEC-S005, TC-SEC-S008

---

### T-009: Audit Log Tampering

| Field | Value |
|-------|-------|
| **STRIDE** | R, T |
| **Component** | Audit trail module |
| **Attack Vector** | Admin deleting/modifying audit logs to cover tracks |
| **Impact** | Loss of accountability, compliance violation |
| **Likelihood** | Low (requires admin access) |
| **Risk** | High (compliance critical) |

**Mitigations:**

| # | Mitigation | Implementation |
|---|-----------|----------------|
| 1 | Append-only audit table | No UPDATE/DELETE permissions on audit schema |
| 2 | Hash chain integrity | Each entry includes hash of previous entry |
| 3 | Separate DB role | Audit write role separate from admin role |
| 4 | Audit the auditor | Meta-audit: log any retention cleanup |
| 5 | Periodic integrity check | Background job verifies hash chain |

**Test Cases:** TC-AUD-U010, TC-AUD-I003

---

### T-010: SSRF (Server-Side Request Forgery)

| Field | Value |
|-------|-------|
| **STRIDE** | I, E |
| **Component** | Media URL imports, webhook notifications |
| **Attack Vector** | User provides internal URL (localhost, 169.254.x.x) as media source or webhook |
| **Impact** | Access to internal services, cloud metadata |
| **Likelihood** | Medium |
| **Risk** | High |

**Mitigations:**

| # | Mitigation | Implementation |
|---|-----------|----------------|
| 1 | URL validation | Block private IP ranges, localhost, link-local |
| 2 | DNS resolution check | Resolve hostname, verify not internal |
| 3 | Allowlist for webhooks | Only predefined webhook targets |
| 4 | Egress firewall | Docker network rules restricting outbound |

**Test Cases:** TC-SEC-S006

---

### T-011: CSRF (Cross-Site Request Forgery)

| Field | Value |
|-------|-------|
| **STRIDE** | T |
| **Component** | State-changing API endpoints |
| **Attack Vector** | Malicious site triggers API call using user's session |
| **Impact** | Unauthorized state changes |
| **Likelihood** | Low (API uses Bearer tokens, not cookies, for most operations) |
| **Risk** | Medium |

**Mitigations:**

| # | Mitigation | Implementation |
|---|-----------|----------------|
| 1 | Bearer token auth | API uses Authorization header, not cookies |
| 2 | SameSite cookies | For admin panel cookie auth: `SameSite=Strict` |
| 3 | Anti-forgery tokens | Orchard Core built-in for form submissions |
| 4 | CORS policy | Strict origin whitelist |

**Test Cases:** TC-SEC-S003

## 5. Risk Matrix

| Threat | Likelihood | Impact | Risk | Priority |
|--------|-----------|--------|------|----------|
| T-001 SQL Injection | High | Critical | **Critical** | P0 |
| T-002 XSS | High | High | **High** | P0 |
| T-003 Auth Bypass | Medium | Critical | **Critical** | P0 |
| T-004 RBAC Bypass | Medium | Critical | **Critical** | P0 |
| T-005 Tenant Leak | Medium | Critical | **Critical** | P0 |
| T-006 DoS | Medium | High | **High** | P1 |
| T-007 File Upload | Medium | Critical | **Critical** | P0 |
| T-008 Data Exposure | High | Medium | **Medium** | P1 |
| T-009 Audit Tampering | Low | High | **Medium** | P1 |
| T-010 SSRF | Medium | High | **High** | P1 |
| T-011 CSRF | Low | Medium | **Low** | P2 |

## 6. Security Testing Schedule

| Sprint | Security Focus |
|--------|---------------|
| S0 | Security architecture review, threat model approval |
| S1-S4 | Input validation testing (SQL injection, XSS) |
| S5-S8 | RBAC testing, auth bypass testing |
| S9-S10 | Tenant isolation testing, API security |
| S11 | Theme Management + cross-cutting security |
| S12 | Full OWASP scan, security hardening, penetration testing |
| S13 | Security regression, UAT, final validation |

## 7. Cross-References

- **Test Cases**: [../.claude/testing/test-cases.md](../.claude/testing/test-cases.md) — security test cases (TC-SEC-*, TC-URP-S*)
- **Data Governance**: [data-governance.md](data-governance.md) — data classification and encryption
- **Resilience**: [resilience-and-chaos-tests.md](resilience-and-chaos-tests.md) — DoS scenarios
- **Module Boundaries**: [module-boundaries.md](module-boundaries.md) — trust boundaries between modules
- **API Contract**: [api-contract.yaml](api-contract.yaml) — endpoint security requirements
