using System.Diagnostics;
using HealthTrack.Helpers;
using HealthTrack.Models;
using HealthTrack.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace HealthTrack.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return User.GetRole() switch
                {
                    UserRole.Admin => RedirectToAction("Dashboard", "Admin"),
                    UserRole.Trainer => RedirectToAction("Dashboard", "Trainer"),
                    _ => RedirectToAction("Dashboard", "Client")
                };
            }
            return View();
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                StatusCode = 500
            });
        }

        /// <summary>Friendly page for 404, 403 etc. (wired up with UseStatusCodePagesWithReExecute).</summary>
        public IActionResult HttpStatus(int code)
        {
            return View(new ErrorViewModel { StatusCode = code, RequestId = HttpContext.TraceIdentifier });
        }
    }
}
