# Property Inventory System

ISB Technologies technical test (ISB001) — Property Inventory REST API & UI.

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
client/                              # Angular 22 UI (foundation + API layer)
postman/                             # API collection (later phase)
```

**Dependency direction:** Api → Application + Infrastructure; Application → Domain; Infrastructure → Domain + Application.

## Prerequisites

- .NET SDK 10 (LTS)
- Node.js 22+ and npm 10+ (for the Angular client)
- SQL Server LocalDB (or another SQL Server instance; update `ConnectionStrings:DefaultConnection`)
- EF Core tools (optional): `dotnet tool install --global dotnet-ef`

## Local setup

### Backend

```bash
dotnet restore PropertyInventory.sln
dotnet build PropertyInventory.sln
dotnet test PropertyInventory.sln
dotnet run --project src/PropertyInventory.Api --launch-profile http
```

API (Development http profile): `http://localhost:5248`

In Development, the host applies pending migrations, seeds sample data when empty, and enables CORS for `http://localhost:4200`.

### Frontend

```bash
cd client
npm install
npm start
```

Angular app: `http://localhost:4200`  
API base URL (configured in `client/src/environments/environment.ts`): `http://localhost:5248`

Frontend structure:

```
client/src/app/
  core/           # API config, models, HTTP services, ProblemDetails interceptor
  features/       # dashboard + properties + contacts UI
  shared/         # shared utilities and placeholder component
```

Typed API services mirror the backend controllers.

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

Ownership is temporal (`EffectiveFrom` / nullable `EffectiveTill`). Current owner has `EffectiveTill = null`. Periods use half-open bounds: meeting at the same boundary date is contiguous, not overlapping. A POST with `EffectiveTill = null` closes any existing current owner at the new `EffectiveFrom`, then creates the new current period atomically. Acquisition price/currency/USD are stored on the ownership record and do **not** change `Property.Price`.

**USD conversion:** deterministic configured rates (no live FX API). EUR→USD uses `1.08733` (aligned with the brief sample `100,000 → 108,733`). Seeded historical `AcquisitionPriceUsd` values are stored as-is and not recalculated.

### Asking price history

| Method | Route | Notes |
|--------|-------|--------|
| GET | `/api/properties/{propertyId}/prices` | Chronological asking-price history; empty list if none; 404 if property missing |
| POST | `/api/properties/{propertyId}/prices` | Record a new asking price (`amount`, `currency`, `effectiveDate`) and update current `Property.Price`/`Currency` atomically |

Asking-price history (`PropertyPriceHistory`) is separate from ownership acquisition/sold price. Changing asking price never updates ownership acquisition fields; ownership transfer never updates asking price.

Price changes go through one application path (`PropertyPriceService`): property create records an initial history row at `DateOfRegistration`; `PUT /api/properties/{id}` records history only when Price/Currency actually change (EffectiveDate = UTC today); `POST .../prices` always records the supplied `EffectiveDate`. Same-day or out-of-order EffectiveDate values are allowed; results are ordered by EffectiveDate then Id.

### Sales dashboard

| Method | Route | Notes |
|--------|-------|--------|
| GET | `/api/dashboard/sales` | One row per ownership acquisition/sale event (including current owners) |

Field mapping:

- `Id` → ownership Id
- `PropertyName` → property name
- `AskingPrice` / `AskingCurrency` → **current** property asking price
- `Owner` → contact first + last name
- `DateOfPurchase` → ownership `EffectiveFrom`
- `SoldAtPrice` / `SoldAtCurrency` / `SoldAtPriceUsd` → ownership acquisition fields (stored USD; not recalculated)

Current ownership (`EffectiveTill = null`) is included because each acquisition is a sale to that owner and the client sample lists current owners (e.g. Carmen Attard, Joe Borg). Ordered by `DateOfPurchase` descending, then property name, then Id.

### Dashboard UI

- Route: `/dashboard`
- Consumes: `GET /api/dashboard/sales` via `DashboardApiService`
- Table columns: ID (truncated GUID + tooltip), Property Name, Asking Price, Owner, Date of Purchase (`d MMM yyyy`), Sold At Price (original currency), Sold At Price (USD)
- Loading / empty / error + Retry states are handled in the page component

### Property UI

- `/properties` — paged list with filters (`name`, `address`, `minPrice`, `maxPrice`), Apply/Clear, Edit link
- `/properties/new` — create form (name, address, price, currency, date of registration)
- `/properties/:id` — edit form; loads by id; 404 if missing
- Filters and pagination call `GET /api/properties` (no client-side full-dataset filtering)
- Create uses `POST /api/properties`; update uses `PUT /api/properties/{id}` (backend records asking-price history when price/currency change — UI does not call the price-history API)
- Ownership history / transfer live on `/properties/:id` via `OwnershipApiService` (see Ownership UI)
- Price-history screen is not implemented yet (placeholder on the edit page)

### Ownership UI

- Shown on `/properties/:id` (property edit/detail), not as a top-level nav area
- Loads `GET /api/properties/{propertyId}/ownerships`
- Current owner = record with `EffectiveTill = null` (also exposed as `isCurrent`)
- Add / Transfer uses a single `POST /api/properties/{propertyId}/ownerships`; the backend closes the previous current owner when a new open-ended period is created
- Owner contact is chosen from contacts loaded via `ContactApiService` (pages of 100 until complete)
- Acquisition USD is displayed from the API response; the UI never submits or calculates USD

### Asking Price History UI

- Shown on `/properties/:id` below Ownership History
- Loads `GET /api/properties/{propertyId}/prices` and displays Effective Date + Amount/Currency in backend order
- **Record Price Change** uses only `POST /api/properties/{propertyId}/prices` (updates current asking price + history)
- Editing current Price/Currency via the property form uses only `PUT /api/properties/{id}` (backend records history when values change)
- The UI never calls both APIs for one user action
- After a successful price-history POST, the property form’s current price/currency is synced from the API response

### Contact UI

- `/contacts` — paged list with filters (`firstName`, `lastName`, `email`, `phone`), Apply/Clear, Edit link
- `/contacts/new` — create form (first name, last name, phone, email)
- `/contacts/:id` — edit form; loads by id; 404 if missing
- Filters and pagination call `GET /api/contacts`
- Create uses `POST /api/contacts`; update uses `PUT /api/contacts/{id}`
- Duplicate email returns HTTP 409; UI shows a field-level message and does not attempt client-side uniqueness checks
