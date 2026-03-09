# Sprint S01 — Admin Panel

## Kapsam (Scope)
- Spec items: 4.1.1.1, 4.1.1.2, 4.1.1.3
- Stories: US-101, US-102, US-103
- Cross-references: 21 spec items across 4.1.2–4.1.11 depend on admin panel

## Is Analizi Ozeti (Business Analysis Summary)

### Gereksinimler (Requirements from Teknik Sartname)

| Spec | Requirement | Summary |
|------|------------|---------|
| 4.1.1.1 | Tum yonetsel yetkinliklerine erisim icin sadece yetkili kullanicilar tarafindan erisilebilecek bir yonetim paneline sahip olmasi | Authenticated-only admin panel access |
| 4.1.1.2 | Yonetim paneli uzerinde olusturulan cesitli farkli baglanti ve bilgi varliklari icin sadece belirli roldeki kullanicilar tarafindan kendilerine verilen yetkilendirme uyarinca kullanilabilecek ozel menulerin olusturulmasina imkan vermesi | Role-based custom menu creation |
| 4.1.1.3 | Yonetim paneli uzerinde platforma yuklenen tum gorsel vb. dosyalarin yonetimi (yukleme, silme, klasor olusturma, yeniden adlandirma, vb.) icin dosya/medya yonetim modulunun bulunmasi | File/media management module |

### Kisitlar ve Bagimliliklar (Constraints & Dependencies)
- **Dependency**: S0 complete (project skeleton, Docker, CI)
- **Orchard Core**: Admin panel is built-in via `OrchardCore.Admin` module — we configure and extend, not build from scratch
- **Authentication**: OpenID Connect via `OrchardCore.OpenId` module — JWT bearer tokens
- **RBAC**: Orchard Core's built-in permission system — we define custom permissions per module
- **Multi-tenant**: Each tenant gets its own admin panel instance via `OrchardCore.Tenants`

### RBAC Gereksinimleri
- **SuperAdmin**: Full admin panel access, all menus, all media operations, tenant management
- **TenantAdmin**: Full access within own tenant scope
- **Editor/Author**: Access to assigned menu items only, media upload within own folders
- **Viewer/Anonymous**: No admin panel access (403)

### Story Decomposition

| Story | Spec | Priority | Description |
|-------|------|----------|-------------|
| US-101 | 4.1.1.1 | P0 | Admin panel authenticated access (security foundation) |
| US-102 | 4.1.1.2 | P1 | Role-based custom menu management |
| US-103 | 4.1.1.3 | P1 | Media/file management module |

## Teknik Kararlar (Technical Decisions)

### D-001: Leverage Orchard Core Admin Module
- Use `OrchardCore.Admin` built-in admin panel as the foundation
- Customize via `INavigationProvider` for menu structure
- Custom permissions via `IPermissionProvider`
- No custom admin panel UI framework needed

### D-002: Media Management via OrchardCore.Media
- Use `OrchardCore.Media` module with MinIO (S3-compatible) backend
- Configure `OrchardCore.Media.AmazonS3` provider for MinIO
- Folder-based organization, RBAC on upload/delete operations

### D-003: Admin Panel Module Structure
- New module: `ProjectDora.Modules.AdminPanel`
- Implements: `INavigationProvider`, `IPermissionProvider`
- Depends on: `ProjectDora.Core` (abstractions)

### Abstraction Layer Extensions
- `IAuthService` — Admin panel authentication check methods
- No new interfaces needed — Orchard Core handles admin panel routing

## Test Plani (Test Plan)

### New Test Cases
| Test ID | Category | Description |
|---------|----------|-------------|
| TC-101-01 | Unit | Admin panel requires authentication |
| TC-101-02 | Unit | Unauthenticated user redirected to login |
| TC-101-03 | Integration | JWT token validates for admin access |
| TC-101-04 | Security | Expired token rejected |
| TC-102-01 | Unit | Custom menu created for role |
| TC-102-02 | Unit | Menu items filtered by user permissions |
| TC-102-03 | Integration | Different roles see different menus |
| TC-103-01 | Unit | File upload succeeds with valid type |
| TC-103-02 | Unit | File upload rejected for invalid type |
| TC-103-03 | Unit | Folder CRUD operations |
| TC-103-04 | Integration | Media stored in MinIO via S3 API |
| TC-103-05 | Security | Unauthorized user cannot upload |

### Coverage Target
- Unit test coverage: >= 80% for AdminPanel module
- Integration test coverage: >= 60%
- Security tests: 3 minimum (auth, RBAC, media access)

## Sprint Sonucu (Sprint Outcome)
- [ ] US-101 complete
- [ ] US-102 complete
- [ ] US-103 complete

## Dokumantasyon Notlari (Documentation Notes)
> Information to include in end-of-project user manual and technical documentation

### Kullanici Kilavuzu (User Manual)
- Admin panel login flow (URL, credentials, 2FA if configured)
- Menu navigation and customization
- Media library: upload, organize in folders, rename, delete
- Supported file types and size limits

### Teknik Dokumantasyon (Technical Documentation)
- `OrchardCore.Admin` configuration in `Startup.cs`
- `INavigationProvider` implementation for custom menus
- MinIO configuration for media storage
- Permission registration via `IPermissionProvider`
- Admin panel URL routing: `/admin/*`

### API Endpoints
- `POST /api/v1/media` — Upload media file
- `GET /admin` — Admin panel entry point (browser)
- `POST /api/v1/auth/token` — Authentication token endpoint
