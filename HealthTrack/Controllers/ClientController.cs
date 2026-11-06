using HealthTrack.Data;
using HealthTrack.Helpers;
using HealthTrack.Models;
using HealthTrack.Services;
using HealthTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Controllers
{
    /// <summary>Everything a client does with their own data: dashboard, workout/meal logs, health metrics and plans.</summary>
    [Authorize(Roles = nameof(UserRole.Client))]
    public class ClientController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IActivityLogger _activity;
        private readonly INotificationService _notifications;
        private readonly ILogger<ClientController> _logger;

        public ClientController(AppDbContext db, IActivityLogger activity, INotificationService notifications, ILogger<ClientController> logger)
        {
            _db = db;
            _activity = activity;
            _notifications = notifications;
            _logger = logger;
        }

        private int CurrentUserId => User.GetUserId();

        // ================================================================== DASHBOARD
        public async Task<IActionResult> Dashboard()
        {
            int id = CurrentUserId;
            var now = DateTime.Now;
            var today = DateTime.Today;
            var weekStart = today.StartOfWeek();
            var dayOfWeek = today.DayOfWeek;

            var client = await _db.Users.Include(u => u.Trainer).FirstOrDefaultAsync(u => u.Id == id);
            if (client == null) return NotFound();

            var metrics = await _db.HealthMetrics.Where(m => m.ClientId == id)
                .OrderBy(m => m.RecordedAt).ToListAsync();

            var weekWorkouts = await _db.WorkoutLogs
                .Where(w => w.ClientId == id && w.Date >= weekStart).ToListAsync();

            var workoutPlan = await _db.WorkoutPlans.Include(p => p.Exercises).Include(p => p.Trainer)
                .Where(p => p.ClientId == id && p.IsActive && p.EndDate >= today)
                .OrderByDescending(p => p.StartDate).FirstOrDefaultAsync();

            var dietPlan = await _db.DietPlans.Include(p => p.Items).Include(p => p.Trainer)
                .Where(p => p.ClientId == id && p.IsActive && p.EndDate >= today)
                .OrderByDescending(p => p.StartDate).FirstOrDefaultAsync();

            var model = new ClientDashboardViewModel
            {
                Client = client,
                Trainer = client.Trainer,
                LatestMetric = metrics.LastOrDefault(),
                WeightChange = metrics.Count >= 2 ? Math.Round(metrics.Last().WeightKg - metrics.First().WeightKg, 1) : null,
                WorkoutsThisWeek = weekWorkouts.Count,
                MinutesThisWeek = weekWorkouts.Sum(w => w.DurationMinutes),
                CaloriesBurnedThisWeek = weekWorkouts.Sum(w => w.CaloriesBurned),
                CaloriesConsumedToday = await _db.MealLogs
                    .Where(m => m.ClientId == id && m.LoggedAt >= today)
                    .SumAsync(m => (int?)m.Calories) ?? 0,
                ActiveWorkoutPlan = workoutPlan,
                TodaysExercises = workoutPlan?.Exercises.Where(e => e.Day == dayOfWeek).ToList() ?? new List<WorkoutExercise>(),
                ActiveDietPlan = dietPlan,
                UpcomingAppointments = await _db.Appointments.Include(a => a.Trainer)
                    .Where(a => a.ClientId == id && a.ScheduledAt >= now
                                && (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Approved))
                    .OrderBy(a => a.ScheduledAt).Take(4).ToListAsync(),
                RecentFeedback = await _db.Feedbacks.Include(f => f.Trainer)
                    .Where(f => f.ClientId == id).OrderByDescending(f => f.CreatedAt).Take(3).ToListAsync(),
                RecentWorkouts = await _db.WorkoutLogs
                    .Where(w => w.ClientId == id).OrderByDescending(w => w.Date).ThenByDescending(w => w.CreatedAt).Take(5).ToListAsync(),
                WeightLabels = metrics.TakeLast(12).Select(m => m.RecordedAt.ToString("dd MMM")).ToList(),
                WeightValues = metrics.TakeLast(12).Select(m => m.WeightKg).ToList()
            };

            return View(model);
        }

        // ================================================================== WORKOUTS
        public async Task<IActionResult> WorkoutLogs()
        {
            var from = DateTime.Today.AddDays(-90);
            var logs = await _db.WorkoutLogs
                .Where(w => w.ClientId == CurrentUserId && w.Date >= from)
                .OrderByDescending(w => w.Date).ThenByDescending(w => w.CreatedAt)
                .ToListAsync();
            return View(logs);
        }

        [HttpGet]
        public async Task<IActionResult> LogWorkout(string exercise = null)
        {
            var model = new WorkoutLogFormViewModel
            {
                ExerciseName = exercise,
                SuggestedExercises = await GetSuggestedExercisesAsync()
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> LogWorkout(WorkoutLogFormViewModel model)
        {
            if (model.Date.Date > DateTime.Today)
                ModelState.AddModelError(nameof(model.Date), "The workout date can't be in the future.");
            if (model.Date.Date < DateTime.Today.AddYears(-1))
                ModelState.AddModelError(nameof(model.Date), "Workouts older than a year can't be logged.");

            if (!ModelState.IsValid)
            {
                model.SuggestedExercises = await GetSuggestedExercisesAsync();
                return View(model);
            }

            int? activePlanId = await _db.WorkoutPlans
                .Where(p => p.ClientId == CurrentUserId && p.IsActive)
                .OrderByDescending(p => p.StartDate)
                .Select(p => (int?)p.Id)
                .FirstOrDefaultAsync();

            var log = new WorkoutLog
            {
                ClientId = CurrentUserId,
                WorkoutPlanId = activePlanId,
                Date = model.Date.Date,
                ExerciseName = model.ExerciseName.Trim(),
                DurationMinutes = model.DurationMinutes,
                Intensity = model.Intensity,
                CaloriesBurned = model.CaloriesBurned ?? HealthCalculator.EstimateCaloriesBurned(model.DurationMinutes, model.Intensity),
                Sets = model.Sets,
                Reps = model.Reps,
                Notes = model.Notes?.Trim()
            };

            try
            {
                _db.WorkoutLogs.Add(log);
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Could not save workout log");
                ModelState.AddModelError(string.Empty, "The workout couldn't be saved. Try again.");
                model.SuggestedExercises = await GetSuggestedExercisesAsync();
                return View(model);
            }

            await _activity.LogAsync(CurrentUserId, "LogWorkout", $"{log.ExerciseName}, {log.DurationMinutes} min");
            TempData["Success"] = $"Workout logged: {log.ExerciseName}, {log.DurationMinutes} min, {log.CaloriesBurned} kcal.";
            return RedirectToAction(nameof(WorkoutLogs));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteWorkout(int id)
        {
            var log = await _db.WorkoutLogs.FirstOrDefaultAsync(w => w.Id == id && w.ClientId == CurrentUserId);
            if (log == null) return NotFound();

            _db.WorkoutLogs.Remove(log);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Workout entry deleted.";
            return RedirectToAction(nameof(WorkoutLogs));
        }

        // ================================================================== MEALS
        public async Task<IActionResult> MealLogs()
        {
            var from = DateTime.Today.AddDays(-30);
            var meals = await _db.MealLogs
                .Where(m => m.ClientId == CurrentUserId && m.LoggedAt >= from)
                .OrderByDescending(m => m.LoggedAt)
                .ToListAsync();

            ViewBag.CalorieTarget = await _db.DietPlans
                .Where(p => p.ClientId == CurrentUserId && p.IsActive)
                .OrderByDescending(p => p.StartDate)
                .Select(p => (int?)p.DailyCalorieTarget)
                .FirstOrDefaultAsync();

            return View(meals);
        }

        [HttpGet]
        public async Task<IActionResult> LogMeal()
        {
            var model = new MealLogFormViewModel
            {
                LoggedAt = DateTime.Now,
                MealType = GuessMealType(DateTime.Now),
                ActiveDietPlan = await GetActiveDietPlanAsync()
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> LogMeal(MealLogFormViewModel model)
        {
            if (model.LoggedAt > DateTime.Now.AddMinutes(5))
                ModelState.AddModelError(nameof(model.LoggedAt), "The meal time can't be in the future.");

            if (!ModelState.IsValid)
            {
                model.ActiveDietPlan = await GetActiveDietPlanAsync();
                return View(model);
            }

            var meal = new MealLog
            {
                ClientId = CurrentUserId,
                LoggedAt = model.LoggedAt,
                MealType = model.MealType,
                FoodItems = model.FoodItems.Trim(),
                Calories = model.Calories,
                ProteinG = model.ProteinG,
                CarbsG = model.CarbsG,
                FatG = model.FatG
            };

            try
            {
                _db.MealLogs.Add(meal);
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Could not save meal log");
                ModelState.AddModelError(string.Empty, "The meal couldn't be saved. Try again.");
                model.ActiveDietPlan = await GetActiveDietPlanAsync();
                return View(model);
            }

            await _activity.LogAsync(CurrentUserId, "LogMeal", $"{meal.MealType}, {meal.Calories} kcal");
            TempData["Success"] = $"{meal.MealType} logged ({meal.Calories} kcal).";
            return RedirectToAction(nameof(MealLogs));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMeal(int id)
        {
            var meal = await _db.MealLogs.FirstOrDefaultAsync(m => m.Id == id && m.ClientId == CurrentUserId);
            if (meal == null) return NotFound();

            _db.MealLogs.Remove(meal);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Meal entry deleted.";
            return RedirectToAction(nameof(MealLogs));
        }

        // ================================================================== HEALTH METRICS
        public async Task<IActionResult> Metrics()
        {
            var metrics = await _db.HealthMetrics
                .Where(m => m.ClientId == CurrentUserId)
                .OrderByDescending(m => m.RecordedAt)
                .ToListAsync();
            return View(metrics);
        }

        [HttpGet]
        public async Task<IActionResult> AddMetric()
        {
            var user = await _db.Users.FindAsync(CurrentUserId);
            var lastMetric = await _db.HealthMetrics.Where(m => m.ClientId == CurrentUserId)
                .OrderByDescending(m => m.RecordedAt).FirstOrDefaultAsync();

            return View(new HealthMetricFormViewModel
            {
                RecordedAt = DateTime.Now,
                HeightCm = user?.HeightCm ?? lastMetric?.HeightCm,
                WeightKg = lastMetric?.WeightKg
            });
        }

        [HttpPost]
        public async Task<IActionResult> AddMetric(HealthMetricFormViewModel model)
        {
            if (model.RecordedAt > DateTime.Now.AddMinutes(5))
                ModelState.AddModelError(nameof(model.RecordedAt), "The date can't be in the future.");

            if (model.SystolicBp.HasValue != model.DiastolicBp.HasValue)
                ModelState.AddModelError(nameof(model.DiastolicBp), "Enter both blood pressure values, or leave both empty.");
            else if (model.SystolicBp.HasValue && model.SystolicBp <= model.DiastolicBp)
                ModelState.AddModelError(nameof(model.SystolicBp), "Systolic pressure must be higher than diastolic.");

            if (!ModelState.IsValid) return View(model);

            double weight = model.WeightKg.Value;
            double height = model.HeightCm.Value;

            var metric = new HealthMetric
            {
                ClientId = CurrentUserId,
                RecordedAt = model.RecordedAt,
                WeightKg = Math.Round(weight, 1),
                HeightCm = Math.Round(height, 1),
                Bmi = HealthCalculator.CalculateBmi(weight, height),
                BodyFatPercent = model.BodyFatPercent,
                RestingHeartRate = model.RestingHeartRate,
                SystolicBp = model.SystolicBp,
                DiastolicBp = model.DiastolicBp,
                SleepHours = model.SleepHours,
                Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim()
            };

            var user = await _db.Users.FindAsync(CurrentUserId);
            if (user != null) user.HeightCm = metric.HeightCm;

            try
            {
                _db.HealthMetrics.Add(metric);
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Could not save health metric");
                ModelState.AddModelError(string.Empty, "The measurement couldn't be saved. Try again.");
                return View(model);
            }

            await _activity.LogAsync(CurrentUserId, "AddMetric", $"Weight {metric.WeightKg} kg, BMI {metric.Bmi}");

            // Let the trainer know about readings that need attention.
            if (user?.TrainerId != null && (HealthCalculator.IsHighBloodPressure(metric.SystolicBp, metric.DiastolicBp) || metric.Bmi >= 30))
            {
                await _notifications.NotifyAsync(user.TrainerId.Value, "Health reading needs attention",
                    $"{user.FullName} recorded BMI {metric.Bmi}" +
                    (metric.SystolicBp.HasValue ? $" and blood pressure {metric.SystolicBp}/{metric.DiastolicBp}." : "."),
                    NotificationType.HealthAlert, $"/Trainer/ClientDetails/{user.Id}");
            }

            TempData["Success"] = $"Measurement saved. Your BMI is {metric.Bmi} ({HealthCalculator.BmiCategory(metric.Bmi)}).";
            return RedirectToAction(nameof(Metrics));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMetric(int id)
        {
            var metric = await _db.HealthMetrics.FirstOrDefaultAsync(m => m.Id == id && m.ClientId == CurrentUserId);
            if (metric == null) return NotFound();

            _db.HealthMetrics.Remove(metric);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Measurement deleted.";
            return RedirectToAction(nameof(Metrics));
        }

        // ================================================================== PLANS
        public async Task<IActionResult> MyPlans()
        {
            int id = CurrentUserId;
            var model = new MyPlansViewModel
            {
                WorkoutPlans = await _db.WorkoutPlans.Include(p => p.Exercises).Include(p => p.Trainer)
                    .Where(p => p.ClientId == id).OrderByDescending(p => p.IsActive).ThenByDescending(p => p.StartDate).ToListAsync(),
                DietPlans = await _db.DietPlans.Include(p => p.Items).Include(p => p.Trainer)
                    .Where(p => p.ClientId == id).OrderByDescending(p => p.IsActive).ThenByDescending(p => p.StartDate).ToListAsync(),
                Feedback = await _db.Feedbacks.Include(f => f.Trainer)
                    .Where(f => f.ClientId == id).OrderByDescending(f => f.CreatedAt).ToListAsync()
            };
            return View(model);
        }

        // ================================================================== HELPERS
        private async Task<List<string>> GetSuggestedExercisesAsync()
        {
            var planned = await _db.WorkoutExercises
                .Where(e => e.WorkoutPlan.ClientId == CurrentUserId && e.WorkoutPlan.IsActive)
                .Select(e => e.Name).Distinct().ToListAsync();

            var recent = await _db.WorkoutLogs
                .Where(w => w.ClientId == CurrentUserId)
                .OrderByDescending(w => w.Date)
                .Select(w => w.ExerciseName).Take(30).ToListAsync();

            return planned.Concat(recent)
                .Concat(new[] { "Walking", "Running", "Cycling", "Swimming", "Yoga", "Skipping" })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private Task<DietPlan> GetActiveDietPlanAsync() =>
            _db.DietPlans.Include(p => p.Items)
                .Where(p => p.ClientId == CurrentUserId && p.IsActive)
                .OrderByDescending(p => p.StartDate)
                .FirstOrDefaultAsync();

        private static MealType GuessMealType(DateTime time) => time.Hour switch
        {
            < 11 => MealType.Breakfast,
            < 15 => MealType.Lunch,
            < 18 => MealType.Snack,
            _ => MealType.Dinner
        };
    }
}
