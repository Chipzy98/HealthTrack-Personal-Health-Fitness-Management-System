using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models
{
    /// <summary>Audit trail entry. Powers the admin "user activity / system usage" views and engagement reports.</summary>
    public class ActivityLog
    {
        public int Id { get; set; }

        public int? UserId { get; set; }
        public User User { get; set; }

        [Required, StringLength(50)]
        public string Action { get; set; }

        [StringLength(300)]
        public string Details { get; set; }

        [StringLength(50)]
        public string IpAddress { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
