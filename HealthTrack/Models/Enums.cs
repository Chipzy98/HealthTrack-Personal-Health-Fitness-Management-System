using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models
{
    /// <summary>The three roles supported by the system. Stored as an int, exposed as a role claim string.</summary>
    public enum UserRole
    {
        Client,
        Trainer,
        Admin
    }

    public enum GenderType
    {
        Male,
        Female,
        Other
    }

    /// <summary>Life-cycle of an appointment: Pending -> Approved -> Completed, or Cancelled at any open stage.</summary>
    public enum AppointmentStatus
    {
        Pending,
        Approved,
        Cancelled,
        Completed
    }

    public enum AppointmentType
    {
        [Display(Name = "Training consultation")] TrainingConsultation,
        [Display(Name = "Nutrition consultation")] NutritionConsultation,
        [Display(Name = "Health check-up")] HealthCheck,
        [Display(Name = "Physiotherapy session")] Physiotherapy,
        [Display(Name = "Progress review")] ProgressReview
    }

    public enum MealType
    {
        Breakfast,
        Lunch,
        Dinner,
        Snack
    }

    public enum WorkoutIntensity
    {
        Low,
        Moderate,
        High
    }

    public enum NotificationType
    {
        General,
        Appointment,
        WorkoutReminder,
        MealReminder,
        Plan,
        Feedback,
        Account,
        HealthAlert
    }
}
