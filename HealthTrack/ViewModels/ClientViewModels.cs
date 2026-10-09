using System.ComponentModel.DataAnnotations;
using HealthTrack.Models;

namespace HealthTrack.ViewModels
{
    public class ClientDashboardViewModel
    {
        public User Client { get; set; }
        public User Trainer { get; set; }

        public HealthMetric LatestMetric { get; set; }
        public double? WeightChange { get; set; }

        public int WorkoutsThisWeek { get; set; }
        public int MinutesThisWeek { get; set; }
        public int CaloriesBurnedThisWeek { get; set; }
        public int CaloriesConsumedToday { get; set; }

        public WorkoutPlan ActiveWorkoutPlan { get; set; }
        public List<WorkoutExercise> TodaysExercises { get; set; } = new();
        public DietPlan ActiveDietPlan { get; set; }

        public List<Appointment> UpcomingAppointments { get; set; } = new();
        public List<Feedback> RecentFeedback { get; set; } = new();
        public List<WorkoutLog> RecentWorkouts { get; set; } = new();

        public List<string> WeightLabels { get; set; } = new();
        public List<double> WeightValues { get; set; } = new();
    }

    public class WorkoutLogFormViewModel
    {
        [Required(ErrorMessage = "Choose the workout date.")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Enter the exercise or activity.")]
        [StringLength(100)]
        [Display(Name = "Exercise / activity")]
        public string ExerciseName { get; set; }

        [Range(1, 600, ErrorMessage = "Duration must be between 1 and 600 minutes.")]
        [Display(Name = "Duration (minutes)")]
        public int DurationMinutes { get; set; } = 30;

        public WorkoutIntensity Intensity { get; set; } = WorkoutIntensity.Moderate;

        [Range(0, 5000, ErrorMessage = "Calories must be between 0 and 5000.")]
        [Display(Name = "Calories burned (leave empty to estimate)")]
        public int? CaloriesBurned { get; set; }

        [Range(0, 50)]
        public int? Sets { get; set; }

        [Range(0, 500)]
        public int? Reps { get; set; }

        [StringLength(300)]
        public string Notes { get; set; }

        public List<string> SuggestedExercises { get; set; } = new();
    }

    public class MealLogFormViewModel
    {
        [Required(ErrorMessage = "Choose when you ate.")]
        [Display(Name = "Date and time")]
        public DateTime LoggedAt { get; set; } = DateTime.Now;

        [Display(Name = "Meal")]
        public MealType MealType { get; set; }

        [Required(ErrorMessage = "Describe what you ate.")]
        [StringLength(300)]
        [Display(Name = "What did you eat?")]
        public string FoodItems { get; set; }

        [Range(0, 5000, ErrorMessage = "Calories must be between 0 and 5000.")]
        public int Calories { get; set; }

        [Range(0, 500)]
        [Display(Name = "Protein (g)")]
        public double? ProteinG { get; set; }

        [Range(0, 1000)]
        [Display(Name = "Carbs (g)")]
        public double? CarbsG { get; set; }

        [Range(0, 500)]
        [Display(Name = "Fat (g)")]
        public double? FatG { get; set; }

        public DietPlan ActiveDietPlan { get; set; }
    }

    public class HealthMetricFormViewModel
    {
        [Required]
        [Display(Name = "Date and time")]
        public DateTime RecordedAt { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Enter your weight.")]
        [Range(20, 400, ErrorMessage = "Weight must be between 20 and 400 kg.")]
        [Display(Name = "Weight (kg)")]
        public double? WeightKg { get; set; }

        [Required(ErrorMessage = "Enter your height.")]
        [Range(50, 260, ErrorMessage = "Height must be between 50 and 260 cm.")]
        [Display(Name = "Height (cm)")]
        public double? HeightCm { get; set; }

        [Range(2, 70, ErrorMessage = "Body fat must be between 2 and 70%.")]
        [Display(Name = "Body fat (%)")]
        public double? BodyFatPercent { get; set; }

        [Range(30, 220, ErrorMessage = "Heart rate must be between 30 and 220 bpm.")]
        [Display(Name = "Resting heart rate (bpm)")]
        public int? RestingHeartRate { get; set; }

        [Range(70, 250, ErrorMessage = "Systolic pressure must be between 70 and 250.")]
        [Display(Name = "Blood pressure - systolic")]
        public int? SystolicBp { get; set; }

        [Range(40, 150, ErrorMessage = "Diastolic pressure must be between 40 and 150.")]
        [Display(Name = "Blood pressure - diastolic")]
        public int? DiastolicBp { get; set; }

        [Range(0, 24, ErrorMessage = "Sleep must be between 0 and 24 hours.")]
        [Display(Name = "Sleep last night (hours)")]
        public double? SleepHours { get; set; }

        [StringLength(1000)]
        [Display(Name = "Private notes (encrypted)")]
        public string Notes { get; set; }
    }

    public class MyPlansViewModel
    {
        public List<WorkoutPlan> WorkoutPlans { get; set; } = new();
        public List<DietPlan> DietPlans { get; set; } = new();
        public List<Feedback> Feedback { get; set; } = new();
    }
}
