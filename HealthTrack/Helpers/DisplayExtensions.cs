using System.ComponentModel.DataAnnotations;
using System.Reflection;
using HealthTrack.Models;

namespace HealthTrack.Helpers
{
    public static class DisplayExtensions
    {
        /// <summary>Returns the [Display(Name)] of an enum value, or the value name.</summary>
        public static string GetDisplayName(this Enum value)
        {
            var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
            return member?.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? value.ToString();
        }

        /// <summary>Monday of the week that contains the date.</summary>
        public static DateTime StartOfWeek(this DateTime date)
        {
            int diff = ((int)date.DayOfWeek + 6) % 7;
            return date.Date.AddDays(-diff);
        }

        public static string StatusTone(this AppointmentStatus status) => status switch
        {
            AppointmentStatus.Approved => "good",
            AppointmentStatus.Pending => "warn",
            AppointmentStatus.Cancelled => "alert",
            _ => "neutral"
        };

        public static string Icon(this NotificationType type) => type switch
        {
            NotificationType.Appointment => "bi-calendar-event",
            NotificationType.WorkoutReminder => "bi-activity",
            NotificationType.MealReminder => "bi-egg-fried",
            NotificationType.Plan => "bi-clipboard2-pulse",
            NotificationType.Feedback => "bi-chat-left-text",
            NotificationType.Account => "bi-person-badge",
            NotificationType.HealthAlert => "bi-heart-pulse",
            _ => "bi-bell"
        };

        /// <summary>Human friendly relative time, e.g. "3 h ago".</summary>
        public static string TimeAgo(this DateTime dateTime)
        {
            var span = DateTime.Now - dateTime;
            if (span.TotalMinutes < 1) return "just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} min ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours} h ago";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays} d ago";
            return dateTime.ToString("dd MMM yyyy");
        }

        /// <summary>Escapes a value for a CSV cell.</summary>
        public static string ToCsvCell(this object value)
        {
            string text = value?.ToString() ?? string.Empty;
            // Neutralise spreadsheet formula injection.
            if (value is string && text.Length > 0 && "=+-@".Contains(text[0])) text = "'" + text;
            return text.Contains(',') || text.Contains('"') || text.Contains('\n')
                ? "\"" + text.Replace("\"", "\"\"") + "\""
                : text;
        }
    }
}
