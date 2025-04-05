
using AuthCsvApp.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AuthCsvApp.Controllers
{
    [Route("student")]
    public class StudentController : Controller
    {
        private readonly string subjectsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "subjects.csv");
        private readonly string subjectRegistrationsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "subject_registrations.csv");
        private readonly string classesFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "classes.csv");
        private readonly string semestersFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "semesters.csv");
        private readonly string usersFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "users.csv");
        private readonly string notificationsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "notifications.csv");
        private readonly string gradesFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "grades.csv");

        [HttpGet]
        [Route("dashboard")]
        public IActionResult Dashboard()
        {
            var studentUsername = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(studentUsername) || HttpContext.Session.GetString("Role") != "Student")
            {
                return RedirectToAction("Login", "Auth");
            }

            var subjects = ReadSubjectsFromCsv();
            var registrations = ReadSubjectRegistrationsFromCsv();
            var classes = ReadClassesFromCsv();
            var semesters = ReadSemestersFromCsv();
            var users = ReadUsersFromCsv();
            var grades = ReadGradesFromCsv().Where(g => g.StudentUsername == studentUsername).ToList();

            // Lấy thông tin sinh viên
            var student = users.FirstOrDefault(u => u.Username == studentUsername);
            if (student == null)
            {
                return NotFound("Student not found.");
            }

            var registeredSubjectIds = registrations.Where(r => r.StudentUsername == studentUsername).Select(r => r.SubjectId).ToList();
            var registeredSubjects = subjects.Where(s => registeredSubjectIds.Contains(s.Id)).ToList();
            var availableSubjects = subjects.Where(s => !registeredSubjectIds.Contains(s.Id)).ToList();

            var notifications = ReadNotificationsFromCsv().Where(n => n.StudentUsername == studentUsername).ToList();

            // Tạo danh sách thông tin môn học đã đăng ký, bao gồm điểm số
            var registeredSubjectsWithDetails = new List<object>();
            foreach (var subject in registeredSubjects)
            {
                var registration = registrations.FirstOrDefault(r => r.SubjectId == subject.Id && r.StudentUsername == studentUsername);
                var assignedClass = registration != null ? classes.FirstOrDefault(c => c.Id == registration.ClassId) : null;
                var semester = assignedClass != null ? semesters.FirstOrDefault(s => s.Id == assignedClass.SemesterId) : null;
                var teacher = assignedClass != null ? users.FirstOrDefault(u => u.Username == assignedClass.TeacherUsername) : null;
                var grade = assignedClass != null ? grades.FirstOrDefault(g => g.ClassId == assignedClass.Id) : null;

                registeredSubjectsWithDetails.Add(new
                {
                    SubjectId = subject.Id,
                    SubjectName = subject.Name,
                    SubjectDescription = subject.Description,
                    ClassId = assignedClass?.Id ?? 0,
                    SemesterName = semester?.Name ?? "N/A",
                    TeacherName = teacher?.FullName ?? "N/A",
                    Schedule = assignedClass?.Schedule ?? "N/A",
                    MidtermScore = grade?.MidtermScore.ToString() ?? "N/A",
                    FinalScore = grade?.FinalScore.ToString() ?? "N/A",
                    TotalScore = grade?.TotalScore.ToString() ?? "N/A",
                    Classification = grade?.Classification ?? "N/A"
                });
            }

            ViewBag.Student = student; // Truyền thông tin sinh viên vào View
            ViewBag.RegisteredSubjectsWithDetails = registeredSubjectsWithDetails;
            ViewBag.Registrations = registrations;
            ViewBag.Classes = classes;
            ViewBag.Semesters = semesters;
            ViewBag.Teachers = users.Where(u => u.Role == UserRole.Teacher).ToList();
            ViewBag.Notifications = notifications;

            return View("~/Views/Student/Dashboard.cshtml", availableSubjects);
        }

        [HttpGet]
        [Route("register-subject/{subjectId}")]
        public IActionResult RegisterSubject(int subjectId)
        {
            var studentUsername = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(studentUsername) || HttpContext.Session.GetString("Role") != "Student")
            {
                return RedirectToAction("Login", "Auth");
            }

            var subjects = ReadSubjectsFromCsv();
            var subject = subjects.FirstOrDefault(s => s.Id == subjectId);
            if (subject == null)
            {
                TempData["Error"] = "Subject not found.";
                return RedirectToAction("Dashboard");
            }

            var registrations = ReadSubjectRegistrationsFromCsv();
            if (registrations.Any(r => r.StudentUsername == studentUsername && r.SubjectId == subjectId))
            {
                TempData["Error"] = "You have already registered for this subject.";
                return RedirectToAction("Dashboard");
            }

            var classes = ReadClassesFromCsv();
            var availableClass = classes.FirstOrDefault(c => c.SubjectId == subjectId);
            if (availableClass == null)
            {
                TempData["Error"] = "No available class for this subject.";
                return RedirectToAction("Dashboard");
            }

            var newRegistration = new SubjectRegistration
            {
                Id = registrations.Any() ? registrations.Max(r => r.Id) + 1 : 1,
                SubjectId = subjectId,
                StudentUsername = studentUsername,
                ClassId = availableClass.Id,
                RegistrationDate = DateTime.Now
            };
            registrations.Add(newRegistration);
            WriteSubjectRegistrationsToCsv(registrations);

            TempData["Success"] = "Subject registered successfully!";
            return RedirectToAction("Dashboard");
        }

        [HttpGet]
        [Route("unregister-subject/{subjectId}")]
        public IActionResult UnregisterSubject(int subjectId)
        {
            var studentUsername = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(studentUsername) || HttpContext.Session.GetString("Role") != "Student")
            {
                return RedirectToAction("Login", "Auth");
            }

            var registrations = ReadSubjectRegistrationsFromCsv();
            var registration = registrations.FirstOrDefault(r => r.StudentUsername == studentUsername && r.SubjectId == subjectId);
            if (registration == null)
            {
                TempData["Error"] = "You are not registered for this subject.";
                return RedirectToAction("Dashboard");
            }

            registrations.Remove(registration);
            WriteSubjectRegistrationsToCsv(registrations);

            TempData["Success"] = "Subject unregistered successfully!";
            return RedirectToAction("Dashboard");
        }

        [HttpGet]
        [Route("profile")]
        public IActionResult Profile()
        {
            var studentUsername = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(studentUsername) || HttpContext.Session.GetString("Role") != "Student")
            {
                return RedirectToAction("Login", "Auth");
            }

            // Lấy thông tin sinh viên
            var users = ReadUsersFromCsv();
            var student = users.FirstOrDefault(u => u.Username == studentUsername);
            if (student == null)
            {
                return NotFound("Student not found.");
            }

            // Lấy danh sách đăng ký môn học
            var registrations = ReadSubjectRegistrationsFromCsv().Where(r => r.StudentUsername == studentUsername).ToList();
            var classes = ReadClassesFromCsv();
            var subjects = ReadSubjectsFromCsv();
            var semesters = ReadSemestersFromCsv();
            var grades = ReadGradesFromCsv().Where(g => g.StudentUsername == studentUsername).ToList();
            var notifications = ReadNotificationsFromCsv().Where(n => n.StudentUsername == studentUsername).OrderByDescending(n => n.NotificationDate).ToList();

            // Tạo danh sách thông tin lớp học đã đăng ký
            var registeredClasses = new List<object>();
            foreach (var reg in registrations)
            {
                var classInfo = classes.FirstOrDefault(c => c.Id == reg.ClassId);
                if (classInfo != null)
                {
                    var subject = subjects.FirstOrDefault(s => s.Id == classInfo.SubjectId);
                    var semester = semesters.FirstOrDefault(s => s.Id == classInfo.SemesterId);
                    var grade = grades.FirstOrDefault(g => g.ClassId == reg.ClassId);
                    var teacher = users.FirstOrDefault(u => u.Username == classInfo.TeacherUsername);

                    registeredClasses.Add(new
                    {
                        ClassId = classInfo.Id,
                        SubjectName = subject?.Name ?? "Unknown",
                        SemesterName = semester?.Name ?? "Unknown",
                        Schedule = classInfo.Schedule,
                        TeacherName = teacher?.FullName ?? "Unknown",
                        MidtermScore = grade?.MidtermScore.ToString() ?? "N/A",
                        FinalScore = grade?.FinalScore.ToString() ?? "N/A",
                        TotalScore = grade?.TotalScore.ToString() ?? "N/A",
                        Classification = grade?.Classification ?? "N/A"
                    });
                }
            }

            // Truyền dữ liệu vào View
            ViewBag.Student = student;
            ViewBag.RegisteredClasses = registeredClasses;
            ViewBag.Notifications = notifications;

            return View("~/Views/Student/Profile.cshtml");
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

        private void WriteSubjectRegistrationsToCsv(List<SubjectRegistration> registrations)
        {
            var lines = new List<string> { "Id,SubjectId,StudentUsername,ClassId,RegistrationDate" };
            foreach (var r in registrations)
            {
                lines.Add($"{r.Id},{r.SubjectId},{r.StudentUsername},{r.ClassId},{r.RegistrationDate:yyyy-MM-dd HH:mm:ss}");
            }
            System.IO.File.WriteAllLines(subjectRegistrationsFilePath, lines);
        }
    }
}