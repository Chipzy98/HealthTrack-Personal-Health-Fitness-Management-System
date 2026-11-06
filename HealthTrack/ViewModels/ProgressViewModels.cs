namespace HealthTrack.ViewModels
{
    public class ProgressViewModel
    {
        public int ClientId { get; set; }
        public string ClientName { get; set; }
        public bool ViewedByStaff { get; set; }
        public int Days { get; set; }

        public double? StartWeight { get; set; }
        public double? CurrentWeight { get; set; }
        public double? WeightChange { get; set; }
        public double? CurrentBmi { get; set; }
        public int TotalWorkouts { get; set; }
        public int TotalMinutes { get; set; }
        public int TotalCaloriesBurned { get; set; }
        public int AverageDailyIntake { get; set; }
        public string LatestBloodPressure { get; set; }

        public List<string> MetricLabels { get; set; } = new();
        public List<double> WeightValues { get; set; } = new();
        public List<double> BmiValues { get; set; } = new();
        public List<int?> HeartRateValues { get; set; } = new();

        public List<string> DailyLabels { get; set; } = new();
        public List<int> CaloriesBurnedDaily { get; set; } = new();
        public List<int> CaloriesConsumedDaily { get; set; } = new();

        public List<string> WeekLabels { get; set; } = new();
        public List<int> WorkoutMinutesWeekly { get; set; } = new();
    }
}
