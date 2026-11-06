using HealthTrack.Data;
using HealthTrack.Helpers;
using HealthTrack.Models;
using HealthTrack.Services;
using HealthTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Controllers
{
    /// <summary>
    /// Trainer area: monitor assigned clients, create workout / diet plans and give feedback.
    /// Every action checks that the client really belongs to the logged-in trainer.
    /// </summary>
    [Authorize(Roles = nameof(UserRole.Trainer))]
    public class TrainerController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IActivityLogger _activity;
        private readonly INotificationService _notifications;
        private readonly ILogger<TrainerController> _logger;

        public TrainerController(AppDbContext db, IActivityLogger activity, INotificationService notifications, ILogger<TrainerController> logger)
        {
            _db = db;
            _activity = activity;
            _notifications = notifications;
            _logger = logger;
        }

        private int TrainerId => User.GetUserId();

        // ================================================================== DASHBOARD
        public async Task<IActionResult> Dashboard()
        {
            var now = DateTime.Now;
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var clients = await BuildClientSummariesAsync();
            var clientIds = clients.Select(c => c.ClientId).ToList();

            var model = new TrainerDashboardViewModel
            {
                TrainerName = User.Identity?.Name,
                Clients = clients,
                PendingAppointments = await _db.Appointments.Include(a => a.Client)
                    .Where(a => a.TrainerId == TrainerId && a.Status == AppointmentStatus.Pending && a.ScheduledAt >= now)
                    .OrderBy(a => a.ScheduledAt).ToListAsync(),
                UpcomingAppointments = await _db.Appointments.Include(a => a.Client)
                    .Where(a => a.TrainerId == TrainerId && a.Status == AppointmentStatus.Approved && a.ScheduledAt >= now)
                    .OrderBy(a => a.ScheduledAt).Take(5).ToListAsync(),
                RecentClientWorkouts = await _db.WorkoutLogs.Include(w => w.Client)
                    .Where(w => clientIds.Contains(w.ClientId))
                    .OrderByDescending(w => w.Date).ThenByDescending(w => w.CreatedAt).Take(8).ToListAsync(),
                ActivePlanCount = await _db.WorkoutPlans.CountAsync(p => p.TrainerId == TrainerId && p.IsActive)
                                  + await _db.DietPlans.CountAsync(p => p.TrainerId == TrainerId && p.IsActive),
                FeedbackThisMonth = await _db.Feedbacks.CountAsync(f => f.TrainerId == TrainerId && f.CreatedAt >= monthStart)
            };

            return View(model);
        }

        // ================================================================== CLIENTS
        public async Task<IActionResult> Clients() => View(await BuildClientSummariesAsync());

        public async Task<IActionResult> ClientDetails(int id)
        {
            if (!await IsMyClientAsync(id)) return Forbid();

            var client = await _db.Users.FirstAsync(u => u.Id == id);
            var model = new ClientDetailsViewModel
            {
                Client = client,
                Metrics = await _db.HealthMetrics.Where(m => m.ClientId == id).OrderByDescending(m => m.RecordedAt).Take(10).ToListAsync(),
                Workouts = await _db.WorkoutLogs.Where(w => w.ClientId == id).OrderByDescending(w => w.Date).Take(10).ToListAsync(),
                Meals = await _db.MealLogs.Where(m => m.ClientId == id).OrderByDescending(m => m.LoggedAt).Take(10).ToListAsync(),
                WorkoutPlans = await _db.WorkoutPlans.Include(p => p.Exercises).Include(p => p.Trainer)
                    .Where(p => p.ClientId == id).OrderByDescending(p => p.IsActive).ThenByDescending(p => p.StartDate).ToListAsync(),
                DietPlans = await _db.DietPlans.Include(p => p.Items).Include(p => p.Trainer)
                    .Where(p => p.ClientId == id).OrderByDescending(p => p.IsActive).ThenByDescending(p => p.StartDate).ToListAsync(),
                Feedback = await _db.Feedbacks.Include(f => f.Trainer)
                    .Where(f => f.ClientId == id).OrderByDescending(f => f.CreatedAt).Take(10).ToListAsync(),
                Appointments = await _db.Appointments
                    .Where(a => a.ClientId == id && a.TrainerId == TrainerId).OrderByDescending(a => a.ScheduledAt).Take(5).ToListAsync(),
                NewFeedback = new FeedbackFormViewModel { ClientId = id }
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> GiveFeedback([Bind(Prefix = "NewFeedback")] FeedbackFormViewModel model)
        {
            if (!await IsMyClientAsync(model.ClientId)) return Forbid();

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Feedback must be between 5 and 1000 characters.";
                return RedirectToAction(nameof(ClientDetails), new { id = model.ClientId });
            }

            _db.Feedbacks.Add(new Feedback
            {
                TrainerId = TrainerId,
                ClientId = model.ClientId,
                Message = model.Message.Trim(),
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();

            await _notifications.NotifyAsync(model.ClientId, "New feedback from your trainer",
                $"{User.Identity?.Name}: {model.Message.Trim()}", NotificationType.Feedback, "/Client/MyPlans");
            await _activity.LogAsync(TrainerId, "GiveFeedback", $"Client #{model.ClientId}");

            TempData["Success"] = "Feedback sent.";
            return RedirectToAction(nameof(ClientDetails), new { id = model.ClientId });
        }

        // ================================================================== PLANS
        public async Task<IActionResult> Plans()
        {
            var model = new TrainerPlansViewModel
            {
                WorkoutPlans = await _db.WorkoutPlans.Include(p => p.Client).Include(p => p.Exercises)
                    .Where(p => p.TrainerId == TrainerId)
                    .OrderByDescending(p => p.IsActive).ThenByDescending(p => p.CreatedAt).ToListAsync(),
                DietPlans = await _db.DietPlans.Include(p => p.Client).Include(p => p.Items)
                    .Where(p => p.TrainerId == TrainerId)
                    .OrderByDescending(p => p.IsActive).ThenByDescending(p => p.CreatedAt).ToListAsync()
            };
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> CreateWorkoutPlan(int? clientId)
        {
            var model = new WorkoutPlanFormViewModel
            {
                ClientId = clientId,
                Exercises = new List<WorkoutExerciseInput> { new WorkoutExerciseInput { Sets = 3, Reps = 10, DurationMinutes = 15 } },
                Clients = await GetClientOptionsAsync(clientId)
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CreateWorkoutPlan(WorkoutPlanFormViewModel model)
        {
            model.Exercises = model.Exercises?.Where(e => e != null && !string.IsNullOrWhiteSpace(e.Name)).ToList() ?? new List<WorkoutExerciseInput>();
            ValidatePlanDates(model.StartDate, model.EndDate);

            if (model.Exercises.Count == 0)
                ModelState.AddModelError(string.Empty, "Add at least one exercise to the plan.");

            if (model.ClientId.HasValue && !await IsMyClientAsync(model.ClientId.Value))
                ModelState.AddModelError(nameof(model.ClientId), "You can only create plans for your own clients.");

            if (!ModelState.IsValid)
            {
                model.Clients = await GetClientOptionsAsync(model.ClientId);
                if (model.Exercises.Count == 0) model.Exercises.Add(new WorkoutExerciseInput());
                return View(model);
            }

            int clientId = model.ClientId.Value;

            // Only one active workout plan per client: the new plan replaces the old one.
            var previous = await _db.WorkoutPlans.Where(p => p.ClientId == clientId && p.IsActive).ToListAsync();
            previous.ForEach(p => p.IsActive = false);

            var plan = new WorkoutPlan
            {
                Title = model.Title.Trim(),
                Description = model.Description?.Trim(),
                Goal = model.Goal?.Trim(),
                TrainerId = TrainerId,
                ClientId = clientId,
                StartDate = model.StartDate.Date,
                EndDate = model.EndDate.Date,
                IsActive = true,
                Exercises = model.Exercises.Select(e => new WorkoutExercise
                {
                    Name = e.Name.Trim(),
                    Day = e.Day,
                    Sets = e.Sets,
                    Reps = e.Reps,
                    DurationMinutes = e.DurationMinutes,
                    Notes = e.Notes?.Trim()
                }).ToList()
            };

            try
            {
                _db.WorkoutPlans.Add(plan);
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Could not save workout plan");
                ModelState.AddModelError(string.Empty, "The plan couldn't be saved. Try again.");
                model.Clients = await GetClientOptionsAsync(model.ClientId);
                return View(model);
            }

            await _notifications.NotifyAsync(clientId, "New workout plan",
                $"{User.Identity?.Name} created \"{plan.Title}\" for you ({plan.Exercises.Count} exercises).",
                NotificationType.Plan, "/Client/MyPlans");
            await _activity.LogAsync(TrainerId, "CreateWorkoutPlan", plan.Title);

            TempData["Success"] = $"Workout plan \"{plan.Title}\" created and shared with the client.";
            return RedirectToAction(nameof(ClientDetails), new { id = clientId });
        }

        [HttpGet]
        public async Task<IActionResult> CreateDietPlan(int? clientId)
        {
            var model = new DietPlanFormViewModel
            {
                ClientId = clientId,
                Items = new List<DietItemInput>
                {
                    new DietItemInput { MealType = MealType.Breakfast, ScheduledTime = new TimeSpan(7, 30, 0), Calories = 400 },
                    new DietItemInput { MealType = MealType.Lunch, ScheduledTime = new TimeSpan(12, 30, 0), Calories = 700 },
                    new DietItemInput { MealType = MealType.Dinner, ScheduledTime = new TimeSpan(19, 30, 0), Calories = 600 }
                },
                Clients = await GetClientOptionsAsync(clientId)
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CreateDietPlan(DietPlanFormViewModel model)
        {
            model.Items = model.Items?.Where(i => i != null && !string.IsNullOrWhiteSpace(i.Description)).ToList() ?? new List<DietItemInput>();
            ValidatePlanDates(model.StartDate, model.EndDate);

            if (model.Items.Count == 0)
                ModelState.AddModelError(string.Empty, "Add at least one meal to the plan.");

            if (model.ClientId.HasValue && !await IsMyClientAsync(model.ClientId.Value))
                ModelState.AddModelError(nameof(model.ClientId), "You can only create plans for your own clients.");

            if (!ModelState.IsValid)
            {
                model.Clients = await GetClientOptionsAsync(model.ClientId);
                if (model.Items.Count == 0) model.Items.Add(new DietItemInput());
                return View(model);
            }

            int clientId = model.ClientId.Value;
            var previous = await _db.DietPlans.Where(p => p.ClientId == clientId && p.IsActive).ToListAsync();
            previous.ForEach(p => p.IsActive = false);

            var plan = new DietPlan
            {
                Title = model.Title.Trim(),
                Description = model.Description?.Trim(),
                DailyCalorieTarget = model.DailyCalorieTarget,
                TrainerId = TrainerId,
                ClientId = clientId,
                StartDate = model.StartDate.Date,
                EndDate = model.EndDate.Date,
                IsActive = true,
                Items = model.Items
                    .OrderBy(i => i.ScheduledTime)
                    .Select(i => new DietPlanItem
                    {
                        MealType = i.MealType,
                        Description = i.Description.Trim(),
                        Calories = i.Calories,
                        ScheduledTime = i.ScheduledTime
                    }).ToList()
            };

            try
            {
                _db.DietPlans.Add(plan);
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Could not save diet plan");
                ModelState.AddModelError(string.Empty, "The plan couldn't be saved. Try again.");
                model.Clients = await GetClientOptionsAsync(model.ClientId);
                return View(model);
            }

            await _notifications.NotifyAsync(clientId, "New diet plan",
                $"{User.Identity?.Name} created \"{plan.Title}\" ({plan.DailyCalorieTarget} kcal/day).",
                NotificationType.Plan, "/Client/MyPlans");
            await _activity.LogAsync(TrainerId, "CreateDietPlan", plan.Title);

            TempData["Success"] = $"Diet plan \"{plan.Title}\" created and shared with the client.";
            return RedirectToAction(nameof(ClientDetails), new { id = clientId });
        }

        /// <summary>Activates or deactivates a plan. planType is "workout" or "diet".</summary>
        [HttpPost]
        public async Task<IActionResult> TogglePlan(string planType, int id)
        {
            if (planType == "workout")
            {
                var plan = await _db.WorkoutPlans.FirstOrDefaultAsync(p => p.Id == id && p.TrainerId == TrainerId);
                if (plan == null) return NotFound();
                if (!plan.IsActive)
                    (await _db.WorkoutPlans.Where(p => p.ClientId == plan.ClientId && p.IsActive).ToListAsync()).ForEach(p => p.IsActive = false);
                plan.IsActive = !plan.IsActive;
                TempData["Success"] = plan.IsActive ? "Workout plan activated." : "Workout plan deactivated.";
            }
            else if (planType == "diet")
            {
                var plan = await _db.DietPlans.FirstOrDefaultAsync(p => p.Id == id && p.TrainerId == TrainerId);
                if (plan == null) return NotFound();
                if (!plan.IsActive)
                    (await _db.DietPlans.Where(p => p.ClientId == plan.ClientId && p.IsActive).ToListAsync()).ForEach(p => p.IsActive = false);
                plan.IsActive = !plan.IsActive;
                TempData["Success"] = plan.IsActive ? "Diet plan activated." : "Diet plan deactivated.";
            }
            else
            {
                return BadRequest();
            }

            await _db.SaveChangesAsync();
            await _activity.LogAsync(TrainerId, "TogglePlan", $"{planType} #{id}");
            return RedirectToAction(nameof(Plans));
        }

        [HttpPost]
        public async Task<IActionResult> DeletePlan(string planType, int id)
        {
            if (planType == "workout")
            {
                var plan = await _db.WorkoutPlans.FirstOrDefaultAsync(p => p.Id == id && p.TrainerId == TrainerId);
                if (plan == null) return NotFound();
                _db.WorkoutPlans.Remove(plan);
            }
            else if (planType == "diet")
            {
                var plan = await _db.DietPlans.FirstOrDefaultAsync(p => p.Id == id && p.TrainerId == TrainerId);
                if (plan == null) return NotFound();
                _db.DietPlans.Remove(plan);
            }
            else
            {
                return BadRequest();
            }

            await _db.SaveChangesAsync();
            await _activity.LogAsync(TrainerId, "DeletePlan", $"{planType} #{id}");
            TempData["Success"] = "Plan deleted.";
            return RedirectToAction(nameof(Plans));
        }

        // ================================================================== HELPERS
        private Task<bool> IsMyClientAsync(int clientId) =>
            _db.Users.AnyAsync(u => u.Id == clientId && u.Role == UserRole.Client && u.TrainerId == TrainerId);

        private void ValidatePlanDates(DateTime start, DateTime end)
        {
            if (end.Date < start.Date)
                ModelState.AddModelError("EndDate", "The end date must be on or after the start date.");
            if ((end.Date - start.Date).TotalDays > 366)
                ModelState.AddModelError("EndDate", "A plan can't be longer than one year.");
            if (start.Date < DateTime.Today.AddDays(-30))
                ModelState.AddModelError("StartDate", "The start date can't be more than 30 days in the past.");
        }

        private async Task<IEnumerable<SelectListItem>> GetClientOptionsAsync(int? selectedId)
        {
            var clients = await _db.Users
                .Where(u => u.TrainerId == TrainerId && u.Role == UserRole.Client && u.IsActive)
                .OrderBy(u => u.FullName)
                .Select(u => new { u.Id, u.FullName })
                .ToListAsync();
            return new SelectList(clients, "Id", "FullName", selectedId);
        }

        private async Task<List<ClientSummary>> BuildClientSummariesAsync()
        {
            var weekStart = DateTime.Today.StartOfWeek();

            var clients = await _db.Users
                .Where(u => u.TrainerId == TrainerId && u.Role == UserRole.Client)
                .OrderBy(u => u.FullName)
                .Select(u => new { u.Id, u.FullName, u.Email, u.IsActive })
                .ToListAsync();
            var ids = clients.Select(c => c.Id).ToList();

            var metrics = await _db.HealthMetrics.Where(m => ids.Contains(m.ClientId))
                .Select(m => new { m.ClientId, m.RecordedAt, m.WeightKg, m.Bmi }).ToListAsync();
            var workouts = await _db.WorkoutLogs.Where(w => ids.Contains(w.ClientId))
                .Select(w => new { w.ClientId, w.Date }).ToListAsync();
            var activePlanClients = await _db.WorkoutPlans.Where(p => ids.Contains(p.ClientId) && p.IsActive)
                .Select(p => p.ClientId).ToListAsync();

            return clients.Select(c =>
            {
                var clientMetrics = metrics.Where(m => m.ClientId == c.Id).OrderBy(m => m.RecordedAt).ToList();
                var clientWorkouts = workouts.Where(w => w.ClientId == c.Id).ToList();
                var latest = clientMetrics.LastOrDefault();

                return new ClientSummary
                {
                    ClientId = c.Id,
                    FullName = c.FullName,
                    Email = c.Email,
                    IsActive = c.IsActive,
                    LatestWeight = latest?.WeightKg,
                    LatestBmi = latest?.Bmi,
                    WeightChange = clientMetrics.Count >= 2
                        ? Math.Round(clientMetrics.Last().WeightKg - clientMetrics.First().WeightKg, 1)
                        : null,
                    LastWorkout = clientWorkouts.Count > 0 ? clientWorkouts.Max(w => w.Date) : null,
                    WorkoutsThisWeek = clientWorkouts.Count(w => w.Date >= weekStart),
                    HasActivePlan = activePlanClients.Contains(c.Id)
                };
            }).ToList();
        }
    }
}
