using System.ComponentModel.DataAnnotations;
using HealthTrack.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HealthTrack.ViewModels
{
    public class UserBrief
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Specialization { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class TrainerAssignmentRow
    {
        public int TrainerId { get; set; }
        public string TrainerName { get; set; }
        public string Specialization { get; set; }
        public bool IsActive { get; set; }
        public int ClientCount { get; set; }
        public int UpcomingAppointments { get; set; }
    }

    public class AdminDashboardViewModel
    {
        public int TotalClients { get; set; }
        public int TotalTrainers { get; set; }
        public int TotalAdmins { get; set; }
        public int InactiveUsers { get; set; }
        public int ActiveUsersLast7Days { get; set; }
        public int LoginsToday { get; set; }
        public int TotalWorkoutLogs { get; set; }
        public int TotalMealLogs { get; set; }
        public int TotalMetricEntries { get; set; }
        public int OpenAppointments { get; set; }

        public List<UserBrief> PendingTrainers { get; set; } = new();
        public List<UserBrief> UnassignedClients { get; set; } = new();
        public List<TrainerAssignmentRow> TrainerAssignments { get; set; } = new();
        public List<ActivityLog> RecentActivity { get; set; } = new();
        public IEnumerable<SelectListItem> TrainerOptions { get; set; } = new List<SelectListItem>();

        public List<string> ActivityLabels { get; set; } = new();
        public List<int> LoginCounts { get; set; } = new();
        public List<int> ActionCounts { get; set; } = new();
    }

    public class UserListViewModel
    {
        public List<User> Users { get; set; } = new();
        public UserRole? RoleFilter { get; set; }
        public string Search { get; set; }
    }

    public class AdminUserFormViewModel
    {
        public int Id { get; set; }

        [Required, StringLength(100, MinimumLength = 3)]
        [RegularExpression(ValidationRules.PersonName, ErrorMessage = ValidationRules.PersonNameMessage)]
        [Display(Name = "Full name")]
        public string FullName { get; set; }

        [Required, EmailAddress, StringLength(150)]
        [Display(Name = "Email address")]
        public string Email { get; set; }

        public UserRole Role { get; set; } = UserRole.Client;

        [DataType(DataType.Password)]
        [RegularExpression(ValidationRules.StrongPassword, ErrorMessage = ValidationRules.StrongPasswordMessage)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [RegularExpression(ValidationRules.Phone, ErrorMessage = ValidationRules.PhoneMessage)]
        [Display(Name = "Phone number")]
        public string PhoneNumber { get; set; }

        [StringLength(100)]
        [Display(Name = "Specialisation (trainers)")]
        public string Specialization { get; set; }

        [Display(Name = "Assigned trainer (clients)")]
        public int? TrainerId { get; set; }

        [Display(Name = "Account active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Approved")]
        public bool IsApproved { get; set; } = true;

        public IEnumerable<SelectListItem> Trainers { get; set; } = new List<SelectListItem>();
    }

    public class ActivityLogViewModel
    {
        public List<ActivityLog> Logs { get; set; } = new();
        public List<string> Actions { get; set; } = new();
        public string ActionFilter { get; set; }
        public int Page { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
    }
}
