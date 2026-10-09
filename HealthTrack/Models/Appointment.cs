using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthTrack.Models
{
    /// <summary>A consultation requested by a client with a trainer or healthcare professional.</summary>
    public class Appointment
    {
        public int Id { get; set; }

        public int ClientId { get; set; }
        public User Client { get; set; }

        public int TrainerId { get; set; }
        public User Trainer { get; set; }

        public DateTime ScheduledAt { get; set; }
        public int DurationMinutes { get; set; } = 60;

        public AppointmentType Type { get; set; }

        [StringLength(500)]
        public string Reason { get; set; }

        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

        [StringLength(500)]
        public string TrainerNotes { get; set; }

        /// <summary>Set by the reminder service so a reminder is only sent once.</summary>
        public bool ReminderSent { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        [NotMapped]
        public DateTime EndsAt => ScheduledAt.AddMinutes(DurationMinutes);

        [NotMapped]
        public bool IsOpen => Status == AppointmentStatus.Pending || Status == AppointmentStatus.Approved;
    }
}
