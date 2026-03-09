skill seti iki seviyede düşünmek gerekir:

1️⃣ agent role skill’leri (BA, Architect, Dev, QA vs.)
2️⃣ domain/technical skill’leri (Orchard, CQRS, Elasticsearch, AI vb.)

Sen zaten governance dokümanında agent pipeline’ı tanımlamışsın:

BA → Test Architect → Developer → QA → Reviewer. 

governance

Ama agent’ların gerçekten üretken olması için skill kütüphanesi tanımlamak gerekiyor. Yoksa agent’lar context’i her seferinde yeniden keşfetmeye çalışır.

Ben bu projede 12 ana skill paketi tanımlardım.

1️⃣ Specification Analysis Skill

(agent: BA)

Amaç
Teknik şartname maddelerini AI-parse edilebilir gereksinimlere çevirmek.

Input

teknik şartname

architecture docs

domain model

Output

DoR YAML story

Bu skill şu işleri yapmalı:

spec → user story

acceptance criteria çıkarma

edge case üretme

RBAC mapping

API contract üretme

2️⃣ Architecture Design Skill

(agent: Architect)

Amaç
spec maddesini modüler mimariye çevirmek

Output

module structure

interface definitions

ADR draft

Örnek çıktı

Module: ContentModeling

Interfaces:
- IContentService
- IContentTypeService

Commands:
- CreateContentCommand
- UpdateContentCommand

Queries:
- GetContentQuery
3️⃣ CQRS / MediatR Skill

(agent: Developer)

Bu projede core pattern bu.

Agent şu patternleri bilmeli:

Command
CommandHandler
Query
QueryHandler
Validator
DTO

Örnek:

CreateContentCommand
CreateContentCommandHandler
CreateContentCommandValidator
4️⃣ Orchard Core Skill

(agent: Developer)

Bu kritik.

Agent şu kavramları bilmeli:

ContentTypes

ContentParts

ContentFields

YesSql

Recipes

Workflows

LocalizationPart

AutoroutePart

Örneğin içerik modeli şu şekilde:

ContentItem
 ├ TitlePart
 ├ BodyPart
 ├ CommonPart
 └ AuditTrailPart

Bu model golden dataset’te de kullanılıyor. 

golden-dataset

5️⃣ Query / Search Skill

(agent: Developer + Test)

Bu sistemin önemli parçası:

Lucene

Elasticsearch

SQL

Agent şu konuları bilmeli:

Turkish stemming

fuzzy search

relevance scoring

pagination

Golden dataset search testleri bunu doğruluyor. 

golden-dataset

6️⃣ RBAC / Security Skill

(agent: QA + Dev)

Çünkü sistemde:

role-based permissions

content-type permissions

API authorization

var.

Agent şu security testleri yazabilmeli:

privilege escalation

IDOR

JWT tampering

rate limit bypass

7️⃣ Workflow Engine Skill

(agent: Developer)

Platformda:

drag-drop workflows

event triggers

custom activities

var.

Agent şu aktiviteleri yazabilmeli:

ContentCreated
TimerEvent
HttpRequest
SendEmail
RunQuery
8️⃣ Test Generation Skill

(agent: Test Architect)

DoR YAML’den otomatik test üretimi.

Şu işleri yapmalı:

DoR YAML
↓
xUnit tests
↓
validator tests
↓
integration tests

Test naming pattern zaten tanımlı:

[Module]_[Feature]_[Scenario]_[ExpectedResult]

test-strategy

9️⃣ Integration / API Skill

(agent: Developer)

Platform:

REST

GraphQL

auto API

Agent şu işleri yapmalı:

OpenAPI generation

GraphQL resolver

auth middleware

tenant filtering

10️⃣ AI Skill

(agent: AI Developer)

AI modülleri:

RAG chatbot

NL2SQL

AHP engine

report generator

Özellikle NL2SQL için golden dataset tanımlı. 

golden-dataset

Agent şu işleri yapmalı:

embedding
vector search
prompt template
SQL generation
11️⃣ Observability Skill

(agent: DevOps)

Agent şu araçları bilmeli:

Serilog

Prometheus

OpenTelemetry

Grafana

12️⃣ DevOps Skill

(agent: DevOps)

Platform docker tabanlı.

Agent şu işleri yapmalı:

docker compose
testcontainers
CI pipeline
migration
Bu Projede Minimum Agent Skill Set

Ben şöyle bir skill matrix kurardım:

Skill	Agent
Specification analysis	BA
Architecture design	Architect
CQRS/MediatR	Developer
Orchard Core	Developer
Query/search	Developer
RBAC/security	QA
Workflow engine	Developer
Test generation	Test Architect
API integration	Developer
AI (RAG/NL2SQL)	AI Dev
Observability	DevOps
DevOps automation	DevOps
Gerçekçi Agent Seti

Ben şu 6 agent ile başlardım:

1️⃣ Architect
2️⃣ BA
3️⃣ Test Architect
4️⃣ Developer
5️⃣ QA
6️⃣ DevOps

AI modüller için ekstra:

7️⃣ AI Engineer

Ama En Kritik Skill

Bu projede en kritik skill şu:

👉 test generation

Çünkü pipeline şu:

DoR
↓
test
↓
code

Eğer test doğru değilse:

AI yanlış kod üretir.

Sana Bir Öneri

Bu proje için ayrıca şu dokümanı oluşturmanı öneririm:

.claude/skills/

örneğin:

spec-analysis.md
orchard-core.md
cqrs-mediatr.md
search-engine.md
rbac-security.md
workflow-engine.md
test-generation.md
ai-rag.md
devops-docker.md

Bu skill library AI agent’ların context window’unu stabilize eder.

💡 İstersen sana bir şey daha çıkarabilirim:

ProjectDora için “ideal AI agent + skill architecture”.

Bunu kurarsan AI ajanlarla geliştirme 3-4 kat hızlanır.

Sources
ok

Mustafa, o zaman sana ProjectDora için ideal AI-Agent + Skill Architecture modelini net ve uygulanabilir şekilde çizeyim. Amaç şu olacak:

AI agent’ların deterministic çalışması

SDLC’nin tamamen otomatik ilerleyebilmesi

test → code → review döngüsünün kendi kendini yönetmesi

Bu model özellikle senin kullandığın yaklaşıma (Claude CLI + agentic coding + .md dokümanları) çok uygun.

1️⃣ Agent Orchestrator

Sistemin merkezi.

Rolü:

task dağıtmak

context yönetmek

artefact üretimini takip etmek

Pipeline:

Spec
 ↓
BA Agent
 ↓
Architect Agent
 ↓
Test Architect
 ↓
Developer
 ↓
QA
 ↓
Reviewer

Orchestrator hiçbir zaman kod yazmaz.

Sadece yönetir.

2️⃣ BA Agent

Görev:

spec maddelerini AI-readable requirement haline getirmek

Input

Teknik Şartname
Architecture docs
Domain model

Output

DoR YAML
User story
Acceptance criteria
Edge cases

Örnek çıktı:

story_id: US-CONTENT-001

given:
  user authenticated

when:
  create content item

then:
  status = draft

acceptance_tests:
  - title required
  - body optional
  - max length 50KB
3️⃣ Architect Agent

Görev:

gereksinimi modül tasarımına çevirmek

Output

module
interfaces
commands
queries
events

Örnek:

Module: ContentManagement

Interfaces:
- IContentService

Commands:
- CreateContentCommand
- PublishContentCommand

Queries:
- GetContentQuery
4️⃣ Test Architect Agent

Bu proje için en kritik agent.

Görev:

DoR → test üretmek.

Pipeline:

DoR YAML
 ↓
Unit tests
 ↓
Integration tests
 ↓
Security tests
 ↓
AI validation tests

Örnek:

[Fact]
public void Content_Create_WithMissingTitle_ShouldFail()
5️⃣ Developer Agent

Görev:

testleri geçecek kod yazmak.

Workflow:

read tests
↓
generate code
↓
run tests
↓
fix

Pattern:

CQRS
MediatR
Clean architecture
6️⃣ QA Agent

Görev:

test kapsamını artırmak.

Yapar:

edge case üretir

fuzz test

security test

Örnek:

privilege escalation test
SQL injection test
tenant isolation test
7️⃣ Reviewer Agent

Görev:

PR review.

Kontroller:

architecture rules

dependency violations

performance risk

security risk

8️⃣ DevOps Agent

Görev:

pipeline ve deployment.

Yapar:

docker compose
CI pipeline
migration
release
9️⃣ AI Engineer Agent

Sadece AI modülleri için.

Görev:

RAG pipeline

NL2SQL

AHP engine

prompt optimization

10️⃣ Agent Skill Library

Agent’ların kullanacağı skill set.

Repository içinde şöyle olur:

.claude/
 ├ agents
 └ skills
Skill klasörü
skills/

İçerik:

spec-analysis.md
orchard-core.md
cqrs-mediatr.md
search-engine.md
workflow-engine.md
rbac-security.md
test-generation.md
ai-rag.md
devops-docker.md
11️⃣ Skill Example

Örnek skill:

skills/orchard-core.md

içerik:

Orchard Core concepts

ContentItem
ContentType
ContentPart
ContentField

Rules:

ContentItem must include CommonPart
TitlePart required for public content
AutoroutePart generates SEO URL
12️⃣ Test Generation Skill
skills/test-generation.md

içerik:

From DoR generate tests

1 unit test per acceptance criteria
1 integration test per module
security tests for RBAC
13️⃣ Query Skill
skills/search-engine.md

içerik:

Lucene queries
Elasticsearch queries
SQL queries
Turkish stemming
fuzzy search
14️⃣ AI Skill
skills/ai-rag.md

içerik:

embedding
vector search
prompt template
LLM response validation
15️⃣ Repo Yapısı

Ben şöyle kurardım:

ProjectDora/

docs/
architecture
requirements
test

.claude/
agents/
skills/

src/
modules/

tests/

docker/
16️⃣ Agent Workflow

Bir story pipeline:

spec item
↓
BA agent
↓
DoR YAML
↓
Test architect
↓
tests generated
↓
Developer
↓
code
↓
QA
↓
extra tests
↓
Reviewer
↓
merge
17️⃣ Neden Bu Model Güçlü

Avantajları:

✔ deterministic development
✔ test-driven AI coding
✔ architecture protection
✔ hallucination azaltma

18️⃣ Bu Projede En Kritik 3 Skill

Benim görüşüm:

1️⃣ test generation
2️⃣ orchard core
3️⃣ CQRS/MediatR

19️⃣ Sana Bir Tavsiye

Bu projede mutlaka şu agent’ı ekle:

Architecture Guardian

Bu agent şunu kontrol eder:

module boundaries
forbidden dependencies
architecture violations

Büyük projelerde hayat kurtarır.

💡 Eğer istersen sana bir sonraki adımda şunu da hazırlayabilirim:

ProjectDora için hazır .claude/agents ve .claude/skills klasörü (tam içerikleriyle).

Bu sayede Claude CLI ile projeyi başlattığında agent’lar direkt çalışmaya başlayabilir.

ok

Mustafa, o zaman sana ProjectDora için doğrudan kullanılabilir .claude/agents ve .claude/skills yapısını hazırlayayım. Bunu repo’ya koyduğunda Claude CLI veya başka bir agent framework ile hemen çalışabilir.

Amaç:

agent’ların rolünü netleştirmek

skill library ile context stabil hale getirmek

AI-driven SDLC pipeline kurmak

1️⃣ Repo İçindeki AI Yapısı

Repo içinde şu klasör yapısını öneririm:

.claude/
│
├── agents/
│   ├── orchestrator.md
│   ├── ba-agent.md
│   ├── architect-agent.md
│   ├── test-architect-agent.md
│   ├── developer-agent.md
│   ├── qa-agent.md
│   ├── reviewer-agent.md
│   └── devops-agent.md
│
├── skills/
│   ├── spec-analysis.md
│   ├── orchard-core.md
│   ├── cqrs-mediatr.md
│   ├── search-engine.md
│   ├── rbac-security.md
│   ├── workflow-engine.md
│   ├── test-generation.md
│   ├── ai-rag.md
│   └── devops-docker.md
│
└── rules/
    ├── architecture-rules.md
    └── coding-standards.md
2️⃣ Orchestrator Agent
.claude/agents/orchestrator.md
Role: AI Orchestrator

Purpose:
Coordinate all AI agents during SDLC.

Responsibilities:

- assign tasks to agents
- ensure artefact flow between agents
- enforce architecture rules
- ensure test-first development

Pipeline:

Spec → BA Agent → Architect Agent → Test Architect → Developer → QA → Reviewer

Rules:

- never generate code
- never modify architecture
- always require tests before implementation
3️⃣ BA Agent
.claude/agents/ba-agent.md
Role: Business Analyst Agent

Purpose:
Convert specification items into AI-readable requirements.

Inputs:

- Technical specification
- Architecture blueprint
- Domain model

Outputs:

- DoR YAML story
- acceptance criteria
- edge cases

Output Format:

story_id:
description:
given:
when:
then:
acceptance_tests:
edge_cases:
4️⃣ Architect Agent
.claude/agents/architect-agent.md
Role: System Architect Agent

Purpose:
Transform requirements into modular architecture.

Responsibilities:

- define module boundaries
- design interfaces
- generate ADR proposals
- enforce clean architecture

Output:

Module:
Interfaces:
Commands:
Queries:
Events:
5️⃣ Test Architect Agent
.claude/agents/test-architect-agent.md
Role: Test Architect

Purpose:
Generate deterministic tests from DoR requirements.

Responsibilities:

- convert DoR acceptance tests into xUnit tests
- create integration tests
- create security tests
- generate AI validation tests

Test Types:

- Unit
- Integration
- Security
- Performance
- AI validation
6️⃣ Developer Agent
.claude/agents/developer-agent.md
Role: Software Developer Agent

Purpose:
Implement code that satisfies all generated tests.

Rules:

- follow CQRS pattern
- use MediatR
- follow clean architecture
- never bypass tests
- do not modify tests

Workflow:

1 read tests
2 implement minimal code
3 run tests
4 refactor
7️⃣ QA Agent
.claude/agents/qa-agent.md
Role: QA Engineer Agent

Purpose:
Expand test coverage and detect hidden defects.

Responsibilities:

- generate edge case tests
- fuzz testing
- security testing
- performance test suggestions
8️⃣ Reviewer Agent
.claude/agents/reviewer-agent.md
Role: Code Reviewer Agent

Purpose:
Ensure architectural and coding standards compliance.

Checks:

- dependency violations
- architecture rules
- performance risks
- security vulnerabilities
9️⃣ DevOps Agent
.claude/agents/devops-agent.md
Role: DevOps Agent

Purpose:
Manage infrastructure and CI/CD.

Responsibilities:

- docker compose management
- CI pipeline configuration
- deployment scripts
- environment setup
🔟 Skill Library

Agent’ların context’i buradan gelir.

spec-analysis skill
.claude/skills/spec-analysis.md
Purpose:
Extract requirements from specification text.

Tasks:

- identify actors
- identify actions
- identify constraints
- produce acceptance criteria
orchard-core skill
.claude/skills/orchard-core.md
Key concepts:

ContentItem
ContentType
ContentPart
ContentField

Rules:

- use ContentDefinitionManager for type creation
- use AutoroutePart for SEO URLs
- use LocalizationPart for multilingual content
cqrs-mediatr skill
.claude/skills/cqrs-mediatr.md
Patterns:

Command
CommandHandler
Query
QueryHandler
Validator

Commands modify state.
Queries never modify state.
search-engine skill
.claude/skills/search-engine.md
Supported search engines:

Lucene
Elasticsearch

Important:

Turkish character normalization
fuzzy search
relevance scoring
rbac-security skill
.claude/skills/rbac-security.md
Security model:

Role
Permission
ContentTypePermission

Always enforce:

tenant isolation
authorization middleware
workflow-engine skill
.claude/skills/workflow-engine.md
Workflow concepts:

Trigger
Activity
Event
Action

Examples:

ContentCreated
SendEmail
RunQuery
Timer
test-generation skill
.claude/skills/test-generation.md
Generate tests from DoR:

1 unit test per acceptance rule
integration test per module
security tests for RBAC
ai-rag skill
.claude/skills/ai-rag.md
RAG pipeline:

document ingestion
embedding
vector search
LLM response
validation
devops-docker skill
.claude/skills/devops-docker.md
Container stack:

PostgreSQL
Redis
MinIO
Elasticsearch

Use docker-compose for local environments.
11️⃣ Architecture Rules
.claude/rules/architecture-rules.md
Rules:

Modules cannot access database directly.
Modules communicate through interfaces.

Allowed dependencies:

Modules → Core
Web → Modules
AI → Core
12️⃣ Coding Standards
.claude/rules/coding-standards.md
Code must follow:

Clean Architecture
CQRS
MediatR

Naming:

CommandHandler
QueryHandler
Validator
13️⃣ Bu Yapının Faydası

Bu yapı sayesinde:

agent context stabil olur

hallucination azalır

development deterministic olur

test-driven AI development mümkün olur

14️⃣ Sana Bir Kritik Tavsiye

ProjectDora gibi projelerde mutlaka şu agent’ı ekle:

Architecture Guardian

Bu agent sürekli şu kontrolü yapar:

dependency violation
layer violation
module boundary violation

Büyük projelerde çok kritik.