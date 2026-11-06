using HealthTrack.Data;
using HealthTrack.Models;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Services
{
    /// <summary>
    /// Hosted background service that periodically creates reminders:
    ///  1. Appointments starting within the next 24 hours (client and trainer).
    ///  2. Workouts scheduled today in the client's active plan but not yet logged.
    ///  3. Meals from the active diet plan whose time has passed but that were not logged.
    /// Each reminder is sent at most once per day / per appointment.
    /// </summary>
    public class ReminderBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ReminderBackgroundService> _logger;
        private readonly TimeSpan _interval;

        public ReminderBackgroundService(IServiceScopeFactory scopeFactory, ILogger<ReminderBackgroundService> logger, IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            int minutes = configuration.GetValue<int?>("Reminders:IntervalMinutes") ?? 15;
            _interval = TimeSpan.FromMinutes(Math.Max(1, minutes));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Give the application time to create and seed the database.
            try { await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken); }
            catch (TaskCanceledException) { return; }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    int created = await GenerateRemindersAsync(db, stoppingToken);
                    if (created > 0) _logger.LogInformation("Reminder service created {Count} reminder(s).", created);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Reminder generation failed.");
                }

                try { await Task.Delay(_interval, stoppingToken); }
                catch (TaskCanceledException) { break; }
            }
        }

        /// <summary>Creates any due reminders and returns how many were created. Also called from the Notifications page.</summary>
        public static async Task<int> GenerateRemindersAsync(AppDbContext db, CancellationToken cancellationToken = default)
        {
            var now = DateTime.Now;
            var today = now.Date;
            var todayDayOfWeek = today.DayOfWeek;
            var in24Hours = now.AddHours(24);
            int created = 0;

            // 1. Appointment reminders
            var upcoming = await db.Appointments
                .Include(a => a.Client)
                .Include(a => a.Trainer)
                .Where(a => a.Status == AppointmentStatus.Approved && !a.ReminderSent
                            && a.ScheduledAt > now && a.ScheduledAt <= in24Hours)
                .ToListAsync(cancellationToken);

            foreach (var appointment in upcoming)
            {
                string when = appointment.ScheduledAt.ToString("dddd dd MMM 'at' hh:mm tt");
                db.Notifications.Add(new Notification
                {
                    UserId = appointment.ClientId,
                    Title = "Appointment reminder",
                    Message = $"You have an appointment with {appointment.Trainer.FullName} on {when}.",
                    Type = NotificationType.Appointment,
                    Link = "/Appointments"
                });
                db.Notifications.Add(new Notification
                {
                    UserId = appointment.TrainerId,
                    Title = "Appointment reminder",
                    Message = $"Session with {appointment.Client.FullName} on {when}.",
                    Type = NotificationType.Appointment,
                    Link = "/Appointments"
                });
                appointment.ReminderSent = true;
                created += 2;
            }

            // 2. Workout reminders (only after 6 AM)
            if (now.Hour >= 6)
            {
                var clientsWithWorkoutToday = await db.WorkoutExercises
                    .Where(e => e.Day == todayDayOfWeek && e.WorkoutPlan.IsActive
                                && e.WorkoutPlan.StartDate <= today && e.WorkoutPlan.EndDate >= today)
                    .Select(e => e.WorkoutPlan.ClientId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                foreach (var clientId in clientsWithWorkoutToday)
                {
                    bool loggedToday = await db.WorkoutLogs.AnyAsync(w => w.ClientId == clientId && w.Date >= today, cancellationToken);
                    bool remindedToday = await db.Notifications.AnyAsync(n => n.UserId == clientId && n.Type == NotificationType.WorkoutReminder && n.CreatedAt >= today, cancellationToken);
                    if (loggedToday || remindedToday) continue;

                    var names = await db.WorkoutExercises
                        .Where(e => e.WorkoutPlan.ClientId == clientId && e.WorkoutPlan.IsActive && e.Day == todayDayOfWeek)
                        .Select(e => e.Name)
                        .ToListAsync(cancellationToken);

                    db.Notifications.Add(new Notification
                    {
                        UserId = clientId,
                        Title = "Workout reminder",
                        Message = $"Today's session: {string.Join(", ", names)}. Log it when you're done.",
                        Type = NotificationType.WorkoutReminder,
                        Link = "/Client/LogWorkout"
                    });
                    created++;
                }
            }

            // 3. Meal reminders
            var mealItems = await db.DietPlanItems
                .Include(i => i.DietPlan)
                .Where(i => i.DietPlan.IsActive && i.DietPlan.StartDate <= today && i.DietPlan.EndDate >= today)
                .ToListAsync(cancellationToken);

            foreach (var item in mealItems.Where(i => i.ScheduledTime <= now.TimeOfDay))
            {
                int clientId = item.DietPlan.ClientId;
                var mealType = item.MealType;
                string title = $"Meal reminder: {mealType}";

                bool logged = await db.MealLogs.AnyAsync(m => m.ClientId == clientId && m.MealType == mealType && m.LoggedAt >= today, cancellationToken);
                bool reminded = await db.Notifications.AnyAsync(n => n.UserId == clientId && n.Type == NotificationType.MealReminder && n.CreatedAt >= today && n.Title == title, cancellationToken);
                if (logged || reminded) continue;

                db.Notifications.Add(new Notification
                {
                    UserId = clientId,
                    Title = title,
                    Message = $"Planned at {DateTime.Today.Add(item.ScheduledTime):hh:mm tt}: {item.Description} (~{item.Calories} kcal). Remember to log it.",
                    Type = NotificationType.MealReminder,
                    Link = "/Client/LogMeal"
                });
                created++;
            }

            if (created > 0 || upcoming.Count > 0)
                await db.SaveChangesAsync(cancellationToken);

            return created;
        }
    }
}
