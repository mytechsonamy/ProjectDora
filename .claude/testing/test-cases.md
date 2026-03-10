# Test Cases Registry

> Version: 1.0 | Status: Draft | Last Updated: 2026-03-09

## 1. Overview

This document catalogs all planned test cases for ProjectDora, mapped to modules, spec refs, and priorities. Test cases are generated from DoR acceptance tests and expanded with edge cases and cross-cutting concerns.

**Format:** TC-{Module}-{Number} | Module | Spec Ref | Scenario | Priority | Type

**Priority Classification:**

| Priority | Label | Definition | Examples |
|----------|-------|-----------|----------|
| P0 | Blocker | Security bypass, data loss, system crash | Auth bypass, data corruption, tenant leak |
| P1 | Critical | Core feature failure | CRUD fails, workflow broken, query returns wrong data |
| P2 | Major | Feature degradation, UX issues | Pagination broken, slow response, UI misrender |
| P3 | Minor | Cosmetic, non-functional | Typo in message, alignment, tooltip missing |

**Test Types:** Unit (U), Integration (I), E2E (E), Security (S), Performance (P)

## 2. Module 4.1.1 — Admin Panel

### Unit Tests (8)

| TC-ID | Spec Ref | Scenario | Priority | Type |
|-------|----------|----------|----------|------|
| TC-ADM-U001 | 4.1.1.1 | Admin menu renders correct items for SuperAdmin role | P1 | U |
| TC-ADM-U002 | 4.1.1.1 | Admin menu hides items for insufficient permissions | P0 | U |
| TC-ADM-U003 | 4.1.1.2 | Dashboard widget data aggregation returns correct counts | P1 | U |
| TC-ADM-U004 | 4.1.1.3 | Media upload validator accepts allowed file types (jpg, png, pdf) | P1 | U |
| TC-ADM-U005 | 4.1.1.3 | Media upload validator rejects disallowed file types (exe, bat) | P0 | U |
| TC-ADM-U006 | 4.1.1.3 | Media upload validator enforces max file size (10MB) | P1 | U |
| TC-ADM-U007 | 4.1.1.4 | Admin panel settings serialization/deserialization | P2 | U |
| TC-ADM-U008 | 4.1.1.1 | Navigation breadcrumb generation for nested pages | P3 | U |

### Integration Tests (5)

| TC-ID | Spec Ref | Scenario | Priority | Type |
|-------|----------|----------|----------|------|
| TC-ADM-I001 | 4.1.1.1 | Admin login with valid credentials returns JWT token | P0 | I |
| TC-ADM-I002 | 4.1.1.1 | Admin login with invalid credentials returns 401 | P0 | I |
| TC-ADM-I003 | 4.1.1.3 | Media upload to MinIO stores file and returns URL | P1 | I |
| TC-ADM-I004 | 4.1.1.3 | Media delete removes file from MinIO and database | P1 | I |
| TC-ADM-I005 | 4.1.1.2 | Dashboard API returns correct widget data from PostgreSQL | P1 | I |

### E2E Tests (3)

| TC-ID | Spec Ref | Scenario | Priority | Type |
|-------|----------|----------|----------|------|
| TC-ADM-E001 | 4.1.1.1 | Full admin login → dashboard → navigate to content | P1 | E |
| TC-ADM-E002 | 4.1.1.3 | Upload media file → verify in gallery → delete | P2 | E |
| TC-ADM-E003 | 4.1.1.1 | Admin with Viewer role cannot access admin panel | P0 | E |

**Subtotal: 16 tests (8U + 5I + 3E)**

## 3. Module 4.1.2 — Content Modeling

### Unit Tests (15)

| TC-ID | Spec Ref | Scenario | Priority | Type |
|-------|----------|----------|----------|------|
| TC-CM-U001 | 4.1.2.1 | Create content type with valid fields returns success | P1 | U |
| TC-CM-U002 | 4.1.2.1 | Create content type with duplicate name throws ConflictException | P1 | U |
| TC-CM-U003 | 4.1.2.1 | Create content type with invalid name (special chars) throws ValidationException | P1 | U |
| TC-CM-U004 | 4.1.2.2 | Add field to content type updates field definitions | P1 | U |
| TC-CM-U005 | 4.1.2.2 | Add field with duplicate name within type throws ValidationException | P1 | U |
| TC-CM-U006 | 4.1.2.3 | Content type with TitlePart validates title not empty | P1 | U |
| TC-CM-U007 | 4.1.2.3 | Content type with BodyPart accepts Turkish HTML content | P1 | U |
| TC-CM-U008 | 4.1.2.4 | Alias generation from Turkish title produces valid slug | P2 | U |
| TC-CM-U009 | 4.1.2.4 | Alias handles special chars: ş→s, ç→c, ğ→g, ı→i, ö→o, ü→u | P2 | U |
| TC-CM-U010 | 4.1.2.5 | Gallery field accepts multiple images with ordering | P2 | U |
| TC-CM-U011 | 4.1.2.1 | Remove content type with no instances succeeds | P1 | U |
| TC-CM-U012 | 4.1.2.1 | Remove content type with existing instances requires force flag | P0 | U |
| TC-CM-U013 | 4.1.2.2 | Field type validation (TextField, NumericField, DateField, etc.) | P1 | U |
| TC-CM-U014 | 4.1.2.1 | Content type definition serialization roundtrip preserves all fields | P1 | U |
| TC-CM-U015 | 4.1.2.3 | Rich text sanitization strips XSS vectors | P0 | U |

### Integration Tests (10)

| TC-ID | Spec Ref | Scenario | Priority | Type |
|-------|----------|----------|----------|------|
| TC-CM-I001 | 4.1.2.1 | Create content type persists to database and is retrievable | P1 | I |
| TC-CM-I002 | 4.1.2.1 | Delete content type removes from database | P1 | I |
| TC-CM-I003 | 4.1.2.2 | Add/remove fields persists field definitions | P1 | I |
| TC-CM-I004 | 4.1.2.3 | Content type with all part types (Title, Body, Common, Audit) | P1 | I |
| TC-CM-I005 | 4.1.2.1 | Content type API endpoint returns correct schema | P1 | I |
| TC-CM-I006 | 4.1.2.4 | Alias uniqueness enforced across tenant | P1 | I |
| TC-CM-I007 | 4.1.2.5 | Gallery images stored in MinIO with correct paths | P2 | I |
| TC-CM-I008 | 4.1.2.1 | Content type creation emits ContentTypeCreated audit event | P1 | I |
| TC-CM-I009 | 4.1.2.1 | Content type with 50+ fields performance within 500ms | P2 | I |
| TC-CM-I010 | 4.1.2.1 | Concurrent content type creation handles race condition | P1 | I |

### E2E Tests (3)

| TC-ID | Spec Ref | Scenario | Priority | Type |
|-------|----------|----------|----------|------|
| TC-CM-E001 | 4.1.2.1 | Admin creates content type → adds fields → verifies in API | P1 | E |
| TC-CM-E002 | 4.1.2.5 | Admin creates gallery field → uploads images → verifies order | P2 | E |
| TC-CM-E003 | 4.1.2.1 | Non-admin user cannot access content type management | P0 | E |

**Subtotal: 28 tests (15U + 10I + 3E)**

## 4. Module 4.1.3 — Content Management

### Unit Tests (18)

| TC-ID | Spec Ref | Scenario | Priority | Type |
|-------|----------|----------|----------|------|
| TC-CNT-U001 | 4.1.3.1 | Create content item with valid data returns ContentItemId | P1 | U |
| TC-CNT-U002 | 4.1.3.1 | Create content item with missing required field throws ValidationException | P1 | U |
| TC-CNT-U003 | 4.1.3.2 | Publish draft content changes status to Published | P1 | U |
| TC-CNT-U004 | 4.1.3.2 | Unpublish content changes status to Draft | P1 | U |
| TC-CNT-U005 | 4.1.3.3 | Version increment on content update | P1 | U |
| TC-CNT-U006 | 4.1.3.3 | Retrieve specific version returns correct data | P1 | U |
| TC-CNT-U007 | 4.1.3.3 | Rollback to previous version restores content | P1 | U |
| TC-CNT-U008 | 4.1.3.4 | SEO URL generation from Turkish title | P2 | U |
| TC-CNT-U009 | 4.1.3.4 | SEO meta fields (title, description, keywords) validation | P2 | U |
| TC-CNT-U010 | 4.1.3.1 | Delete content item (soft delete) marks as deleted | P1 | U |
| TC-CNT-U011 | 4.1.3.1 | Restore soft-deleted content item | P1 | U |
| TC-CNT-U012 | 4.1.3.1 | Hard delete content item removes from system | P0 | U |
| TC-CNT-U013 | 4.1.3.5 | Content list with pagination returns correct page | P1 | U |
| TC-CNT-U014 | 4.1.3.5 | Content list with filter by status (Draft/Published) | P1 | U |
| TC-CNT-U015 | 4.1.3.5 | Content list with sort by date/title | P2 | U |
| TC-CNT-U016 | 4.1.3.1 | Content item with Turkish special characters preserved | P1 | U |
| TC-CNT-U017 | 4.1.3.1 | Content item with maximum body length (50KB) | P2 | U |
| TC-CNT-U018 | 4.1.3.2 | Scheduled publish sets future date correctly | P1 | U |

### Integration Tests (10)

| TC-ID | Spec Ref | Scenario | Priority | Type |
|-------|----------|----------|----------|------|
| TC-CNT-I001 | 4.1.3.1 | CRUD lifecycle: create → read → update → delete via API | P1 | I |
| TC-CNT-I002 | 4.1.3.2 | Draft/publish lifecycle persists state to database | P1 | I |
| TC-CNT-I003 | 4.1.3.3 | Version history stored and retrievable | P1 | I |
| TC-CNT-I004 | 4.1.3.1 | Content creation emits ContentItemCreated audit event | P1 | I |
| TC-CNT-I005 | 4.1.3.1 | Content update emits ContentItemUpdated audit event with diff | P1 | I |
| TC-CNT-I006 | 4.1.3.1 | Tenant isolation: content from tenant-a not visible to tenant-b | P0 | I |
| TC-CNT-I007 | 4.1.3.4 | SEO URL resolves to correct content item | P2 | I |
| TC-CNT-I008 | 4.1.3.5 | Content list API with 1000+ items returns within 1s | P2 | I |
| TC-CNT-I009 | 4.1.3.1 | GraphQL query returns content item with all parts | P1 | I |
| TC-CNT-I010 | 4.1.3.1 | REST and GraphQL return consistent data for same item | P1 | I |

### E2E Tests (5)

| TC-ID | Spec Ref | Scenario | Priority | Type |
|-------|----------|----------|----------|------|
| TC-CNT-E001 | 4.1.3.1 | Editor creates content → saves draft → publishes → verifies on frontend | P1 | E |
| TC-CNT-E002 | 4.1.3.3 | Editor edits content → new version created → rollback to v1 | P1 | E |
| TC-CNT-E003 | 4.1.3.1 | Author creates content, Editor publishes it (role separation) | P1 | E |
| TC-CNT-E004 | 4.1.3.5 | Content list with search, filter, sort, paginate | P2 | E |
| TC-CNT-E005 | 4.1.3.1 | Viewer role can read but cannot create/edit/delete | P0 | E |

**Subtotal: 33 tests (18U + 10I + 5E)**

## 5. Module 4.1.4 — Theme Management

### Unit Tests (6)

| TC-ID | Spec Ref | Scenario | Priority | Type |
|-------|----------|----------|----------|------|
| TC-THM-U001 | 4.1.4.1 | Liquid template parsing with valid syntax | P1 | U |
| TC-THM-U002 | 4.1.4.1 | Liquid template parsing with invalid syntax returns error | P1 | U |
| TC-THM-U003 | 4.1.4.2 | Theme activation switches active theme | P1 | U |
| TC-THM-U004 | 4.1.4.1 | Template variable resolution for content parts | P2 | U |
| TC-THM-U005 | 4.1.4.3 | Monaco editor configuration serialization | P3 | U |
| TC-THM-U006 | 4.1.4.1 | Liquid template XSS prevention (script tag stripping) | P0 | U |

### Integration Tests (4)

| TC-ID | Spec Ref | Scenario | Priority | Type |
|-------|----------|----------|----------|------|
| TC-THM-I001 | 4.1.4.1 | Template renders content item with Turkish text | P1 | I |
| TC-THM-I002 | 4.1.4.2 | Theme switch applies to all pages | P1 | I |
| TC-THM-I003 | 4.1.4.1 | Template edit persists and renders on next request | P1 | I |
| TC-THM-I004 | 4.1.4.1 | Template rendering performance < 100ms per page | P2 | I |

**Subtotal: 10 tests (6U + 4I)**

## 6. Module 4.1.5 — Query Management

### Unit Tests (12)

| TC-ID | Spec Ref | Scenario | Priority | Type |
|-------|----------|----------|----------|------|
| TC-QRY-U001 | 4.1.5.1 | Lucene query builder generates valid query syntax | P1 | U |
| TC-QRY-U002 | 4.1.5.2 | Elasticsearch query builder generates valid JSON | P1 | U |
| TC-QRY-U003 | 4.1.5.3 | SQL query with parameterized values prevents injection | P0 | U |
| TC-QRY-U004 | 4.1.5.1 | Query parameter validation (type, range, required) | P1 | U |
| TC-QRY-U005 | 4.1.5.1 | Lucene query with Turkish characters (ş, ç, ğ, ı, ö, ü) | P1 | U |
| TC-QRY-U006 | 4.1.5.2 | Elasticsearch fuzzy query configuration | P2 | U |
| TC-QRY-U007 | 4.1.5.3 | SQL query execution timeout enforcement | P1 | U |
| TC-QRY-U008 | 4.1.5.1 | Query result pagination (offset, limit) | P1 | U |
| TC-QRY-U009 | 4.1.5.3 | SQL query blocks mutation statements (INSERT, UPDATE, DELETE) | P0 | U |
| TC-QRY-U010 | 4.1.5.1 | Empty query returns validation error | P2 | U |
| TC-QRY-U011 | 4.1.5.4 | Query definition save/load roundtrip | P1 | U |
| TC-QRY-U012 | 4.1.5.1 | Multi-field Lucene query with boolean operators | P1 | U |

### Integration Tests (8)

| TC-ID | Spec Ref | Scenario | Priority | Type |
|-------|----------|----------|----------|------|
| TC-QRY-I001 | 4.1.5.1 | Lucene query against indexed golden dataset returns correct results | P1 | I |
| TC-QRY-I002 | 4.1.5.2 | Elasticsearch query returns results with relevance scoring | P1 | I |
| TC-QRY-I003 | 4.1.5.3 | SQL query against PostgreSQL returns correct dataset | P1 | I |
| TC-QRY-I004 | 4.1.5.1 | Turkish text search with stemming (destekler → destek) | P1 | I |
| TC-QRY-I005 | 4.1.5.2 | Elasticsearch query with 500+ records paginates correctly | P2 | I |
| TC-QRY-I006 | 4.1.5.3 | SQL query with tenant filter returns only tenant data | P0 | I |
| TC-QRY-I007 | 4.1.5.4 | Saved query retrieved and executed successfully | P1 | I |
| TC-QRY-I008 | 4.1.5.1 | Query execution audit event logged | P1 | I |

### E2E Tests (3)

| TC-ID | Spec Ref | Scenario | Priority | Type |
|-------|----------|----------|----------|------|
| TC-QRY-E001 | 4.1.5.1 | Admin creates Lucene query → executes → views results | P1 | E |
| TC-QRY-E002 | 4.1.5.4 | Admin saves query → publishes as API endpoint → client queries | P1 | E |
| TC-QRY-E003 | 4.1.5.3 | Non-admin cannot execute SQL queries | P0 | E |

**Subtotal: 23 tests (12U + 8I + 3E)**

## 7. Module 4.1.6 — User/Role/Permission

### Unit Tests (12)

| TC-ID | Spec Ref | Scenario | Priority | Type |
|-------|----------|----------|----------|------|
| TC-URP-U001 | 4.1.6.1 | Create user with valid data returns UserId | P1 | U |
| TC-URP-U002 | 4.1.6.1 | Create user with duplicate email throws ConflictException | P1 | U |
| TC-URP-U003 | 4.1.6.2 | Create role with permissions set | P1 | U |
| TC-URP-U004 | 4.1.6.2 | Assign role to user updates user roles | P1 | U |
| TC-URP-U005 | 4.1.6.3 | Check permission for user with required role returns true | P0 | U |
| TC-URP-U006 | 4.1.6.3 | Check permission for user without role returns false | P0 | U |
| TC-URP-U007 | 4.1.6.3 | Content-type-level permission check | P0 | U |
| TC-URP-U008 | 4.1.6.1 | Password validation (min length, complexity) | P1 | U |
| TC-URP-U009 | 4.1.6.2 | Role with no permissions is valid (empty permission set) | P2 | U |
| TC-URP-U010 | 4.1.6.1 | User disable/enable toggling | P1 | U |
| TC-URP-U011 | 4.1.6.3 | Multi-role user has union of all role permissions | P1 | U |
| TC-URP-U012 | 4.1.6.1 | User profile update with Turkish characters in name | P2 | U |

### Integration Tests (8)

| TC-ID | Spec Ref | Scenario | Priority | Type |
|-------|----------|----------|----------|------|
| TC-URP-I001 | 4.1.6.1 | User CRUD persists to database correctly | P1 | I |
| TC-URP-I002 | 4.1.6.2 | Role assignment persists and is reflected in auth token | P0 | I |
| TC-URP-I003 | 4.1.6.3 | API endpoint returns 403 for user without required permission | P0 | I |
| TC-URP-I004 | 4.1.6.1 | User creation emits UserCreated audit event | P1 | I |
| TC-URP-I005 | 4.1.6.2 | Role change emits RoleAssigned audit event | P1 | I |
| TC-URP-I006 | 4.1.6.1 | Tenant isolation: users from tenant-a not visible to tenant-b | P0 | I |
| TC-URP-I007 | 4.1.6.3 | OpenID Connect token issuance with correct claims | P0 | I |
| TC-URP-I008 | 4.1.6.1 | Concurrent user creation handles race condition | P2 | I |

### Security Tests (5)

| TC-ID | Spec Ref | Scenario | Priority | Type |
|-------|----------|----------|----------|------|
| TC-URP-S001 | 4.1.6.3 | Privilege escalation attempt via direct API call blocked | P0 | S |
| TC-URP-S002 | 4.1.6.3 | IDOR: user cannot access another user's profile by ID manipulation | P0 | S |
| TC-URP-S003 | 4.1.6.1 | Brute force login protection (rate limiting) | P0 | S |
| TC-URP-S004 | 4.1.6.1 | Password stored as bcrypt hash, not plaintext | P0 | S |
| TC-URP-S005 | 4.1.6.3 | JWT token tampering detection | P0 | S |

### E2E Tests (3)

| TC-ID | Spec Ref | Scenario | Priority | Type |
|-------|----------|----------|----------|------|
| TC-URP-E001 | 4.1.6.1 | Admin creates user → assigns role → user logs in with correct permissions | P1 | E |
| TC-URP-E002 | 4.1.6.2 | Admin creates role → assigns permissions → verifies access control | P1 | E |
| TC-URP-E003 | 4.1.6.3 | no-permission-user cannot access any protected endpoint | P0 | E |

**Subtotal: 28 tests (12U + 8I + 5S + 3E)**

## 8. Module 4.1.7 — Workflow Engine

### Unit Tests (10)

| TC-ID | Spec Ref | Scenario | Priority | Type |
|-------|----------|----------|----------|------|
| TC-WF-U001 | 4.1.7.1 | Workflow definition deserialization from JSON | P1 | U |
| TC-WF-U002 | 4.1.7.1 | Workflow with invalid transition (missing target) throws ValidationException | P1 | U |
| TC-WF-U003 | 4.1.7.2 | Event trigger matches content type filter | P1 | U |
| TC-WF-U004 | 4.1.7.2 | Event trigger ignores non-matching content type | P1 | U |
| TC-WF-U005 | 4.1.7.3 | Activity execution returns correct output | P1 | U |
| TC-WF-U006 | 4.1.7.3 | Custom activity validation | P1 | U |
| TC-WF-U007 | 4.1.7.4 | Workflow execution state machine transitions | P1 | U |
| TC-WF-U008 | 4.1.7.1 | Workflow enable/disable toggling | P2 | U |
| TC-WF-U009 | 4.1.7.4 | Workflow timeout handling | P1 | U |
| TC-WF-U010 | 4.1.7.4 | Workflow error handling (activity failure) | P1 | U |

### Integration Tests (7)

| TC-ID | Spec Ref | Scenario | Priority | Type |
|-------|----------|----------|----------|------|
| TC-WF-I001 | 4.1.7.2 | Content publish triggers workflow execution | P1 | I |
| TC-WF-I002 | 4.1.7.3 | Email notification activity sends via SMTP | P1 | I |
| TC-WF-I003 | 4.1.7.4 | Multi-step workflow executes all activities in order | P1 | I |
| TC-WF-I004 | 4.1.7.1 | Workflow definition persists and loads from database | P1 | I |
| TC-WF-I005 | 4.1.7.4 | Workflow execution log persists to audit trail | P1 | I |
| TC-WF-I006 | 4.1.7.2 | Timer trigger fires at scheduled time | P1 | I |
| TC-WF-I007 | 4.1.7.4 | Concurrent workflow executions do not interfere | P1 | I |

### E2E Tests (3)

| TC-ID | Spec Ref | Scenario | Priority | Type |
|-------|----------|----------|----------|------|
| TC-WF-E001 | 4.1.7.1 | Admin designs workflow → enables → content triggers it | P1 | E |
| TC-WF-E002 | 4.1.7.1 | Content approval workflow: submit → review → approve → publish | P1 | E |
| TC-WF-E003 | 4.1.7.1 | Non-WorkflowAdmin cannot create or modify workflows | P0 | E |

**Subtotal: 20 tests (10U + 7I + 3E)**

## 9. Module 4.1.8 — Multi-language

### Unit Tests (8)

| TC-ID | Spec Ref | Scenario | Priority | Type |
|-------|----------|----------|----------|------|
| TC-I18N-U001 | 4.1.8.1 | CulturePicker returns available cultures | P1 | U |
| TC-I18N-U002 | 4.1.8.1 | Culture switch changes localization context | P1 | U |
| TC-I18N-U003 | 4.1.8.2 | LocalizationPart links content to culture | P1 | U |
| TC-I18N-U004 | 4.1.8.3 | PO file parsing extracts translations correctly | P1 | U |
| TC-I18N-U005 | 4.1.8.3 | Missing translation key falls back to default culture | P1 | U |
| TC-I18N-U006 | 4.1.8.2 | Localized content URL includes culture prefix (/tr/, /en/) | P2 | U |
| TC-I18N-U007 | 4.1.8.1 | Turkish-specific formatting (date: dd.MM.yyyy, currency: ₺) | P2 | U |
| TC-I18N-U008 | 4.1.8.3 | PO file with pluralization rules | P2 | U |

### Integration Tests (5)

| TC-ID | Spec Ref | Scenario | Priority | Type |
|-------|----------|----------|----------|------|
| TC-I18N-I001 | 4.1.8.2 | Content item with Turkish and English versions linked | P1 | I |
| TC-I18N-I002 | 4.1.8.1 | API returns content in requested culture via Accept-Language | P1 | I |
| TC-I18N-I003 | 4.1.8.2 | Localized search returns culture-specific results | P1 | I |
| TC-I18N-I004 | 4.1.8.3 | PO file changes reflect immediately (no restart) | P2 | I |
| TC-I18N-I005 | 4.1.8.1 | Culture detection from browser Accept-Language header | P2 | I |

**Subtotal: 13 tests (8U + 5I)**

## 10. Module 4.1.9 — Audit Logs

### Unit Tests (10)

| TC-ID | Spec Ref | Scenario | Priority | Type |
|-------|----------|----------|----------|------|
| TC-AUD-U001 | 4.1.9.1 | Audit event serialization with all fields | P1 | U |
| TC-AUD-U002 | 4.1.9.1 | Audit event includes user ID, timestamp, action, entity | P1 | U |
| TC-AUD-U003 | 4.1.9.2 | Version diff computation between two content versions | P1 | U |
| TC-AUD-U004 | 4.1.9.2 | Diff for added/removed/modified fields | P1 | U |
| TC-AUD-U005 | 4.1.9.3 | Retention policy filters events older than threshold | P2 | U |
| TC-AUD-U006 | 4.1.9.4 | Rollback restores content to specified version | P1 | U |
| TC-AUD-U007 | 4.1.9.1 | Audit query with filters (user, date range, action type) | P1 | U |
| TC-AUD-U008 | 4.1.9.1 | Audit event for failed actions (permission denied) | P1 | U |
| TC-AUD-U009 | 4.1.9.1 | Audit log pagination and sorting | P2 | U |
| TC-AUD-U010 | 4.1.9.1 | Audit event tamper detection (hash chain) | P0 | U |

### Integration Tests (6)

| TC-ID | Spec Ref | Scenario | Priority | Type |
|-------|----------|----------|----------|------|
| TC-AUD-I001 | 4.1.9.1 | Audit events persist to audit schema in PostgreSQL | P1 | I |
| TC-AUD-I002 | 4.1.9.2 | Version diff stored alongside audit event | P1 | I |
| TC-AUD-I003 | 4.1.9.4 | Rollback creates new version (not destructive) | P0 | I |
| TC-AUD-I004 | 4.1.9.1 | Audit log API returns filtered results | P1 | I |
| TC-AUD-I005 | 4.1.9.3 | Retention cleanup removes old events per policy | P2 | I |
| TC-AUD-I006 | 4.1.9.1 | Tenant isolation in audit logs | P0 | I |

**Subtotal: 16 tests (10U + 6I)**

## 11. Module 4.1.10 — Infrastructure

### Unit Tests (8)

| TC-ID | Spec Ref | Scenario | Priority | Type |
|-------|----------|----------|----------|------|
| TC-INF-U001 | 4.1.10.1 | Tenant creation with valid configuration | P1 | U |
| TC-INF-U002 | 4.1.10.1 | Tenant creation with duplicate name throws ConflictException | P1 | U |
| TC-INF-U003 | 4.1.10.2 | Redis cache key generation with tenant prefix | P1 | U |
| TC-INF-U004 | 4.1.10.3 | Recipe deserialization and step validation | P1 | U |
| TC-INF-U005 | 4.1.10.4 | OpenID Connect token validation | P0 | U |
| TC-INF-U006 | 4.1.10.5 | Sitemap XML generation for published content | P2 | U |
| TC-INF-U007 | 4.1.10.3 | Deployment plan export/import roundtrip | P1 | U |
| TC-INF-U008 | 4.1.10.1 | Tenant schema isolation configuration | P0 | U |

### Integration Tests (8)

| TC-ID | Spec Ref | Scenario | Priority | Type |
|-------|----------|----------|----------|------|
| TC-INF-I001 | 4.1.10.1 | Tenant creation provisions separate database schema | P0 | I |
| TC-INF-I002 | 4.1.10.2 | Redis cache operations (set, get, invalidate) | P1 | I |
| TC-INF-I003 | 4.1.10.3 | Recipe execution creates content types and items | P1 | I |
| TC-INF-I004 | 4.1.10.4 | OpenID Connect auth flow (authorize → token → userinfo) | P0 | I |
| TC-INF-I005 | 4.1.10.1 | Data from tenant-a cannot be accessed by tenant-b | P0 | I |
| TC-INF-I006 | 4.1.10.2 | Cache invalidation on content update | P1 | I |
| TC-INF-I007 | 4.1.10.5 | Sitemap endpoint returns valid XML with all published URLs | P2 | I |
| TC-INF-I008 | 4.1.10.3 | Deployment plan import creates expected state | P1 | I |

### Performance Tests (3)

| TC-ID | Spec Ref | Scenario | Priority | Type |
|-------|----------|----------|----------|------|
| TC-INF-P001 | 4.1.10.2 | Redis cache hit reduces response time by > 50% | P2 | P |
| TC-INF-P002 | 4.1.10.1 | Multi-tenant request routing < 5ms overhead | P2 | P |
| TC-INF-P003 | 4.1.10.1 | 100 concurrent users, p95 response < 500ms | P1 | P |

**Subtotal: 19 tests (8U + 8I + 3P)**

## 12. Module 4.1.11 — Integration (REST/GraphQL)

### Unit Tests (10)

| TC-ID | Spec Ref | Scenario | Priority | Type |
|-------|----------|----------|----------|------|
| TC-API-U001 | 4.1.11.1 | REST endpoint routing for content CRUD | P1 | U |
| TC-API-U002 | 4.1.11.1 | REST request validation (content type, required fields) | P1 | U |
| TC-API-U003 | 4.1.11.2 | GraphQL schema generation from content types | P1 | U |
| TC-API-U004 | 4.1.11.2 | GraphQL query resolves content with nested parts | P1 | U |
| TC-API-U005 | 4.1.11.3 | Headless CMS response format (JSON:API compatible) | P2 | U |
| TC-API-U006 | 4.1.11.4 | Auto-API from saved query generates correct endpoint | P1 | U |
| TC-API-U007 | 4.1.11.1 | REST error response format (RFC 7807 Problem Details) | P2 | U |
| TC-API-U008 | 4.1.11.2 | GraphQL mutation for content creation | P1 | U |
| TC-API-U009 | 4.1.11.1 | API versioning (v1 prefix) | P2 | U |
| TC-API-U010 | 4.1.11.1 | CORS configuration validation | P1 | U |

### Integration Tests (6)

| TC-ID | Spec Ref | Scenario | Priority | Type |
|-------|----------|----------|----------|------|
| TC-API-I001 | 4.1.11.1 | REST CRUD operations against live database | P1 | I |
| TC-API-I002 | 4.1.11.2 | GraphQL query execution against live database | P1 | I |
| TC-API-I003 | 4.1.11.1 | REST endpoint auth check (401/403) | P0 | I |
| TC-API-I004 | 4.1.11.2 | GraphQL query depth limiting (prevent DoS) | P0 | I |
| TC-API-I005 | 4.1.11.4 | Auto-generated API from query returns correct data | P1 | I |
| TC-API-I006 | 4.1.11.1 | API rate limiting enforcement | P1 | I |

### E2E Tests (4)

| TC-ID | Spec Ref | Scenario | Priority | Type |
|-------|----------|----------|----------|------|
| TC-API-E001 | 4.1.11.1 | External client authenticates → creates content → queries via GraphQL | P1 | E |
| TC-API-E002 | 4.1.11.3 | Headless CMS: frontend app fetches and renders content | P1 | E |
| TC-API-E003 | 4.1.11.4 | Admin creates query → publishes as API → client queries | P1 | E |
| TC-API-E004 | 4.1.11.1 | API documentation (OpenAPI/Swagger) is accessible and valid | P2 | E |

**Subtotal: 20 tests (10U + 6I + 4E)**

## 13. Cross-Cutting: Security Tests

| TC-ID | Spec Ref | Scenario | Priority | Type |
|-------|----------|----------|----------|------|
| TC-SEC-S001 | OWASP | SQL injection via REST API parameters | P0 | S |
| TC-SEC-S002 | OWASP | XSS via content body field | P0 | S |
| TC-SEC-S003 | OWASP | CSRF token validation on state-changing endpoints | P0 | S |
| TC-SEC-S004 | OWASP | Authentication bypass via direct URL access | P0 | S |
| TC-SEC-S005 | OWASP | Sensitive data exposure in API error responses | P0 | S |
| TC-SEC-S006 | OWASP | SSRF via URL fields (media upload, webhook) | P0 | S |
| TC-SEC-S007 | OWASP | File upload path traversal | P0 | S |
| TC-SEC-S008 | OWASP | HTTP security headers (CSP, HSTS, X-Frame-Options) | P1 | S |
| TC-SEC-S009 | OWASP | API rate limiting prevents abuse | P1 | S |
| TC-SEC-S010 | OWASP | Cookie security flags (HttpOnly, Secure, SameSite) | P1 | S |

**Subtotal: 10 security tests**

## 14. Cross-Cutting: Performance Tests

| TC-ID | Spec Ref | Scenario | Priority | Type |
|-------|----------|----------|----------|------|
| TC-PERF-P001 | NFR | Content API: 500 req/s, p95 < 200ms | P1 | P |
| TC-PERF-P002 | NFR | GraphQL query: 200 req/s, p95 < 500ms | P1 | P |
| TC-PERF-P003 | NFR | Search query: 300 req/s, p95 < 300ms | P1 | P |
| TC-PERF-P004 | NFR | Concurrent users: 200 users, no errors | P1 | P |
| TC-PERF-P005 | NFR | Database connection pool: 100 connections, no timeout | P1 | P |

**Subtotal: 5 performance tests**

## 15. Summary

| Module | Unit | Integration | E2E | Security | Performance | Total |
|--------|------|-------------|-----|----------|-------------|-------|
| 4.1.1 Admin Panel | 8 | 5 | 3 | — | — | 16 |
| 4.1.2 Content Modeling | 15 | 10 | 3 | — | — | 28 |
| 4.1.3 Content Management | 18 | 10 | 5 | — | — | 33 |
| 4.1.4 Theme Management | 6 | 4 | — | — | — | 10 |
| 4.1.5 Query Management | 12 | 8 | 3 | — | — | 23 |
| 4.1.6 User/Role/Permission | 12 | 8 | 3 | 5 | — | 28 |
| 4.1.7 Workflow Engine | 10 | 7 | 3 | — | — | 20 |
| 4.1.8 Multi-language | 8 | 5 | — | — | — | 13 |
| 4.1.9 Audit Logs | 10 | 6 | — | — | — | 16 |
| 4.1.10 Infrastructure | 8 | 8 | — | — | 3 | 19 |
| 4.1.11 Integration (API) | 10 | 6 | 4 | — | — | 20 |
| Cross-cutting Security | — | — | — | 10 | — | 10 |
| Cross-cutting Performance | — | — | — | — | 5 | 5 |
| **TOTAL** | **117** | **77** | **24** | **15** | **8** | **241** |

### Distribution

- Unit: 117 (48.5%)
- Integration: 77 (32.0%)
- E2E: 24 (10.0%)
- Security: 15 (6.2%)
- Performance: 8 (3.3%)

### Priority Distribution

- P0 (Blocker): ~38 tests (16%)
- P1 (Critical): ~152 tests (63%)
- P2 (Major): ~40 tests (17%)
- P3 (Minor): ~11 tests (5%)

## 16. Cross-References

- **Test Strategy**: [test-strategy.md](test-strategy.md) — pyramid percentages, coverage targets, naming convention
- **Golden Dataset**: [golden-dataset.md](golden-dataset.md) — fixture data referenced by integration and E2E tests
- **DoR Template**: [definition-of-ready.md](../ai-sdlc/definition-of-ready.md) — acceptance tests flow into this registry
- **Governance**: [governance.md](../ai-sdlc/governance.md) — Test Architect agent generates tests from this registry
