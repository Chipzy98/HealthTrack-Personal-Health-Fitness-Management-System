using HealthTrack.Data;
using HealthTrack.Models;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Services
{
    public interface INotificationService
    {
        Task NotifyAsync(int userId, string title, string message, NotificationType type, string link = null);
        Task NotifyAdminsAsync(string title, string message, NotificationType type, string link = null);
    }

    /// <summary>Creates in-app notifications for events such as appointment changes, new plans and feedback.</summary>
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(AppDbContext db, ILogger<NotificationService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task NotifyAsync(int userId, string title, string message, NotificationType type, string link = null)
        {
            try
            {
                _db.Notifications.Add(Build(userId, title, message, type, link));
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not create notification for user {UserId}", userId);
            }
        }

        public async Task NotifyAdminsAsync(string title, string message, NotificationType type, string link = null)
        {
            try
            {
                var adminIds = await _db.Users
                    .Where(u => u.Role == UserRole.Admin && u.IsActive)
                    .Select(u => u.Id)
                    .ToListAsync();

                foreach (var adminId in adminIds)
                    _db.Notifications.Add(Build(adminId, title, message, type, link));

                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not notify administrators");
            }
        }

        private static Notification Build(int userId, string title, string message, NotificationType type, string link) => new Notification
        {
            UserId = userId,
            Title = Truncate(title, 150),
            Message = Truncate(message, 500),
            Type = type,
            Link = link,
            CreatedAt = DateTime.Now
        };

        private static string Truncate(string value, int max) =>
            string.IsNullOrEmpty(value) || value.Length <= max ? value : value.Substring(0, max - 1) + "…";
    }
}
