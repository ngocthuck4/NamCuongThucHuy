
using AuthCsvApp.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AuthCsvApp.Controllers
{
    [Route("admin/classes")]
    public class AdminClassesController : Controller
    {
        private readonly string classesFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "classes.csv");
        private readonly string subjectsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "subjects.csv");
        private readonly string semestersFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "semesters.csv");
        private readonly string usersFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "users.csv");

        [HttpGet]
        [Route("index")]
        public IActionResult Index()
        {
            var adminUsername = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(adminUsername) || HttpContext.Session.GetString("Role") != "Admin")
            {
                return RedirectToAction("Login", "Auth");
            }

            var classes = ReadClassesFromCsv();
            ViewBag.Subjects = ReadSubjectsFromCsv();
            ViewBag.Semesters = ReadSemestersFromCsv();
            ViewBag.Teachers = ReadUsersFromCsv().Where(u => u.Role == UserRole.Teacher).ToList();
            return View("~/Views/Admin/Classes/Index.cshtml", classes);
        }

        [HttpGet]
        [Route("add")]
        public IActionResult Add()
        {
            var adminUsername = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(adminUsername) || HttpContext.Session.GetString("Role") != "Admin")
            {
                return RedirectToAction("Login", "Auth");
            }

            ViewBag.Subjects = ReadSubjectsFromCsv();
            ViewBag.Semesters = ReadSemestersFromCsv();
            ViewBag.Teachers = ReadUsersFromCsv().Where(u => u.Role == UserRole.Teacher).ToList();
            return View("~/Views/Admin/Classes/Add.cshtml");
        }

        [HttpPost]
        [Route("add")]
        public IActionResult Add(Class model)
        {
            var adminUsername = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(adminUsername) || HttpContext.Session.GetString("Role") != "Admin")
            {
                return RedirectToAction("Login", "Auth");
            }

            if (ModelState.IsValid)
            {
                var subjects = ReadSubjectsFromCsv();
                var semesters = ReadSemestersFromCsv();
                var teachers = ReadUsersFromCsv().Where(u => u.Role == UserRole.Teacher).ToList();
                var classes = ReadClassesFromCsv();

                // Validate SubjectId, SemesterId, TeacherUsername
                if (!subjects.Any(s => s.Id == model.SubjectId))
                {
                    ModelState.AddModelError("SubjectId", "Invalid subject selected.");
                    ViewBag.Subjects = subjects;
                    ViewBag.Semesters = semesters;
                    ViewBag.Teachers = teachers;
                    return View(model);
                }

                if (!semesters.Any(s => s.Id == model.SemesterId))
                {
                    ModelState.AddModelError("SemesterId", "Invalid semester selected.");
                    ViewBag.Subjects = subjects;
                    ViewBag.Semesters = semesters;
                    ViewBag.Teachers = teachers;
                    return View(model);
                }

                if (!teachers.Any(t => t.Username == model.TeacherUsername))
                {
                    ModelState.AddModelError("TeacherUsername", "Invalid teacher selected.");
                    ViewBag.Subjects = subjects;
                    ViewBag.Semesters = semesters;
                    ViewBag.Teachers = teachers;
                    return View(model);
                }

                // Check for schedule conflicts for the teacher
                var teacherClasses = classes.Where(c => c.TeacherUsername == model.TeacherUsername && c.SemesterId == model.SemesterId).ToList();
                if (teacherClasses.Any(c => c.Schedule == model.Schedule))
                {
                    ModelState.AddModelError("Schedule", "The teacher already has a class scheduled at this time.");
                    ViewBag.Subjects = subjects;
                    ViewBag.Semesters = semesters;
                    ViewBag.Teachers = teachers;
                    return View(model);
                }

                model.Id = classes.Any() ? classes.Max(c => c.Id) + 1 : 1;
                classes.Add(model);
                WriteClassesToCsv(classes);

                TempData["Success"] = "Class added successfully!";
                return RedirectToAction("Index");
            }

            ViewBag.Subjects = ReadSubjectsFromCsv();
            ViewBag.Semesters = ReadSemestersFromCsv();
            ViewBag.Teachers = ReadUsersFromCsv().Where(u => u.Role == UserRole.Teacher).ToList();
            return View(model);
        }

        [HttpGet]
        [Route("edit/{id}")]
        public IActionResult Edit(int id)
        {
            var adminUsername = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(adminUsername) || HttpContext.Session.GetString("Role") != "Admin")
            {
                return RedirectToAction("Login", "Auth");
            }

            var classes = ReadClassesFromCsv();
            var classToEdit = classes.FirstOrDefault(c => c.Id == id);
            if (classToEdit == null)
            {
                return NotFound("Class not found.");
            }

            ViewBag.Subjects = ReadSubjectsFromCsv();
            ViewBag.Semesters = ReadSemestersFromCsv();
            ViewBag.Teachers = ReadUsersFromCsv().Where(u => u.Role == UserRole.Teacher).ToList();
            return View("~/Views/Admin/Classes/Edit.cshtml", classToEdit);
        }

        [HttpPost]
        [Route("edit/{id}")]
        public IActionResult Edit(int id, Class model)
        {
            var adminUsername = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(adminUsername) || HttpContext.Session.GetString("Role") != "Admin")
            {
                return RedirectToAction("Login", "Auth");
            }

            if (ModelState.IsValid)
            {
                var classes = ReadClassesFromCsv();
                var classToEdit = classes.FirstOrDefault(c => c.Id == id);
                if (classToEdit == null)
                {
                    return NotFound("Class not found.");
                }

                classToEdit.SubjectId = model.SubjectId;
                classToEdit.SemesterId = model.SemesterId;
                classToEdit.TeacherUsername = model.TeacherUsername;
                classToEdit.Schedule = model.Schedule;
                WriteClassesToCsv(classes);

                TempData["Success"] = "Class updated successfully!";
                return RedirectToAction("Index");
            }

            ViewBag.Subjects = ReadSubjectsFromCsv();
            ViewBag.Semesters = ReadSemestersFromCsv();
            ViewBag.Teachers = ReadUsersFromCsv().Where(u => u.Role == UserRole.Teacher).ToList();
            return View(model);
        }

        [HttpPost]
        [Route("delete/{id}")]
        public IActionResult Delete(int id)
        {
            var adminUsername = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(adminUsername) || HttpContext.Session.GetString("Role") != "Admin")
            {
                return RedirectToAction("Login", "Auth");
            }

            var classes = ReadClassesFromCsv();
            var classToDelete = classes.FirstOrDefault(c => c.Id == id);
            if (classToDelete == null)
            {
                return NotFound("Class not found.");
            }

            classes.Remove(classToDelete);
            WriteClassesToCsv(classes);

            TempData["Success"] = "Class deleted successfully!";
            return RedirectToAction("Index");
        }

        private List<Class> ReadClassesFromCsv()
        {
            List<Class> classes = new List<Class>();
            if (!System.IO.File.Exists(classesFilePath))
            {
                System.IO.File.WriteAllLines(classesFilePath, new[] { "Id,SubjectId,SemesterId,TeacherUsername,Schedule" });
                return classes;
            }

            var lines = System.IO.File.ReadAllLines(classesFilePath);
            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var values = line.Split(',');
                if (values.Length < 5)
                {
                    System.Diagnostics.Debug.WriteLine($"Invalid line in classes.csv: {line}");
                    continue;
                }

                try
                {
                    classes.Add(new Class
                    {
                        Id = int.Parse(values[0]),
                        SubjectId = int.Parse(values[1]),
                        SemesterId = int.Parse(values[2]),
                        TeacherUsername = values[3],
                        Schedule = values[4]
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error parsing data in classes.csv: {line}. Error: {ex.Message}");
                    continue;
                }
            }

            return classes;
        }

        private void WriteClassesToCsv(List<Class> classes)
        {
            var lines = new List<string> { "Id,SubjectId,SemesterId,TeacherUsername,Schedule" };
            foreach (var c in classes)
            {
                var schedule = c.Schedule.Replace(",", " ");
                lines.Add($"{c.Id},{c.SubjectId},{c.SemesterId},{c.TeacherUsername},{schedule}");
            }
            System.IO.File.WriteAllLines(classesFilePath, lines);
        }

        private List<Subject> ReadSubjectsFromCsv()
        {
            List<Subject> subjects = new List<Subject>();
            if (!System.IO.File.Exists(subjectsFilePath))
            {
                System.IO.File.WriteAllLines(subjectsFilePath, new[] { "Id,Name,Description" });
                return subjects;
            }

            var lines = System.IO.File.ReadAllLines(subjectsFilePath);
            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var values = line.Split(',');
                if (values.Length < 3)
                {
                    System.Diagnostics.Debug.WriteLine($"Invalid line in subjects.csv: {line}");
                    continue;
                }

                try
                {
                    subjects.Add(new Subject
                    {
                        Id = int.Parse(values[0]),
                        Name = values[1],
                        Description = values[2]
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error parsing data in subjects.csv: {line}. Error: {ex.Message}");
                    continue;
                }
            }

            return subjects;
        }

        private List<Semester> ReadSemestersFromCsv()
        {
            List<Semester> semesters = new List<Semester>();
            if (!System.IO.File.Exists(semestersFilePath))
            {
                System.IO.File.WriteAllLines(semestersFilePath, new[] { "Id,Name,StartDate,EndDate" });
                return semesters;
            }

            var lines = System.IO.File.ReadAllLines(semestersFilePath);
            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var values = line.Split(',');
                if (values.Length < 4)
                {
                    System.Diagnostics.Debug.WriteLine($"Invalid line in semesters.csv: {line}");
                    continue;
                }

                try
                {
                    semesters.Add(new Semester
                    {
                        Id = int.Parse(values[0]),
                        Name = values[1],
                        StartDate = DateTime.Parse(values[2]),
                        EndDate = DateTime.Parse(values[3])
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error parsing data in semesters.csv: {line}. Error: {ex.Message}");
                    continue;
                }
            }

            return semesters;
        }

        private List<User> ReadUsersFromCsv()
        {
            List<User> users = new List<User>();
            if (!System.IO.File.Exists(usersFilePath))
            {
                System.IO.File.WriteAllLines(usersFilePath, new[] { "Id,FullName,Address,Username,Password,Role" });
                return users;
            }

            var lines = System.IO.File.ReadAllLines(usersFilePath);
            foreach (var line in lines.Skip(1))
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
    }
}