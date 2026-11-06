using HealthTrack.Data;
using HealthTrack.Helpers;
using HealthTrack.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Controllers
{
    /// <summary>Notification centre: list, open, mark as read, delete, and trigger reminder generation.</summary>
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly AppDbContext _db;

        public NotificationsController(AppDbContext db)
        {
            _db = db;
        }

        private int CurrentUserId => User.GetUserId();

        public async Task<IActionResult> Index()
        {
            var notifications = await _db.Notifications
                .Where(n => n.UserId == CurrentUserId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(100)
                .ToListAsync();
            return View(notifications);
        }

        /// <summary>Marks a notification as read and follows its link.</summary>
        [HttpPost]
        public async Task<IActionResult> Open(int id)
        {
            var notification = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == CurrentUserId);
            if (notification == null) return NotFound();

            notification.IsRead = true;
            await _db.SaveChangesAsync();

            // Only redirect to local URLs (prevents open-redirect attacks).
            if (!string.IsNullOrEmpty(notification.Link) && Url.IsLocalUrl(notification.Link))
                return LocalRedirect(notification.Link);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllRead()
        {
            var unread = await _db.Notifications.Where(n => n.UserId == CurrentUserId && !n.IsRead).ToListAsync();
            unread.ForEach(n => n.IsRead = true);
            await _db.SaveChangesAsync();

            TempData["Success"] = unread.Count > 0 ? $"{unread.Count} notification(s) marked as read." : "You're all caught up.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var notification = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == CurrentUserId);
            if (notification == null) return NotFound();

            _db.Notifications.Remove(notification);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        /// <summary>Runs the reminder check immediately (the background service also runs it every few minutes).</summary>
        [HttpPost]
        public async Task<IActionResult> CheckReminders()
        {
            int created = await ReminderBackgroundService.GenerateRemindersAsync(_db);
            TempData["Success"] = created > 0 ? $"{created} new reminder(s) created." : "No reminders are due right now.";
            return RedirectToAction(nameof(Index));
        }
    }
}
