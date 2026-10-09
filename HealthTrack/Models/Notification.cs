using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models
{
    /// <summary>An in-app notification / reminder shown in the bell menu.</summary>
    public class Notification
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        [Required, StringLength(150)]
        public string Title { get; set; }

        [Required, StringLength(500)]
        public string Message { get; set; }

        public NotificationType Type { get; set; }

        /// <summary>Local URL the user is taken to when opening the notification.</summary>
        [StringLength(200)]
        public string Link { get; set; }

        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
