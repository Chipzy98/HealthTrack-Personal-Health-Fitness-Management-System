using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models
{
    /// <summary>Written feedback from a trainer to one of their clients.</summary>
    public class Feedback
    {
        public int Id { get; set; }

        public int TrainerId { get; set; }
        public User Trainer { get; set; }

        public int ClientId { get; set; }
        public User Client { get; set; }

        [Required, StringLength(1000)]
        public string Message { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
