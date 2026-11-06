using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models
{
    /// <summary>A personalised workout programme created by a trainer for one client.</summary>
    public class WorkoutPlan
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Title { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [StringLength(100)]
        public string Goal { get; set; }

        public int TrainerId { get; set; }
        public User Trainer { get; set; }

        public int ClientId { get; set; }
        public User Client { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<WorkoutExercise> Exercises { get; set; } = new List<WorkoutExercise>();
    }

    /// <summary>One exercise in a workout plan, scheduled on a given day of the week.</summary>
    public class WorkoutExercise
    {
        public int Id { get; set; }

        public int WorkoutPlanId { get; set; }
        public WorkoutPlan WorkoutPlan { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        public DayOfWeek Day { get; set; }
        public int Sets { get; set; }
        public int Reps { get; set; }
        public int DurationMinutes { get; set; }

        [StringLength(200)]
        public string Notes { get; set; }
    }
}
