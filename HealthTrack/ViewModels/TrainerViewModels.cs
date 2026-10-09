using System.ComponentModel.DataAnnotations;
using HealthTrack.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HealthTrack.ViewModels
{
    /// <summary>One row of the trainer's client overview.</summary>
    public class ClientSummary
    {
        public int ClientId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public bool IsActive { get; set; }
        public double? LatestWeight { get; set; }
        public double? LatestBmi { get; set; }
        public double? WeightChange { get; set; }
        public DateTime? LastWorkout { get; set; }
        public int WorkoutsThisWeek { get; set; }
        public bool HasActivePlan { get; set; }

        public bool NeedsAttention => !LastWorkout.HasValue || LastWorkout.Value < DateTime.Today.AddDays(-7);
    }

    public class TrainerDashboardViewModel
    {
        public string TrainerName { get; set; }
        public List<ClientSummary> Clients { get; set; } = new();
        public List<Appointment> PendingAppointments { get; set; } = new();
        public List<Appointment> UpcomingAppointments { get; set; } = new();
        public List<WorkoutLog> RecentClientWorkouts { get; set; } = new();
        public int ActivePlanCount { get; set; }
        public int FeedbackThisMonth { get; set; }
    }

    public class ClientDetailsViewModel
    {
        public User Client { get; set; }
        public List<HealthMetric> Metrics { get; set; } = new();
        public List<WorkoutLog> Workouts { get; set; } = new();
        public List<MealLog> Meals { get; set; } = new();
        public List<WorkoutPlan> WorkoutPlans { get; set; } = new();
        public List<DietPlan> DietPlans { get; set; } = new();
        public List<Feedback> Feedback { get; set; } = new();
        public List<Appointment> Appointments { get; set; } = new();
        public FeedbackFormViewModel NewFeedback { get; set; } = new();
    }

    public class FeedbackFormViewModel
    {
        public int ClientId { get; set; }

        [Required(ErrorMessage = "Write your feedback.")]
        [StringLength(1000, MinimumLength = 5, ErrorMessage = "Feedback must be 5 to 1000 characters.")]
        public string Message { get; set; }
    }

    public class WorkoutPlanFormViewModel
    {
        [Required(ErrorMessage = "Choose a client.")]
        [Display(Name = "Client")]
        public int? ClientId { get; set; }

        [Required(ErrorMessage = "Give the plan a title.")]
        [StringLength(100)]
        public string Title { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [StringLength(100)]
        public string Goal { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Start date")]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [DataType(DataType.Date)]
        [Display(Name = "End date")]
        public DateTime EndDate { get; set; } = DateTime.Today.AddDays(28);

        public List<WorkoutExerciseInput> Exercises { get; set; } = new();

        public IEnumerable<SelectListItem> Clients { get; set; } = new List<SelectListItem>();
    }

    public class WorkoutExerciseInput
    {
        [Required(ErrorMessage = "Enter the exercise name.")]
        [StringLength(100)]
        public string Name { get; set; }

        public DayOfWeek Day { get; set; } = DayOfWeek.Monday;

        [Range(0, 20)]
        public int Sets { get; set; }

        [Range(0, 100)]
        public int Reps { get; set; }

        [Range(0, 300)]
        [Display(Name = "Minutes")]
        public int DurationMinutes { get; set; }

        [StringLength(200)]
        public string Notes { get; set; }
    }

    public class DietPlanFormViewModel
    {
        [Required(ErrorMessage = "Choose a client.")]
        [Display(Name = "Client")]
        public int? ClientId { get; set; }

        [Required(ErrorMessage = "Give the plan a title.")]
        [StringLength(100)]
        public string Title { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Range(800, 6000, ErrorMessage = "Daily target must be between 800 and 6000 kcal.")]
        [Display(Name = "Daily calorie target")]
        public int DailyCalorieTarget { get; set; } = 2000;

        [DataType(DataType.Date)]
        [Display(Name = "Start date")]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [DataType(DataType.Date)]
        [Display(Name = "End date")]
        public DateTime EndDate { get; set; } = DateTime.Today.AddDays(28);

        public List<DietItemInput> Items { get; set; } = new();

        public IEnumerable<SelectListItem> Clients { get; set; } = new List<SelectListItem>();
    }

    public class DietItemInput
    {
        public MealType MealType { get; set; }

        [Required(ErrorMessage = "Describe the meal.")]
        [StringLength(300)]
        public string Description { get; set; }

        [Range(0, 3000)]
        public int Calories { get; set; }

        [Display(Name = "Time")]
        public TimeSpan ScheduledTime { get; set; } = new TimeSpan(8, 0, 0);
    }

    public class TrainerPlansViewModel
    {
        public List<WorkoutPlan> WorkoutPlans { get; set; } = new();
        public List<DietPlan> DietPlans { get; set; } = new();
    }
}
