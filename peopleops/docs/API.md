# API reference

Authentication: `Authorization: Bearer <token>`. Login accepts `{ "email": "...", "password": "..." }`; its response includes `token`, `user`, and `expiresIn`.

| Method | Route |
| --- | --- |
| GET | `/api/health` |
| POST | `/api/auth/login` |
| GET | `/api/auth/me` |
| GET | `/api/users` |
| POST | `/api/users` |
| PUT | `/api/users/{id:int}/active` |
| GET | `/api/audit` |
| GET | `/api/workspace` |
| POST | `/api/departments` |
| PUT | `/api/departments/{id:int}` |
| DELETE | `/api/departments/{id:int}` |
| POST | `/api/employees` |
| PUT | `/api/employees/{id:int}` |
| POST | `/api/attendance/check-in` |
| POST | `/api/attendance/check-out` |
| POST | `/api/leaves` |
| POST | `/api/leaves/{id:int}/decision` |
| POST | `/api/leaves/{id:int}/cancel` |

Request DTOs are in `backend/Common.cs` and `backend/Models.cs`. `tests/api_checks.py` contains executable requests for the main workflows. `GET /api/workspace` returns role-filtered data and report totals used by Angular. It is intended for small portfolio datasets; attendance/movement lists are limited to 500 rows and audits to 200.

Mutating existing records generally require the `version` returned by the workspace API. Missing versions are rejected as stale. Domain action endpoints use POST; they do not accept unrestricted status updates. User activation uses `{ "active": false }`.

Errors use `{ "message": "..." }` for business validation, with 400 invalid input, 401 missing/expired login, 403 forbidden, 404 missing records, and 409 conflicting or stale state. Framework model-binding failures may use ASP.NET problem details.

Timestamps should be sent as UTC ISO 8601 strings ending in `Z`. HR date-only fields use `YYYY-MM-DD`.
