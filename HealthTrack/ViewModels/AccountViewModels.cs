using System.ComponentModel.DataAnnotations;
using HealthTrack.Models;

namespace HealthTrack.ViewModels
{
    /// <summary>Shared validation rules so the same policy is used everywhere.</summary>
    public static class ValidationRules
    {
        public const string StrongPassword = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,100}$";
        public const string StrongPasswordMessage = "Use at least 8 characters with an upper-case letter, a lower-case letter, a number and a symbol.";
        public const string PersonName = @"^[a-zA-Z][a-zA-Z\s\.\-']*$";
        public const string PersonNameMessage = "Names can contain letters, spaces, dots, hyphens and apostrophes only.";
        public const string Phone = @"^\+?[0-9]{9,15}$";
        public const string PhoneMessage = "Enter a phone number with 9 to 15 digits, e.g. 0771234567.";
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "Enter your email address.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [Display(Name = "Email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Enter your password.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Keep me signed in")]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Enter your full name.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be 3 to 100 characters.")]
        [RegularExpression(ValidationRules.PersonName, ErrorMessage = ValidationRules.PersonNameMessage)]
        [Display(Name = "Full name")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Enter your email address.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(150)]
        [Display(Name = "Email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Choose a password.")]
        [DataType(DataType.Password)]
        [RegularExpression(ValidationRules.StrongPassword, ErrorMessage = ValidationRules.StrongPasswordMessage)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirm your password.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; }

        [Display(Name = "I'm joining as")]
        public UserRole AccountType { get; set; } = UserRole.Client;

        [RegularExpression(ValidationRules.Phone, ErrorMessage = ValidationRules.PhoneMessage)]
        [Display(Name = "Phone number")]
        public string PhoneNumber { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of birth")]
        public DateTime? DateOfBirth { get; set; }

        public GenderType? Gender { get; set; }

        [Range(50, 260, ErrorMessage = "Height must be between 50 and 260 cm.")]
        [Display(Name = "Height (cm)")]
        public double? HeightCm { get; set; }

        [StringLength(100)]
        [Display(Name = "Specialisation")]
        public string Specialization { get; set; }

        [Display(Name = "I agree to the privacy policy")]
        public bool AcceptPrivacyPolicy { get; set; }
    }

    public class ProfileViewModel
    {
        [Required, StringLength(100, MinimumLength = 3)]
        [RegularExpression(ValidationRules.PersonName, ErrorMessage = ValidationRules.PersonNameMessage)]
        [Display(Name = "Full name")]
        public string FullName { get; set; }

        [Display(Name = "Email address")]
        public string Email { get; set; }

        public UserRole Role { get; set; }

        [RegularExpression(ValidationRules.Phone, ErrorMessage = ValidationRules.PhoneMessage)]
        [Display(Name = "Phone number")]
        public string PhoneNumber { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of birth")]
        public DateTime? DateOfBirth { get; set; }

        public GenderType? Gender { get; set; }

        [Range(50, 260, ErrorMessage = "Height must be between 50 and 260 cm.")]
        [Display(Name = "Height (cm)")]
        public double? HeightCm { get; set; }

        [StringLength(100)]
        [Display(Name = "Specialisation")]
        public string Specialization { get; set; }

        [StringLength(1000)]
        [Display(Name = "Medical conditions / allergies")]
        public string MedicalConditions { get; set; }

        public string TrainerName { get; set; }
        public DateTime MemberSince { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Enter your current password.")]
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "Enter a new password.")]
        [DataType(DataType.Password)]
        [RegularExpression(ValidationRules.StrongPassword, ErrorMessage = ValidationRules.StrongPasswordMessage)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Confirm the new password.")]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "The passwords do not match.")]
        [Display(Name = "Confirm new password")]
        public string ConfirmNewPassword { get; set; }
    }
}
