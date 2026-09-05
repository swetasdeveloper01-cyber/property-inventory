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
client/                              # Angular UI (later phase)
postman/                             # API collection (later phase)
```

**Dependency direction:** Api → Application + Infrastructure; Application → Domain; Infrastructure → Domain + Application.

## Prerequisites

- .NET SDK 10 (LTS)
- SQL Server LocalDB (or another SQL Server instance; update `ConnectionStrings:DefaultConnection`)
- EF Core tools (optional): `dotnet tool install --global dotnet-ef`

## Local setup

```bash
dotnet restore PropertyInventory.sln
dotnet build PropertyInventory.sln
dotnet test PropertyInventory.sln
dotnet run --project src/PropertyInventory.Api
```

In Development, the host applies pending migrations and seeds sample data when the database is empty.

## Implemented API endpoints (current slice)

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

> Angular UI is not in this slice.
