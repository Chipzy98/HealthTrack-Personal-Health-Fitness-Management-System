using System.ComponentModel.DataAnnotations;
using HealthTrack.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HealthTrack.ViewModels
{
    public class AppointmentFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Choose who you want to meet.")]
        [Display(Name = "Trainer / professional")]
        public int? TrainerId { get; set; }

        [Required(ErrorMessage = "Choose a date and time.")]
        [Display(Name = "Date and time")]
        public DateTime ScheduledAt { get; set; } = DateTime.Today.AddDays(1).AddHours(10);

        [Range(15, 180, ErrorMessage = "Duration must be between 15 and 180 minutes.")]
        [Display(Name = "Duration (minutes)")]
        public int DurationMinutes { get; set; } = 60;

        [Display(Name = "Consultation type")]
        public AppointmentType Type { get; set; }

        [StringLength(500)]
        [Display(Name = "What would you like to discuss?")]
        public string Reason { get; set; }

        // Trainer-only fields when updating
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

        [StringLength(500)]
        [Display(Name = "Notes for the client")]
        public string TrainerNotes { get; set; }

        public string ClientName { get; set; }

        public IEnumerable<SelectListItem> Trainers { get; set; } = new List<SelectListItem>();
    }

    public class AppointmentListViewModel
    {
        public List<Appointment> Upcoming { get; set; } = new();
        public List<Appointment> Past { get; set; } = new();
        public AppointmentStatus? StatusFilter { get; set; }
    }
}
