using HealthTrack.Models;

namespace HealthTrack.ViewModels
{
    public class NotificationBellViewModel
    {
        public int UnreadCount { get; set; }
        public List<Notification> Latest { get; set; } = new();
    }

    public class ErrorViewModel
    {
        public string RequestId { get; set; }
        public int StatusCode { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
