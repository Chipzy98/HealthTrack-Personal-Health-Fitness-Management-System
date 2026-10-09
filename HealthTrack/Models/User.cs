using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthTrack.Models
{
    /// <summary>
    /// A single account table for every role. Clients optionally point to the trainer
    /// who manages them (self-referencing relationship Trainer -> Clients).
    /// </summary>
    public class User
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string FullName { get; set; }

        [Required, StringLength(150)]
        public string Email { get; set; }

        /// <summary>PBKDF2 hash in the format "iterations.salt.hash". The plain password is never stored.</summary>
        [Required]
        public string PasswordHash { get; set; }

        public UserRole Role { get; set; }

        [StringLength(20)]
        public string PhoneNumber { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public GenderType? Gender { get; set; }

        public double? HeightCm { get; set; }

        /// <summary>Trainer / healthcare professional speciality, e.g. "Nutritionist".</summary>
        [StringLength(100)]
        public string Specialization { get; set; }

        /// <summary>Sensitive health information. Encrypted at rest by an EF Core value converter (see AppDbContext).</summary>
        public string MedicalConditions { get; set; }

        public int? TrainerId { get; set; }
        public User Trainer { get; set; }
        public ICollection<User> Clients { get; set; } = new List<User>();

        public bool IsActive { get; set; } = true;

        /// <summary>Self-registered trainers must be approved by an administrator before they can log in.</summary>
        public bool IsApproved { get; set; } = true;

        public int FailedLoginAttempts { get; set; }
        public DateTime? LockoutEnd { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? LastLoginAt { get; set; }

        [NotMapped]
        public int? Age
        {
            get
            {
                if (!DateOfBirth.HasValue) return null;
                var today = DateTime.Today;
                int age = today.Year - DateOfBirth.Value.Year;
                if (DateOfBirth.Value.Date > today.AddYears(-age)) age--;
                return age;
            }
        }

        [NotMapped]
        public string Initials
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FullName)) return "?";
                var parts = FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                return parts.Length == 1
                    ? parts[0].Substring(0, 1).ToUpper()
                    : (parts[0].Substring(0, 1) + parts[^1].Substring(0, 1)).ToUpper();
            }
        }
    }
}
