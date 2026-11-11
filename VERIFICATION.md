# Verification report

| Project | API HTTP assertions passed | Browser workflow |
| --- | ---: | --- |
| PeopleOps | 44 | Login, all navigation, department create/edit, leave approval, logout |
| StockPilot | 43 | Login, all navigation, sale creation/posting, invoice view, logout |
| DeskFlow | 44 | Login, all navigation, ticket creation, conversation comment, logout |
| PropertyHub | 36 | Login, all navigation, availability check, booking creation/cancellation, logout |

167 HTTP assertions passed, plus explicit business-state checks. The API suites exercised authorization failures, stale writes, account deactivation, and domain-specific conditions. They used the same C# endpoints through the separate SQLite test harness. Desktop, login, workflow, and mobile screenshots were captured for each application.

All four Angular production builds and all four C# API/test-harness builds passed. SQL Server EF migrations and idempotent T-SQL scripts were generated. Screenshots show the real Angular/C# applications with fictional seeded data in the portable test database.

Not verified in this environment: SQL Server runtime or migration application, SQL Server-specific concurrent locking, hosted GitHub Actions, and production deployment. Each repository includes a CI workflow configured to run the API tests with SQL Server 2022. The same tests can also be run against a disposable local SQL Server database using the documented --base-url option.
