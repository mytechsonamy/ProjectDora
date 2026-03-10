# Sprint S13 — UAT & Release Preparation

## Kapsam (Scope)
- Spec items: Cross-cutting (4.1.1–4.1.11 tum urun; S12 ciktilari dahil)
- Stories: US-1301, US-1302, US-1303, US-1304, US-1305, US-1306
- Cross-references: docs/release-management.md, docs/runbook.md, docs/migration-strategy.md, docs/resilience-and-chaos-tests.md

## Is Analizi Ozeti (Business Analysis Summary)

### Gereksinimler (Requirements from Teknik Sartname)

| Spec | Turkce Metin | English Summary |
|------|-------------|-----------------|
| Cross-cutting | Kullanici kabul testlerinin (UAT) gercek senaryo verileriyle, paydas temsilcileriyle birlikte yurutulmesi; resmi onay belgesinin hazirlanmasi | User acceptance testing with stakeholder representatives using real-world scenarios across all 11 modules; formal sign-off document produced |
| Cross-cutting | Staging ortaminda yuklenme testi yapilarak platformun performans temel cizgisinin (performance baseline) belirlenmesi ve dokumante edilmesi | Performance baseline on staging — load test results for key scenarios; P95/P99 response times and throughput documented for v1.0.0 |
| Cross-cutting | Uretim ortamina ozdes bir staging ortaminin kurulumu; veri migrasyonu kuru calistirilmasi (dry-run) ve deployment paketinin hazirlanmasi | Staging deployment — production-identical environment; schema migration dry-run; Docker image build and release package prepared |
| Cross-cutting | Surum notlari (release notes), CHANGELOG ve kullanici kilavuzunun nihai haline getirilmesi | Release notes and CHANGELOG finalization — v1.0.0 release notes covering all sprints (S0–S12), user manual review, API documentation freeze |
| Cross-cutting | Teknik dokumantasyon (ADR'ler, modul kilavuzlari, API sozlesmesi, veri gizliligi) incelenmesi ve eksiklerin giderilmesi | Documentation review — all ADRs (001-010), module guides, api-contract.yaml, data-governance.md, threat-model.md reviewed and finalized |
| Cross-cutting | Canli ortama gecis kontrol listesi (go-live checklist) hazirlama; canli ortam izleme (monitoring), uyari (alerting) ve geri alma prosedurlerinin (rollback plan) dogrulanmasi | Go-live checklist — monitoring (Grafana/Prometheus), alerting thresholds, rollback decision matrix, on-call runbook, KOSGEB stakeholder notification plan |

### Kisitlar ve Bagimliliklar (Constraints & Dependencies)

- **Dependency**: S12 tamamlanmis olmali — sifir Critical guvenlik bulgusunun olmasi go-live on kosuldur
- **Dependency**: Tum S0–S11 sprint ciktilari teslim edilmis ve DoD karsılanmis olmali
- **No new features**: Bu sprint icinde yeni fonksiyonel gelistirme yapilmaz; yalnizca stabilizasyon, duzeltme ve hazirlik
- **Bug fix scope**: Yalnizca P0/P1 bug'larin duzeltilmesi; P2/P3 bug'lar bir sonraki release icin backlog'a alinir
- **UAT**: KOSGEB temsilcilerinin katilimini gerektirir; takvim ve erisim onceden koordine edilmeli
- **Staging**: Uretim ortamiyla ayni Docker Compose profili, PostgreSQL, Redis, Elasticsearch, MinIO surumlerini kullanmali
- **Data migration**: Gercek KOSGEB verisi (varsa) ile dry-run yapilmali; geri alma proseduru dokumante edilmeli
- **Freeze**: UAT baslangicinda feature freeze — test sureci boyunca kod degisikliklerine izin verilmez (yalnizca bloker bug fix'ler)
- **Version**: Surum `1.0.0-rc.1` olarak etiketlenir; UAT onayinin ardindan `1.0.0` etiketi alinir
- **Documentation**: API dokumantasyonu `api-contract.yaml` bu sprintte dondurulur — artik degisiklik yapilmaz

### RBAC Gereksinimleri

Bu sprint yeni izinler tanimlamamaktadir. Mevcut izin seti, UAT senaryolari kapsaminda dogrulanir.

| UAT Katilimcisi | Rol | Test Kapsamı |
|----------------|-----|--------------|
| KOSGEB Yonetici | TenantAdmin | Kullanici yonetimi, icerik yonetimi, raporlama, sistem ayarlari |
| KOSGEB Icerik Editoru | Editor | Icerik olusturma, duzenleme, yayinlama, medya yukleme |
| KOSGEB Denetcisi | Denetci | Denetim kayitlari, icerik gecmisini goruntuleme |
| KOSGEB Analist | Analyst | Sorgulari calistirma, raporlari goruntuleme |
| Sistem Testi (otomasyon) | SuperAdmin | Tum modul uctan uca senaryolar |

### Story Decomposition

| Story | Spec Refs | Priority | Description |
|-------|-----------|----------|-------------|
| US-1301 | Tum modüller | P0 | Kullanici kabul testi (UAT) — 11 modulu kapsayan en az 30 UAT senaryosu; KOSGEB paydas temsilcileriyle yurutulur; bulgu kaydi; resmi onay belgesi |
| US-1302 | Cross-cutting | P0 | Performans temel cizgisi — staging ortaminda k6/NBomber ile yuklenme testi; anahtar senaryolar icin P95 yanitlama suresi, saniyedeki istek, hata orani olcumu; baseline dokumane et |
| US-1303 | Cross-cutting | P0 | Staging deployment & migration dry-run — uretim ozdes staging kurulumu; schema migrasyonlari dry-run; Docker image build; deployment paketi + surumu etiketleme (1.0.0-rc.1) |
| US-1304 | Cross-cutting | P1 | Surum notlari ve CHANGELOG — v1.0.0 surum notlari (S0–S12 tum ozellikler); CHANGELOG.md hazirlanmasi; kullanici kilavuzu son incelemesi; API dokumantasyonu dondurulmasi |
| US-1305 | Cross-cutting | P1 | Teknik dokumantasyon incelemesi — 10 ADR, modul kilavuzlari, api-contract.yaml, data-governance.md, threat-model.md, runbook.md son dogrulama; eksiklerin giderilmesi |
| US-1306 | Cross-cutting | P1 | Canli ortam gecis kontrol listesi — Grafana/Prometheus uyarilari tanimlanmis; rollback karar matrisi; on-call runbook; KOSGEB paydas bildirim plani; nihai guvenlik taramasi (S12 bulgularinin kapatildigini dogrulama) |

### Priority Rationale

- **P0 (Blocker)**: US-1301 (UAT onay belgesi olmadan go-live yapilmaz), US-1302 (performans temel cizgisi go-live kararinin temelini olusturur), US-1303 (deployment paketi olmadan release edilemez) — bu uc story alinmadan sprint Done kabul edilmez
- **P1 (Critical but not blocking)**: US-1304 (release notes), US-1305 (teknik dok), US-1306 (go-live checklist) — UAT surecinde paralel olarak tamamlanabilir; release oncesinde mutlaka bitmeli

## Teknik Kararlar (Technical Decisions)

### D-001: UAT Senaryo Cercevesi
- Her UAT senaryosu: ID (UAT-XXX), modul, on kosul, adimlar, beklenen sonuc, gercek sonuc, onay/red sutunlari icerir
- Minimum senaryo kapsamı: Her 11 modul icin en az 2-3 pozitif senaryo + 1 negatif senaryo (yetki reddi)
- Ortam: Staging URL'si uzerinden gercek tarayici testi; mobil uyumluluk dogrulama (KOSGEB gerekmiyorsa atlana bilir)
- Bulgular: P0/P1 buglar ayni sprint icinde; P2/P3 bir sonraki release icin backlog

### D-002: Performans Temel Cizgisi Kriterleri
- Arac: k6 (MIT lisansi) — JavaScript tabanli yuklenme test araci; Docker Compose'a entegre
- Senaryolar: Icerik listesi (GET), icerik olusturma (POST), arama sorgusu, GraphQL query, kullanici girisi
- Hedefler (kabul kriteri):

| Senaryo | P95 Yanitlama Suresi | Hata Orani | Eskezamanlı Kullanici |
|---------|---------------------|------------|----------------------|
| GET /api/v1/content/* | < 500 ms | < 1% | 100 |
| POST /api/v1/content/* | < 1000 ms | < 1% | 50 |
| GET /api/v1/search | < 800 ms | < 1% | 100 |
| GraphQL query | < 600 ms | < 1% | 50 |
| POST /connect/token (login) | < 300 ms | < 1% | 20 |

- Sonuclar `docs/performance/baseline-v1.0.0.md` dosyasina yazilir
- Hedef kacirilirsa sprint sonucu bloklenir ve performans iyilestirme yapilir

### D-003: Staging Ortami Gereksinimleri
- Docker Compose profili: `docker/docker-compose.prod.yml` — uretimle özdeş
- Minimum donanim: 4 vCPU, 8 GB RAM, 50 GB SSD — performans testleri icin yeterli
- Servisler: PostgreSQL 16, Redis 7, Elasticsearch 8, MinIO 2024, .NET 8 runtime
- Tenant: `staging.projectdora.local` — en az 2 test kiracisi hazir olmali
- Veri: Golden dataset (golden-dataset.md) ile doldurulmus; gercek KOSGEB verisi varsa anonimlestirilmis kopya
- Migration dry-run: `dotnet ef database update --dry-run` ile dogrulama; geri alma scripti hazir olmali

### D-004: Release Package Yapisi
- Docker image: `projectdora/web:1.0.0` — multi-stage Dockerfile, final imaj Alpine tabanlı
- Artifact'lar: Docker image tar, docker-compose.prod.yml, .env.example, migration scripts, kullanici kilavuzu (PDF)
- Imza: Docker image SHA256 digest dokumante edilir
- Etiketleme: `git tag -a v1.0.0-rc.1` (UAT oncesi) → `git tag -a v1.0.0` (UAT sonrasi)
- CHANGELOG.md: Keep a Changelog formatı; her sprint katkisi listelenir

### D-005: Monitoring ve Alerting Konfigurasyonu
- Grafana dashboard'lari: HTTP error rate, P95 yanitlama suresi, CPU/bellek, veritabani baglanti havuzu
- Prometheus uyarilari:

| Uyari | Esik | Oncelik |
|-------|------|---------|
| HTTP 5xx rate > %2 (5 dk) | Kritik | PagerDuty |
| P95 response > 2s (10 dk) | Uyari | Slack |
| DB connection pool > %90 | Uyari | Slack |
| Disk kullanimi > %80 | Bilgi | E-posta |
| Audit hash chain integrity fail | Kritik | PagerDuty |

- Health check endpoint: `GET /health/ready` ve `GET /health/live` — Docker liveness/readiness probe olarak kullanilir
- Rollback karar zamanlayicisi: Deployment sonrasi 1 saat izleme; kritik uyari tetiklenirse otomatik rollback onerisi

### D-006: Go-Live Gecis Akisi
Deployment gununde uygulanacak sira:

```
1. Veritabanı yedeği al (pg_dump)
2. Maintenance mode aç (503 sayfası)
3. Docker image çek: docker pull projectdora/web:1.0.0
4. Migration'ları uygula: dotnet ef database update
5. Container'ları başlat: docker compose up -d
6. Health check: GET /health/ready (200 bekle)
7. Smoke testleri çalıştır
8. Maintenance mode kaldır
9. 1 saat izle
10. Paydaşlara bildir
```

Geri alma tetikleyicisi: Herhangi bir P0 hatası, HTTP 5xx > %5, veya deployment sonrası 30 dakika icinde health check başarısız olursa adım 3'e dön (önceki image ile).

See `docs/sprint-analyses/S13-uat-release/decisions.md` for full decision details.

## Test Plani (Test Plan)

### New Test Cases

| Test ID | Category | Story | Description |
|---------|----------|-------|-------------|
| TC-1301-01 | UAT | US-1301 | Admin panel girisi ve menu yapisi dogrulama — TenantAdmin roluyle basarili giris; tum 11 modul menu ogesine erisim |
| TC-1301-02 | UAT | US-1301 | Icerik olusturma ve yayinlama — Editor roluyle yeni icerik olustur; taslak kaydet; yayinla; URL dogrula |
| TC-1301-03 | UAT | US-1301 | Medya yukleme ve galeri — resim dosyasi yukle; galeri goruntule; icerige ekle |
| TC-1301-04 | UAT | US-1301 | Kullanici ve rol yonetimi — yeni kullanici olustur; rol ata; giris dogrula |
| TC-1301-05 | UAT | US-1301 | Is akisi (workflow) baslat ve izle — icerik yayinlamayi tetikle; workflow calistir; sonucu goruntle |
| TC-1301-06 | UAT | US-1301 | Denetim kaydini goruntule — Denetci roluyle icerik gecmisini goruntle; diff goster; rollback dene (reddet) |
| TC-1301-07 | UAT | US-1301 | Cok dilli icerik — Turkce ve Ingilizce versiyon olustur; dil secicisini dene |
| TC-1301-08 | UAT | US-1301 | API uzerinden icerik al — REST API ile GET /api/v1/content/article cagır; JSON donusunu dogrula |
| TC-1301-09 | UAT | US-1301 | Arama islevi — Elasticsearch'e gercek icerikle sorgu; alakali sonuclar donmeli |
| TC-1301-10 | UAT | US-1301 | Tenant izolasyon gorsel dogrulama — Tenant A kullanicisi Tenant B icerigini gorememeli |
| TC-1302-01 | Performance | US-1302 | k6 yuklenme testi — 100 eskezamanli kullanici, GET /api/v1/content/*; P95 < 500 ms |
| TC-1302-02 | Performance | US-1302 | k6 yuklenme testi — 50 eskezamanli kullanici, POST /api/v1/content/*; P95 < 1000 ms |
| TC-1302-03 | Performance | US-1302 | k6 yuklenme testi — 100 eskezamanli kullanici, arama sorgusu; P95 < 800 ms |
| TC-1302-04 | Performance | US-1302 | k6 yuklenme testi — 50 eskezamanli kullanici, GraphQL query; P95 < 600 ms |
| TC-1302-05 | Performance | US-1302 | k6 stress testi — 200 kullanici aniden; sistem graceful degradation gostermeli (429/503, 500 degil) |
| TC-1302-06 | Performance | US-1302 | k6 soak testi — 50 kullanici, 30 dakika; bellek sizintisi yok (RSS artisi < %10) |
| TC-1303-01 | Integration | US-1303 | Staging ortami kurulumu — tum Docker servisleri saglikli; health check 200 donuyor |
| TC-1303-02 | Integration | US-1303 | Migration dry-run — hic hatasiz tamamlaniyor; migration scripti once uygulanmamis ortamda da calisiyor |
| TC-1303-03 | Integration | US-1303 | Docker image build — `projectdora/web:1.0.0-rc.1` basariyla olusturuluyor; imaj boyutu < 500 MB |
| TC-1303-04 | Integration | US-1303 | Rollback proseduru — onceki Docker image ile staging'e deployment; veri kaybi yok |
| TC-1303-05 | Smoke | US-1303 | Staging smoke test paketi — 10 kritik yol; hepsinde 200/201 donuyor |
| TC-1304-01 | Review | US-1304 | CHANGELOG.md — S0'dan S12'ye kadar tum story'ler listeli; bicim dogru |
| TC-1304-02 | Review | US-1304 | api-contract.yaml — tum implement edilen endpoint'ler dokumante edilmis; Swagger dogrulaniyor |
| TC-1305-01 | Review | US-1305 | ADR'ler — ADR-001'den ADR-010'a kadar tumu guncellenmis; kararlar koda yansimis |
| TC-1305-02 | Review | US-1305 | module-boundaries.md — tum modul sinirlarinin gercek implementasyonla uymasi |
| TC-1306-01 | Ops | US-1306 | Grafana dashboard — HTTP error rate ve P95 response time grafikleri gosteriliyor |
| TC-1306-02 | Ops | US-1306 | Prometheus uyarisi — HTTP 5xx orani %2'yi asarsa Slack bildirimi geliyor (test ortaminda dogrulama) |
| TC-1306-03 | Ops | US-1306 | Health check endpoint — GET /health/ready; GET /health/live; her ikisi de 200 donuyor |
| TC-1306-04 | Ops | US-1306 | S12 guvenlik bulgu takibi — tum Critical ve High bulgular kapatilmis olarak isaretlenmis |

### Smoke Test Paketi (Staging + Production)

| Smoke TC | Endpoint | Beklenen |
|----------|----------|---------|
| SMK-01 | GET /health/ready | 200 |
| SMK-02 | GET /api/version | 200, `"version":"1.0.0"` |
| SMK-03 | POST /connect/token | 200, access_token alanı mevcut |
| SMK-04 | GET /api/v1/content/article (auth) | 200, items dizisi |
| SMK-05 | POST /api/v1/content/article (auth) | 201, id alanı mevcut |
| SMK-06 | GET /api/v1/search?q=test | 200, results dizisi |
| SMK-07 | POST /graphql `{ articles { id title } }` | 200, data objesi |
| SMK-08 | GET /api/v1/audit (auth Denetci) | 200, events listesi |
| SMK-09 | GET /api/v1/users (auth TenantAdmin) | 200, users listesi |
| SMK-10 | GET /api/v1/workflows (auth TenantAdmin) | 200, definitions listesi |

### Coverage Target
- UAT senaryosu kapsami: En az 30 senaryo; 11 modulun hepsinden en az 1 senaryo
- Performans: Tum P95 hedefleri staging'de karsilanmali (bkz. D-002 tablosu)
- Smoke tests: 10/10 basarili olmadan production deployment yapilmaz
- Dokumantasyon inceleme: Hic "TODO" veya taslak ("Draft") isaretli bolum kalmamali

## Sprint Sonucu (Sprint Outcome)
- [ ] US-1301 complete — UAT tamamlandi, resmi onay belgesi KOSGEB temsilcisinin imzasiyla alinmis
- [ ] US-1302 complete — Performans baseline dokumante edildi, tum P95 hedefleri karsilandi
- [ ] US-1303 complete — Staging deployment basarili, migration dry-run temiz, `1.0.0-rc.1` etiketi alindi
- [ ] US-1304 complete — CHANGELOG.md ve release notes hazirlandi, api-contract.yaml donduruldu
- [ ] US-1305 complete — Tum teknik dokumantasyon gozden gecirildi ve onaylandi
- [ ] US-1306 complete — Go-live checklist tamamlandi, izleme konfigurasyonu dogrulandi, `1.0.0` etiketi alinmaya hazir

## Dokumantasyon Notlari (Documentation Notes)
> Information to include in end-of-project user manual and technical documentation

### Kullanici Kilavuzu (User Manual)
- Sisteme baslangic (getting started): Kullanici olusturma, ilk giris, admin paneli gezintisi
- Modul kilavuzlari: Admin Panel, Icerik Yonetimi, Arama, Is Akisi, Denetim Kayitlari, Entegrasyon API
- Sikca sorulan sorular (SSS): Sifre sifirla, oturumu uzat, icerik geri yukle, rol degistir
- Hata mesajlari rehberi: En sik gordulen 20 hata mesaji ve cozumleri
- Sistem gereksinimleri: Tarayici uyumlulugu, ekran cozunurlugu, gerekli izinler

### Teknik Dokumantasyon (Technical Documentation)
- Kurulum kilavuzu: Docker Compose ile baslangic, ortam degiskenleri, ilk admin hesabi olusturma
- Konfigurasyona referans: Tum `appsettings.json` ve Docker env degiskenleri; varsayilan degerler ve aciklamalar
- API referansi: api-contract.yaml Swagger UI; kimlik dogrulama, sayfalama, filtre parametreleri
- Migration kilavuzu: Schema migrasyonlari nasil uygulanir; geri alma proseduru
- Monitoring kilavuzu: Grafana dashboard'larini import etme; Prometheus uyarilarini konfigure etme
- Yedekleme ve geri yukleme: PostgreSQL `pg_dump` takvimi, MinIO yedekleme, geri yukleme adimlari
- Sorun giderme (troubleshooting): En sik 10 deployment sorunu ve cozumleri

### API Endpoints
Bu sprintte yeni endpoint tanimlamamaktadir. Asagidaki endpoint'ler dogrulama ve final inceleme kapsamindadir:

- `GET /health/ready` — Readiness probe; tum bagimliliklar hazirsa 200 doner
- `GET /health/live` — Liveness probe; proses canli ise 200 doner
- `GET /api/version` — Surumu doner: `{"version":"1.0.0","buildDate":"...","environment":"production"}`
- `GET /api/v1/security/headers-check` — Aktif guvenlik header'larını listele (SuperAdmin, S12'den devralinmis)

### Configuration Parameters
- `Application:Version` — Urun surum bilgisi (varsayilan: 1.0.0)
- `Application:Environment` — Ortam adi: development / staging / production
- `Monitoring:Grafana:Enabled` — Grafana metrik ihracati (varsayilan: true production'da)
- `Monitoring:HealthCheck:Enabled` — Health check endpoint'leri etkin (varsayilan: true)
- `Monitoring:HealthCheck:DetailedResponse` — Saglik kontrol detay goster (varsayilan: false production'da)
- `Release:MaintenanceModeEnabled` — Bakım modu — tum isteklere 503 doner (varsayilan: false)
- `Release:MaintenanceModeMessage` — Bakım modu mesaji (Turkce)

## Canli Ortam Gecis Hazirlik Kontrol Listesi (Go-Live Checklist)

### Pre-Deployment
- [ ] S12 — Sifir Critical guvenlik bulgusunun kalmadigi dogrulandi
- [ ] UAT resmi onay belgesi imzalandi (US-1301)
- [ ] Performans baseline hedefleri karsilandi (US-1302)
- [ ] Migration dry-run temiz (US-1303)
- [ ] CHANGELOG.md ve release notes hazir (US-1304)
- [ ] Tum dokumantasyon incelendi (US-1305)
- [ ] Grafana/Prometheus uyarilari yapilandirildi (US-1306)
- [ ] Veritabani yedeği alinmasi icin zamanlayici tanimli
- [ ] `git tag -a v1.0.0-rc.1` etiketi mevcuttur

### Deployment
- [ ] Veritabani yedeği al: `pg_dump -h ... projectdora > backup_v1.0.0.dump`
- [ ] Maintenance mode ac: `Release:MaintenanceModeEnabled=true`
- [ ] Schema migrasyonlari uygula: `dotnet ef database update`
- [ ] Docker Compose: `docker compose -f docker/docker-compose.prod.yml up -d`
- [ ] Health check: `curl http://localhost:5000/health/ready` → 200
- [ ] Smoke test paketi calistir (SMK-01 – SMK-10): 10/10 basarili
- [ ] Maintenance mode kaldir

### Post-Deployment (1 saat izleme)
- [ ] HTTP 5xx orani < %1 (Grafana)
- [ ] P95 yanitlama suresi < 500 ms (Grafana)
- [ ] Hic kritik Prometheus uyarisi yok
- [ ] `git tag -a v1.0.0` etiketi alinir
- [ ] KOSGEB paydaslarina bildirim gonderilir
- [ ] Surum notlari yayinlanir
