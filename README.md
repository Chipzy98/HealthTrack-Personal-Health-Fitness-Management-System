# HealthTrack – Personal Health & Fitness Management System

CS6004ES Coursework 2 (2025/26) · ASP.NET Core 8 MVC · C# · Entity Framework Core · SQL Server LocalDB

---

## 1. Installation guide

### Requirements
- **Visual Studio 2022** (17.8 or later) with the **ASP.NET and web development** workload
- **.NET 8 SDK** (installed with VS 2022 17.8+)
- **SQL Server Express LocalDB** (installed with the ASP.NET workload)
- Internet connection (Bootstrap, icons, jQuery validation, Chart.js and fonts are loaded from CDNs)

### Reset the database
Open *View → SQL Server Object Explorer*, expand `(localdb)\MSSQLLocalDB → Databases`, delete **HealthTrackDb** (tick *Close existing connections*) and run the project again. It is recreated with fresh demo data. Do this also if you change the model classes.

To use a different SQL Server, change `ConnectionStrings:DefaultConnection` in `appsettings.json`.

### Demo accounts

| Role | Email | Password |
|---|---|---|
| Administrator | admin@healthtrack.com | Admin@123 |
| Trainer (fitness) | trainer@healthtrack.com | Trainer@123 |
| Trainer (nutritionist) | nimali@healthtrack.com | Trainer@123 |
| Trainer awaiting approval | ravi@healthtrack.com | Trainer@123 |
| Client (has plans & history) | client@healthtrack.com | Client@123 |
| Clients | dilini@healthtrack.com, amal@healthtrack.com | Client@123 |
| Client without a trainer | sachini@healthtrack.com | Client@123 |

