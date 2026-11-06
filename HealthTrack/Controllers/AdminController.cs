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
    /// <summary>Administration: system overview, user management, trainer approval / assignment and audit log.</summary>
    [Authorize(Roles = nameof(UserRole.Admin))]
    public class AdminController : Controller
    {
        private const int PageSize = 25;

        private readonly AppDbContext _db;
        private readonly IActivityLogger _activity;
        private readonly INotificationService _notifications;
        private readonly ILogger<AdminController> _logger;

        public AdminController(AppDbContext db, IActivityLogger activity, INotificationService notifications, ILogger<AdminController> logger)
        {
            _db = db;
            _activity = activity;
            _notifications = notifications;
            _logger = logger;
        }

        private int AdminId => User.GetUserId();

        // ================================================================== DASHBOARD
        public async Task<IActionResult> Dashboard()
        {
            var now = DateTime.Now;
            var today = DateTime.Today;
            var since7 = today.AddDays(-6);
            var since14 = today.AddDays(-13);

            var users = await _db.Users
                .Select(u => new { u.Id, u.Role, u.IsActive, u.IsApproved, u.TrainerId })
                .ToListAsync();

            var recentLogs = await _db.ActivityLogs
                .Where(a => a.Timestamp >= since14)
                .Select(a => new { a.UserId, a.Action, a.Timestamp })
                .ToListAsync();

            var upcomingByTrainer = await _db.Appointments
                .Where(a => a.Status == AppointmentStatus.Approved && a.ScheduledAt >= now)
                .GroupBy(a => a.TrainerId)
                .Select(g => new { TrainerId = g.Key, Count = g.Count() })
                .ToListAsync();

            var trainers = await _db.Users
                .Where(u => u.Role == UserRole.Trainer && u.IsApproved)
                .OrderBy(u => u.FullName)
                .Select(u => new { u.Id, u.FullName, u.Specialization, u.IsActive })
                .ToListAsync();

            var model = new AdminDashboardViewModel
            {
                TotalClients = users.Count(u => u.Role == UserRole.Client),
                TotalTrainers = users.Count(u => u.Role == UserRole.Trainer),
                TotalAdmins = users.Count(u => u.Role == UserRole.Admin),
                InactiveUsers = users.Count(u => !u.IsActive),
                ActiveUsersLast7Days = recentLogs.Where(l => l.Timestamp >= since7 && l.UserId.HasValue)
                    .Select(l => l.UserId).Distinct().Count(),
                LoginsToday = recentLogs.Count(l => l.Action == "Login" && l.Timestamp >= today),
                TotalWorkoutLogs = await _db.WorkoutLogs.CountAsync(),
                TotalMealLogs = await _db.MealLogs.CountAsync(),
                TotalMetricEntries = await _db.HealthMetrics.CountAsync(),
                OpenAppointments = await _db.Appointments.CountAsync(a =>
                    (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Approved) && a.ScheduledAt >= now),

                PendingTrainers = await _db.Users
                    .Where(u => u.Role == UserRole.Trainer && !u.IsApproved)
                    .Select(u => new UserBrief { Id = u.Id, FullName = u.FullName, Email = u.Email, Specialization = u.Specialization, CreatedAt = u.CreatedAt })
                    .ToListAsync(),
                UnassignedClients = await _db.Users
                    .Where(u => u.Role == UserRole.Client && u.TrainerId == null && u.IsActive)
                    .OrderBy(u => u.CreatedAt)
                    .Select(u => new UserBrief { Id = u.Id, FullName = u.FullName, Email = u.Email, CreatedAt = u.CreatedAt })
                    .ToListAsync(),
                TrainerAssignments = trainers.Select(t => new TrainerAssignmentRow
                {
                    TrainerId = t.Id,
                    TrainerName = t.FullName,
                    Specialization = t.Specialization,
                    IsActive = t.IsActive,
                    ClientCount = users.Count(u => u.TrainerId == t.Id),
                    UpcomingAppointments = upcomingByTrainer.FirstOrDefault(x => x.TrainerId == t.Id)?.Count ?? 0
                }).ToList(),
                TrainerOptions = new SelectList(trainers.Where(t => t.IsActive), "Id", "FullName"),
                RecentActivity = await _db.ActivityLogs.Include(a => a.User)
                    .OrderByDescending(a => a.Timestamp).Take(12).ToListAsync()
            };

            for (var day = since14; day <= today; day = day.AddDays(1))
            {
                var next = day.AddDays(1);
                model.ActivityLabels.Add(day.ToString("dd MMM"));
                model.LoginCounts.Add(recentLogs.Count(l => l.Action == "Login" && l.Timestamp >= day && l.Timestamp < next));
                model.ActionCounts.Add(recentLogs.Count(l => l.Action != "Login" && l.Timestamp >= day && l.Timestamp < next));
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveTrainer(int id)
        {
            var trainer = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.Role == UserRole.Trainer);
            if (trainer == null) return NotFound();

            trainer.IsApproved = true;
            await _db.SaveChangesAsync();

            await _notifications.NotifyAsync(trainer.Id, "Account approved",
                "An administrator approved your trainer account. You can now manage clients.", NotificationType.Account, "/Trainer/Dashboard");
            await _activity.LogAsync(AdminId, "ApproveTrainer", trainer.FullName);

            TempData["Success"] = $"{trainer.FullName} can now sign in as a trainer.";
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost]
        public async Task<IActionResult> AssignTrainer(int clientId, int? trainerId, string returnTo = null)
        {
            var client = await _db.Users.FirstOrDefaultAsync(u => u.Id == clientId && u.Role == UserRole.Client);
            if (client == null) return NotFound();

            User trainer = null;
            if (trainerId.HasValue)
            {
                trainer = await _db.Users.FirstOrDefaultAsync(u => u.Id == trainerId && u.Role == UserRole.Trainer && u.IsActive && u.IsApproved);
                if (trainer == null)
                {
                    TempData["Error"] = "Choose an active, approved trainer.";
                    return RedirectToAction(nameof(Dashboard));
                }
            }

            client.TrainerId = trainer?.Id;
            await _db.SaveChangesAsync();

            if (trainer != null)
            {
                await _notifications.NotifyAsync(client.Id, "Trainer assigned",
                    $"{trainer.FullName} ({trainer.Specialization}) is now your trainer.", NotificationType.Account, "/Client/Dashboard");
                await _notifications.NotifyAsync(trainer.Id, "New client assigned",
                    $"{client.FullName} has been assigned to you.", NotificationType.Account, $"/Trainer/ClientDetails/{client.Id}");
            }

            await _activity.LogAsync(AdminId, "AssignTrainer", $"{client.FullName} -> {trainer?.FullName ?? "none"}");
            TempData["Success"] = trainer != null ? $"{client.FullName} assigned to {trainer.FullName}." : $"{client.FullName} no longer has a trainer.";

            return returnTo == "users" ? RedirectToAction(nameof(Users)) : RedirectToAction(nameof(Dashboard));
        }

        // ================================================================== USERS
        public async Task<IActionResult> Users(UserRole? role, string search)
        {
            var query = _db.Users.Include(u => u.Trainer).AsQueryable();
            if (role.HasValue) query = query.Where(u => u.Role == role.Value);
            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim();
                query = query.Where(u => u.FullName.Contains(term) || u.Email.Contains(term));
            }

            return View(new UserListViewModel
            {
                Users = await query.OrderBy(u => u.Role).ThenBy(u => u.FullName).ToListAsync(),
                RoleFilter = role,
                Search = search
            });
        }

        [HttpGet]
        public async Task<IActionResult> CreateUser()
        {
            return View(new AdminUserFormViewModel { Trainers = await GetTrainerOptionsAsync(null) });
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(AdminUserFormViewModel model)
        {
            string email = model.Email?.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(model.Password))
                ModelState.AddModelError(nameof(model.Password), "Set a password for the new account.");
            if (!string.IsNullOrEmpty(email) && await _db.Users.AnyAsync(u => u.Email == email))
                ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");
            await ValidateTrainerAssignmentAsync(model);

            if (!ModelState.IsValid)
            {
                model.Trainers = await GetTrainerOptionsAsync(model.TrainerId);
                return View(model);
            }

            var user = new User
            {
                FullName = model.FullName.Trim(),
                Email = email,
                PasswordHash = SecurePasswordHasher.Hash(model.Password),
                Role = model.Role,
                PhoneNumber = model.PhoneNumber?.Trim(),
                Specialization = model.Role == UserRole.Trainer ? model.Specialization?.Trim() : null,
                TrainerId = model.Role == UserRole.Client ? model.TrainerId : null,
                IsActive = model.IsActive,
                IsApproved = true,
                CreatedAt = DateTime.Now
            };

            try
            {
                _db.Users.Add(user);
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Admin could not create user {Email}", email);
                ModelState.AddModelError(string.Empty, "The account couldn't be created. Try again.");
                model.Trainers = await GetTrainerOptionsAsync(model.TrainerId);
                return View(model);
            }

            await _activity.LogAsync(AdminId, "CreateUser", $"{user.FullName} ({user.Role})");
            TempData["Success"] = $"{user.Role} account created for {user.FullName}.";
            return RedirectToAction(nameof(Users));
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            return View(new AdminUserFormViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                PhoneNumber = user.PhoneNumber,
                Specialization = user.Specialization,
                TrainerId = user.TrainerId,
                IsActive = user.IsActive,
                IsApproved = user.IsApproved,
                Trainers = await GetTrainerOptionsAsync(user.TrainerId)
            });
        }

        [HttpPost]
        public async Task<IActionResult> EditUser(AdminUserFormViewModel model)
        {
            var user = await _db.Users.FindAsync(model.Id);
            if (user == null) return NotFound();

            string email = model.Email?.Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(email) && await _db.Users.AnyAsync(u => u.Email == email && u.Id != model.Id))
                ModelState.AddModelError(nameof(model.Email), "Another account already uses this email.");

            if (user.Id == AdminId && (model.Role != UserRole.Admin || !model.IsActive))
                ModelState.AddModelError(string.Empty, "You can't remove your own administrator access or deactivate yourself.");

            await ValidateTrainerAssignmentAsync(model);

            if (!ModelState.IsValid)
            {
                model.Trainers = await GetTrainerOptionsAsync(model.TrainerId);
                return View(model);
            }

            // If a trainer stops being a trainer, their clients become unassigned.
            if (user.Role == UserRole.Trainer && model.Role != UserRole.Trainer)
            {
                var clients = await _db.Users.Where(u => u.TrainerId == user.Id).ToListAsync();
                clients.ForEach(c => c.TrainerId = null);
            }

            user.FullName = model.FullName.Trim();
            user.Email = email;
            user.Role = model.Role;
            user.PhoneNumber = model.PhoneNumber?.Trim();
            user.Specialization = model.Role == UserRole.Trainer ? model.Specialization?.Trim() : null;
            user.TrainerId = model.Role == UserRole.Client ? model.TrainerId : null;
            user.IsActive = model.IsActive;
            user.IsApproved = model.IsApproved;

            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                user.PasswordHash = SecurePasswordHasher.Hash(model.Password);
                user.FailedLoginAttempts = 0;
                user.LockoutEnd = null;
            }

            await _db.SaveChangesAsync();
            await _activity.LogAsync(AdminId, "EditUser", $"{user.FullName} ({user.Role})");

            TempData["Success"] = $"Changes to {user.FullName} saved.";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleActive(int id)
        {
            if (id == AdminId)
            {
                TempData["Error"] = "You can't deactivate your own account.";
                return RedirectToAction(nameof(Users));
            }

            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.IsActive = !user.IsActive;
            await _db.SaveChangesAsync();
            await _activity.LogAsync(AdminId, user.IsActive ? "ActivateUser" : "DeactivateUser", user.FullName);

            TempData["Success"] = user.IsActive ? $"{user.FullName} reactivated." : $"{user.FullName} deactivated. They can no longer sign in.";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        public async Task<IActionResult> UnlockUser(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.LockoutEnd = null;
            user.FailedLoginAttempts = 0;
            await _db.SaveChangesAsync();
            await _activity.LogAsync(AdminId, "UnlockUser", user.FullName);

            TempData["Success"] = $"{user.FullName} unlocked.";
            return RedirectToAction(nameof(Users));
        }

        // ================================================================== ACTIVITY LOG
        public async Task<IActionResult> ActivityLogs(string filter, int page = 1)
        {
            var query = _db.ActivityLogs.AsQueryable();
            if (!string.IsNullOrWhiteSpace(filter)) query = query.Where(a => a.Action == filter);

            int total = await query.CountAsync();
            int totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
            page = Math.Clamp(page, 1, totalPages);

            return View(new ActivityLogViewModel
            {
                Logs = await query.Include(a => a.User)
                    .OrderByDescending(a => a.Timestamp)
                    .Skip((page - 1) * PageSize).Take(PageSize)
                    .ToListAsync(),
                Actions = await _db.ActivityLogs.Select(a => a.Action).Distinct().OrderBy(a => a).ToListAsync(),
                ActionFilter = filter,
                Page = page,
                TotalPages = totalPages,
                TotalCount = total
            });
        }

        // ================================================================== HELPERS
        private async Task ValidateTrainerAssignmentAsync(AdminUserFormViewModel model)
        {
            if (model.Role == UserRole.Client && model.TrainerId.HasValue)
            {
                bool valid = await _db.Users.AnyAsync(u => u.Id == model.TrainerId && u.Role == UserRole.Trainer && u.IsActive && u.IsApproved);
                if (!valid) ModelState.AddModelError(nameof(model.TrainerId), "Choose an active, approved trainer.");
            }
            if (model.Role == UserRole.Trainer && string.IsNullOrWhiteSpace(model.Specialization))
                ModelState.AddModelError(nameof(model.Specialization), "Enter the trainer's specialisation.");
        }

        private async Task<IEnumerable<SelectListItem>> GetTrainerOptionsAsync(int? selectedId)
        {
            var trainers = await _db.Users
                .Where(u => u.Role == UserRole.Trainer && u.IsActive && u.IsApproved)
                .OrderBy(u => u.FullName)
                .Select(u => new { u.Id, Name = u.FullName + " (" + u.Specialization + ")" })
                .ToListAsync();
            return new SelectList(trainers, "Id", "Name", selectedId);
        }
    }
}
