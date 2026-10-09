using HealthTrack.Data;
using HealthTrack.Helpers;
using HealthTrack.Models;
using HealthTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Controllers
{
    /// <summary>Trend charts for weight, BMI, heart rate, calories and workout volume.</summary>
    [Authorize]
    public class ProgressController : Controller
    {
        private readonly AppDbContext _db;

        public ProgressController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index(int? clientId, int days = 90)
        {
            int userId = User.GetUserId();
            int targetId;

            if (User.IsInRole(nameof(UserRole.Client)))
            {
                targetId = userId; // clients only ever see their own data
            }
            else
            {
                if (!clientId.HasValue)
                {
                    TempData["Error"] = "Choose a client to see their progress.";
                    return User.IsInRole(nameof(UserRole.Trainer))
                        ? RedirectToAction("Clients", "Trainer")
                        : RedirectToAction("Users", "Admin", new { role = UserRole.Client });
                }

                targetId = clientId.Value;
                if (User.IsInRole(nameof(UserRole.Trainer)) &&
                    !await _db.Users.AnyAsync(u => u.Id == targetId && u.TrainerId == userId))
                    return Forbid();
            }

            var client = await _db.Users
                .Where(u => u.Id == targetId && u.Role == UserRole.Client)
                .Select(u => new { u.Id, u.FullName })
                .FirstOrDefaultAsync();
            if (client == null) return NotFound();

            days = Math.Clamp(days, 7, 365);
            var from = DateTime.Today.AddDays(-days + 1);

            var metrics = await _db.HealthMetrics
                .Where(m => m.ClientId == targetId && m.RecordedAt >= from)
                .OrderBy(m => m.RecordedAt)
                .Select(m => new { m.RecordedAt, m.WeightKg, m.Bmi, m.RestingHeartRate, m.SystolicBp, m.DiastolicBp })
                .ToListAsync();

            var workouts = await _db.WorkoutLogs
                .Where(w => w.ClientId == targetId && w.Date >= from)
                .Select(w => new { w.Date, w.DurationMinutes, w.CaloriesBurned })
                .ToListAsync();

            var meals = await _db.MealLogs
                .Where(m => m.ClientId == targetId && m.LoggedAt >= from)
                .Select(m => new { m.LoggedAt, m.Calories })
                .ToListAsync();

            var model = new ProgressViewModel
            {
                ClientId = client.Id,
                ClientName = client.FullName,
                ViewedByStaff = !User.IsInRole(nameof(UserRole.Client)),
                Days = days,
                StartWeight = metrics.FirstOrDefault()?.WeightKg,
                CurrentWeight = metrics.LastOrDefault()?.WeightKg,
                CurrentBmi = metrics.LastOrDefault()?.Bmi,
                TotalWorkouts = workouts.Count,
                TotalMinutes = workouts.Sum(w => w.DurationMinutes),
                TotalCaloriesBurned = workouts.Sum(w => w.CaloriesBurned),
                MetricLabels = metrics.Select(m => m.RecordedAt.ToString("dd MMM")).ToList(),
                WeightValues = metrics.Select(m => m.WeightKg).ToList(),
                BmiValues = metrics.Select(m => m.Bmi).ToList(),
                HeartRateValues = metrics.Select(m => m.RestingHeartRate).ToList()
            };

            if (model.StartWeight.HasValue && model.CurrentWeight.HasValue)
                model.WeightChange = Math.Round(model.CurrentWeight.Value - model.StartWeight.Value, 1);

            var lastBp = metrics.LastOrDefault(m => m.SystolicBp.HasValue);
            model.LatestBloodPressure = lastBp != null ? $"{lastBp.SystolicBp}/{lastBp.DiastolicBp}" : null;

            var daysWithMeals = meals.Select(m => m.LoggedAt.Date).Distinct().Count();
            model.AverageDailyIntake = daysWithMeals > 0 ? meals.Sum(m => m.Calories) / daysWithMeals : 0;

            // Daily calories in vs out
            for (var day = from; day <= DateTime.Today; day = day.AddDays(1))
            {
                model.DailyLabels.Add(day.ToString("dd MMM"));
                model.CaloriesBurnedDaily.Add(workouts.Where(w => w.Date.Date == day).Sum(w => w.CaloriesBurned));
                model.CaloriesConsumedDaily.Add(meals.Where(m => m.LoggedAt.Date == day).Sum(m => m.Calories));
            }

            // Weekly workout minutes
            for (var week = from.StartOfWeek(); week <= DateTime.Today; week = week.AddDays(7))
            {
                var weekEnd = week.AddDays(7);
                model.WeekLabels.Add("w/c " + week.ToString("dd MMM"));
                model.WorkoutMinutesWeekly.Add(workouts.Where(w => w.Date >= week && w.Date < weekEnd).Sum(w => w.DurationMinutes));
            }

            return View(model);
        }
    }
}
