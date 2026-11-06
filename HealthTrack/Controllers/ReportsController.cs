using System.Text;
using HealthTrack.Data;
using HealthTrack.Helpers;
using HealthTrack.Models;
using HealthTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Controllers
{
    /// <summary>
    /// Reports for trainers (their own clients) and administrators (every client):
    /// client performance, popular routines, engagement statistics and trainer performance.
    /// </summary>
    [Authorize(Roles = nameof(UserRole.Trainer) + "," + nameof(UserRole.Admin))]
    public class ReportsController : Controller
    {
        private readonly AppDbContext _db;

        public ReportsController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index(DateTime? from, DateTime? to)
        {
            var (start, end) = NormaliseRange(from, to);
            return View(await BuildReportAsync(start, end));
        }

        /// <summary>Downloads the client performance table as a CSV file (opens in Excel).</summary>
        public async Task<IActionResult> ExportCsv(DateTime? from, DateTime? to)
        {
            var (start, end) = NormaliseRange(from, to);
            var report = await BuildReportAsync(start, end);

            var csv = new StringBuilder();
            csv.AppendLine($"HealthTrack client performance report,{start:yyyy-MM-dd} to {end:yyyy-MM-dd}");
            csv.AppendLine("Client,Trainer,Workouts,Minutes,Calories burned,Meals logged,Avg daily intake,Start weight,Current weight,Weight change,BMI,Active days,Engagement %");
            foreach (var r in report.ClientPerformance)
            {
                csv.AppendLine(string.Join(",", new object[]
                {
                    r.ClientName.ToCsvCell(), r.TrainerName.ToCsvCell(), r.Workouts, r.TotalMinutes, r.CaloriesBurned,
                    r.MealsLogged, r.AverageDailyIntake, r.StartWeight, r.CurrentWeight, r.WeightChange, r.CurrentBmi,
                    r.ActiveDays, r.EngagementPercent
                }));
            }

            byte[] bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
            return File(bytes, "text/csv", $"HealthTrack-report-{start:yyyyMMdd}-{end:yyyyMMdd}.csv");
        }

        // ------------------------------------------------------------------ REPORT BUILDER
        private async Task<ReportViewModel> BuildReportAsync(DateTime from, DateTime to)
        {
            int userId = User.GetUserId();
            bool isAdmin = User.IsInRole(nameof(UserRole.Admin));
            var endExclusive = to.AddDays(1);
            int totalDays = (int)(to - from).TotalDays + 1;

            // Scope: admin = all clients, trainer = own clients.
            var clientQuery = _db.Users.Where(u => u.Role == UserRole.Client);
            if (!isAdmin) clientQuery = clientQuery.Where(u => u.TrainerId == userId);

            var clients = await clientQuery
                .OrderBy(u => u.FullName)
                .Select(u => new { u.Id, u.FullName, TrainerName = u.Trainer != null ? u.Trainer.FullName : "Unassigned" })
                .ToListAsync();
            var ids = clients.Select(c => c.Id).ToList();

            var workouts = await _db.WorkoutLogs
                .Where(w => ids.Contains(w.ClientId) && w.Date >= from && w.Date < endExclusive)
                .Select(w => new { w.ClientId, w.Date, w.ExerciseName, w.DurationMinutes, w.CaloriesBurned })
                .ToListAsync();
            var meals = await _db.MealLogs
                .Where(m => ids.Contains(m.ClientId) && m.LoggedAt >= from && m.LoggedAt < endExclusive)
                .Select(m => new { m.ClientId, m.LoggedAt, m.Calories })
                .ToListAsync();
            var metrics = await _db.HealthMetrics
                .Where(m => ids.Contains(m.ClientId) && m.RecordedAt >= from && m.RecordedAt < endExclusive)
                .OrderBy(m => m.RecordedAt)
                .Select(m => new { m.ClientId, m.RecordedAt, m.WeightKg, m.Bmi })
                .ToListAsync();
            var logins = await _db.ActivityLogs
                .Where(a => a.UserId.HasValue && ids.Contains(a.UserId.Value) && a.Action == "Login"
                            && a.Timestamp >= from && a.Timestamp < endExclusive)
                .Select(a => new { a.UserId, a.Timestamp })
                .ToListAsync();

            var appointmentQuery = _db.Appointments.Where(a => a.ScheduledAt >= from && a.ScheduledAt < endExclusive);
            if (!isAdmin) appointmentQuery = appointmentQuery.Where(a => a.TrainerId == userId);
            var appointmentStatuses = await appointmentQuery.Select(a => a.Status).ToListAsync();

            var report = new ReportViewModel
            {
                From = from,
                To = to,
                IsAdmin = isAdmin,
                TotalClients = clients.Count,
                TotalWorkoutLogs = workouts.Count,
                TotalMealLogs = meals.Count,
                TotalMetricEntries = metrics.Count,
                Logins = logins.Count,
                AppointmentsByStatus = Enum.GetValues<AppointmentStatus>()
                    .ToDictionary(s => s, s => appointmentStatuses.Count(x => x == s))
            };

            // ---- Client performance ----
            foreach (var client in clients)
            {
                var cw = workouts.Where(w => w.ClientId == client.Id).ToList();
                var cm = meals.Where(m => m.ClientId == client.Id).ToList();
                var cMetrics = metrics.Where(m => m.ClientId == client.Id).ToList();

                var activeDays = cw.Select(w => w.Date.Date)
                    .Concat(cm.Select(m => m.LoggedAt.Date))
                    .Concat(cMetrics.Select(m => m.RecordedAt.Date))
                    .Distinct().Count();
                int mealDays = cm.Select(m => m.LoggedAt.Date).Distinct().Count();

                var row = new ClientPerformanceRow
                {
                    ClientId = client.Id,
                    ClientName = client.FullName,
                    TrainerName = client.TrainerName,
                    Workouts = cw.Count,
                    TotalMinutes = cw.Sum(w => w.DurationMinutes),
                    CaloriesBurned = cw.Sum(w => w.CaloriesBurned),
                    MealsLogged = cm.Count,
                    AverageDailyIntake = mealDays > 0 ? cm.Sum(m => m.Calories) / mealDays : 0,
                    StartWeight = cMetrics.FirstOrDefault()?.WeightKg,
                    CurrentWeight = cMetrics.LastOrDefault()?.WeightKg,
                    CurrentBmi = cMetrics.LastOrDefault()?.Bmi,
                    ActiveDays = activeDays,
                    EngagementPercent = totalDays > 0 ? (int)Math.Round(activeDays * 100.0 / totalDays) : 0
                };
                if (row.StartWeight.HasValue && row.CurrentWeight.HasValue && cMetrics.Count > 1)
                    row.WeightChange = Math.Round(row.CurrentWeight.Value - row.StartWeight.Value, 1);

                report.ClientPerformance.Add(row);
                if (activeDays > 0) report.ActiveClients++;
            }
            report.ClientPerformance = report.ClientPerformance.OrderByDescending(r => r.EngagementPercent).ThenBy(r => r.ClientName).ToList();

            // ---- Popular routines (what clients actually do) ----
            report.PopularRoutines = workouts
                .GroupBy(w => w.ExerciseName.Trim().ToLowerInvariant())
                .Select(g => new RoutinePopularityRow
                {
                    Name = g.First().ExerciseName.Trim(),
                    TimesLogged = g.Count(),
                    TotalMinutes = g.Sum(w => w.DurationMinutes),
                    UniqueClients = g.Select(w => w.ClientId).Distinct().Count()
                })
                .OrderByDescending(r => r.TimesLogged).ThenByDescending(r => r.TotalMinutes)
                .Take(10).ToList();

            // ---- Most assigned exercises (what trainers prescribe) ----
            var planExercises = await _db.WorkoutExercises
                .Where(e => ids.Contains(e.WorkoutPlan.ClientId) && e.WorkoutPlan.IsActive)
                .Select(e => new { e.Name, e.WorkoutPlan.ClientId, e.DurationMinutes })
                .ToListAsync();
            report.MostAssignedExercises = planExercises
                .GroupBy(e => e.Name.Trim().ToLowerInvariant())
                .Select(g => new RoutinePopularityRow
                {
                    Name = g.First().Name.Trim(),
                    TimesLogged = g.Count(),
                    TotalMinutes = g.Sum(e => e.DurationMinutes),
                    UniqueClients = g.Select(e => e.ClientId).Distinct().Count()
                })
                .OrderByDescending(r => r.UniqueClients).ThenByDescending(r => r.TimesLogged)
                .Take(10).ToList();

            // ---- Daily engagement (entries per day) ----
            for (var day = from; day <= to; day = day.AddDays(1))
            {
                report.EngagementLabels.Add(day.ToString("dd MMM"));
                report.EngagementValues.Add(
                    workouts.Count(w => w.Date.Date == day) +
                    meals.Count(m => m.LoggedAt.Date == day) +
                    metrics.Count(m => m.RecordedAt.Date == day));
            }

            // ---- Trainer performance (admin only) ----
            if (isAdmin)
            {
                var trainers = await _db.Users.Where(u => u.Role == UserRole.Trainer && u.IsApproved)
                    .Select(u => new { u.Id, u.FullName, u.Specialization, Clients = u.Clients.Count })
                    .ToListAsync();
                var workoutPlanCounts = await _db.WorkoutPlans.Where(p => p.CreatedAt >= from && p.CreatedAt < endExclusive)
                    .GroupBy(p => p.TrainerId).Select(g => new { g.Key, Count = g.Count() }).ToListAsync();
                var dietPlanCounts = await _db.DietPlans.Where(p => p.CreatedAt >= from && p.CreatedAt < endExclusive)
                    .GroupBy(p => p.TrainerId).Select(g => new { g.Key, Count = g.Count() }).ToListAsync();
                var feedbackCounts = await _db.Feedbacks.Where(f => f.CreatedAt >= from && f.CreatedAt < endExclusive)
                    .GroupBy(f => f.TrainerId).Select(g => new { g.Key, Count = g.Count() }).ToListAsync();
                var completedCounts = await _db.Appointments
                    .Where(a => a.Status == AppointmentStatus.Completed && a.ScheduledAt >= from && a.ScheduledAt < endExclusive)
                    .GroupBy(a => a.TrainerId).Select(g => new { g.Key, Count = g.Count() }).ToListAsync();

                report.TrainerPerformance = trainers.Select(t => new TrainerPerformanceRow
                {
                    TrainerName = t.FullName,
                    Specialization = t.Specialization,
                    Clients = t.Clients,
                    PlansCreated = (workoutPlanCounts.FirstOrDefault(x => x.Key == t.Id)?.Count ?? 0)
                                   + (dietPlanCounts.FirstOrDefault(x => x.Key == t.Id)?.Count ?? 0),
                    FeedbackGiven = feedbackCounts.FirstOrDefault(x => x.Key == t.Id)?.Count ?? 0,
                    AppointmentsCompleted = completedCounts.FirstOrDefault(x => x.Key == t.Id)?.Count ?? 0
                }).OrderByDescending(t => t.Clients).ToList();
            }

            return report;
        }

        private static (DateTime from, DateTime to) NormaliseRange(DateTime? from, DateTime? to)
        {
            var end = (to ?? DateTime.Today).Date;
            var start = (from ?? end.AddDays(-29)).Date;
            if (start > end) (start, end) = (end, start);
            if ((end - start).TotalDays > 366) start = end.AddDays(-366);
            return (start, end);
        }
    }
}
