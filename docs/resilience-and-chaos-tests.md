# Resilience & Chaos Test Scenarios

> Version: 1.0 | Status: Draft | Last Updated: 2026-03-09

## 1. Purpose

This document defines failure scenarios, expected degraded behaviors, and chaos test plans for ProjectDora. The platform must handle infrastructure failures gracefully — no data loss, no security bypass, clear user feedback.

## 2. Resilience Principles

1. **Fail safe**: On failure, default to deny (auth), read-only (data), or disabled (features) — never fail open
2. **Degrade gracefully**: Disable affected feature, keep rest of platform running
3. **No data loss**: All writes must be durable before acknowledged
4. **Recover automatically**: Services should reconnect when dependencies come back
5. **Alert on failure**: Every degraded state emits an alert

## 3. Infrastructure Failure Scenarios

### 3.1 PostgreSQL Down

| Aspect | Detail |
|--------|--------|
| **Scenario** | PostgreSQL primary is unreachable (crash, network, maintenance) |
| **Detection** | Health check fails, connection pool timeout |
| **Expected Behavior** | |
| — Content reads | **UNAVAILABLE** — content stored in DB |
| — Content writes | **UNAVAILABLE** — returns 503 |
| — Auth (token validation) | **DEGRADED** — validate existing JWTs (public key cached), no new token issuance |
| — Search | **AVAILABLE** — Elasticsearch/Lucene serve cached index |
| — Audit logging | **QUEUED** — buffer audit events in memory/file, flush when DB recovers |
| **User Message** | "Sistem bakım nedeniyle geçici olarak kullanılamıyor. Lütfen birkaç dakika sonra tekrar deneyin." |
| **Recovery** | Auto-reconnect via connection pool; flush queued audit events; verify data integrity |
| **Max Acceptable Downtime** | 5 minutes for auto-recovery; alert after 1 minute |

### 3.2 Elasticsearch Down

| Aspect | Detail |
|--------|--------|
| **Scenario** | Elasticsearch cluster unreachable or unhealthy |
| **Detection** | Health check fails, search request timeout |
| **Expected Behavior** | |
| — Full-text search | **DEGRADED** — fallback to Lucene.NET (local index) |
| — Content CRUD | **AVAILABLE** — DB operations unaffected |
| — Search quality | **DEGRADED** — Lucene may have stale index, no fuzzy/advanced features |
| **User Message** | "Arama geçici olarak sınırlı modda çalışmaktadır." |
| **Recovery** | Auto-reconnect; trigger full reindex from DB to Elasticsearch |
| **Fallback Config** | `SearchProvider: Lucene` in `ISearchIndexService` |

### 3.3 Redis Down

| Aspect | Detail |
|--------|--------|
| **Scenario** | Redis instance unreachable |
| **Detection** | Connection timeout, health check |
| **Expected Behavior** | |
| — All reads | **AVAILABLE** — bypass cache, hit DB directly |
| — All writes | **AVAILABLE** — skip cache invalidation |
| — Performance | **DEGRADED** — higher DB load, slower responses |
| — Session storage | **DEGRADED** — if sessions in Redis, force re-login |
| — Rate limiting | **DEGRADED** — if Redis-backed, rate limiting disabled (fail-open for usability, log warning) |
| **User Message** | None (transparent to user, just slower) |
| **Recovery** | Auto-reconnect; cache warms up naturally on subsequent requests |
| **Implementation** | `ICacheService` returns `default(T)` on connection failure, caller falls through to DB |

### 3.4 MinIO (Object Storage) Down

| Aspect | Detail |
|--------|--------|
| **Scenario** | MinIO service unreachable |
| **Detection** | S3 API timeout, health check |
| **Expected Behavior** | |
| — Media display | **DEGRADED** — existing cached URLs may still work; new requests fail |
| — Media upload | **UNAVAILABLE** — returns 503 for upload endpoints |
| — Report download | **UNAVAILABLE** — returns 503 |
| — Document upload | **UNAVAILABLE** — returns 503 |
| — Content CRUD (text) | **AVAILABLE** — text content unaffected |
| **User Message** | "Dosya yükleme/indirme geçici olarak kullanılamıyor." |
| **Recovery** | Auto-reconnect; no data loss (uploads fail before acknowledge) |

### 3.5 Network Partition (Between Services)

| Aspect | Detail |
|--------|--------|
| **Scenario** | Network partition between app and one or more backend services |
| **Detection** | Circuit breaker trips after N consecutive failures |
| **Expected Behavior** | Circuit breaker pattern per external dependency |
| **Circuit Breaker Config** | |
| — Failure threshold | 5 consecutive failures |
| — Open duration | 30 seconds |
| — Half-open | Allow 1 request through to test |
| **Implementation** | Polly library for .NET |

### 3.6 Disk Full

| Aspect | Detail |
|--------|--------|
| **Scenario** | Server disk reaches capacity |
| **Detection** | Write failures, health check |
| **Expected Behavior** | |
| — Application logs | **DEGRADED** — Serilog drops to console only |
| — Database writes | **UNAVAILABLE** — PostgreSQL rejects writes |
| — Search index | **DEGRADED** — no new indexing |
| **Mitigation** | Log rotation (90 day retention), disk usage monitoring, alert at 80% |

### 3.7 High Load / DDoS

| Aspect | Detail |
|--------|--------|
| **Scenario** | Sudden traffic spike or DDoS attack |
| **Detection** | Request queue depth, response time degradation, rate limiter triggers |
| **Expected Behavior** | |
| — Rate limiting | Active (per-user, per-IP) |
| — API responses | Return 429 Too Many Requests for excess |
| — Static content | Served from cache/CDN |
| **Rate Limits** | |
| — Content API | 500 req/min per user |
| — Auth (login) | 10 req/min per IP |

### 3.8 Tenant Database Corruption

| Aspect | Detail |
|--------|--------|
| **Scenario** | Single tenant's data becomes corrupted |
| **Detection** | Integrity check failure, query errors |
| **Expected Behavior** | |
| — Affected tenant | **UNAVAILABLE** — returns 503 for that tenant |
| — Other tenants | **AVAILABLE** — tenant isolation prevents spread |
| — Recovery | Restore from backup for affected tenant only |

## 4. Chaos Test Plan

### 4.1 Test Categories

| Category | Method | Environment |
|----------|--------|-------------|
| Infrastructure kill | Docker container stop/kill | `docker-compose.test.yml` |
| Network fault | tc/iptables rules or Docker network disconnect | Test |
| Resource exhaustion | Memory/CPU limits via cgroups | Test |
| Clock skew | NTP manipulation | Test |
| Data corruption | Corrupt file/row | Test (isolated) |

### 4.2 Test Execution Matrix

| # | Test | Command | Expected | Verify |
|---|------|---------|----------|--------|
| 1 | Kill PostgreSQL | `docker stop projectdora-postgres` | API returns 503 for writes; cached reads work | HTTP 503 on POST, cached GET succeeds |
| 2 | Kill Elasticsearch | `docker stop projectdora-elasticsearch` | Search falls back to Lucene | Search still returns results (may differ) |
| 3 | Kill Redis | `docker stop projectdora-redis` | All endpoints work, slower | Response times increase, no errors |
| 4 | Kill MinIO | `docker stop projectdora-minio` | Upload/download fail; text content works | HTTP 503 on /media, content API works |
| 5 | Network partition (DB) | `docker network disconnect` | Circuit breaker trips, 503 | Polly circuit state = Open |
| 6 | Restart PostgreSQL | `docker restart projectdora-postgres` | Auto-reconnect, queued audits flush | Audit logs appear after reconnect |
| 7 | Restart all services | `docker compose restart` | Full recovery within 60s | All health checks green |
| 8 | Fill disk (Docker volume) | `dd if=/dev/zero of=/vol/fill bs=1M count=5000` | Write failures, app stays up | Errors logged, no crash |
| 9 | Simulate slow DB | Network delay injection (500ms) | Responses slow but no timeout | p95 < 2s (vs normal < 200ms) |
| 10 | Kill app mid-write | `docker kill projectdora-web` during POST | Data not partially committed | DB state consistent on restart |
| 11 | Concurrent tenant creation | 10 parallel tenant creates | No duplicate, no deadlock | All succeed or proper conflict error |

### 4.3 Automated Chaos Tests

```csharp
// Integration test using Testcontainers
[Fact]
[Trait("Category", "Chaos")]
public async Task ContentAPI_WhenPostgresDown_Returns503()
{
    // Arrange
    await _postgresContainer.StopAsync();

    // Act
    var response = await _client.PostAsync("/api/v1/content/Duyuru", content);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

    // Cleanup
    await _postgresContainer.StartAsync();
}

[Fact]
[Trait("Category", "Chaos")]
public async Task Search_WhenElasticsearchDown_FallsBackToLucene()
{
    // Arrange
    await _elasticContainer.StopAsync();

    // Act
    var response = await _client.GetAsync("/api/v1/content/Duyuru?search=destek");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var results = await response.Content.ReadFromJsonAsync<PagedResult>();
    results.Items.Should().NotBeEmpty(); // Lucene fallback works
}
```

## 5. Health Check Endpoints

| Endpoint | Checks | Response |
|----------|--------|----------|
| `/health` | App is running | 200 OK / 503 |
| `/health/ready` | All dependencies reachable | 200 OK / 503 with details |
| `/health/live` | App process is alive | 200 OK |

### Health Check Details (`/health/ready`)

```json
{
  "status": "Degraded",
  "checks": {
    "postgresql": { "status": "Healthy", "responseTime": "12ms" },
    "redis": { "status": "Unhealthy", "error": "Connection refused" },
    "elasticsearch": { "status": "Healthy", "responseTime": "45ms" },
    "minio": { "status": "Healthy", "responseTime": "8ms" }
  }
}
```

## 6. Monitoring & Alerting

| Metric | Warning Threshold | Critical Threshold | Alert Channel |
|--------|-------------------|-------------------|---------------|
| API response time (p95) | > 500ms | > 2000ms | Grafana → Slack |
| Error rate (5xx) | > 1% | > 5% | Grafana → PagerDuty |
| DB connection pool usage | > 70% | > 90% | Prometheus |
| Redis memory usage | > 70% | > 90% | Prometheus |
| Elasticsearch cluster health | Yellow | Red | Prometheus |
| Disk usage | > 80% | > 95% | Prometheus |
| Circuit breaker state | Half-Open | Open | App logs + Grafana |

## 7. Recovery Playbooks

### PostgreSQL Recovery

```
1. Check container: docker logs projectdora-postgres
2. If OOM: Increase memory limit in docker-compose
3. If corruption: Restore from latest backup
4. If disk full: Expand volume, run VACUUM
5. Verify: SELECT 1; on each schema
6. Flush queued audit events
```

### Elasticsearch Recovery

```
1. Check cluster health: GET /_cluster/health
2. If Red: Check node logs, restart unhealthy nodes
3. After recovery: Trigger full reindex
   POST /api/internal/search/reindex
4. Verify: Compare document counts with DB
```

## 8. Cross-References

- **Test Strategy**: [../.claude/testing/test-strategy.md](../.claude/testing/test-strategy.md) — chaos tests in performance layer
- **Test Cases**: [../.claude/testing/test-cases.md](../.claude/testing/test-cases.md) — infrastructure tests (TC-INF-*)
- **Runbook**: [runbook.md](runbook.md) — operational recovery procedures
- **Threat Model**: [threat-model.md](threat-model.md) — DoS scenarios
- **Module Boundaries**: [module-boundaries.md](module-boundaries.md) — which module handles which fallback
