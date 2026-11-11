 C# + Angular portfolio starters

TeamDesk (employees), Stockroom (inventory), and SupportFlow (support tickets). Each folder contains its own Angular frontend, C# API, README, sample HTTP requests, and screenshot. Extract one folder and upload its contents as a separate GitHub repository. Run only one project at a time on the default ports.
TeamDesk (employees), Stockroom (inventory), and SupportFlow (support tickets). Each folder contains its own Angular frontend, C# API, README, sample HTTP requests, and screenshot. Extract one folder and upload its contents as a separate GitHub repository. Run only one project at a time on the default ports.

See each README for setup, connection flow, endpoint documentation, and scope. These are starter projects intended for learning and customization, not complete production systems.





# Four C# + Angular + Microsoft SQL Server projects

| Folder | Project | Main workflows |
| --- | --- | --- |
| peopleops | PeopleOps | Employees, attendance, leave requests and approvals |
| stockpilot | StockPilot | Inventory, purchases, sales, stock ledger, invoices |
| deskflow | DeskFlow | Tickets, assignment, public/private comments, lifecycle |
| propertyhub | PropertyHub | Properties, rooms, residents, amenities, bookings |

Each folder is an independent GitHub repository with an Angular frontend, ASP.NET Core API, SQL Server EF migrations, demo accounts, screenshots, API workflow tests, documentation, and GitHub Actions configuration. Extract one folder and follow its README. Upload each folder to a separate repository; the included CI paths assume this layout.

Run one project at a time on ports 5080/4200, or change both API and proxy ports. The application projects use SQL Server. The separate portable test harnesses use SQLite only for test execution and screenshot capture.

These are implemented portfolio applications with explicitly documented scope. SQL Server runtime and hosted CI have not been executed in this environment. Read each project's docs/VERIFICATION.md before presenting deployment/testing claims.
