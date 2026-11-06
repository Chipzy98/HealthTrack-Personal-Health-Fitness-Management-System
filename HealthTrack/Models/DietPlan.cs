using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models
{
    /// <summary>A personalised meal schedule created by a trainer / nutritionist for one client.</summary>
    public class DietPlan
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Title { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public int DailyCalorieTarget { get; set; }

        public int TrainerId { get; set; }
        public User Trainer { get; set; }

        public int ClientId { get; set; }
        public User Client { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<DietPlanItem> Items { get; set; } = new List<DietPlanItem>();
    }

    /// <summary>A scheduled meal inside a diet plan. ScheduledTime drives the meal reminders.</summary>
    public class DietPlanItem
    {
        public int Id { get; set; }

        public int DietPlanId { get; set; }
        public DietPlan DietPlan { get; set; }

        public MealType MealType { get; set; }

        [Required, StringLength(300)]
        public string Description { get; set; }

        public int Calories { get; set; }

        public TimeSpan ScheduledTime { get; set; }
    }
}
