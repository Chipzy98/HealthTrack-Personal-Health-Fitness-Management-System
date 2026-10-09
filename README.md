# HealthTrack – Personal Health & Fitness Management System

CS6004ES Coursework 2 (2025/26) · ASP.NET Core 8 MVC · C# · Entity Framework Core · SQL Server

---

## 1. Installation guide

### Requirements
- **Visual Studio 2022** (17.8 or later) with the **ASP.NET and web development** workload
- **.NET 8 SDK** (installed with VS 2022 17.8+)
- **SQL Server** 2016 or later – Developer or Express edition (LocalDB also works, see below)
- **SQL Server Management Studio (SSMS)** to run the database script
- Internet connection (Bootstrap, icons, jQuery validation, Chart.js and fonts are loaded from CDNs)

### Step 1 – Create the database and tables
1. Open **SSMS** and connect to your SQL Server. Note the **Server name** you used in the connect dialog (for example `localhost`, `.\SQLEXPRESS` or `DESKTOP-ABC123\SQLEXPRESS`).
2. *File → Open → File…* and open **`HealthTrack/Database/HealthTrackDb.sql`**.
3. Press **F5**. The script creates the database **`HealthTrackDb`** with 12 tables, keys and indexes, and finishes by listing the tables (0 rows each).

The script can be run again safely – it skips anything that already exists.

### Step 2 – Set the connection string
Open **`HealthTrack/appsettings.json`** and set `ConnectionStrings:DefaultConnection` to match your server. Use the same Server name as in SSMS. In JSON every `\` must be written as `\\`.

| Your SQL Server | DefaultConnection |
|---|---|
| Default instance, Windows login (the setting shipped) | `Server=localhost;Database=HealthTrackDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True` |
| SQL Server Express, Windows login | `Server=localhost\\SQLEXPRESS;Database=HealthTrackDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True` |
| Username and password (SQL Server Authentication) | `Server=localhost;Database=HealthTrackDb;User Id=healthtrack_app;Password=YourPassword;MultipleActiveResultSets=true;TrustServerCertificate=True` |
| LocalDB (the original setting) | `Server=(localdb)\\mssqllocaldb;Database=HealthTrackDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True` |

For the username/password option, section 10 of the SQL script creates the `healthtrack_app` login. Change its password first. SQL Server must also allow *SQL Server and Windows Authentication mode* (SSMS → right-click the server → *Properties → Security*).

### Step 3 – Run the project
1. Extract the zip and open **`HealthTrack.sln`** in Visual Studio.
2. Wait for NuGet packages to restore (EF Core SqlServer 8.0.8). If they don't, right-click the solution → *Restore NuGet Packages*.
3. Press **F5** (or Ctrl+F5). The site opens at `https://localhost:7215`.
4. On first run the application finds the **Users** table empty and fills every table with the demo data below. Everything you add in the app from then on is saved to SQL Server. Check it in SSMS, for example `SELECT * FROM HealthTrackDb.dbo.Users`.

If you skip Step 1, the application creates the database and tables itself on first run. This only works if your Windows login has permission to create databases.

If Visual Studio asks to trust the ASP.NET Core development certificate, click **Yes**.

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

---

## 2. User manual

### Client
- **Dashboard** – latest weight, BMI, blood pressure, today's calories, weekly workout totals, weight trend chart, today's exercises from the active plan, diet schedule, upcoming appointments and trainer feedback.
- **My plans** – workout plans (grouped by day), diet plans (meal times and calories) and all feedback.
- **Workouts → Log workout** – exercise, date, duration, intensity, sets/reps. Leave calories empty to estimate automatically (Low 4, Moderate 7, High 10 kcal/min).
- **Meals → Log meal** – meal type, time, food, calories and macros. Click a meal from your diet plan to fill the form.
- **Health metrics → Add measurement** – weight and height (BMI calculated), body fat, heart rate, blood pressure, sleep and private notes (encrypted). High BP or BMI ≥ 30 alerts your trainer.
- **Progress** – charts for weight, BMI, calories in vs out, weekly workout minutes and resting heart rate over 30 days to 1 year.
- **Appointments** – book a consultation (6 AM–9 PM, at least 1 hour ahead, max 3 months ahead, no overlaps) and cancel open bookings.
- **Notifications** – bell in the top bar plus a full list. Reminders are generated automatically for workouts, meals and appointments.

### Trainer
- **Dashboard** – client progress table, pending appointment requests (approve in one click), upcoming sessions, recent client workouts and clients who haven't trained for 7 days.
- **My clients → Details** – measurements, workouts, meals, plans, medical notes and a feedback form.
- **Plans** – create workout plans (add/remove exercise rows) and diet plans (meal schedule with a running calorie total); activate, deactivate or delete plans.
- **Appointments** – approve, reschedule/update (time, type, status, notes), mark completed or cancel with a reason.
- **Reports** – performance of your clients, popular routines, engagement chart; export to CSV.

### Administrator
- **Dashboard** – user counts, active users, sign-ins, usage chart (14 days), trainer approval queue, assign trainers to clients, trainer workload and recent activity.
- **Users** – filter by role, search, create, edit, deactivate/reactivate and unlock accounts.
- **Reports** – system-wide client performance, trainer performance, popular routines, engagement and appointment statistics; CSV export.
- **Activity log** – every sign-in, failed attempt and data change with IP address, filterable and paged.

---

## 3. How the requirements are met

| Requirement | Implementation |
|---|---|
| Secure registration & login | `AccountController`, PBKDF2 hashing (`SecurePasswordHasher`), cookie authentication with role claims, lockout after 5 failed attempts, strong-password rule, trainer approval |
| Role-based access | `[Authorize(Roles = ...)]` on every controller; trainers can only open their own clients (`IsMyClientAsync`) |
| Client / Trainer / Admin dashboards | `ClientController.Dashboard`, `TrainerController.Dashboard`, `AdminController.Dashboard` |
| Workout & diet management | `TrainerController.CreateWorkoutPlan/CreateDietPlan`, `ClientController.LogWorkout/LogMeal/AddMetric` |
| Appointment system | `AppointmentsController` (Create, Approve, Edit, Complete, Cancel) with time-slot and overlap validation |
| Progress tracking & analytics | `ProgressController` (Chart.js trends), `ReportsController` (client performance, popular routines, engagement, CSV export) |
| Notifications | `NotificationService`, `ReminderBackgroundService` (hosted service every 15 min), `NotificationBellViewComponent` |
| Security & data protection | Data Protection API encryption of medical conditions and metric notes, anti-forgery tokens on all POSTs, server + client validation, security headers, HTTPS/HSTS, activity logging, deactivate instead of delete |

---

## 4. Project structure

```
HealthTrack/
├─ Controllers/        MVC controllers (one per area of the system)
├─ Data/               AppDbContext (EF Core, encryption converter) and DbSeeder (demo data)
├─ Database/           HealthTrackDb.sql – SQL Server script that creates the database and tables
├─ Helpers/            Extension methods (claims, display names, CSV, time-ago)
├─ Models/             Entity classes: User, WorkoutPlan, WorkoutExercise, DietPlan, DietPlanItem,
│                      WorkoutLog, MealLog, HealthMetric, Appointment, Feedback, Notification, ActivityLog, Enums
├─ Services/           Password hashing, health calculations, notifications, reminders, activity logging
├─ ViewComponents/     Notification bell in the top bar
├─ ViewModels/         Form and page models with validation attributes
├─ Views/              Razor views (Shared layout, partials, one folder per controller)
├─ wwwroot/            site.css (design system) and site.js (UI helpers, dynamic form rows, chart defaults)
├─ App_Data/           Data Protection keys (created at runtime)
└─ Program.cs          Service registration, security middleware, database creation
```

## 5. Troubleshooting
- **"A network-related or instance-specific error" / "server was not found"** – the Server name in the connection string is wrong or SQL Server isn't running. Use exactly the name you connect with in SSMS (remember `\\` in JSON), and check that the *SQL Server (…)* service is running in *Services*.
- **"Cannot open database HealthTrackDb requested by the login"** – run `Database/HealthTrackDb.sql` first, or give your login access to the database.
- **"Login failed for user"** – with a username/password, enable *SQL Server and Windows Authentication mode* and restart the SQL Server service.
- **LocalDB errors** – run `sqllocaldb start MSSQLLocalDB` in a command prompt, or install LocalDB via the Visual Studio Installer.
- **Invalid column / table errors after changing models** – delete the database (see *Reset the database*).
- **Page has no styling** – check the internet connection (CDN files).
- **Error when opening profiles or measurements** – the encryption keys in `App_Data/Keys` were deleted, so old encrypted data can't be read; reset the database.
