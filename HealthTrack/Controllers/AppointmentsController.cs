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
    /// Appointment system. Clients request consultations; trainers approve, reschedule, complete or cancel them.
    /// Double booking is prevented for both the trainer and the client.
    /// </summary>
    [Authorize]
    public class AppointmentsController : Controller
    {
        private const int OpeningHour = 6;
        private const int ClosingHour = 21;

        private readonly AppDbContext _db;
        private readonly IActivityLogger _activity;
        private readonly INotificationService _notifications;
        private readonly ILogger<AppointmentsController> _logger;

        public AppointmentsController(AppDbContext db, IActivityLogger activity, INotificationService notifications, ILogger<AppointmentsController> logger)
        {
            _db = db;
            _activity = activity;
            _notifications = notifications;
            _logger = logger;
        }

        private int CurrentUserId => User.GetUserId();

        // ================================================================== LIST
        public async Task<IActionResult> Index(AppointmentStatus? status)
        {
            var query = _db.Appointments.Include(a => a.Client).Include(a => a.Trainer).AsQueryable();

            if (User.IsInRole(nameof(UserRole.Client))) query = query.Where(a => a.ClientId == CurrentUserId);
            else if (User.IsInRole(nameof(UserRole.Trainer))) query = query.Where(a => a.TrainerId == CurrentUserId);
            // Admin sees everything.

            if (status.HasValue) query = query.Where(a => a.Status == status.Value);

            var all = await query.ToListAsync();
            var now = DateTime.Now;

            return View(new AppointmentListViewModel
            {
                Upcoming = all.Where(a => a.ScheduledAt >= now && a.IsOpen).OrderBy(a => a.ScheduledAt).ToList(),
                Past = all.Where(a => a.ScheduledAt < now || !a.IsOpen).OrderByDescending(a => a.ScheduledAt).ToList(),
                StatusFilter = status
            });
        }

        // ================================================================== CREATE (client)
        [HttpGet]
        [Authorize(Roles = nameof(UserRole.Client))]
        public async Task<IActionResult> Create(int? trainerId)
        {
            var client = await _db.Users.FindAsync(CurrentUserId);
            var model = new AppointmentFormViewModel
            {
                TrainerId = trainerId ?? client?.TrainerId,
                ScheduledAt = DateTime.Today.AddDays(1).AddHours(10)
            };
            model.Trainers = await GetTrainerOptionsAsync(model.TrainerId);
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = nameof(UserRole.Client))]
        public async Task<IActionResult> Create(AppointmentFormViewModel model)
        {
            ValidateTimeSlot(model.ScheduledAt, model.DurationMinutes, requireFuture: true);

            User trainer = null;
            if (model.TrainerId.HasValue)
            {
                trainer = await _db.Users.FirstOrDefaultAsync(u => u.Id == model.TrainerId && u.Role == UserRole.Trainer && u.IsActive && u.IsApproved);
                if (trainer == null) ModelState.AddModelError(nameof(model.TrainerId), "Choose an available trainer or professional.");
            }

            if (ModelState.IsValid && await HasConflictAsync(model.TrainerId.Value, CurrentUserId, model.ScheduledAt, model.DurationMinutes, null, false))
                ModelState.AddModelError(nameof(model.ScheduledAt), "That time overlaps with another booking. Choose a different time.");

            if (!ModelState.IsValid)
            {
                model.Trainers = await GetTrainerOptionsAsync(model.TrainerId);
                return View(model);
            }

            var appointment = new Appointment
            {
                ClientId = CurrentUserId,
                TrainerId = trainer.Id,
                ScheduledAt = model.ScheduledAt,
                DurationMinutes = model.DurationMinutes,
                Type = model.Type,
                Reason = model.Reason?.Trim(),
                Status = AppointmentStatus.Pending,
                CreatedAt = DateTime.Now
            };

            try
            {
                _db.Appointments.Add(appointment);
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Could not save appointment");
                ModelState.AddModelError(string.Empty, "The appointment couldn't be booked. Try again.");
                model.Trainers = await GetTrainerOptionsAsync(model.TrainerId);
                return View(model);
            }

            await _notifications.NotifyAsync(trainer.Id, "New appointment request",
                $"{User.Identity?.Name} requested a {appointment.Type.GetDisplayName().ToLower()} on {appointment.ScheduledAt:ddd dd MMM, hh:mm tt}.",
                NotificationType.Appointment, "/Appointments");
            await _activity.LogAsync(CurrentUserId, "BookAppointment", $"With {trainer.FullName} on {appointment.ScheduledAt:g}");

            TempData["Success"] = $"Request sent to {trainer.FullName}. You'll be notified when it's approved.";
            return RedirectToAction(nameof(Index));
        }

        // ================================================================== TRAINER ACTIONS
        [HttpPost]
        [Authorize(Roles = nameof(UserRole.Trainer))]
        public async Task<IActionResult> Approve(int id)
        {
            var appointment = await _db.Appointments.Include(a => a.Client)
                .FirstOrDefaultAsync(a => a.Id == id && a.TrainerId == CurrentUserId);
            if (appointment == null) return NotFound();

            if (appointment.Status != AppointmentStatus.Pending)
            {
                TempData["Error"] = "Only pending requests can be approved.";
                return RedirectToAction(nameof(Index));
            }
            if (appointment.ScheduledAt <= DateTime.Now)
            {
                TempData["Error"] = "This request is in the past. Reschedule it instead.";
                return RedirectToAction(nameof(Edit), new { id });
            }
            if (await HasConflictAsync(appointment.TrainerId, appointment.ClientId, appointment.ScheduledAt, appointment.DurationMinutes, appointment.Id, true))
            {
                TempData["Error"] = "You already have an approved session at that time. Reschedule this request instead.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            appointment.Status = AppointmentStatus.Approved;
            appointment.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            await _notifications.NotifyAsync(appointment.ClientId, "Appointment approved",
                $"{User.Identity?.Name} confirmed your appointment on {appointment.ScheduledAt:ddd dd MMM, hh:mm tt}.",
                NotificationType.Appointment, "/Appointments");
            await _activity.LogAsync(CurrentUserId, "ApproveAppointment", $"#{appointment.Id}");

            TempData["Success"] = $"Appointment with {appointment.Client.FullName} approved.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = nameof(UserRole.Trainer))]
        public async Task<IActionResult> Complete(int id)
        {
            var appointment = await _db.Appointments.FirstOrDefaultAsync(a => a.Id == id && a.TrainerId == CurrentUserId);
            if (appointment == null) return NotFound();

            if (appointment.Status != AppointmentStatus.Approved)
            {
                TempData["Error"] = "Only approved appointments can be marked as completed.";
                return RedirectToAction(nameof(Index));
            }

            appointment.Status = AppointmentStatus.Completed;
            appointment.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();
            await _activity.LogAsync(CurrentUserId, "CompleteAppointment", $"#{appointment.Id}");

            TempData["Success"] = "Appointment marked as completed.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>Clients can cancel their own open appointments; trainers theirs; admins any.</summary>
        [HttpPost]
        public async Task<IActionResult> Cancel(int id, string reason)
        {
            var appointment = await _db.Appointments.Include(a => a.Client).Include(a => a.Trainer)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (appointment == null) return NotFound();

            bool isClient = appointment.ClientId == CurrentUserId;
            bool isTrainer = appointment.TrainerId == CurrentUserId;
            bool isAdmin = User.IsInRole(nameof(UserRole.Admin));
            if (!isClient && !isTrainer && !isAdmin) return Forbid();

            if (!appointment.IsOpen)
            {
                TempData["Error"] = "This appointment is already closed.";
                return RedirectToAction(nameof(Index));
            }

            appointment.Status = AppointmentStatus.Cancelled;
            appointment.UpdatedAt = DateTime.Now;
            if (!string.IsNullOrWhiteSpace(reason))
            {
                string note = $"Cancelled: {reason.Trim()}";
                appointment.TrainerNotes = note.Length > 500 ? note.Substring(0, 500) : note;
            }
            await _db.SaveChangesAsync();

            string when = appointment.ScheduledAt.ToString("ddd dd MMM, hh:mm tt");
            string suffix = string.IsNullOrWhiteSpace(reason) ? "" : $" Reason: {reason.Trim()}";
            if (!isClient)
                await _notifications.NotifyAsync(appointment.ClientId, "Appointment cancelled",
                    $"Your appointment with {appointment.Trainer.FullName} on {when} was cancelled.{suffix}", NotificationType.Appointment, "/Appointments");
            if (!isTrainer)
                await _notifications.NotifyAsync(appointment.TrainerId, "Appointment cancelled",
                    $"{appointment.Client.FullName} cancelled the appointment on {when}.{suffix}", NotificationType.Appointment, "/Appointments");

            await _activity.LogAsync(CurrentUserId, "CancelAppointment", $"#{appointment.Id}");
            TempData["Success"] = "Appointment cancelled.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Roles = nameof(UserRole.Trainer))]
        public async Task<IActionResult> Edit(int id)
        {
            var appointment = await _db.Appointments.Include(a => a.Client)
                .FirstOrDefaultAsync(a => a.Id == id && a.TrainerId == CurrentUserId);
            if (appointment == null) return NotFound();

            return View(new AppointmentFormViewModel
            {
                Id = appointment.Id,
                TrainerId = appointment.TrainerId,
                ScheduledAt = appointment.ScheduledAt,
                DurationMinutes = appointment.DurationMinutes,
                Type = appointment.Type,
                Reason = appointment.Reason,
                Status = appointment.Status,
                TrainerNotes = appointment.TrainerNotes,
                ClientName = appointment.Client.FullName
            });
        }

        [HttpPost]
        [Authorize(Roles = nameof(UserRole.Trainer))]
        public async Task<IActionResult> Edit(AppointmentFormViewModel model)
        {
            var appointment = await _db.Appointments.Include(a => a.Client)
                .FirstOrDefaultAsync(a => a.Id == model.Id && a.TrainerId == CurrentUserId);
            if (appointment == null) return NotFound();

            bool timeChanged = appointment.ScheduledAt != model.ScheduledAt || appointment.DurationMinutes != model.DurationMinutes;
            bool willBeOpen = model.Status == AppointmentStatus.Pending || model.Status == AppointmentStatus.Approved;

            if (timeChanged || willBeOpen)
                ValidateTimeSlot(model.ScheduledAt, model.DurationMinutes, requireFuture: willBeOpen);

            if (ModelState.IsValid && willBeOpen &&
                await HasConflictAsync(appointment.TrainerId, appointment.ClientId, model.ScheduledAt, model.DurationMinutes, appointment.Id, false))
                ModelState.AddModelError(nameof(model.ScheduledAt), "That time overlaps with another booking.");

            if (!ModelState.IsValid)
            {
                model.ClientName = appointment.Client.FullName;
                model.Reason = appointment.Reason;
                return View(model);
            }

            var oldStatus = appointment.Status;
            appointment.ScheduledAt = model.ScheduledAt;
            appointment.DurationMinutes = model.DurationMinutes;
            appointment.Type = model.Type;
            appointment.Status = model.Status;
            appointment.TrainerNotes = model.TrainerNotes?.Trim();
            appointment.UpdatedAt = DateTime.Now;
            if (timeChanged) appointment.ReminderSent = false;

            await _db.SaveChangesAsync();

            var changes = new List<string>();
            if (timeChanged) changes.Add($"new time {appointment.ScheduledAt:ddd dd MMM, hh:mm tt} ({appointment.DurationMinutes} min)");
            if (oldStatus != appointment.Status) changes.Add($"status {appointment.Status}");
            if (!string.IsNullOrWhiteSpace(appointment.TrainerNotes)) changes.Add($"note: {appointment.TrainerNotes}");

            await _notifications.NotifyAsync(appointment.ClientId, "Appointment updated",
                $"{User.Identity?.Name} updated your appointment" + (changes.Count > 0 ? ": " + string.Join("; ", changes) : "."),
                NotificationType.Appointment, "/Appointments");
            await _activity.LogAsync(CurrentUserId, "UpdateAppointment", $"#{appointment.Id}");

            TempData["Success"] = "Appointment updated and the client has been notified.";
            return RedirectToAction(nameof(Index));
        }

        // ================================================================== HELPERS
        private void ValidateTimeSlot(DateTime start, int durationMinutes, bool requireFuture)
        {
            var end = start.AddMinutes(durationMinutes);
            if (requireFuture && start < DateTime.Now.AddHours(1))
                ModelState.AddModelError(nameof(AppointmentFormViewModel.ScheduledAt), "Book at least one hour ahead.");
            if (start > DateTime.Now.AddMonths(3))
                ModelState.AddModelError(nameof(AppointmentFormViewModel.ScheduledAt), "Appointments can be booked up to three months ahead.");
            if (start.Hour < OpeningHour || end > start.Date.AddHours(ClosingHour))
                ModelState.AddModelError(nameof(AppointmentFormViewModel.ScheduledAt), $"Sessions run between {OpeningHour}:00 AM and {ClosingHour - 12}:00 PM.");
        }

        /// <summary>
        /// True if the trainer or the client already has an open appointment overlapping [start, start + duration).
        /// Two intervals overlap when each one starts before the other ends.
        /// </summary>
        private async Task<bool> HasConflictAsync(int trainerId, int clientId, DateTime start, int durationMinutes, int? excludeId, bool approvedOnly)
        {
            var end = start.AddMinutes(durationMinutes);
            int exclude = excludeId ?? 0;

            var query = _db.Appointments.Where(a => a.Id != exclude
                && (a.TrainerId == trainerId || a.ClientId == clientId)
                && a.ScheduledAt < end
                && a.ScheduledAt.AddMinutes(a.DurationMinutes) > start);

            query = approvedOnly
                ? query.Where(a => a.Status == AppointmentStatus.Approved)
                : query.Where(a => a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Approved);

            return await query.AnyAsync();
        }

        private async Task<IEnumerable<SelectListItem>> GetTrainerOptionsAsync(int? selectedId)
        {
            var trainers = await _db.Users
                .Where(u => u.Role == UserRole.Trainer && u.IsActive && u.IsApproved)
                .OrderBy(u => u.FullName)
                .Select(u => new { u.Id, Name = u.FullName + " - " + (u.Specialization ?? "Trainer") })
                .ToListAsync();
            return new SelectList(trainers, "Id", "Name", selectedId);
        }
    }
}
