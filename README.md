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

> Ownership transfer, price-history APIs, dashboard, and Angular UI are not in this slice.
