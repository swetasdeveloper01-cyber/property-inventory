# Property Inventory System

ISB Technologies technical test (ISB001) — Property Inventory REST API & Angular UI.

## Solution structure

```
PropertyInventory.sln
src/
  PropertyInventory.Api              # ASP.NET Core host / composition root
  PropertyInventory.Application      # Application services & DTOs
  PropertyInventory.Domain           # Entities
  PropertyInventory.Infrastructure   # EF Core, SQL Server, migrations, seed
tests/
  PropertyInventory.UnitTests
  PropertyInventory.IntegrationTests
client/                              # Angular 22 UI
```

**Dependency direction:** Api → Application + Infrastructure; Application → Domain; Infrastructure → Domain + Application.

## Prerequisites

- .NET SDK 10 (LTS)
- Node.js 22+ and npm 10+ (for the Angular client)
- SQL Server LocalDB (default), or another SQL Server instance (update `ConnectionStrings:DefaultConnection` in `src/PropertyInventory.Api/appsettings.json`)
- Optional: EF Core tools — `dotnet tool install --global dotnet-ef`

## Local setup

### Clone and restore

```bash
git clone <repository-url>
cd property-inventory
dotnet restore PropertyInventory.sln
```

Default connection string (LocalDB, Windows auth):

```
Server=(localdb)\mssqllocaldb;Database=PropertyInventory;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True
```

### Backend

```bash
dotnet build PropertyInventory.sln
dotnet test PropertyInventory.sln
dotnet run --project src/PropertyInventory.Api --launch-profile http
```

API (Development http profile): `http://localhost:5248`

In Development, startup:

- applies pending EF Core migrations
- seeds sample data when the database is empty
- enables CORS for `http://localhost:4200`

Manual migration alternative (optional; not required if you use Development startup):

```bash
dotnet ef database update --project src/PropertyInventory.Infrastructure --startup-project src/PropertyInventory.Api
```

### Frontend

```bash
cd client
npm install
npm start
```

Angular app: `http://localhost:4200`  
API base URL: `client/src/environments/environment.ts` → `http://localhost:5248`

## Assumptions

- Dashboard **Asking Price** is the property’s **current** asking price (`Property.Price` / `Currency`), not a historical asking price as-of the purchase date.
- Ownership periods use half-open intervals **`[EffectiveFrom, EffectiveTill)`**. `EffectiveTill = null` means the current owner. Contiguous periods meet at the same boundary date.
- USD conversion uses a **configured/deterministic** exchange-rate service (EUR→USD `1.08733`, matching the brief sample `100,000 → 108,733`). There is no live external FX API. Seeded `AcquisitionPriceUsd` values are stored as-is.
- **Property asking price** (`Property.Price` / price history) and **ownership acquisition/sold price** are separate concepts and never overwrite each other.
- **Currency** is stored explicitly on monetary amounts (asking price and acquisition price) because a numeric price alone is incomplete.

## Out of Scope

- Authentication / login
- RBAC / authorization
- Live external FX-rate integration
- Delete endpoints
- Batch create/update operations exposed through the Angular UI (batch APIs exist on the server)

## AI Tool Usage

- **Cursor** was the primary AI-assisted development tool used for backend implementation, EF Core/database work, Angular frontend, testing, debugging, and refactoring.
- **Claude** was used during initial planning for an independent requirements/architecture review and to challenge the proposed approach.
- The implementation was manually reviewed and validated against the requirements, automated tests, builds, and domain behaviour.

## Implemented API endpoints

### Properties

| Method | Route | Notes |
|--------|-------|--------|
| GET | `/api/properties` | Paging (`page`, `pageSize`) + filters (`name`, `address`, `minPrice`, `maxPrice`) |
| GET | `/api/properties/{id}` | 404 when missing |
| POST | `/api/properties` | Create single |
| POST | `/api/properties/batch` | Create many (all-or-nothing) |
| PUT | `/api/properties/{id}` | Update existing |
| PUT | `/api/properties/batch` | Update many (all-or-nothing) |

### Contacts

| Method | Route | Notes |
|--------|-------|--------|
| GET | `/api/contacts` | Paging + filters (`firstName`, `lastName`, `email`, `phone`) |
| GET | `/api/contacts/{id}` | 404 when missing |
| POST | `/api/contacts` | Create single |
| POST | `/api/contacts/batch` | Create many (all-or-nothing) |
| PUT | `/api/contacts/{id}` | Update existing |
| PUT | `/api/contacts/batch` | Update many (all-or-nothing) |

Defaults: `page=1`, `pageSize=10` (max 100). Contact emails are unique (case-insensitive check). Errors use ProblemDetails.

### Ownership

| Method | Route | Notes |
|--------|-------|--------|
| GET | `/api/properties/{propertyId}/ownerships` | Chronological ownership history (empty list if none); 404 if property missing |
| POST | `/api/properties/{propertyId}/ownerships` | Create historical period or transfer/current ownership |

A POST with `EffectiveTill = null` closes any existing current owner at the new `EffectiveFrom`, then creates the new current period atomically. Acquisition price/currency/USD are stored on the ownership record and do **not** change `Property.Price`.

### Asking price history

| Method | Route | Notes |
|--------|-------|--------|
| GET | `/api/properties/{propertyId}/prices` | Chronological asking-price history; empty list if none; 404 if property missing |
| POST | `/api/properties/{propertyId}/prices` | Record a new asking price (`amount`, `currency`, `effectiveDate`) and update current `Property.Price`/`Currency` atomically |

Property create records an initial history row at `DateOfRegistration`. `PUT /api/properties/{id}` records history only when Price/Currency actually change (EffectiveDate = UTC today). `POST .../prices` always records the supplied `EffectiveDate`.

### Sales dashboard

| Method | Route | Notes |
|--------|-------|--------|
| GET | `/api/dashboard/sales` | One row per ownership acquisition/sale event (including current owners) |

Field mapping: ownership Id; property name; **current** asking price/currency; owner name; `DateOfPurchase` = `EffectiveFrom`; sold-at price/currency/USD from acquisition fields. Ordered by `DateOfPurchase` descending, then property name, then Id.

## Angular UI

### Dashboard UI

- Route: `/dashboard`
- Consumes: `GET /api/dashboard/sales`
- Columns: ID (truncated GUID + tooltip), Property Name, Asking Price, Owner, Date of Purchase, Sold At Price, Sold At Price (USD)
- Loading / empty / error + Retry states

### Property UI

- `/properties` — paged list with filters (`name`, `address`, `minPrice`, `maxPrice`), Apply/Clear, Edit
- `/properties/new` — create form
- `/properties/:id` — edit form; includes Ownership History and Asking Price History sections
- Create: `POST /api/properties`; update: `PUT /api/properties/{id}` (backend may record price history when Price/Currency change — UI does not also call the price-history API)

### Ownership UI

- On `/properties/:id`
- Loads `GET /api/properties/{propertyId}/ownerships`
- Current owner = `EffectiveTill = null`
- Add / Transfer uses a single `POST /api/properties/{propertyId}/ownerships`
- Acquisition USD is display-only from the API response

### Asking Price History UI

- On `/properties/:id`
- Loads `GET /api/properties/{propertyId}/prices`
- **Record Price Change** uses only `POST /api/properties/{propertyId}/prices`
- Property form Save uses only Property PUT — the UI never calls both APIs for one user action
- After a successful price-history POST, the property form’s current price/currency is synced from the API response

### Contact UI

- `/contacts`, `/contacts/new`, `/contacts/:id`
- Filters: `firstName`, `lastName`, `email`, `phone`
- Duplicate email returns HTTP 409 with a field-level message
