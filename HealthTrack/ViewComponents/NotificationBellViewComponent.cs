using HealthTrack.Data;
using HealthTrack.Helpers;
using HealthTrack.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.ViewComponents
{
    /// <summary>Renders the bell icon with the unread count and latest notifications in the top bar.</summary>
    public class NotificationBellViewComponent : ViewComponent
    {
        private readonly AppDbContext _db;

        public NotificationBellViewComponent(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            int userId = UserClaimsPrincipal.GetUserId();
            var unread = _db.Notifications.Where(n => n.UserId == userId && !n.IsRead);

            var model = new NotificationBellViewModel
            {
                UnreadCount = await unread.CountAsync(),
                Latest = await unread.OrderByDescending(n => n.CreatedAt).Take(5).ToListAsync()
            };
            return View(model);
        }
    }
}
