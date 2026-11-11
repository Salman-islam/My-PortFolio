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
    Category ||--o{ Ticket : categorizes
    AppUser ||--o{ Ticket : requests
    Ticket ||--o{ Comment : contains
    AppUser ||--o{ Comment : writes
    Ticket ||--o{ TicketEvent : records
```

## Business rules

- Customers can see only their own tickets. Customer API calls cannot read other customers' conversations or internal notes.
- Admins can assign any active staff/admin account. Staff can claim unassigned tickets or retain their own assignment; they cannot take another agent's ticket.
- Only an assigned agent or admin can advance a ticket. A ticket must be assigned before work starts.
- Customers can reopen their resolved tickets. Reopening clears ResolvedAt; closing retains the latest resolution time.
- Closed tickets reject comments. Tickets are retained instead of deleted so their history remains available.
- Resolution metrics use the latest resolution timestamp; SLA calendars, email delivery, attachments, and escalation jobs are outside this version.

## Authentication behavior

Passwords are hashed with ASP.NET Core PasswordHasher. Login is rate-limited by IP. Tokens expire after one hour and are kept only in browser memory; refreshing the page requires sign-in again. Sign out clears the local token. Existing issued tokens are not individually revoked by logout, but deactivating an account blocks its tokens on subsequent requests. API calls recheck account activity and role. There is no public registration; admins provision accounts. Password reset, MFA, and token refresh are not implemented.

## Concurrency

Mutable entities carry a numeric concurrency version incremented on updates. Update/action requests compare the client version and EF adds a version predicate when saving. Critical balance, inventory, and booking operations use short serializable transactions. SQL Server deadlocks and concurrency conflicts return 409 for the client to refresh/retry; there is no automatic replay of mutations. Unique indexes and restrictive foreign keys protect identity and referenced history. SQL Server locking behavior must be verified using the supplied SQL-backed CI workflow or a local SQL instance.
