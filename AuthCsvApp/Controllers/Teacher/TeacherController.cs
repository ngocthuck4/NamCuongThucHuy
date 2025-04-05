
using AuthCsvApp.Models;
using AuthCsvApp.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AuthCsvApp.Controllers
{
    [Route("teacher")]
    public class TeacherController : Controller
    {
        private readonly string classesFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "classes.csv");
        private readonly string subjectsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "subjects.csv");
        private readonly string semestersFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "semesters.csv");
        private readonly string subjectRegistrationsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "subject_registrations.csv");
        private readonly string usersFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "users.csv");
        private readonly string attendancesFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "attendances.csv");
        private readonly string gradesFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "grades.csv");

        [HttpGet]
        [Route("dashboard")]
        public IActionResult Dashboard()
        {
            var teacherUsername = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(teacherUsername) || HttpContext.Session.GetString("Role") != "Teacher")
            {
                return RedirectToAction("Login", "Auth");
            }

            var classes = ReadClassesFromCsv();
            var teacherClasses = classes.Where(c => c.TeacherUsername == teacherUsername).ToList();

            var subjects = ReadSubjectsFromCsv();
            var semesters = ReadSemestersFromCsv();

            ViewBag.Subjects = subjects;
            ViewBag.Semesters = semesters;

            return View("~/Views/Teacher/Dashboard.cshtml", teacherClasses);
        }

        [HttpGet]
        [Route("take-attendance/{classId}")]
        public IActionResult TakeAttendance(int classId)
        {
            var teacherUsername = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(teacherUsername) || HttpContext.Session.GetString("Role") != "Teacher")
            {
                return RedirectToAction("Login", "Auth");
            }

            var classes = ReadClassesFromCsv();
            var selectedClass = classes.FirstOrDefault(c => c.Id == classId && c.TeacherUsername == teacherUsername);
            if (selectedClass == null)
            {
                TempData["Error"] = "Class not found or you are not authorized to take attendance for this class.";
                return RedirectToAction("Dashboard");
            }

            var registrations = ReadSubjectRegistrationsFromCsv();
            var studentUsernames = registrations.Where(r => r.ClassId == classId).Select(r => r.StudentUsername).ToList();
            var students = ReadUsersFromCsv().Where(u => studentUsernames.Contains(u.Username) && u.Role == UserRole.Student).ToList();

            ViewBag.Class = selectedClass;
            return View("~/Views/Teacher/TakeAttendance.cshtml", students);
        }

        [HttpPost]
        [Route("take-attendance/{classId}")]
        public IActionResult TakeAttendance(int classId, Dictionary<string, bool> attendance)
        {
            var teacherUsername = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(teacherUsername) || HttpContext.Session.GetString("Role") != "Teacher")
            {
                return RedirectToAction("Login", "Auth");
            }

            var classes = ReadClassesFromCsv();
            var selectedClass = classes.FirstOrDefault(c => c.Id == classId && c.TeacherUsername == teacherUsername);
            if (selectedClass == null)
            {
                TempData["Error"] = "Class not found or you are not authorized to take attendance for this class.";
                return RedirectToAction("Dashboard");
            }

            var registrations = ReadSubjectRegistrationsFromCsv();
            var studentUsernames = registrations.Where(r => r.ClassId == classId).Select(r => r.StudentUsername).ToList();

            var attendances = ReadAttendancesFromCsv();
            foreach (var entry in attendance)
            {
                var studentUsername = entry.Key;
                if (!studentUsernames.Contains(studentUsername))
                {
                    continue;
                }

                var newAttendance = new Attendance
                {
                    Id = attendances.Any() ? attendances.Max(a => a.Id) + 1 : 1,
                    ClassId = classId,
                    StudentUsername = studentUsername,
                    AttendanceDate = DateTime.Now,
                    IsPresent = entry.Value
                };
                attendances.Add(newAttendance);
            }

            WriteAttendancesToCsv(attendances);

            TempData["Success"] = "Attendance recorded successfully!";
            return RedirectToAction("Dashboard");
        }

        [HttpGet]
        [Route("enter-grades/{classId}")]
        public IActionResult EnterGrades(int classId)
        {
            var teacherUsername = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(teacherUsername) || HttpContext.Session.GetString("Role") != "Teacher")
            {
                return RedirectToAction("Login", "Auth");
            }

            var classes = ReadClassesFromCsv();
            var selectedClass = classes.FirstOrDefault(c => c.Id == classId && c.TeacherUsername == teacherUsername);
            if (selectedClass == null)
            {
                TempData["Error"] = "Class not found or you are not authorized to enter grades for this class.";
                return RedirectToAction("Dashboard");
            }

            var registrations = ReadSubjectRegistrationsFromCsv();
            var studentUsernames = registrations.Where(r => r.ClassId == classId).Select(r => r.StudentUsername).ToList();
            var students = ReadUsersFromCsv().Where(u => studentUsernames.Contains(u.Username) && u.Role == UserRole.Student).ToList();

            var grades = ReadGradesFromCsv();
            var studentGrades = grades.Where(g => g.ClassId == classId).ToDictionary(g => g.StudentUsername, g => g);

            ViewBag.Class = selectedClass;
            ViewBag.StudentGrades = studentGrades;
            return View("~/Views/Teacher/EnterGrades.cshtml", students);
        }

        [HttpPost]
        [Route("enter-grades/{classId}")]
        public IActionResult EnterGrades(int classId, Dictionary<string, string> midtermScores, Dictionary<string, string> finalScores)
        {
            var teacherUsername = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(teacherUsername) || HttpContext.Session.GetString("Role") != "Teacher")
            {
                return RedirectToAction("Login", "Auth");
            }

            var classes = ReadClassesFromCsv();
            var selectedClass = classes.FirstOrDefault(c => c.Id == classId && c.TeacherUsername == teacherUsername);
            if (selectedClass == null)
            {
                TempData["Error"] = "Class not found or you are not authorized to enter grades for this class.";
                return RedirectToAction("Dashboard");
            }

            var registrations = ReadSubjectRegistrationsFromCsv();
            var studentUsernames = registrations.Where(r => r.ClassId == classId).Select(r => r.StudentUsername).ToList();

            var grades = ReadGradesFromCsv();
            var existingGrades = grades.Where(g => g.ClassId != classId).ToList(); // Loại bỏ điểm cũ của lớp này

            // Chọn Strategy
            IGradeStrategy gradeStrategy = new AverageGradeStrategy();

            foreach (var studentUsername in studentUsernames)
            {
                if (!midtermScores.ContainsKey(studentUsername) || !finalScores.ContainsKey(studentUsername))
                {
                    continue;
                }

                if (!double.TryParse(midtermScores[studentUsername], out double midtermScore) ||
                    !double.TryParse(finalScores[studentUsername], out double finalScore))
                {
                    TempData["Error"] = "Invalid score format. Please enter valid numbers.";
                    return RedirectToAction("EnterGrades", new { classId });
                }

                if (midtermScore < 0 || midtermScore > 10 || finalScore < 0 || finalScore > 10)
                {
                    TempData["Error"] = "Scores must be between 0 and 10.";
                    return RedirectToAction("EnterGrades", new { classId });
                }

                var newGrade = new Grade
                {
                    Id = grades.Any() ? grades.Max(g => g.Id) + 1 : 1,
                    ClassId = classId,
                    StudentUsername = studentUsername,
                    MidtermScore = midtermScore,
                    FinalScore = finalScore,
                    TotalScore = gradeStrategy.CalculateTotalScore(new Grade { MidtermScore = midtermScore, FinalScore = finalScore }),
                    Classification = gradeStrategy.CalculateClassification(gradeStrategy.CalculateTotalScore(new Grade { MidtermScore = midtermScore, FinalScore = finalScore }))
                };
                existingGrades.Add(newGrade);

                // Tạo thông báo cho sinh viên
                var notifications = ReadNotificationsFromCsv();
                var newNotification = new Notification
                {
                    Id = notifications.Any() ? notifications.Max(n => n.Id) + 1 : 1,
                    StudentUsername = studentUsername,
                    Message = $"Your grade has been updated to {newGrade.TotalScore} ({newGrade.Classification})",
                    NotificationDate = DateTime.Now
                };
                notifications.Add(newNotification);
                WriteNotificationsToCsv(notifications);
            }

            WriteGradesToCsv(existingGrades);

            TempData["Success"] = "Grades entered successfully!";
            return RedirectToAction("Dashboard");
        }

        // Thêm phương thức ReadNotificationsFromCsv
        private List<Notification> ReadNotificationsFromCsv()
        {
            List<Notification> notifications = new List<Notification>();
            var notificationsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "notifications.csv");
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

        // Thêm phương thức WriteNotificationsToCsv
        private void WriteNotificationsToCsv(List<Notification> notifications)
        {
            var notificationsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "notifications.csv");
            var lines = new List<string> { "Id,StudentUsername,Message,NotificationDate" };
            foreach (var n in notifications)
            {
                lines.Add($"{n.Id},{n.StudentUsername},{n.Message},{n.NotificationDate:yyyy-MM-dd HH:mm:ss}");
            }
            System.IO.File.WriteAllLines(notificationsFilePath, lines);
        }

        private List<Class> ReadClassesFromCsv()
        {
            List<Class> classes = new List<Class>();
            if (!System.IO.File.Exists(classesFilePath)) // Sửa lỗi CS0118
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

        private List<Subject> ReadSubjectsFromCsv()
        {
            List<Subject> subjects = new List<Subject>();
            if (!System.IO.File.Exists(subjectsFilePath)) // Sửa lỗi CS0118
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
            if (!System.IO.File.Exists(semestersFilePath)) // Sửa lỗi CS0118
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

        private List<SubjectRegistration> ReadSubjectRegistrationsFromCsv()
        {
            List<SubjectRegistration> registrations = new List<SubjectRegistration>();
            if (!System.IO.File.Exists(subjectRegistrationsFilePath)) // Sửa lỗi CS0118
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

        private List<User> ReadUsersFromCsv()
        {
            List<User> users = new List<User>();
            if (!System.IO.File.Exists(usersFilePath)) // Sửa lỗi CS0118
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

        private List<Attendance> ReadAttendancesFromCsv()
        {
            List<Attendance> attendances = new List<Attendance>();
            if (!System.IO.File.Exists(attendancesFilePath)) // Sửa lỗi CS0118
            {
                System.IO.File.WriteAllLines(attendancesFilePath, new[] { "Id,ClassId,StudentUsername,AttendanceDate,IsPresent" });
                return attendances;
            }

            var lines = System.IO.File.ReadAllLines(attendancesFilePath);
            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var values = line.Split(',');
                if (values.Length < 5)
                {
                    System.Diagnostics.Debug.WriteLine($"Invalid line in attendances.csv: {line}");
                    continue;
                }

                try
                {
                    attendances.Add(new Attendance
                    {
                        Id = int.Parse(values[0]),
                        ClassId = int.Parse(values[1]),
                        StudentUsername = values[2],
                        AttendanceDate = DateTime.Parse(values[3]),
                        IsPresent = bool.Parse(values[4])
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error parsing data in attendances.csv: {line}. Error: {ex.Message}");
                    continue;
                }
            }

            return attendances;
        }

        private List<Grade> ReadGradesFromCsv()
        {
            List<Grade> grades = new List<Grade>();
            if (!System.IO.File.Exists(gradesFilePath)) // Sửa lỗi CS0118
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

        private void WriteAttendancesToCsv(List<Attendance> attendances)
        {
            var lines = new List<string> { "Id,ClassId,StudentUsername,AttendanceDate,IsPresent" };
            foreach (var a in attendances)
            {
                lines.Add($"{a.Id},{a.ClassId},{a.StudentUsername},{a.AttendanceDate:yyyy-MM-dd HH:mm:ss},{a.IsPresent}");
            }
            System.IO.File.WriteAllLines(attendancesFilePath, lines);
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
    }
}