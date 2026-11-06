using System.Security.Claims;
using HealthTrack.Data;
using HealthTrack.Helpers;
using HealthTrack.Models;
using HealthTrack.Services;
using HealthTrack.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Controllers
{
    /// <summary>Registration, login / logout, account lockout, profile and password management.</summary>
    public class AccountController : Controller
    {
        private const int MaxFailedAttempts = 5;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        private readonly AppDbContext _db;
        private readonly IActivityLogger _activity;
        private readonly INotificationService _notifications;
        private readonly ILogger<AccountController> _logger;

        public AccountController(AppDbContext db, IActivityLogger activity, INotificationService notifications, ILogger<AccountController> logger)
        {
            _db = db;
            _activity = activity;
            _notifications = notifications;
            _logger = logger;
        }

        // ------------------------------------------------------------------ LOGIN
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToDashboard(User.GetRole() ?? UserRole.Client);
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid) return View(model);

            string email = model.Email.Trim().ToLowerInvariant();
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

            // Same message for unknown email and wrong password: do not reveal which accounts exist.
            const string invalidMessage = "The email or password is incorrect.";

            if (user == null)
            {
                await _activity.LogAsync(null, "LoginFailed", $"Unknown email {email}");
                ModelState.AddModelError(string.Empty, invalidMessage);
                return View(model);
            }

            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.Now)
            {
                ModelState.AddModelError(string.Empty,
                    $"This account is locked after too many failed attempts. Try again after {user.LockoutEnd.Value:hh:mm tt}.");
                return View(model);
            }

            if (!SecurePasswordHasher.Verify(model.Password, user.PasswordHash))
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= MaxFailedAttempts)
                {
                    user.LockoutEnd = DateTime.Now.Add(LockoutDuration);
                    user.FailedLoginAttempts = 0;
                    await _notifications.NotifyAsync(user.Id, "Account temporarily locked",
                        "Your account was locked for 15 minutes after 5 failed sign-in attempts.", NotificationType.Account);
                }
                await _db.SaveChangesAsync();
                await _activity.LogAsync(user.Id, "LoginFailed", "Wrong password");
                ModelState.AddModelError(string.Empty, invalidMessage);
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "This account has been deactivated. Contact the administrator.");
                return View(model);
            }

            if (!user.IsApproved)
            {
                ModelState.AddModelError(string.Empty, "Your trainer account is waiting for administrator approval.");
                return View(model);
            }

            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            user.LastLoginAt = DateTime.Now;
            await _db.SaveChangesAsync();

            await SignInUserAsync(user, model.RememberMe);
            await _activity.LogAsync(user.Id, "Login");

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToDashboard(user.Role);
        }

        // ------------------------------------------------------------------ REGISTER
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
            return View(new RegisterViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            // Only Client and Trainer can self-register. Administrators are created by another administrator.
            if (model.AccountType == UserRole.Admin)
                ModelState.AddModelError(nameof(model.AccountType), "Choose client or trainer.");

            if (model.AccountType == UserRole.Trainer && string.IsNullOrWhiteSpace(model.Specialization))
                ModelState.AddModelError(nameof(model.Specialization), "Enter your specialisation, e.g. Fitness coach or Nutritionist.");

            if (model.DateOfBirth.HasValue && (model.DateOfBirth.Value > DateTime.Today.AddYears(-13) || model.DateOfBirth.Value < DateTime.Today.AddYears(-110)))
                ModelState.AddModelError(nameof(model.DateOfBirth), "You must be at least 13 years old.");

            if (!model.AcceptPrivacyPolicy)
                ModelState.AddModelError(nameof(model.AcceptPrivacyPolicy), "You need to accept the privacy policy to create an account.");

            string email = model.Email?.Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(email) && await _db.Users.AnyAsync(u => u.Email == email))
                ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");

            if (!ModelState.IsValid) return View(model);

            var user = new User
            {
                FullName = model.FullName.Trim(),
                Email = email,
                PasswordHash = SecurePasswordHasher.Hash(model.Password),
                Role = model.AccountType,
                PhoneNumber = model.PhoneNumber?.Trim(),
                DateOfBirth = model.DateOfBirth,
                Gender = model.Gender,
                HeightCm = model.HeightCm,
                Specialization = model.AccountType == UserRole.Trainer ? model.Specialization.Trim() : null,
                IsActive = true,
                IsApproved = model.AccountType == UserRole.Client, // trainers wait for approval
                CreatedAt = DateTime.Now
            };

            try
            {
                _db.Users.Add(user);
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Registration failed for {Email}", email);
                ModelState.AddModelError(string.Empty, "Your account could not be created. Try again in a moment.");
                return View(model);
            }

            await _activity.LogAsync(user.Id, "Register", user.Role.ToString());

            if (user.Role == UserRole.Trainer)
            {
                await _notifications.NotifyAdminsAsync("Trainer awaiting approval",
                    $"{user.FullName} ({user.Specialization}) registered as a trainer.", NotificationType.Account, "/Admin/Dashboard");
                TempData["Success"] = "Account created. You can sign in once an administrator approves your trainer account.";
                return RedirectToAction(nameof(Login));
            }

            await _notifications.NotifyAsync(user.Id, "Welcome to HealthTrack",
                "Start by recording your weight and height under Health metrics.", NotificationType.General, "/Client/AddMetric");
            await _notifications.NotifyAdminsAsync("New client registered",
                $"{user.FullName} joined and needs a trainer.", NotificationType.Account, "/Admin/Dashboard");

            await SignInUserAsync(user, false);
            TempData["Success"] = $"Welcome, {user.FullName}! Your account is ready.";
            return RedirectToDashboard(user.Role);
        }

        // ------------------------------------------------------------------ LOGOUT
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _activity.LogAsync(User.GetUserId(), "Logout");
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["Success"] = "You have been signed out.";
            return RedirectToAction(nameof(Login));
        }

        public IActionResult AccessDenied() => View();

        // ------------------------------------------------------------------ PROFILE
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _db.Users.Include(u => u.Trainer).FirstOrDefaultAsync(u => u.Id == User.GetUserId());
            if (user == null) return NotFound();

            return View(new ProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                HeightCm = user.HeightCm,
                Specialization = user.Specialization,
                MedicalConditions = user.MedicalConditions,
                TrainerName = user.Trainer?.FullName,
                MemberSince = user.CreatedAt
            });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            var user = await _db.Users.Include(u => u.Trainer).FirstOrDefaultAsync(u => u.Id == User.GetUserId());
            if (user == null) return NotFound();

            if (!ModelState.IsValid)
            {
                model.Email = user.Email;
                model.Role = user.Role;
                model.TrainerName = user.Trainer?.FullName;
                model.MemberSince = user.CreatedAt;
                return View(model);
            }

            user.FullName = model.FullName.Trim();
            user.PhoneNumber = model.PhoneNumber?.Trim();
            user.DateOfBirth = model.DateOfBirth;
            user.Gender = model.Gender;
            user.HeightCm = model.HeightCm;
            user.MedicalConditions = string.IsNullOrWhiteSpace(model.MedicalConditions) ? null : model.MedicalConditions.Trim();
            if (user.Role == UserRole.Trainer) user.Specialization = model.Specialization?.Trim();

            await _db.SaveChangesAsync();
            await _activity.LogAsync(user.Id, "UpdateProfile");

            // Refresh the cookie so the new name shows in the header.
            await SignInUserAsync(user, false);
            TempData["Success"] = "Profile saved.";
            return RedirectToAction(nameof(Profile));
        }

        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _db.Users.FindAsync(User.GetUserId());
            if (user == null) return NotFound();

            if (!SecurePasswordHasher.Verify(model.CurrentPassword, user.PasswordHash))
            {
                ModelState.AddModelError(nameof(model.CurrentPassword), "The current password is incorrect.");
                return View(model);
            }

            user.PasswordHash = SecurePasswordHasher.Hash(model.NewPassword);
            await _db.SaveChangesAsync();
            await _activity.LogAsync(user.Id, "ChangePassword");

            TempData["Success"] = "Password changed.";
            return RedirectToAction(nameof(Profile));
        }

        // ------------------------------------------------------------------ HELPERS
        private async Task SignInUserAsync(User user, bool isPersistent)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var properties = new AuthenticationProperties
            {
                IsPersistent = isPersistent,
                ExpiresUtc = isPersistent ? DateTimeOffset.UtcNow.AddDays(7) : (DateTimeOffset?)null
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), properties);
        }

        private IActionResult RedirectToDashboard(UserRole role) => role switch
        {
            UserRole.Admin => RedirectToAction("Dashboard", "Admin"),
            UserRole.Trainer => RedirectToAction("Dashboard", "Trainer"),
            _ => RedirectToAction("Dashboard", "Client")
        };
    }
}
