/* =====================================================================================
   HealthTrack – SQL Server database script
-- ---------------------------------------------------------------------------------------
-- 0. (OPTIONAL) Reset – uncomment these lines to delete the database and start again.
-- ---------------------------------------------------------------------------------------
-- USE [master];
-- GO
-- IF DB_ID(N'HealthTrackDb') IS NOT NULL
-- BEGIN
--     ALTER DATABASE [HealthTrackDb] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
--     DROP DATABASE [HealthTrackDb];
-- END
-- GO

-- ---------------------------------------------------------------------------------------
-- 1. Database
-- ---------------------------------------------------------------------------------------
USE [master];
GO

IF DB_ID(N'HealthTrackDb') IS NULL
    CREATE DATABASE [HealthTrackDb];
GO

USE [HealthTrackDb];
GO

-- ---------------------------------------------------------------------------------------
-- 2. Users (Admin, Trainer and Client accounts – one table for every role)
--    Role:   0 = Client, 1 = Trainer, 2 = Admin
--    Gender: 0 = Male,   1 = Female,  2 = Other
--    MedicalConditions is encrypted by the application before it is saved.
-- ---------------------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Users] (
        [Id]                  INT            IDENTITY(1,1) NOT NULL,
        [FullName]            NVARCHAR(100)  NOT NULL,
        [Email]               NVARCHAR(150)  NOT NULL,
        [PasswordHash]        NVARCHAR(MAX)  NOT NULL,
        [Role]                INT            NOT NULL,
        [PhoneNumber]         NVARCHAR(20)   NULL,
        [DateOfBirth]         DATETIME2      NULL,
        [Gender]              INT            NULL,
        [HeightCm]            FLOAT          NULL,
        [Specialization]      NVARCHAR(100)  NULL,
        [MedicalConditions]   NVARCHAR(MAX)  NULL,
        [TrainerId]           INT            NULL,
        [IsActive]            BIT            NOT NULL,
        [IsApproved]          BIT            NOT NULL,
        [FailedLoginAttempts] INT            NOT NULL,
        [LockoutEnd]          DATETIME2      NULL,
        [CreatedAt]           DATETIME2      NOT NULL,
        [LastLoginAt]         DATETIME2      NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Users_Users_TrainerId] FOREIGN KEY ([TrainerId]) REFERENCES [dbo].[Users] ([Id])
    );

    CREATE UNIQUE INDEX [IX_Users_Email]     ON [dbo].[Users] ([Email]);
    CREATE        INDEX [IX_Users_TrainerId] ON [dbo].[Users] ([TrainerId]);
END
GO

-- ---------------------------------------------------------------------------------------
-- 3. WorkoutPlans + WorkoutExercises
--    WorkoutExercises.Day: 0 = Sunday ... 6 = Saturday (.NET DayOfWeek)
-- ---------------------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.WorkoutPlans', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[WorkoutPlans] (
        [Id]          INT            IDENTITY(1,1) NOT NULL,
        [Title]       NVARCHAR(100)  NOT NULL,
        [Description] NVARCHAR(500)  NULL,
        [Goal]        NVARCHAR(100)  NULL,
        [TrainerId]   INT            NOT NULL,
        [ClientId]    INT            NOT NULL,
        [StartDate]   DATETIME2      NOT NULL,
        [EndDate]     DATETIME2      NOT NULL,
        [IsActive]    BIT            NOT NULL,
        [CreatedAt]   DATETIME2      NOT NULL,
        CONSTRAINT [PK_WorkoutPlans] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WorkoutPlans_Users_ClientId]  FOREIGN KEY ([ClientId])  REFERENCES [dbo].[Users] ([Id]),
        CONSTRAINT [FK_WorkoutPlans_Users_TrainerId] FOREIGN KEY ([TrainerId]) REFERENCES [dbo].[Users] ([Id])
    );

    CREATE INDEX [IX_WorkoutPlans_ClientId]  ON [dbo].[WorkoutPlans] ([ClientId]);
    CREATE INDEX [IX_WorkoutPlans_TrainerId] ON [dbo].[WorkoutPlans] ([TrainerId]);
END
GO

IF OBJECT_ID(N'dbo.WorkoutExercises', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[WorkoutExercises] (
        [Id]              INT            IDENTITY(1,1) NOT NULL,
        [WorkoutPlanId]   INT            NOT NULL,
        [Name]            NVARCHAR(100)  NOT NULL,
        [Day]             INT            NOT NULL,
        [Sets]            INT            NOT NULL,
        [Reps]            INT            NOT NULL,
        [DurationMinutes] INT            NOT NULL,
        [Notes]           NVARCHAR(200)  NULL,
        CONSTRAINT [PK_WorkoutExercises] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WorkoutExercises_WorkoutPlans_WorkoutPlanId] FOREIGN KEY ([WorkoutPlanId])
            REFERENCES [dbo].[WorkoutPlans] ([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_WorkoutExercises_WorkoutPlanId] ON [dbo].[WorkoutExercises] ([WorkoutPlanId]);
END
GO

-- ---------------------------------------------------------------------------------------
-- 4. DietPlans + DietPlanItems
--    MealType: 0 = Breakfast, 1 = Lunch, 2 = Dinner, 3 = Snack
-- ---------------------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.DietPlans', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[DietPlans] (
        [Id]                 INT            IDENTITY(1,1) NOT NULL,
        [Title]              NVARCHAR(100)  NOT NULL,
        [Description]        NVARCHAR(500)  NULL,
        [DailyCalorieTarget] INT            NOT NULL,
        [TrainerId]          INT            NOT NULL,
        [ClientId]           INT            NOT NULL,
        [StartDate]          DATETIME2      NOT NULL,
        [EndDate]            DATETIME2      NOT NULL,
        [IsActive]           BIT            NOT NULL,
        [CreatedAt]          DATETIME2      NOT NULL,
        CONSTRAINT [PK_DietPlans] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DietPlans_Users_ClientId]  FOREIGN KEY ([ClientId])  REFERENCES [dbo].[Users] ([Id]),
        CONSTRAINT [FK_DietPlans_Users_TrainerId] FOREIGN KEY ([TrainerId]) REFERENCES [dbo].[Users] ([Id])
    );

    CREATE INDEX [IX_DietPlans_ClientId]  ON [dbo].[DietPlans] ([ClientId]);
    CREATE INDEX [IX_DietPlans_TrainerId] ON [dbo].[DietPlans] ([TrainerId]);
END
GO

IF OBJECT_ID(N'dbo.DietPlanItems', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[DietPlanItems] (
        [Id]            INT            IDENTITY(1,1) NOT NULL,
        [DietPlanId]    INT            NOT NULL,
        [MealType]      INT            NOT NULL,
        [Description]   NVARCHAR(300)  NOT NULL,
        [Calories]      INT            NOT NULL,
        [ScheduledTime] TIME           NOT NULL,
        CONSTRAINT [PK_DietPlanItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DietPlanItems_DietPlans_DietPlanId] FOREIGN KEY ([DietPlanId])
            REFERENCES [dbo].[DietPlans] ([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_DietPlanItems_DietPlanId] ON [dbo].[DietPlanItems] ([DietPlanId]);
END
GO

-- ---------------------------------------------------------------------------------------
-- 5. Client logs: WorkoutLogs, MealLogs, HealthMetrics
--    WorkoutLogs.Intensity: 0 = Low, 1 = Moderate, 2 = High
--    Deleting a workout plan keeps the log but sets WorkoutPlanId to NULL.
--    HealthMetrics.Notes is encrypted by the application before it is saved.
-- ---------------------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.WorkoutLogs', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[WorkoutLogs] (
        [Id]              INT            IDENTITY(1,1) NOT NULL,
        [ClientId]        INT            NOT NULL,
        [WorkoutPlanId]   INT            NULL,
        [Date]            DATETIME2      NOT NULL,
        [ExerciseName]    NVARCHAR(100)  NOT NULL,
        [DurationMinutes] INT            NOT NULL,
        [CaloriesBurned]  INT            NOT NULL,
        [Sets]            INT            NULL,
        [Reps]            INT            NULL,
        [Intensity]       INT            NOT NULL,
        [Notes]           NVARCHAR(300)  NULL,
        [CreatedAt]       DATETIME2      NOT NULL,
        CONSTRAINT [PK_WorkoutLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WorkoutLogs_Users_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [dbo].[Users] ([Id]),
        CONSTRAINT [FK_WorkoutLogs_WorkoutPlans_WorkoutPlanId] FOREIGN KEY ([WorkoutPlanId])
            REFERENCES [dbo].[WorkoutPlans] ([Id]) ON DELETE SET NULL
    );

    CREATE INDEX [IX_WorkoutLogs_ClientId_Date]  ON [dbo].[WorkoutLogs] ([ClientId], [Date]);
    CREATE INDEX [IX_WorkoutLogs_WorkoutPlanId]  ON [dbo].[WorkoutLogs] ([WorkoutPlanId]);
END
GO

IF OBJECT_ID(N'dbo.MealLogs', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[MealLogs] (
        [Id]        INT            IDENTITY(1,1) NOT NULL,
        [ClientId]  INT            NOT NULL,
        [LoggedAt]  DATETIME2      NOT NULL,
        [MealType]  INT            NOT NULL,
        [FoodItems] NVARCHAR(300)  NOT NULL,
        [Calories]  INT            NOT NULL,
        [ProteinG]  FLOAT          NULL,
        [CarbsG]    FLOAT          NULL,
        [FatG]      FLOAT          NULL,
        CONSTRAINT [PK_MealLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MealLogs_Users_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [dbo].[Users] ([Id])
    );

    CREATE INDEX [IX_MealLogs_ClientId_LoggedAt] ON [dbo].[MealLogs] ([ClientId], [LoggedAt]);
END
GO

IF OBJECT_ID(N'dbo.HealthMetrics', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[HealthMetrics] (
        [Id]               INT            IDENTITY(1,1) NOT NULL,
        [ClientId]         INT            NOT NULL,
        [RecordedAt]       DATETIME2      NOT NULL,
        [WeightKg]         FLOAT          NOT NULL,
        [HeightCm]         FLOAT          NOT NULL,
        [Bmi]              FLOAT          NOT NULL,
        [BodyFatPercent]   FLOAT          NULL,
        [RestingHeartRate] INT            NULL,
        [SystolicBp]       INT            NULL,
        [DiastolicBp]      INT            NULL,
        [SleepHours]       FLOAT          NULL,
        [Notes]            NVARCHAR(MAX)  NULL,
        CONSTRAINT [PK_HealthMetrics] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_HealthMetrics_Users_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [dbo].[Users] ([Id])
    );

    CREATE INDEX [IX_HealthMetrics_ClientId_RecordedAt] ON [dbo].[HealthMetrics] ([ClientId], [RecordedAt]);
END
GO

-- ---------------------------------------------------------------------------------------
-- 6. Appointments
--    Type:   0 = TrainingConsultation, 1 = NutritionConsultation, 2 = HealthCheck,
--            3 = Physiotherapy, 4 = ProgressReview
--    Status: 0 = Pending, 1 = Approved, 2 = Cancelled, 3 = Completed
-- ---------------------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Appointments', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Appointments] (
        [Id]              INT            IDENTITY(1,1) NOT NULL,
        [ClientId]        INT            NOT NULL,
        [TrainerId]       INT            NOT NULL,
        [ScheduledAt]     DATETIME2      NOT NULL,
        [DurationMinutes] INT            NOT NULL,
        [Type]            INT            NOT NULL,
        [Reason]          NVARCHAR(500)  NULL,
        [Status]          INT            NOT NULL,
        [TrainerNotes]    NVARCHAR(500)  NULL,
        [ReminderSent]    BIT            NOT NULL,
        [CreatedAt]       DATETIME2      NOT NULL,
        [UpdatedAt]       DATETIME2      NULL,
        CONSTRAINT [PK_Appointments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Appointments_Users_ClientId]  FOREIGN KEY ([ClientId])  REFERENCES [dbo].[Users] ([Id]),
        CONSTRAINT [FK_Appointments_Users_TrainerId] FOREIGN KEY ([TrainerId]) REFERENCES [dbo].[Users] ([Id])
    );

    CREATE INDEX [IX_Appointments_ClientId]              ON [dbo].[Appointments] ([ClientId]);
    CREATE INDEX [IX_Appointments_TrainerId_ScheduledAt] ON [dbo].[Appointments] ([TrainerId], [ScheduledAt]);
END
GO

-- ---------------------------------------------------------------------------------------
-- 7. Feedbacks (trainer -> client)
-- ---------------------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Feedbacks', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Feedbacks] (
        [Id]        INT             IDENTITY(1,1) NOT NULL,
        [TrainerId] INT             NOT NULL,
        [ClientId]  INT             NOT NULL,
        [Message]   NVARCHAR(1000)  NOT NULL,
        [CreatedAt] DATETIME2       NOT NULL,
        CONSTRAINT [PK_Feedbacks] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Feedbacks_Users_ClientId]  FOREIGN KEY ([ClientId])  REFERENCES [dbo].[Users] ([Id]),
        CONSTRAINT [FK_Feedbacks_Users_TrainerId] FOREIGN KEY ([TrainerId]) REFERENCES [dbo].[Users] ([Id])
    );

    CREATE INDEX [IX_Feedbacks_ClientId]  ON [dbo].[Feedbacks] ([ClientId]);
    CREATE INDEX [IX_Feedbacks_TrainerId] ON [dbo].[Feedbacks] ([TrainerId]);
END
GO

-- ---------------------------------------------------------------------------------------
-- 8. Notifications
--    Type: 0 = General, 1 = Appointment, 2 = WorkoutReminder, 3 = MealReminder,
--          4 = Plan, 5 = Feedback, 6 = Account, 7 = HealthAlert
-- ---------------------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Notifications', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Notifications] (
        [Id]        INT            IDENTITY(1,1) NOT NULL,
        [UserId]    INT            NOT NULL,
        [Title]     NVARCHAR(150)  NOT NULL,
        [Message]   NVARCHAR(500)  NOT NULL,
        [Type]      INT            NOT NULL,
        [Link]      NVARCHAR(200)  NULL,
        [IsRead]    BIT            NOT NULL,
        [CreatedAt] DATETIME2      NOT NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Notifications_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id])
    );

    CREATE INDEX [IX_Notifications_UserId_IsRead] ON [dbo].[Notifications] ([UserId], [IsRead]);
END
GO

-- ---------------------------------------------------------------------------------------
-- 9. ActivityLogs (sign-ins, failed attempts and data changes)
-- ---------------------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.ActivityLogs', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ActivityLogs] (
        [Id]        INT            IDENTITY(1,1) NOT NULL,
        [UserId]    INT            NULL,
        [Action]    NVARCHAR(50)   NOT NULL,
        [Details]   NVARCHAR(300)  NULL,
        [IpAddress] NVARCHAR(50)   NULL,
        [Timestamp] DATETIME2      NOT NULL,
        CONSTRAINT [PK_ActivityLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ActivityLogs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id])
    );

    CREATE INDEX [IX_ActivityLogs_Timestamp] ON [dbo].[ActivityLogs] ([Timestamp]);
    CREATE INDEX [IX_ActivityLogs_UserId]    ON [dbo].[ActivityLogs] ([UserId]);
END
GO

-- ---------------------------------------------------------------------------------------
-- 10. (OPTIONAL) SQL login for the application
--     Only needed if you connect with a SQL Server username/password instead of
--     Windows Authentication. Change the password, uncomment and run, then use the
--     "SQL Server Authentication" connection string in appsettings.json.
-- ---------------------------------------------------------------------------------------
-- USE [master];
-- GO
-- IF SUSER_ID(N'healthtrack_app') IS NULL
--     CREATE LOGIN [healthtrack_app] WITH PASSWORD = N'Change_This_P@ssw0rd', CHECK_POLICY = ON;
-- GO
-- USE [HealthTrackDb];
-- GO
-- IF USER_ID(N'healthtrack_app') IS NULL
--     CREATE USER [healthtrack_app] FOR LOGIN [healthtrack_app];
-- ALTER ROLE [db_datareader] ADD MEMBER [healthtrack_app];
-- ALTER ROLE [db_datawriter] ADD MEMBER [healthtrack_app];
-- GO

-- ---------------------------------------------------------------------------------------
-- 11. Check – lists every table with its row count (all 0 until the app first runs)
-- ---------------------------------------------------------------------------------------
USE [HealthTrackDb];
GO

SELECT t.name AS [Table], SUM(p.rows) AS [Rows]
FROM sys.tables t
JOIN sys.partitions p ON p.object_id = t.object_id AND p.index_id IN (0, 1)
GROUP BY t.name
ORDER BY t.name;
GO
