# Teryaq — ترياق

**Cloud pharmacy management SaaS for Egypt and  the Gulf.**

> *ترياق* — Arabic for *antidote / theriac*. The classic word for a universal remedy.

---

## The Problem

Egypt has **60,000+ pharmacies**. More than 90% run on Excel or decade-old desktop software. The real cost is visible every month:

- Expired drugs thrown away ($200–500/month per pharmacy on average)
- Stockouts on high-demand items
- Manual supplier ordering with no audit trail
- Zero patient dispensing history
- Incoming Ministry of Health compliance requirements for digital dispensing records

Existing desktop software (Dawaa and similar) is not cloud-based, has no mobile access, no automatic expiry alerts, and no supplier API integration. Generic inventory SaaS does not understand drug-specific needs: batch/expiry tracking, controlled substance logs, drug interaction checks, or FEFO dispensing.

---

## What Teryaq Does

Teryaq is a cloud, multi-tenant, bilingual (Arabic / English, RTL) SaaS that replaces Excel and legacy desktop software with a system purpose-built for pharmacies.

### Core value proposition
> "It pays for itself from the drugs you stop throwing away."

### Phase 1 features (MVP)
| Feature | What it solves |
|---------|---------------|
| **Drug Catalog** | Shared, curated database of all registered drugs — the moat. Pharmacies search and stock against it rather than typing from scratch. |
| **Batch & expiry inventory** | Track every stock batch with its expiry date. FEFO (first-expired-first-out) dispensing enforced automatically. |
| **Near-expiry & low-stock alerts** | Daily scan flags batches expiring within a configurable window and items below reorder level. |
| **POS dispensing** | Fast keyboard-first dispensing screen. Each sale decrements the correct batch and writes a full audit trail. |
| **Dashboard** | Today's sales, open alert count, near-expiry value at risk, low-stock items. |
| **Multi-branch** | Owner account spans the whole business; each branch has its own inventory. |

### Phase 2+ roadmap
- Supplier order management (POs, receiving against POs)
- Daily / monthly P&L reports
- Ministry of Health compliance reports (controlled substances, dispensing records)
- Patient history
- AI reorder prediction, drug interaction checks at dispensing, shrinkage detection
- Offline-first POS, mobile app, Gulf market expansion (KSA, KW, UAE)

---

## Market

| Market | Pharmacies | Price point |
|--------|-----------|-------------|
| Egypt | 60,000 | $50–200/month |
| Saudi Arabia | 15,000 | $150–600/month |
| Kuwait / UAE | ~5,000 | Premium |

**Realistic TAM:** $150M+/year regional.

**Monetization:**
- Solo pharmacy — $50/month
- 2 branches — $100/month
- Chain (up to 10 branches) — $200+/month
- Onboarding fee — $200–500 (drug database setup + white-glove onboarding)
- Annual plans (discounted)
- Long-term: transaction fees on digital supplier orders

---

## Technical Stack

### Backend (this repo)
| Concern | Choice |
|---------|--------|
| Runtime | .NET 10 |
| Architecture | Clean Architecture — Domain / Application / Infrastructure / API |
| Pattern | Service layer, `Result<T>` railway error handling, no MediatR |
| ORM | EF Core 10 (SQL Server in prod, SQLite for tests) |
| Identity | ASP.NET Core Identity (`ApplicationUser : IdentityUser<Guid>`) |
| Auth | JWT Bearer + refresh tokens |
| Validation | FluentValidation 12 |
| Mapping | AutoMapper 16 |
| DI scanning | Scrutor 5 |
| API docs | Swagger / Scalar |
| Logging | Serilog |
| Background jobs | Hangfire (Phase 1 alerts) |
| Tests | xUnit + NSubstitute + Shouldly |

### Frontend (separate repo — `Teryaq.Web`)
Angular 21, standalone components, Angular Signals, Angular Material (RTL theme), `@ngx-translate` for AR/EN.

### Database
SQL Server (LocalDB for local dev). Shared database per-tenant via `TenantId` column + global EF query filter. The global `Drug` catalog is **not** tenant-scoped — it is shared across all pharmacies and is Teryaq's data moat.

---

## Project Structure

```
Teryaq/
├── src/
│   ├── Teryaq.Domain/          # Entities, interfaces, exceptions, enums — zero deps
│   ├── Teryaq.Application/     # Service interfaces, DTOs, validators, AutoMapper profiles
│   ├── Teryaq.Infrastructure/  # EF Core, Identity, repositories, interceptors, AuthService
│   └── Teryaq.API/             # Controllers, Program.cs, appsettings.json
├── tests/
│   ├── Teryaq.UnitTests/       # xUnit + NSubstitute + Shouldly
│   └── Teryaq.IntegrationTests/# WebApplicationFactory over SQLite in-memory
├── docs/
│   └── superpowers/specs/      # Design documents
├── Directory.Packages.props    # Central NuGet version management
├── Directory.Build.props       # Shared build settings (TreatWarningsAsErrors, etc.)
└── Teryaq.slnx                 # Solution file
```

---

## Getting Started

### Prerequisites
- .NET 10 SDK
- SQL Server LocalDB (ships with Visual Studio) or any SQL Server instance

### 1. Clone and configure secrets

```bash
cd src/Teryaq.API
dotnet user-secrets set "Jwt:Secret" "your-secret-key-minimum-32-characters-long"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\\MSSQLLocalDB;Database=TeryaqDb;Trusted_Connection=True;"
```

### 2. Restore tools and packages

```bash
dotnet tool restore   # installs dotnet-ef
dotnet restore
```

### 3. Apply the database migration

```bash
dotnet ef database update \
  --project src/Teryaq.Infrastructure \
  --startup-project src/Teryaq.API
```

### 4. Run

```bash
dotnet run --project src/Teryaq.API
```

API docs available at: `https://localhost:{port}/scalar/v1`

### 5. Run tests

```bash
dotnet test
```

---

## API Overview

All endpoints are versioned under `/api/v1/`.

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/api/v1/auth/register` | POST | Public | Register a new pharmacy (tenant + owner + branch) |
| `/api/v1/auth/login` | POST | Public | Authenticate — returns JWT + refresh token |
| `/api/v1/auth/refresh` | POST | Public | Exchange refresh token for a new access token |
| `/api/v1/drugs` | GET | Staff | Search the shared drug catalog |
| `/api/v1/drugs` | POST | Owner | Add a manual drug entry to the catalog |
| `/api/v1/inventory` | GET / POST | Staff | Per-branch inventory list / receive new stock batch |
| `/api/v1/alerts` | GET | Staff | Open near-expiry and low-stock alerts |
| `/api/v1/sales` | GET / POST | Staff | Sales history / create a POS sale |
| `/api/v1/dashboard` | GET | Staff | Today's summary (sales, alerts, at-risk value) |

**Auth policies:**
- `OwnerOnly` — Owner role required
- `PharmacyStaff` — Owner or Pharmacist

---

## Architecture Decisions

**Shared database, per-tenant row filtering** — One SQL Server database. Every tenant-scoped entity carries a `TenantId` column. EF Core global query filters ensure tenants can never read each other's data. Chosen for simplicity and cost-effectiveness at 60k-tenant scale.

**Shared Drug catalog** — The `Drug` table is not tenant-scoped. All 60,000 pharmacies read from the same curated catalog. This is the product's data moat: a pharmacy signs up and immediately searches 20,000+ drugs by name, barcode, or ingredient — no manual entry needed. Teryaq curates once; all tenants benefit.

**Service layer, no CQRS** — Deliberately kept simple. MediatR adds indirection without benefit at this scale. Services return `Result<T>`; controllers delegate and return.

**No offline POS in Phase 1** — Cloud-only ships in 12–16 weeks. Offline sync (local store + conflict resolution) is a major engineering undertaking and is deferred to Phase 5.

---

## Go-to-Market

- Partner with pharmaceutical distributors (EgyDrug, Amin, others) — they visit every pharmacy weekly and can demo the product.
- Target pharmacy Facebook groups and WhatsApp networks.
- Free 90-day trial with white-glove onboarding.
- One reference pharmacy per district = social proof.
- Long-term: transaction fees on digital supplier orders create a revenue layer beyond subscriptions.
