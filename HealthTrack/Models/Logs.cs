using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models
{
    /// <summary>A workout session recorded by a client.</summary>
    public class WorkoutLog
    {
        public int Id { get; set; }

        public int ClientId { get; set; }
        public User Client { get; set; }

        /// <summary>The plan that was active when the workout was logged (optional).</summary>
        public int? WorkoutPlanId { get; set; }
        public WorkoutPlan WorkoutPlan { get; set; }

        public DateTime Date { get; set; }

        [Required, StringLength(100)]
        public string ExerciseName { get; set; }

        public int DurationMinutes { get; set; }
        public int CaloriesBurned { get; set; }
        public int? Sets { get; set; }
        public int? Reps { get; set; }
        public WorkoutIntensity Intensity { get; set; }

        [StringLength(300)]
        public string Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    /// <summary>A meal recorded by a client.</summary>
    public class MealLog
    {
        public int Id { get; set; }

        public int ClientId { get; set; }
        public User Client { get; set; }

        public DateTime LoggedAt { get; set; }
        public MealType MealType { get; set; }

        [Required, StringLength(300)]
        public string FoodItems { get; set; }

        public int Calories { get; set; }
        public double? ProteinG { get; set; }
        public double? CarbsG { get; set; }
        public double? FatG { get; set; }
    }

    /// <summary>Body measurements recorded by a client. BMI is calculated on the server.</summary>
    public class HealthMetric
    {
        public int Id { get; set; }

        public int ClientId { get; set; }
        public User Client { get; set; }

        public DateTime RecordedAt { get; set; }

        public double WeightKg { get; set; }
        public double HeightCm { get; set; }
        public double Bmi { get; set; }

        public double? BodyFatPercent { get; set; }
        public int? RestingHeartRate { get; set; }
        public int? SystolicBp { get; set; }
        public int? DiastolicBp { get; set; }
        public double? SleepHours { get; set; }

        /// <summary>Private health notes. Encrypted at rest.</summary>
        public string Notes { get; set; }
    }
}
