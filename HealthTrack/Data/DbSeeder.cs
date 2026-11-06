using HealthTrack.Models;
using HealthTrack.Services;

namespace HealthTrack.Data
{
    /// <summary>
    /// Creates demo accounts and realistic sample data the first time the application runs,
    /// so every dashboard, chart and report has something to show in the viva.
    /// </summary>
    public static class DbSeeder
    {
        public static void Seed(AppDbContext db)
        {
            if (db.Users.Any()) return;

            var random = new Random(2026);
            var today = DateTime.Today;

            // ---------------- Users ----------------
            var admin = NewUser("System Administrator", "admin@healthtrack.com", "Admin@123", UserRole.Admin);

            var trainerJohn = NewUser("John Perera", "trainer@healthtrack.com", "Trainer@123", UserRole.Trainer);
            trainerJohn.Specialization = "Strength & Conditioning Coach";
            trainerJohn.PhoneNumber = "0771234567";

            var trainerNimali = NewUser("Dr. Nimali Silva", "nimali@healthtrack.com", "Trainer@123", UserRole.Trainer);
            trainerNimali.Specialization = "Nutritionist / Dietitian";
            trainerNimali.PhoneNumber = "0772345678";

            var pendingTrainer = NewUser("Ravi Jayasinghe", "ravi@healthtrack.com", "Trainer@123", UserRole.Trainer);
            pendingTrainer.Specialization = "Physiotherapist";
            pendingTrainer.IsApproved = false;

            var kasun = NewUser("Kasun Fernando", "client@healthtrack.com", "Client@123", UserRole.Client);
            kasun.DateOfBirth = new DateTime(1998, 4, 12);
            kasun.Gender = GenderType.Male;
            kasun.HeightCm = 175;
            kasun.PhoneNumber = "0711234567";
            kasun.MedicalConditions = "Mild asthma - carries inhaler during cardio sessions.";
            kasun.Trainer = trainerJohn;

            var dilini = NewUser("Dilini Wickramasinghe", "dilini@healthtrack.com", "Client@123", UserRole.Client);
            dilini.DateOfBirth = new DateTime(2001, 9, 3);
            dilini.Gender = GenderType.Female;
            dilini.HeightCm = 162;
            dilini.Trainer = trainerJohn;

            var amal = NewUser("Amal Rodrigo", "amal@healthtrack.com", "Client@123", UserRole.Client);
            amal.DateOfBirth = new DateTime(1990, 1, 25);
            amal.Gender = GenderType.Male;
            amal.HeightCm = 180;
            amal.Trainer = trainerNimali;

            var sachini = NewUser("Sachini Herath", "sachini@healthtrack.com", "Client@123", UserRole.Client);
            sachini.HeightCm = 158;
            sachini.Gender = GenderType.Female;

            db.Users.AddRange(admin, trainerJohn, trainerNimali, pendingTrainer, kasun, dilini, amal, sachini);
            db.SaveChanges();

            // ---------------- Plans ----------------
            var kasunPlan = new WorkoutPlan
            {
                Title = "Fat loss & strength - Phase 1",
                Description = "Three strength days plus two cardio days. Progressive overload every week.",
                Goal = "Lose 5 kg and improve strength",
                Trainer = trainerJohn,
                Client = kasun,
                StartDate = today.AddDays(-21),
                EndDate = today.AddDays(35),
                Exercises = new List<WorkoutExercise>()
            };
            AddExercise(kasunPlan, "Barbell squat", DayOfWeek.Monday, 4, 8, 20);
            AddExercise(kasunPlan, "Bench press", DayOfWeek.Monday, 4, 8, 20);
            AddExercise(kasunPlan, "Treadmill intervals", DayOfWeek.Tuesday, 0, 0, 30);
            AddExercise(kasunPlan, "Deadlift", DayOfWeek.Wednesday, 3, 6, 25);
            AddExercise(kasunPlan, "Pull-ups", DayOfWeek.Wednesday, 3, 10, 15);
            AddExercise(kasunPlan, "Cycling", DayOfWeek.Thursday, 0, 0, 40);
            AddExercise(kasunPlan, "Overhead press", DayOfWeek.Friday, 4, 10, 20);
            AddExercise(kasunPlan, "Plank", DayOfWeek.Friday, 3, 1, 10);
            AddExercise(kasunPlan, "Brisk walk", DayOfWeek.Saturday, 0, 0, 45);
            AddExercise(kasunPlan, "Yoga stretch", DayOfWeek.Sunday, 0, 0, 30);

            var diliniPlan = new WorkoutPlan
            {
                Title = "Beginner full body",
                Description = "Build a consistent habit with light full-body sessions.",
                Goal = "Improve fitness and stamina",
                Trainer = trainerJohn,
                Client = dilini,
                StartDate = today.AddDays(-10),
                EndDate = today.AddDays(20),
                Exercises = new List<WorkoutExercise>()
            };
            foreach (DayOfWeek day in new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday })
            {
                AddExercise(diliniPlan, "Bodyweight squat", day, 3, 15, 10);
                AddExercise(diliniPlan, "Brisk walk", day, 0, 0, 30);
            }

            var kasunDiet = new DietPlan
            {
                Title = "Balanced 2000 kcal plan",
                Description = "High protein, moderate carbs. Drink at least 3 litres of water a day.",
                DailyCalorieTarget = 2000,
                Trainer = trainerNimali,
                Client = kasun,
                StartDate = today.AddDays(-21),
                EndDate = today.AddDays(35),
                Items = new List<DietPlanItem>
                {
                    new DietPlanItem { MealType = MealType.Breakfast, Description = "Oats with banana and 2 boiled eggs", Calories = 450, ScheduledTime = new TimeSpan(7, 30, 0) },
                    new DietPlanItem { MealType = MealType.Lunch, Description = "Red rice, chicken curry, dhal and green salad", Calories = 700, ScheduledTime = new TimeSpan(12, 30, 0) },
                    new DietPlanItem { MealType = MealType.Snack, Description = "Greek yoghurt and a handful of nuts", Calories = 250, ScheduledTime = new TimeSpan(16, 0, 0) },
                    new DietPlanItem { MealType = MealType.Dinner, Description = "Grilled fish, vegetables and a small portion of rice", Calories = 600, ScheduledTime = new TimeSpan(19, 30, 0) }
                }
            };

            var amalDiet = new DietPlan
            {
                Title = "Heart healthy plan",
                Description = "Low sodium, high fibre.",
                DailyCalorieTarget = 2200,
                Trainer = trainerNimali,
                Client = amal,
                StartDate = today.AddDays(-5),
                EndDate = today.AddDays(25),
                Items = new List<DietPlanItem>
                {
                    new DietPlanItem { MealType = MealType.Breakfast, Description = "Whole-grain bread, avocado and tea without sugar", Calories = 500, ScheduledTime = new TimeSpan(8, 0, 0) },
                    new DietPlanItem { MealType = MealType.Lunch, Description = "Brown rice, vegetable curries and fish", Calories = 800, ScheduledTime = new TimeSpan(13, 0, 0) },
                    new DietPlanItem { MealType = MealType.Dinner, Description = "String hoppers with dhal and pol sambol (light)", Calories = 700, ScheduledTime = new TimeSpan(20, 0, 0) }
                }
            };

            db.WorkoutPlans.AddRange(kasunPlan, diliniPlan);
            db.DietPlans.AddRange(kasunDiet, amalDiet);
            db.SaveChanges();

            // ---------------- Health metrics (weekly) ----------------
            SeedMetrics(db, kasun, 84.0, -0.45, 10, random);
            SeedMetrics(db, dilini, 58.0, -0.1, 4, random);
            SeedMetrics(db, amal, 92.0, -0.3, 6, random);

            // ---------------- Workout & meal logs ----------------
            string[] kasunExercises = { "Barbell squat", "Bench press", "Treadmill intervals", "Deadlift", "Cycling", "Brisk walk", "Plank" };
            SeedWorkouts(db, kasun, kasunPlan.Id, kasunExercises, 30, 0.75, random);
            SeedWorkouts(db, dilini, diliniPlan.Id, new[] { "Bodyweight squat", "Brisk walk", "Yoga stretch" }, 14, 0.5, random);
            SeedWorkouts(db, amal, null, new[] { "Brisk walk", "Swimming", "Cycling" }, 20, 0.4, random);

            SeedMeals(db, kasun, 21, random);
            SeedMeals(db, amal, 10, random);

            // ---------------- Appointments ----------------
            db.Appointments.AddRange(
                new Appointment
                {
                    Client = kasun, Trainer = trainerJohn, Type = AppointmentType.ProgressReview,
                    ScheduledAt = today.AddDays(1).AddHours(10), DurationMinutes = 45,
                    Reason = "Monthly progress review and plan adjustment", Status = AppointmentStatus.Approved
                },
                new Appointment
                {
                    Client = kasun, Trainer = trainerNimali, Type = AppointmentType.NutritionConsultation,
                    ScheduledAt = today.AddDays(4).AddHours(15), DurationMinutes = 30,
                    Reason = "Questions about pre-workout meals", Status = AppointmentStatus.Pending
                },
                new Appointment
                {
                    Client = dilini, Trainer = trainerJohn, Type = AppointmentType.TrainingConsultation,
                    ScheduledAt = today.AddDays(2).AddHours(17), DurationMinutes = 60,
                    Reason = "Would like to learn correct squat form", Status = AppointmentStatus.Pending
                },
                new Appointment
                {
                    Client = kasun, Trainer = trainerJohn, Type = AppointmentType.TrainingConsultation,
                    ScheduledAt = today.AddDays(-14).AddHours(9), DurationMinutes = 60,
                    Reason = "Initial assessment", Status = AppointmentStatus.Completed,
                    TrainerNotes = "Baseline measurements taken. Good mobility."
                });

            // ---------------- Feedback ----------------
            db.Feedbacks.AddRange(
                new Feedback { Trainer = trainerJohn, Client = kasun, Message = "Great consistency this week! Increase squat weight by 2.5 kg next session.", CreatedAt = DateTime.Now.AddDays(-2) },
                new Feedback { Trainer = trainerNimali, Client = kasun, Message = "Protein intake looks good. Try to cut down on evening snacks.", CreatedAt = DateTime.Now.AddDays(-5) });

            // ---------------- Notifications ----------------
            db.Notifications.AddRange(
                new Notification { User = kasun, Title = "Welcome to HealthTrack", Message = "Your trainer John Perera has created your first workout plan.", Type = NotificationType.Plan, Link = "/Client/MyPlans", CreatedAt = DateTime.Now.AddDays(-21) },
                new Notification { User = trainerJohn, Title = "New appointment request", Message = "Dilini Wickramasinghe requested a training consultation.", Type = NotificationType.Appointment, Link = "/Appointments" },
                new Notification { User = admin, Title = "Trainer awaiting approval", Message = "Ravi Jayasinghe registered as a trainer and needs approval.", Type = NotificationType.Account, Link = "/Admin/Dashboard" });

            // ---------------- Activity (logins over the last two weeks) ----------------
            var activeUsers = new[] { kasun, dilini, amal, trainerJohn, trainerNimali, admin };
            for (int d = 13; d >= 0; d--)
            {
                foreach (var user in activeUsers)
                {
                    if (random.NextDouble() < 0.55)
                    {
                        db.ActivityLogs.Add(new ActivityLog
                        {
                            User = user, Action = "Login", IpAddress = "127.0.0.1",
                            Timestamp = today.AddDays(-d).AddHours(7 + random.Next(0, 13)).AddMinutes(random.Next(0, 60))
                        });
                    }
                }
            }

            db.SaveChanges();
        }

        private static User NewUser(string name, string email, string password, UserRole role) => new User
        {
            FullName = name,
            Email = email.ToLowerInvariant(),
            PasswordHash = SecurePasswordHasher.Hash(password),
            Role = role,
            IsActive = true,
            IsApproved = true,
            CreatedAt = DateTime.Now.AddDays(-60)
        };

        private static void AddExercise(WorkoutPlan plan, string name, DayOfWeek day, int sets, int reps, int minutes)
        {
            plan.Exercises.Add(new WorkoutExercise { Name = name, Day = day, Sets = sets, Reps = reps, DurationMinutes = minutes });
        }

        private static void SeedMetrics(AppDbContext db, User client, double startWeight, double weeklyChange, int weeks, Random random)
        {
            double height = client.HeightCm ?? 170;
            for (int w = weeks - 1; w >= 0; w--)
            {
                double weight = Math.Round(startWeight + weeklyChange * (weeks - 1 - w) + (random.NextDouble() - 0.5) * 0.6, 1);
                db.HealthMetrics.Add(new HealthMetric
                {
                    Client = client,
                    RecordedAt = DateTime.Today.AddDays(-7 * w).AddHours(7),
                    WeightKg = weight,
                    HeightCm = height,
                    Bmi = HealthCalculator.CalculateBmi(weight, height),
                    RestingHeartRate = 62 + random.Next(0, 14),
                    SystolicBp = 112 + random.Next(0, 18),
                    DiastolicBp = 72 + random.Next(0, 12),
                    SleepHours = Math.Round(6 + random.NextDouble() * 2, 1)
                });
            }
        }

        private static void SeedWorkouts(AppDbContext db, User client, int? planId, string[] exercises, int days, double probability, Random random)
        {
            for (int d = days; d >= 1; d--)
            {
                if (random.NextDouble() > probability) continue;
                var intensity = (WorkoutIntensity)random.Next(0, 3);
                int minutes = 20 + random.Next(0, 5) * 10;
                db.WorkoutLogs.Add(new WorkoutLog
                {
                    Client = client,
                    WorkoutPlanId = planId,
                    Date = DateTime.Today.AddDays(-d),
                    ExerciseName = exercises[random.Next(exercises.Length)],
                    DurationMinutes = minutes,
                    Intensity = intensity,
                    CaloriesBurned = HealthCalculator.EstimateCaloriesBurned(minutes, intensity)
                });
            }
        }

        private static void SeedMeals(AppDbContext db, User client, int days, Random random)
        {
            string[] breakfasts = { "Oats with banana", "String hoppers and dhal", "Egg sandwich", "Milk rice with lunu miris" };
            string[] lunches = { "Rice and chicken curry", "Rice and fish curry", "Vegetable fried rice", "Kottu (half portion)" };
            string[] dinners = { "Grilled fish and vegetables", "Hoppers with egg", "Chapati with dhal", "Chicken soup" };

            for (int d = days; d >= 1; d--)
            {
                var day = DateTime.Today.AddDays(-d);
                db.MealLogs.Add(new MealLog { Client = client, LoggedAt = day.AddHours(7.5), MealType = MealType.Breakfast, FoodItems = breakfasts[random.Next(breakfasts.Length)], Calories = 350 + random.Next(0, 200), ProteinG = 15 + random.Next(0, 15), CarbsG = 40 + random.Next(0, 30), FatG = 8 + random.Next(0, 10) });
                db.MealLogs.Add(new MealLog { Client = client, LoggedAt = day.AddHours(12.5), MealType = MealType.Lunch, FoodItems = lunches[random.Next(lunches.Length)], Calories = 600 + random.Next(0, 300), ProteinG = 25 + random.Next(0, 20), CarbsG = 70 + random.Next(0, 40), FatG = 15 + random.Next(0, 15) });
                db.MealLogs.Add(new MealLog { Client = client, LoggedAt = day.AddHours(19.5), MealType = MealType.Dinner, FoodItems = dinners[random.Next(dinners.Length)], Calories = 450 + random.Next(0, 250), ProteinG = 20 + random.Next(0, 20), CarbsG = 40 + random.Next(0, 30), FatG = 10 + random.Next(0, 12) });
            }
        }
    }
}
