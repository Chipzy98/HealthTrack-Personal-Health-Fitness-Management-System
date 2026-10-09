using HealthTrack.Data;
using HealthTrack.Models;

namespace HealthTrack.Services
{
    public interface IActivityLogger
    {
        Task LogAsync(int? userId, string action, string details = null);
    }

    /// <summary>Writes audit entries (logins, logs, plan changes...) used by the admin dashboard and reports.</summary>
    public class ActivityLogger : IActivityLogger
    {
        private readonly AppDbContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ActivityLogger> _logger;

        public ActivityLogger(AppDbContext db, IHttpContextAccessor httpContextAccessor, ILogger<ActivityLogger> logger)
        {
            _db = db;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task LogAsync(int? userId, string action, string details = null)
        {
            try
            {
                _db.ActivityLogs.Add(new ActivityLog
                {
                    UserId = userId,
                    Action = action,
                    Details = details?.Length > 300 ? details.Substring(0, 300) : details,
                    IpAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                    Timestamp = DateTime.Now
                });
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Auditing must never break the user's action.
                _logger.LogError(ex, "Failed to write activity log for action {Action}", action);
            }
        }
    }
}
