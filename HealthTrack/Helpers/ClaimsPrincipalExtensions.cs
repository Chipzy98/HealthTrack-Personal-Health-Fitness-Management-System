using System.Security.Claims;
using HealthTrack.Models;

namespace HealthTrack.Helpers
{
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>Reads the logged-in user's database Id from the NameIdentifier claim.</summary>
        public static int GetUserId(this ClaimsPrincipal user) =>
            int.TryParse(user?.FindFirstValue(ClaimTypes.NameIdentifier), out int id) ? id : 0;

        public static UserRole? GetRole(this ClaimsPrincipal user) =>
            Enum.TryParse(user?.FindFirstValue(ClaimTypes.Role), out UserRole role) ? role : null;
    }
}
