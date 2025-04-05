
using AuthCsvApp.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ASMAPDP.Controllers
{
    [Route("auth")]
    public class AuthController : Controller
    {
        private readonly string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "users.csv");

        [HttpGet]
        [Route("register")]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [Route("register")]
        public IActionResult Register(User model)
        {
            if (ModelState.IsValid)
            {
                List<User> users = ReadUsersFromCsv();

                // Check if username already exists
                if (users.Any(u => u.Username == model.Username))
                {
                    ModelState.AddModelError("", "Username already exists!");
                    return View(model);
                }

                // Assign a new ID
                model.Id = users.Any() ? users.Max(u => u.Id) + 1 : 1;
                users.Add(model);
                WriteUsersToCsv(users);

                TempData["Success"] = "Registration successful! Please login.";
                return RedirectToAction("Login");
            }

            return View(model);
        }

        [HttpGet]
        [Route("login")]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [Route("login")]
        public IActionResult Login(string username, string password)
        {
            List<User> users = ReadUsersFromCsv();
            var user = users.FirstOrDefault(u => u.Username == username && u.Password == password);

            if (user != null)
            {
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("Role", user.Role.ToString());

                // Redirect based on role
                if (user.Role == UserRole.Admin)
                    return RedirectToAction("Dashboard", "Admin");
                else if (user.Role == UserRole.Teacher)
                    return RedirectToAction("Dashboard", "Teacher");
                else
                    return RedirectToAction("Dashboard", "Student");
            }

            ModelState.AddModelError("", "Invalid username or password!");
            return View();
        }

        [HttpGet]
        [Route("logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        private List<User> ReadUsersFromCsv()
        {
            List<User> users = new List<User>();
            if (!System.IO.File.Exists(filePath))
            {
                // Create the file with headers if it doesn't exist
                System.IO.File.WriteAllLines(filePath, new[] { "Id,FullName,Address,Username,Password,Role" });
                return users;
            }

            var lines = System.IO.File.ReadAllLines(filePath);
            foreach (var line in lines.Skip(1)) // Skip the header line
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var values = line.Split(',');
                if (values.Length < 6)
                {
                    System.Diagnostics.Debug.WriteLine($"Invalid line in users.csv: {line}");
                    continue;
                }

                try
                {
                    users.Add(new User
                    {
                        Id = int.Parse(values[0]),
                        FullName = values[1],
                        Address = values[2],
                        Username = values[3],
                        Password = values[4],
                        Role = Enum.TryParse(values[5], out UserRole role) ? role : UserRole.Student
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error parsing data in users.csv: {line}. Error: {ex.Message}");
                    continue;
                }
            }

            return users;
        }

        private void WriteUsersToCsv(List<User> users)
        {
            var lines = new List<string> { "Id,FullName,Address,Username,Password,Role" };
            foreach (var user in users)
            {
                // Sanitize user input to prevent CSV injection
                var fullName = user.FullName.Replace(",", " ");
                var address = user.Address.Replace(",", " ");
                var username = user.Username.Replace(",", " ");
                var password = user.Password.Replace(",", " ");
                var role = user.Role.ToString();

                lines.Add($"{user.Id},{fullName},{address},{username},{password},{role}");
            }

            System.IO.File.WriteAllLines(filePath, lines);
        }
    }
}