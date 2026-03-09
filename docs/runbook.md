# DevOps Runbook

> Version: 1.0 | Status: Draft | Last Updated: 2026-03-09

## 1. Quick Reference

| Task | Command |
|------|---------|
| Start all services | `docker compose up -d` |
| Stop all services | `docker compose down` |
| View logs | `docker compose logs -f --tail=100 [service]` |
| Restart single service | `docker compose restart [service]` |
| Health check | `curl http://localhost:5000/health/ready` |
| Database backup | See Section 3 |
| Rebuild search index | See Section 5 |
| Rotate logs | See Section 7 |

## 2. Service Map

| Service | Container Name | Port | Health Check |
|---------|---------------|------|-------------|
| Web App | `projectdora-web` | 5000 | `/health/ready` |
| PostgreSQL | `projectdora-postgres` | 5432 | `pg_isready` |
| Redis | `projectdora-redis` | 6379 | `redis-cli ping` |
| Elasticsearch | `projectdora-elasticsearch` | 9200 | `GET /_cluster/health` |
| MinIO | `projectdora-minio` | 9000 (API), 9001 (Console) | `mc admin info` |

## 3. Database Operations

### 3.1 Backup

```bash
# Full backup (all schemas)
docker exec projectdora-postgres pg_dump \
  -U projectdora -d projectdora \
  -F c -f /tmp/backup_$(date +%Y%m%d_%H%M%S).dump

# Copy backup to host
docker cp projectdora-postgres:/tmp/backup_*.dump ./backups/

# Schema-specific backup
docker exec projectdora-postgres pg_dump \
  -U projectdora -d projectdora \
  -n orchard -F c -f /tmp/orchard_backup.dump

docker exec projectdora-postgres pg_dump \
  -U projectdora -d projectdora \
  -n audit -F c -f /tmp/audit_backup.dump

docker exec projectdora-postgres pg_dump \
  -U projectdora -d projectdora \
```

### 3.2 Restore

```bash
# Stop the web app first
docker compose stop web

# Restore full backup
docker exec -i projectdora-postgres pg_restore \
  -U projectdora -d projectdora -c /tmp/backup_20260401_120000.dump

# Restart
docker compose start web

# Verify
curl http://localhost:5000/health/ready
```

### 3.3 Tenant-Specific Backup/Restore

```bash
# Export single tenant data (via app API)
curl -H "Authorization: Bearer $TOKEN" \
  "http://localhost:5000/api/internal/tenants/default/export" \
  -o tenant_default_export.json

# Import to new tenant
curl -X POST -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d @tenant_default_export.json \
  "http://localhost:5000/api/internal/tenants/new-tenant/import"
```

### 3.4 Database Maintenance

```bash
# Vacuum (reclaim space, update statistics)
docker exec projectdora-postgres psql -U projectdora -d projectdora \
  -c "VACUUM ANALYZE;"

# Check table sizes
docker exec projectdora-postgres psql -U projectdora -d projectdora \
  -c "SELECT schemaname, tablename, pg_size_pretty(pg_total_relation_size(schemaname || '.' || tablename)) AS size
      FROM pg_tables WHERE schemaname IN ('orchard','audit','analytics')
      ORDER BY pg_total_relation_size(schemaname || '.' || tablename) DESC;"

# Check connection pool
docker exec projectdora-postgres psql -U projectdora -d projectdora \
  -c "SELECT count(*) AS active, state FROM pg_stat_activity GROUP BY state;"
```

## 4. Redis Operations

### 4.1 Cache Management

```bash
# Check memory usage
docker exec projectdora-redis redis-cli INFO memory

# Flush all cache (use with caution!)
docker exec projectdora-redis redis-cli FLUSHALL

# Flush specific tenant cache
docker exec projectdora-redis redis-cli --scan --pattern "default:*" | \
  xargs docker exec -i projectdora-redis redis-cli DEL

# Check key count
docker exec projectdora-redis redis-cli DBSIZE

# Monitor live commands (debugging)
docker exec projectdora-redis redis-cli MONITOR
```

### 4.2 Redis Restart

```bash
docker compose restart redis
# Cache warms up naturally — no manual action needed
```

## 5. Search Index Operations

### 5.1 Elasticsearch Health

```bash
# Cluster health
curl -s http://localhost:9200/_cluster/health | jq .

# Index list with sizes
curl -s http://localhost:9200/_cat/indices?v

# Index document count
curl -s http://localhost:9200/_cat/count/content_duyuru?v
```

### 5.2 Rebuild Search Index

```bash
# Via API (recommended)
curl -X POST -H "Authorization: Bearer $TOKEN" \
  "http://localhost:5000/api/internal/search/reindex" \
  -H "Content-Type: application/json" \
  -d '{"scope": "full", "async": true}'

# Check reindex status
curl -H "Authorization: Bearer $TOKEN" \
  "http://localhost:5000/api/internal/search/reindex/status"
```

### 5.3 Elasticsearch Recovery

```bash
# If cluster is Red:
# 1. Check node status
curl -s http://localhost:9200/_cat/nodes?v

# 2. Check unassigned shards
curl -s http://localhost:9200/_cat/shards?h=index,shard,prirep,state,unassigned.reason | grep UNASSIGNED

# 3. Force reassign (single node)
curl -X PUT http://localhost:9200/_cluster/settings \
  -H "Content-Type: application/json" \
  -d '{"transient":{"cluster.routing.allocation.enable":"all"}}'

# 4. If still Red, delete and reindex
curl -X DELETE http://localhost:9200/content_*
# Then trigger full reindex via API
```

## 6. MinIO (Object Storage) Operations

### 6.1 Status Check

```bash
# Using mc (MinIO Client)
docker exec projectdora-minio mc admin info local

# List buckets
docker exec projectdora-minio mc ls local/

# Check bucket size
docker exec projectdora-minio mc du local/projectdora-media
```

### 6.2 Backup/Restore Files

```bash
# Backup entire bucket to host
docker exec projectdora-minio mc mirror local/projectdora-media /tmp/media-backup

# Restore
docker exec projectdora-minio mc mirror /tmp/media-backup local/projectdora-media
```

## 7. Log Management

### 7.1 View Logs

```bash
# All services
docker compose logs -f --tail=200

# Specific service
docker compose logs -f --tail=200 web

# Filter errors
docker compose logs web 2>&1 | grep -i "error\|exception\|fatal"

# Application structured logs (if file-based)
docker exec projectdora-web cat /app/logs/app.log | jq '.Level == "Error"'
```

### 7.2 Log Rotation

```bash
# Docker log rotation (configure in docker-compose.yml)
# Already configured via logging driver:
#   logging:
#     driver: "json-file"
#     options:
#       max-size: "50m"
#       max-file: "5"

# Application log rotation (Serilog)
# Configured in appsettings.json:
#   "Serilog": {
#     "WriteTo": [{
#       "Name": "File",
#       "Args": {
#         "path": "/app/logs/app-.log",
#         "rollingInterval": "Day",
#         "retainedFileCountLimit": 90
#       }
#     }]
#   }

# Manual cleanup of old logs
docker exec projectdora-web find /app/logs -name "*.log" -mtime +90 -delete
```

## 8. Application Operations

### 8.1 Deployment

```bash
# Build and deploy (dev)
docker compose -f docker-compose.dev.yml build
docker compose -f docker-compose.dev.yml up -d

# Production deployment
docker compose -f docker-compose.prod.yml pull
docker compose -f docker-compose.prod.yml up -d --remove-orphans

# Rolling restart (zero downtime, if multiple instances)
docker compose -f docker-compose.prod.yml up -d --no-deps web
```

### 8.2 Database Migrations

```bash
# Apply pending migrations
docker exec projectdora-web dotnet ef database update --context AuditDbContext

# Check pending migrations
docker exec projectdora-web dotnet ef migrations list --context AuditDbContext
```

### 8.3 Tenant Operations

```bash
# List tenants
curl -H "Authorization: Bearer $TOKEN" \
  "http://localhost:5000/api/internal/tenants"

# Create tenant
curl -X POST -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  "http://localhost:5000/api/internal/tenants" \
  -d '{"name": "new-tenant", "hostPrefix": "tenant1"}'

# Disable tenant
curl -X PUT -H "Authorization: Bearer $TOKEN" \
  "http://localhost:5000/api/internal/tenants/new-tenant/disable"
```

### 8.4 Recipe Execution

```bash
# Execute a recipe (seed data, configuration)
curl -X POST -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  "http://localhost:5000/api/internal/recipes/execute" \
  -d @path/to/recipe.json
```

## 9. Troubleshooting

### 10.1 Common Issues

| Symptom | Likely Cause | Fix |
|---------|-------------|-----|
| 503 on all endpoints | PostgreSQL down | `docker compose restart postgres` |
| Search returns empty | Elasticsearch index empty/stale | Trigger reindex (Section 5.2) |
| Slow responses (>2s) | Redis down or cache cold | Check Redis, warm cache |
| File upload fails | MinIO unreachable or disk full | Check MinIO logs and disk |
| Login fails | Token service down | Check OpenID Connect config |
| Audit logs missing | Audit queue overflow | Check audit service logs, flush queue |
| Tenant data leak | Missing tenant_id filter | **CRITICAL** — check code, add filter |

### 10.2 Emergency Procedures

```bash
# Emergency stop (all services)
docker compose down

# Emergency: reset to clean state (DESTROYS ALL DATA)
docker compose down -v
docker compose up -d
# Then re-run seed/recipe

# Emergency: disable a tenant
curl -X PUT -H "Authorization: Bearer $TOKEN" \
  "http://localhost:5000/api/internal/tenants/{tenantId}/disable"

```

## 10. Monitoring URLs

| Dashboard | URL | Purpose |
|-----------|-----|---------|
| Grafana | http://localhost:3000 | Metrics dashboards |
| Prometheus | http://localhost:9090 | Metric collection |
| Elasticsearch | http://localhost:9200/_cat/health | Search cluster health |
| MinIO Console | http://localhost:9001 | Object storage UI |
| App Health | http://localhost:5000/health/ready | Application health |

## 11. Cross-References

- **Resilience**: [resilience-and-chaos-tests.md](resilience-and-chaos-tests.md) — failure scenarios and expected behaviors
- **Migration**: [migration-strategy.md](migration-strategy.md) — database migration procedures
- **Data Governance**: [data-governance.md](data-governance.md) — backup retention, data handling
- **Module Boundaries**: [module-boundaries.md](module-boundaries.md) — service architecture
