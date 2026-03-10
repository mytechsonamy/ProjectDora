# Sprint S12 — Security Hardening

## Kapsam (Scope)
- Spec items: Cross-cutting (4.1.1–4.1.11 tum modullere uygulanir)
- Stories: US-1201, US-1202, US-1203, US-1204, US-1205, US-1206, US-1207
- Cross-references: threat-model.md (T-001 – T-011), data-governance.md (KVKK/GDPR), docs/resilience-and-chaos-tests.md, docs/api-contract.yaml

## Is Analizi Ozeti (Business Analysis Summary)

### Gereksinimler (Requirements from Teknik Sartname)

| Spec | Turkce Metin | English Summary |
|------|-------------|-----------------|
| Cross-cutting | Platform uzerindeki tum modullerin guvenlik aciklarinin OWASP Top 10 cercevesinde taranmasi ve giderilen bulgularin raporlanmasi | OWASP Top 10 review across all 11 implemented modules — identify, remediate, and report all findings |
| Cross-cutting | SQL injection saldirilarinin onlenmesi icin tum sorgu uretim noktalarinin parametrik sorgu kullanimi acisindan denetlenmesi | SQL injection prevention audit — verify every DB access point uses parameterized queries; no string interpolation in SQL |
| Cross-cutting | Kullanici kimlik dogrulama altyapisinin (OpenID Connect, JWT) guclendirilmesi; token omurlerinin, imza algoritmalarinin ve middleware kapsaminin dogrulanmasi | Authentication hardening — JWT/OIDC configuration audit, token lifetime enforcement, middleware coverage verification on all endpoints |
| Cross-cutting | RBAC yetkilerinin tum modullerde eksiksiz uygulandiginin dogrulanmasi; yetki atlama (privilege escalation) ve IDOR aciklarina karsi test yapilmasi | RBAC audit — verify permission checks across all 11 modules; test for privilege escalation and IDOR vulnerabilities |
| Cross-cutting | API katmaninda kota sinirlamasi (rate limiting) uygulanmasi; kotu niyetli isteklerin engellenmesi icin kural kumesinin tanimlanmasi | Rate limiting implementation — per-endpoint and per-user rate limits configured; DoS protection rules defined |
| Cross-cutting | Kisisel verilerin islenmesine iliskin KVKK ve GDPR uyumlulugunun gozden gecirilmesi; veri siniflandirmasi, sifreleme ve silme haklari mekanizmalarinin test edilmesi | KVKK/GDPR compliance review — validate data classification (L1–L5), PII encryption, retention schedules, and right-to-erasure flow |
| Cross-cutting | Sizma testi (penetration test) hazirliginin tamamlanmasi; test senaryolarinin dokumante edilmesi ve otomatik tarama araclarinin calistirilmasi | Penetration test preparation — document attack surface, run automated scanners (OWASP ZAP, Semgrep), finalize manual pen-test scope document |

### Kisitlar ve Bagimliliklar (Constraints & Dependencies)

- **Dependency**: S00–S11 tamamlanmis olmali — hardening calismasi, tum modullerin kullanima hazir olmasi durumunda anlamli hale gelir
- **Dependency**: S11 (Theme Management + cross-cutting polish) bitirilmis olmali — Liquid template sandbox guvenlik denetimi S12 kapsamina girmektedir
- **Scope**: Yeni fonksiyonel ozellik gelistirilmemektedir; sprint ciktisi duzeltmeler, konfigurasyonlar ve raporlardir
- **OWASP**: OWASP Top 10 2021 listesi baz alinacaktir — A01 (Broken Access Control), A02 (Crypto Failures), A03 (Injection), A04 (Insecure Design), A05 (Security Misconfiguration), A06 (Vulnerable Components), A07 (Auth Failures), A08 (Integrity Failures), A09 (Logging Failures), A10 (SSRF)
- **Tools**: OWASP ZAP (DAST), Semgrep (SAST), dotnet-retire / OWASP Dependency-Check (SCA), ClamAV (file upload scan)
- **KVKK**: Veri gizliligi denetimi, data-governance.md belgesiyle eslesmeli; silme hakki akisi (right to erasure) uctan uca test edilmelidir
- **Multi-tenant**: Her guvenlik test senaryosu, kiracilararasi veri sizintisini (T-005) da kapsamalidir
- **No Breaking Changes**: Guvenlik duzeltmeleri, API kontratini (api-contract.yaml) bozmamalidir; geriye donuk uyumluluk zorunludur

### RBAC Gereksinimleri

Bu sprint yeni izinler tanimlamamaktadir. Mevcut izin seti denetlenir ve eksik uygulamalar giderilir.

| Denetlenen Modul | Kontrol Edilen Izinler | Beklenen Sonuc |
|-----------------|----------------------|----------------|
| 4.1.1 Admin Panel | AdminPanel.Access, MediaLibrary.* | Anonim erisim yok; Viewer rol erisimi engellenmeli |
| 4.1.2 Content Modeling | ContentTypes.Manage, ContentTypes.View | Editor/Author tur tanimlamasini degistiremez |
| 4.1.3 Content Management | Content.Publish, Content.Edit, Content.Delete | Yayinlama yalnizca Editor+ |
| 4.1.4 Theme Management | ThemeManagement.Manage | Yalnizca TenantAdmin+ |
| 4.1.5 Query Management | Queries.Manage, Queries.Execute | SQL sorgu calistirma yalnizca yetkili roller |
| 4.1.6 User/Role/Permission | Users.Manage, Roles.Manage | Yalnizca TenantAdmin+ |
| 4.1.7 Workflow Engine | Workflows.Manage, Workflows.Execute | Editor workflow calistiramaz |
| 4.1.8 Multi-language | Localization.Manage | Yalnizca TenantAdmin+ |
| 4.1.9 Audit Logs | AuditTrail.View, AuditTrail.Rollback | Denetci rollback yapamaz |
| 4.1.10 Infrastructure | Tenants.Manage, OpenId.Manage | Yalnizca SuperAdmin |
| 4.1.11 Integration | Api.Access, GraphQL.Access | Tum API erisimlerinde auth zorunlu |

### Story Decomposition

| Story | Spec Refs | Priority | Description |
|-------|-----------|----------|-------------|
| US-1201 | T-001, T-002, A03 | P0 | OWASP Top 10 taramasi — tum 11 modul; SAST (Semgrep) + DAST (OWASP ZAP) calistirma; bulgulari S/M/L/C ile siniflandirma; P0/P1 bulgulari ayni sprint icinde giderme |
| US-1202 | T-001, A03 | P0 | SQL injection onleme denetimi — tum sorgu uretim noktalarini kod incelemesiyle dogrula; EF Core parametrik sorgu kullanimi, YesSql index sorgu guvenligi, QueryEngine'deki SQL parser kurallarini genislet |
| US-1203 | T-003, A07 | P0 | Kimlik dogrulama gucleştirme — JWT (RS256) yapilandirma dogrulama; token omru (access: 15 dk, refresh: 7 gun) uygulama; tum endpoint'lerde [Authorize] kapsami; OpenID middleware siparis denetimi |
| US-1204 | T-004, A01 | P0 | RBAC denetimi — 11 modulun tumunde yetki kontrolleri; MediatR pipeline behavior kapsami; IDOR testleri; privilige escalation denemeleri; icerigi olmayan handler'lara mimari test |
| US-1205 | T-006, A05 | P1 | Rate limiting uygulamasi — ASP.NET Core rate limiting middleware; login endpoint'i icin IP bazli sinir (10 istek/dk); API genel icin kullanici bazli sinir (300 istek/dk); GraphQL complexity limiti (maks 1000) dogrulama |
| US-1206 | KVKK, GDPR | P1 | KVKK/GDPR uyumluluk gozden gecirme — veri siniflandirma matrisi dogrulama (L1-L5); PII sifreleme (pgcrypto); silme hakki akisi uctan uca test; PO kimlik dogrulama denetimi; 72 saatlik ihlal bildirim suresinin runbook'ta dokumante edilmesi |
| US-1207 | Tum tehditler | P1 | Sizma testi hazirligini tamamla — saldiri yuzey haritasi; ZAP tarama raporu; Semgrep kural seti; kullanici kilavuzu olmayan guvenlik aciklarini kapatma; pen-test kapsam belgesi teslimi |

### Priority Rationale

- **P0 (Security)**: US-1201 (OWASP tarama), US-1202 (SQL injection), US-1203 (auth hardening), US-1204 (RBAC audit) — kritik guvenlik bulgulari, go-live oncesinde kapanmasi zorunlu; ihlal riski en yuksek alanlar
- **P1 (Compliance/Prep)**: US-1205 (rate limiting), US-1206 (KVKK/GDPR), US-1207 (pen-test hazirlik) — urun kalitesini ve yasal uyumu destekler; P0 tamamlandiktan sonra ele alinir

## Teknik Kararlar (Technical Decisions)

### D-001: SAST + DAST Kombinasyonu
- Static Analysis (SAST): Semgrep ile .NET/C# kurallar kumesi; CI pipeline'a entegre — her PR'da otomatik calisir
- Dynamic Analysis (DAST): OWASP ZAP Spider + Active Scan; staging ortamina karsı calistirilir; Docker Compose test profili ile baslatilir
- Software Composition Analysis (SCA): `dotnet-retire` ve OWASP Dependency-Check; bilinen CVE'lere karsi bagimlilik denetimi
- Bulgu triage: Critical/High — sprint icinde duzeltilmeli; Medium — S13 girisine kadar; Low — backlog

### D-002: ASP.NET Core Rate Limiting Middleware (Built-in)
- .NET 7+ yerlesik `Microsoft.AspNetCore.RateLimiting` paketi kullanilir — dis bagimlilik gerekmez
- Politikalar: `FixedWindowRateLimiter` (login icin), `TokenBucketRateLimiter` (API genel icin), `SlidingWindowRateLimiter` (hassas islemler icin)
- Kiraciya ozel limitler: `ITenantContext`'ten alinan tenant_id politika anahtarinin parcasi olur
- Asimlarda 429 Too Many Requests + `Retry-After` header doner

### D-003: JWT Konfigurasyonu Guclendirilmesi
- RS256 imza algoritmasi; minimum anahtar uzunlugu 2048 bit
- Access token omru: 15 dakika; refresh token: 7 gun, tek kullanimlik (rotate on use)
- Token iptal: Redis'te kara liste (`jti` uzerinden); logout endpoint'i token'i kara listeye ekler
- Tum API endpoint'leri varsayilan olarak `[Authorize]` — acik `[AllowAnonymous]` attribute ile istisna acilir
- HTTPS zorlama: `UseHsts()` + `UseHttpsRedirection()` production'da etkin

### D-004: MediatR Pipeline Behavior Kapsam Dogrulama
- `AuthorizationBehavior<TRequest, TResponse>` — tum `IRequest` implementasyonlarina uygulanmali
- Mimari test: ArchUnitNET kullanilarak her `IRequestHandler`'in auth behavior'dan gectigini dogrula
- Istisna: Anonim endpoint'ler `[AllowAnonymous]` yaninda `IAnonymousRequest` marker interface'i de uygulamalıdir
- IDOR koruması: Her handler, talep edilen kaynağın istekte bulunan kullanıcının kiracısına ait olduğunu doğrulamalıdır

### D-005: KVKK Uyumluluk Dogrulama Protokolu
- Veri siniflandirma matrisi (data-governance.md §3.2) ile tum EF Core entity'lerini karsilastir
- `[PersonalData]` attribute eksik olan L4 alanlarini tespit et ve ekle
- PII sifreleme: pgcrypto `encrypt(value, key, 'aes')` — ornek: `Users.Email` kolonu
- Silme hakki akisi: `DELETE /api/v1/users/{userId}/data` endpoint'i uctan uca test edilir — 7 adim (data-governance.md §4.3) tamamlanmali
- Audit log PII maskeleme: Serilog destructure denetimi — gercek e-posta hic log'lanmamali

### D-006: Liquid Template Sandbox Guvenlik Denetimi
- S11 ciktisi olan Liquid template motorunun `ILiquidTemplateManager` sandbox'i incelenir
- Tehdit: Server-Side Template Injection (SSTI) — `{{ '' | raw }}`, `include` kacisindan korunma
- Orchard Core Liquid parser izin verilen tag/filter whitelist'i dogrulanmali
- Kotu amacli sablonlarin calistirilmadigi konusunda birim testleri eklenir

See `docs/sprint-analyses/S12-security-hardening/decisions.md` for full decision details.

## Test Plani (Test Plan)

### New Test Cases

| Test ID | Category | Story | Description |
|---------|----------|-------|-------------|
| TC-1201-01 | Security | US-1201 | OWASP ZAP Active Scan staging ortamina karsi hic Critical bulgu dondurmesin |
| TC-1201-02 | Security | US-1201 | Semgrep SAST taramasi — SQL injection kurallarinda hic bulgu yok |
| TC-1201-03 | Security | US-1201 | Semgrep SAST taramasi — XSS kurallarinda hic bulgu yok |
| TC-1201-04 | Security | US-1201 | OWASP Dependency-Check — bilinen Critical CVE iceren bagimlilik yok |
| TC-1201-05 | Security | US-1201 | Secure headers dogrulama — CSP, X-Frame-Options, X-Content-Type-Options mevcut |
| TC-1202-01 | Security | US-1202 | QueryEngine SQL endpoint'ine kotu amacli girdi gonderildiginde 400 donerken veri sizdirmaz |
| TC-1202-02 | Security | US-1202 | EF Core LINQ sorgusu — hic bir yerde string concatenation ile SQL uretilmedigini dogrula |
| TC-1202-03 | Security | US-1202 | `; DROP TABLE` girisi iceren sorgu parametresi reddedilir |
| TC-1202-04 | Security | US-1202 | `' OR 1=1 --` girisi iceren kimlik dogrulama denemesi basarisiz olur |
| TC-1202-05 | Unit | US-1202 | SQL parser, SELECT disindaki DML/DDL ifadelerini engeller (UPDATE, DELETE, INSERT, DROP) |
| TC-1203-01 | Security | US-1203 | Suresi dolmus JWT ile yapilan istek 401 Unauthorized doner |
| TC-1203-02 | Security | US-1203 | Sahte imzali JWT (HS256 ile imzalanmis, RS256 beklenen) reddedilir |
| TC-1203-03 | Security | US-1203 | Oturum kapatildiktan sonra kara listedeki token ile yapilan istek 401 doner |
| TC-1203-04 | Security | US-1203 | Tum [Authorize] olmayan endpoint'ler kasitli `[AllowAnonymous]` attribute iceriyor |
| TC-1203-05 | Security | US-1203 | Refresh token ikinci kez kullanilamaz (tek kullanimlik rotasyon) |
| TC-1204-01 | Security | US-1204 | Viewer rol ile admin panel route'larina erisim 403 Forbidden doner |
| TC-1204-02 | Security | US-1204 | Author, baska bir kiracinin icerigini ID degistirerek okuyamaz (IDOR) |
| TC-1204-03 | Security | US-1204 | Editor, Users.Manage gerektiren endpoint'e erisemez |
| TC-1204-04 | Security | US-1204 | Anonim kullanici, kimlik dogrulama gerektiren hicbir endpoint'e erisemez |
| TC-1204-05 | Security | US-1204 | TenantAdmin, baska bir kiracinin ayarlarini degistiremez |
| TC-1204-06 | Architecture | US-1204 | ArchUnitNET: Her IRequestHandler, AuthorizationBehavior'dan gecer |
| TC-1205-01 | Integration | US-1205 | Login endpoint'ine 11. istek dakika icinde 429 doner |
| TC-1205-02 | Integration | US-1205 | API genel endpoint 301. istek (kullanici bazli) 429 doner |
| TC-1205-03 | Unit | US-1205 | Rate limit asildiginda response 429 + Retry-After header icerir |
| TC-1205-04 | Integration | US-1205 | GraphQL complexity 1001 olan sorgu reddedilir |
| TC-1206-01 | Integration | US-1206 | Kullanici silme hakki talep ettiginde 7 adim sirali olarak tamamlanir |
| TC-1206-02 | Security | US-1206 | E-posta adresi hicbir log satirinda acikcasi gorunmez |
| TC-1206-03 | Unit | US-1206 | L4 siniflanan veri (e-posta) DB'de sifrelenmis saklanir |
| TC-1206-04 | Security | US-1206 | Audit log PII maskeleme — kullanici adi log'da m***@domain formatiyla gorunur |
| TC-1207-01 | Security | US-1207 | Liquid template SSTI denemesi — `{{ '' | inject }}` gibi tanimlanmamis filtreler calistirilmaz |
| TC-1207-02 | Security | US-1207 | Dosya yukleme — .exe uzantili dosya reddedilir (magic byte dogrulama) |
| TC-1207-03 | Security | US-1207 | SSRF denemesi — localhost URL'si media kaynagi olarak reddedilir |
| TC-1207-04 | Security | US-1207 | CSRF denemesi — form submit'i anti-forgery token olmadan reddedilir |

### Coverage Target
- Guvenlik test kapsami: Threat Catalog'daki T-001 – T-011 tehditlerinin tamami icin en az 1 test case
- Mevcut 197 birim testine ek olarak en az 30 yeni guvenlik/entegrasyon testi
- RBAC: 11 moduldeki her yetkili endpoint'in en az bir reddetme (403) testi olmali
- OWASP ZAP: sifir Critical, maksimum 3 High (sprint icinde kapatilmak uzere)

## Sprint Sonucu (Sprint Outcome)
- [ ] US-1201 complete — OWASP tarama raporu hazir, Critical bulgular giderilmis
- [ ] US-1202 complete — SQL injection denetimi tamamlandi, kod inceleme onaylandi
- [ ] US-1203 complete — JWT/OIDC hardening uygulamasi dogrulandi
- [ ] US-1204 complete — 11 moduldeki RBAC eksiklikleri giderildi, ArchUnitNET testi yesil
- [ ] US-1205 complete — Rate limiting aktif, 429 senaryolari gosterildi
- [ ] US-1206 complete — KVKK/GDPR uyumluluk dokumante edildi, silme akisi test edildi
- [ ] US-1207 complete — Pen-test kapsam belgesi teslim edildi, ZAP/Semgrep raporlari arsivlendi

## Dokumantasyon Notlari (Documentation Notes)
> Information to include in end-of-project user manual and technical documentation

### Kullanici Kilavuzu (User Manual)
- Sifre politikasi ve hesap kilitleme: Kac basarisiz giris denemesinden sonra hesap kilitlenir ve nasil acilir
- Oturum yonetimi: Access token omru, oturumu uzatma (refresh), aktif oturumu sonlandirma
- KVKK haklari: Kullanicilar kendi verilerinin silinmesini nasil talep eder; onay suresinin 30 gun oldugu
- Veri gizliligi: Hangi bilgiler saklanir, ne kadar sure tutulur, kim erisebilir
- Guvenli kullanim ipuclari: Guclu sifre secimi, paylasimli bilgisayarlarda oturumu kapatma

### Teknik Dokumantasyon (Technical Documentation)
- Rate limiting konfigurasyonu: Politika tanimlari, sinir degerleri ve ayarlama rehberi
- JWT konfigurasyonu: RS256 anahtar rotasyonu proseduru, token omur ayarlari
- OWASP ZAP entegrasyonu: CI pipeline icinde otomatik tarama adımlari
- Semgrep kural seti: Kullanilan .NET kurallar kumesi ve ozellestirme talimatlari
- KVKK uyumluluk matrisi: Hangi entity/alan hangi siniftaydi, sifreli mi, masked mi
- Silme hakki uygulamasi: `DELETE /api/v1/users/{userId}/data` endpoint dokumantasyonu
- Liquid template sandbox: Izin verilen tag ve filter listesi; SSTI riskinin nasil azaltildigi
- MediatR AuthorizationBehavior: Pipeline kapsam kurallari ve `IAnonymousRequest` istisna mekanizmasi

### API Endpoints
- `GET /api/v1/security/scan-report` — Son OWASP ZAP tarama raporu ozeti (SuperAdmin only)
- `POST /api/v1/users/{userId}/data` — Kullanici verilerini disa aktar (KVKK veri tasimasiyeti)
- `DELETE /api/v1/users/{userId}/data` — Kullanicinin kisisel verilerini sil/anonimize et (silme hakki)
- `GET /api/v1/security/headers-check` — Aktif guvenlik HTTP header'larini listele (SuperAdmin only)

### Configuration Parameters
- `Security:Jwt:Algorithm` — JWT imza algoritmasi (varsayilan: RS256)
- `Security:Jwt:AccessTokenLifetimeMinutes` — Access token omru (varsayilan: 15)
- `Security:Jwt:RefreshTokenLifetimeDays` — Refresh token omru (varsayilan: 7)
- `Security:Jwt:EnableTokenBlacklist` — Redis kara liste etkin/devre disi (varsayilan: true)
- `Security:RateLimit:LoginWindowSeconds` — Login rate limit penceresi (varsayilan: 60)
- `Security:RateLimit:LoginMaxRequests` — Login penceresi basina maksimum istek (varsayilan: 10)
- `Security:RateLimit:ApiTokenBucketCapacity` — Genel API token bucket kapasitesi (varsayilan: 300)
- `Security:RateLimit:GraphQlMaxComplexity` — GraphQL maks sorgu karmasikligi (varsayilan: 1000)
- `Security:Liquid:EnableSandbox` — Liquid template sandbox'i etkin (varsayilan: true; kapat'ma!)
- `Security:Https:HstsMaxAgeDays` — HSTS max-age suresi (varsayilan: 365)
- `Kvkk:ErasureConfirmationWithinDays` — Silme hakki onay suresi (varsayilan: 30)
