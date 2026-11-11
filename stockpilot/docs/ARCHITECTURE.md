# Architecture and rules

```mermaid
flowchart TD
  A[Angular routes and forms] --> B[HttpClient service]
  B --> C[Bearer authentication and authorization]
  C --> D[Domain API endpoints]
  D --> E[EF Core transactions]
  E --> F[Microsoft SQL Server]
  D --> G[Audit records]
```

`frontend/src/app/config.ts` defines each screen's columns, form fields, and UI permissions. `workspace.component.ts` implements form submission, workflow actions, filtering, CSV export, and navigation. `api.service.ts` attaches the in-memory bearer token.

`backend/Common.cs` contains authentication, account provisioning, error handling, audit support, version-based concurrency, and transaction helpers. `Models.cs` contains the domain entities, request DTOs, and EF relationship configuration. `Domain.cs` contains endpoints, business rules, and development data seeding. `Migrations/` is the committed SQL Server schema history. The test harness is a separate executable and does not change the application's SQL Server provider.

## Data relationships

```mermaid
erDiagram
    Party ||--o{ Order : places
    Order ||--|{ OrderLine : contains
    Product ||--o{ OrderLine : sold_or_purchased
    Product ||--o{ StockMovement : tracks
    Order o|--o{ StockMovement : posts
```

## Business rules

- New products start at zero stock. Opening balances and corrections go through an audited adjustment.
- Purchase orders require a Supplier; sales orders require a Customer. A product appears once per order.
- Posting a purchase adds stock; posting a sale subtracts stock. Negative stock is rejected.
- Order status and inventory changes commit together in a serializable transaction. Posting an order twice is rejected.
- Only drafts can be edited or cancelled. Posted transactions remain read-only; returns/credit notes are outside this version.
- All values use one business currency, decimal(18,2). Taxes, discounts, payment collection, and multi-currency are outside this version.
- Sales invoices are rendered in the browser for printing or Save as PDF; they are not jurisdiction-specific tax invoices.

## Authentication behavior

Passwords are hashed with ASP.NET Core PasswordHasher. Login is rate-limited by IP. Tokens expire after one hour and are kept only in browser memory; refreshing the page requires sign-in again. Sign out clears the local token. Existing issued tokens are not individually revoked by logout, but deactivating an account blocks its tokens on subsequent requests. API calls recheck account activity and role. There is no public registration; admins provision accounts. Password reset, MFA, and token refresh are not implemented.

## Concurrency

Mutable entities carry a numeric concurrency version incremented on updates. Update/action requests compare the client version and EF adds a version predicate when saving. Critical balance, inventory, and booking operations use short serializable transactions. SQL Server deadlocks and concurrency conflicts return 409 for the client to refresh/retry; there is no automatic replay of mutations. Unique indexes and restrictive foreign keys protect identity and referenced history. SQL Server locking behavior must be verified using the supplied SQL-backed CI workflow or a local SQL instance.
