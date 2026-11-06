using System.ComponentModel.DataAnnotations;
using HealthTrack.Models;

namespace HealthTrack.ViewModels
{
    public class ClientPerformanceRow
    {
        public int ClientId { get; set; }
        public string ClientName { get; set; }
        public string TrainerName { get; set; }
        public int Workouts { get; set; }
        public int TotalMinutes { get; set; }
        public int CaloriesBurned { get; set; }
        public int MealsLogged { get; set; }
        public int AverageDailyIntake { get; set; }
        public double? StartWeight { get; set; }
        public double? CurrentWeight { get; set; }
        public double? WeightChange { get; set; }
        public double? CurrentBmi { get; set; }
        public int ActiveDays { get; set; }
        public int EngagementPercent { get; set; }
    }

    public class RoutinePopularityRow
    {
        public string Name { get; set; }
        public int TimesLogged { get; set; }
        public int TotalMinutes { get; set; }
        public int UniqueClients { get; set; }
    }

    public class TrainerPerformanceRow
    {
        public string TrainerName { get; set; }
        public string Specialization { get; set; }
        public int Clients { get; set; }
        public int PlansCreated { get; set; }
        public int FeedbackGiven { get; set; }
        public int AppointmentsCompleted { get; set; }
    }

    public class ReportViewModel
    {
        [DataType(DataType.Date)]
        public DateTime From { get; set; }

        [DataType(DataType.Date)]
        public DateTime To { get; set; }

        public bool IsAdmin { get; set; }

        public int TotalClients { get; set; }
        public int ActiveClients { get; set; }
        public int TotalWorkoutLogs { get; set; }
        public int TotalMealLogs { get; set; }
        public int TotalMetricEntries { get; set; }
        public int Logins { get; set; }
        public Dictionary<AppointmentStatus, int> AppointmentsByStatus { get; set; } = new();

        public List<ClientPerformanceRow> ClientPerformance { get; set; } = new();
        public List<RoutinePopularityRow> PopularRoutines { get; set; } = new();
        public List<RoutinePopularityRow> MostAssignedExercises { get; set; } = new();
        public List<TrainerPerformanceRow> TrainerPerformance { get; set; } = new();

        public List<string> EngagementLabels { get; set; } = new();
        public List<int> EngagementValues { get; set; } = new();
    }
}
