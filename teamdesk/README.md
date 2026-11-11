# TeamDesk

An employee directory built with Angular and ASP.NET Core, featuring searchable employee records, department filters, CRUD APIs, and SQLite persistence.

![TeamDesk dashboard](docs/dashboard.png)

## Features
- Create, read, update, and delete employees.
- Search loaded records and filter by department.
- Live summary cards, form validation, loading states, and API error feedback.
- C# minimal REST APIs and Entity Framework Core SQLite persistence.
- Demo records seeded on startup when the database is empty.

## Stack
Angular 20 / TypeScript / ASP.NET Core (.NET 10) / EF Core / SQLite.

## Run locally
Install .NET 10 SDK and Node.js 24 LTS with npm. Open two terminals in this repository.

Backend:
```sh
cd backend
dotnet restore
dotnet run --urls http://localhost:5080
```

Frontend:
```sh
cd frontend
npm ci
npm start
```
Open http://localhost:4200. The Angular development proxy forwards `/api` to `http://localhost:5080`. Run one portfolio project at a time because they share these ports.

## API endpoints
| Method | URL | Purpose |
| --- | --- | --- |
| GET | `/api/health` | Health check |
| GET | `/api/employees` | List records |
| GET | `/api/employees/{id}` | Read one record |
| POST | `/api/employees` | Create a record |
| PUT | `/api/employees/{id}` | Update a record |
| DELETE | `/api/employees/{id}` | Delete a record |

Example POST/PUT JSON:
```json
{
  "name": "Ayesha Khan",
  "email": "ayesha@example.com",
  "department": "Engineering",
  "status": "Active"
}
```
Use `backend/requests.http` with an HTTP client or Visual Studio. Invalid input returns HTTP 400; missing IDs return 404. POST returns 201 and DELETE returns 204.

## Connection flow
`Angular component → ApiService / HttpClient → /api development proxy → C# endpoint → EF Core → app.db`

`frontend/src/app/api.service.ts` owns HTTP requests. `backend/Program.cs` owns endpoint mappings, validation, seeding, and the database model. SQLite is created automatically in the backend working directory. The connection can be overridden with the `ConnectionStrings__Default` environment variable.

## Build
```sh
cd frontend
npm run build
```
```sh
cd backend
dotnet build
```

## Upload to GitHub
Create an empty GitHub repository named `teamdesk`, then run from this project folder:
```sh
git init
git add .
git commit -m "Add TeamDesk full-stack starter"
git branch -M main
git remote add origin https://github.com/YOUR_USERNAME/teamdesk.git
git push -u origin main
```
GitHub repository description: **An employee directory built with Angular and ASP.NET Core, featuring searchable employee records, department filters, CRUD APIs, and SQLite persistence.**
Suggested topics: `angular`, `csharp`, `aspnet-core`, `rest-api`, `entity-framework-core`, `sqlite`, `portfolio`.

## Scope and next steps
This is an educational CRUD starter, with no authentication, roles, pagination, audit trail, or production deployment configuration. It uses `EnsureCreated` for a simple local database; adopt EF migrations before evolving a deployed schema. All seed records are fictional. Add unique email/SKU rules if required by your use case. Search/filter operate on the records already loaded in the browser.

For a stronger portfolio, add tests, authentication, and domain features, and describe your own contributions in this README.

## Verification
Both Angular and C# builds passed. Browser checks against the real C# backend covered initial loading, search, filtering, create, edit, delete, and reload. API checks covered invalid input (400) and missing records (404). Desktop and mobile screenshots are in `docs/`. Screenshots use the seeded demo data.
