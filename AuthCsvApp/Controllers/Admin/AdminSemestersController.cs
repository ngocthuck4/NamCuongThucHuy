
using AuthCsvApp.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AuthCsvApp.Controllers
{
    [Route("admin/semesters")]
    public class AdminSemestersController : Controller
    {
        private readonly string semestersFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "semesters.csv");
        private readonly string classesFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "classes.csv");
        private readonly string subjectRegistrationsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "subject_registrations.csv");
        private readonly string gradesFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "grades.csv");
        private readonly string notificationsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "notifications.csv");

        [HttpGet]
        [Route("index")]
        public IActionResult Index()
        {
            var adminUsername = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(adminUsername) || HttpContext.Session.GetString("Role") != "Admin")
            {
                return RedirectToAction("Login", "Auth");
            }

            var semesters = ReadSemestersFromCsv();
            return View("~/Views/Admin/Semesters/Index.cshtml", semesters);
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

            return View("~/Views/Admin/Semesters/Add.cshtml");
        }

        [HttpPost]
        [Route("add")]
        public IActionResult Add(Semester model)
        {
            var adminUsername = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(adminUsername) || HttpContext.Session.GetString("Role") != "Admin")
            {
                return RedirectToAction("Login", "Auth");
            }

            if (ModelState.IsValid)
            {
                var semesters = ReadSemestersFromCsv();

                // Check if semester name already exists
                if (semesters.Any(s => s.Name.Equals(model.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    ModelState.AddModelError("", "Semester name already exists!");
                    return View(model);
                }

                // Validate dates
                if (model.StartDate >= model.EndDate)
                {
                    ModelState.AddModelError("", "Start date must be before end date!");
                    return View(model);
                }

                // Assign new ID
                model.Id = semesters.Any() ? semesters.Max(s => s.Id) + 1 : 1;
                semesters.Add(model);
                WriteSemestersToCsv(semesters);

                TempData["Success"] = "Semester added successfully!";
                return RedirectToAction("Index");
            }

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

            var semesters = ReadSemestersFromCsv();
            var semester = semesters.FirstOrDefault(s => s.Id == id);
            if (semester == null)
            {
                return NotFound("Semester not found.");
            }

            return View("~/Views/Admin/Semesters/Edit.cshtml", semester);
        }

        [HttpPost]
        [Route("edit/{id}")]
        public IActionResult Edit(int id, Semester model)
        {
            var adminUsername = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(adminUsername) || HttpContext.Session.GetString("Role") != "Admin")
            {
                return RedirectToAction("Login", "Auth");
            }

            if (ModelState.IsValid)
            {
                var semesters = ReadSemestersFromCsv();
                var semester = semesters.FirstOrDefault(s => s.Id == id);
                if (semester == null)
                {
                    return NotFound("Semester not found.");
                }

                // Check if the new semester name is taken by another semester
                if (semesters.Any(s => s.Name.Equals(model.Name, StringComparison.OrdinalIgnoreCase) && s.Id != id))
                {
                    ModelState.AddModelError("", "Semester name already exists!");
                    return View(model);
                }

                // Validate dates
                if (model.StartDate >= model.EndDate)
                {
                    ModelState.AddModelError("", "Start date must be before end date!");
                    return View(model);
                }

                // Update semester details
                semester.Name = model.Name;
                semester.StartDate = model.StartDate;
                semester.EndDate = model.EndDate;
                WriteSemestersToCsv(semesters);

                TempData["Success"] = "Semester updated successfully!";
                return RedirectToAction("Index");
            }

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

            var semesters = ReadSemestersFromCsv();
            var semester = semesters.FirstOrDefault(s => s.Id == id);
            if (semester == null)
            {
                return NotFound("Semester not found.");
            }

            // Remove related classes
            var classes = ReadClassesFromCsv();
            var classesToRemove = classes.Where(c => c.SemesterId == id).ToList();
            var classIdsToRemove = classesToRemove.Select(c => c.Id).ToList();
            classes.RemoveAll(c => c.SemesterId == id);
            WriteClassesToCsv(classes);

            // Remove related registrations
            var registrations = ReadSubjectRegistrationsFromCsv();
            registrations.RemoveAll(r => classIdsToRemove.Contains(r.ClassId));
            WriteSubjectRegistrationsToCsv(registrations);

            // Remove related grades
            var grades = ReadGradesFromCsv();
            grades.RemoveAll(g => classIdsToRemove.Contains(g.ClassId));
            WriteGradesToCsv(grades);

            // Remove related notifications
            var notifications = ReadNotificationsFromCsv();
            notifications.RemoveAll(n => grades.Any(g => g.StudentUsername == n.StudentUsername && classIdsToRemove.Contains(g.ClassId)));
            WriteNotificationsToCsv(notifications);

            // Remove the semester
            semesters.Remove(semester);
            WriteSemestersToCsv(semesters);

            TempData["Success"] = "Semester deleted successfully!";
            return RedirectToAction("Index");
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

        private void WriteSemestersToCsv(List<Semester> semesters)
        {
            var lines = new List<string> { "Id,Name,StartDate,EndDate" };
            foreach (var semester in semesters)
            {
                var name = semester.Name.Replace(",", " "); // Prevent CSV injection
                var startDate = semester.StartDate.ToString("yyyy-MM-dd");
                var endDate = semester.EndDate.ToString("yyyy-MM-dd");
                lines.Add($"{semester.Id},{name},{startDate},{endDate}");
            }

            System.IO.File.WriteAllLines(semestersFilePath, lines);
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
            lines.AddRange(classes.Select(c => $"{c.Id},{c.SubjectId},{c.SemesterId},{c.TeacherUsername},{c.Schedule}"));
            System.IO.File.WriteAllLines(classesFilePath, lines);
        }

        private List<SubjectRegistration> ReadSubjectRegistrationsFromCsv()
        {
            List<SubjectRegistration> registrations = new List<SubjectRegistration>();
            if (!System.IO.File.Exists(subjectRegistrationsFilePath))
            {
                System.IO.File.WriteAllLines(subjectRegistrationsFilePath, new[] { "Id,SubjectId,StudentUsername,ClassId,RegistrationDate" });
                return registrations;
            }

            var lines = System.IO.File.ReadAllLines(subjectRegistrationsFilePath);
            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var values = line.Split(',');
                if (values.Length < 5)
                {
                    System.Diagnostics.Debug.WriteLine($"Invalid line in subject_registrations.csv: {line}");
                    continue;
                }

                try
                {
                    registrations.Add(new SubjectRegistration
                    {
                        Id = int.Parse(values[0]),
                        SubjectId = int.Parse(values[1]),
                        StudentUsername = values[2],
                        ClassId = int.Parse(values[3]),
                        RegistrationDate = DateTime.Parse(values[4])
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error parsing data in subject_registrations.csv: {line}. Error: {ex.Message}");
                    continue;
                }
            }

            return registrations;
        }

        private void WriteSubjectRegistrationsToCsv(List<SubjectRegistration> registrations)
        {
            var lines = new List<string> { "Id,SubjectId,StudentUsername,ClassId,RegistrationDate" };
            foreach (var r in registrations)
            {
                lines.Add($"{r.Id},{r.SubjectId},{r.StudentUsername},{r.ClassId},{r.RegistrationDate:yyyy-MM-dd HH:mm:ss}");
            }
            System.IO.File.WriteAllLines(subjectRegistrationsFilePath, lines);
        }

        private List<Grade> ReadGradesFromCsv()
        {
            List<Grade> grades = new List<Grade>();
            if (!System.IO.File.Exists(gradesFilePath))
            {
                System.IO.File.WriteAllLines(gradesFilePath, new[] { "Id,ClassId,StudentUsername,MidtermScore,FinalScore,TotalScore,Classification" });
                return grades;
            }

            var lines = System.IO.File.ReadAllLines(gradesFilePath);
            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var values = line.Split(',');
                if (values.Length < 7)
                {
                    System.Diagnostics.Debug.WriteLine($"Invalid line in grades.csv: {line}");
                    continue;
                }

                try
                {
                    grades.Add(new Grade
                    {
                        Id = int.Parse(values[0]),
                        ClassId = int.Parse(values[1]),
                        StudentUsername = values[2],
                        MidtermScore = double.Parse(values[3]),
                        FinalScore = double.Parse(values[4]),
                        TotalScore = double.Parse(values[5]),
                        Classification = values[6]
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error parsing data in grades.csv: {line}. Error: {ex.Message}");
                    continue;
                }
            }

            return grades;
        }

        private void WriteGradesToCsv(List<Grade> grades)
        {
            var lines = new List<string> { "Id,ClassId,StudentUsername,MidtermScore,FinalScore,TotalScore,Classification" };
            foreach (var g in grades)
            {
                lines.Add($"{g.Id},{g.ClassId},{g.StudentUsername},{g.MidtermScore},{g.FinalScore},{g.TotalScore},{g.Classification}");
            }
            System.IO.File.WriteAllLines(gradesFilePath, lines);
        }

        private List<Notification> ReadNotificationsFromCsv()
        {
            List<Notification> notifications = new List<Notification>();
            if (!System.IO.File.Exists(notificationsFilePath))
            {
                System.IO.File.WriteAllLines(notificationsFilePath, new[] { "Id,StudentUsername,Message,NotificationDate" });
                return notifications;
            }

            var lines = System.IO.File.ReadAllLines(notificationsFilePath);
            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var values = line.Split(',');
                if (values.Length < 4)
                {
                    System.Diagnostics.Debug.WriteLine($"Invalid line in notifications.csv: {line}");
                    continue;
                }

                try
                {
                    notifications.Add(new Notification
                    {
                        Id = int.Parse(values[0]),
                        StudentUsername = values[1],
                        Message = values[2],
                        NotificationDate = DateTime.Parse(values[3])
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error parsing data in notifications.csv: {line}. Error: {ex.Message}");
                    continue;
                }
            }

            return notifications;
        }

        private void WriteNotificationsToCsv(List<Notification> notifications)
        {
            var lines = new List<string> { "Id,StudentUsername,Message,NotificationDate" };
            foreach (var n in notifications)
            {
                lines.Add($"{n.Id},{n.StudentUsername},{n.Message},{n.NotificationDate:yyyy-MM-dd HH:mm:ss}");
            }
            System.IO.File.WriteAllLines(notificationsFilePath, lines);
        }
    }
}