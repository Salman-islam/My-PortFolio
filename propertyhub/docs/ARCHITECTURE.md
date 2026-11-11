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
    Property ||--o{ Room : contains
    Property ||--o{ Amenity : offers
    Property ||--o{ Resident : houses
    Room o|--o| Resident : assigned
    Amenity ||--o{ Booking : reserved
    Resident ||--o{ Booking : makes
    Property ||--o{ Announcement : publishes
```

## Business rules

- A resident profile belongs to one property and optionally one room. Each room supports one resident profile in this version.
- Residents see their own profile/bookings and only their property's amenities and current announcements. Staff/admin manage all properties.
- Reservations must start in the future, within 30 days, last at most 4 hours, and respect amenity capacity.
- Overlap uses existing.Start < requested.End AND existing.End > requested.Start. Adjacent slots are allowed.
- The availability endpoint is advisory; creating a booking repeats the conflict check inside a serializable transaction.
- Only future confirmed bookings can be cancelled. Cancelled rows remain in history.
- Moving a resident across properties or reducing/disabling an amenity is rejected if it would invalidate upcoming reservations.
- Timestamps are normalized to UTC, displayed in browser-local time. Recurring reservations, billing, door-lock integrations, and multi-property staff scoping are outside this version.

## Authentication behavior

Passwords are hashed with ASP.NET Core PasswordHasher. Login is rate-limited by IP. Tokens expire after one hour and are kept only in browser memory; refreshing the page requires sign-in again. Sign out clears the local token. Existing issued tokens are not individually revoked by logout, but deactivating an account blocks its tokens on subsequent requests. API calls recheck account activity and role. There is no public registration; admins provision accounts. Password reset, MFA, and token refresh are not implemented.

## Concurrency

Mutable entities carry a numeric concurrency version incremented on updates. Update/action requests compare the client version and EF adds a version predicate when saving. Critical balance, inventory, and booking operations use short serializable transactions. SQL Server deadlocks and concurrency conflicts return 409 for the client to refresh/retry; there is no automatic replay of mutations. Unique indexes and restrictive foreign keys protect identity and referenced history. SQL Server locking behavior must be verified using the supplied SQL-backed CI workflow or a local SQL instance.
