# HealthTrack – Personal Health & Fitness Management System

## 1. Installation guide

### Requirements
- **Visual Studio 2022** (17.8 or later) with the **ASP.NET and web development** workload
- **.NET 8 SDK** (installed with VS 2022 17.8+)
- **SQL Server** 2016 or later – Developer or Express edition (LocalDB also works, see below)
- **SQL Server Management Studio (SSMS)** to run the database script
- Internet connection (Bootstrap, icons, jQuery validation, Chart.js and fonts are loaded from CDNs)

### Reset the database
Uncomment section 0 at the top of `HealthTrackDb.sql`, run the script again, and run the project. The database is recreated empty and filled with fresh demo data. Do this also if you change the model classes, and update the matching `CREATE TABLE` in the script.

Demo passwords are hashed and medical notes are encrypted by the application, so don't insert users directly with SQL. Register them in the app, or let the seeder create the demo accounts.

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

