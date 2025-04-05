using System.Diagnostics;
using AuthCsvApp.Models;
using Microsoft.AspNetCore.Mvc;

namespace AuthCsvApp.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet]
        [Route("")]
        public IActionResult Index()
        {
            // If user is logged in, redirect to their dashboard
            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(role))
            {
                if (role == "Admin")
                    return RedirectToAction("Dashboard", "Admin");
                else if (role == "Teacher")
                    return RedirectToAction("Dashboard", "Teacher");
                else
                    return RedirectToAction("Dashboard", "Student");
            }

            // If not logged in, redirect to login page
            return RedirectToAction("Login", "Auth");
        }
    }
}
