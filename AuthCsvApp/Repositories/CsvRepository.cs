
using AuthCsvApp.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AuthCsvApp.Repositories
{
    public class CsvRepository
    {
        private readonly string usersFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "users.csv");
        private readonly string classesFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "classes.csv");
        private readonly string subjectRegistrationsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "subject_registrations.csv");
        private readonly string gradesFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "grades.csv");
        private readonly string notificationsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "notifications.csv");

        public List<User> ReadUsers()
        {
            List<User> users = new List<User>();
            if (!File.Exists(usersFilePath))
            {
                File.WriteAllLines(usersFilePath, new[] { "Id,FullName,Address,Username,Password,Role" });
                return users;
            }

            var lines = File.ReadAllLines(usersFilePath);
            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

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

        public void WriteUsers(List<User> users)
        {
            var lines = new List<string> { "Id,FullName,Address,Username,Password,Role" };
            foreach (var user in users)
            {
                var fullName = user.FullName.Replace(",", " ");
                var address = user.Address.Replace(",", " ");
                var username = user.Username.Replace(",", " ");
                var password = user.Password.Replace(",", " ");
                var role = user.Role.ToString();
                lines.Add($"{user.Id},{fullName},{address},{username},{password},{role}");
            }

            File.WriteAllLines(usersFilePath, lines);
        }

        public List<Class> ReadClasses()
        {
            List<Class> classes = new List<Class>();
            if (!File.Exists(classesFilePath))
            {
                File.WriteAllLines(classesFilePath, new[] { "Id,SubjectId,SemesterId,TeacherUsername,Schedule" });
                return classes;
            }

            var lines = File.ReadAllLines(classesFilePath);
            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

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

        public void WriteClasses(List<Class> classes)
        {
            var lines = new List<string> { "Id,SubjectId,SemesterId,TeacherUsername,Schedule" };
            lines.AddRange(classes.Select(c => $"{c.Id},{c.SubjectId},{c.SemesterId},{c.TeacherUsername},{c.Schedule}"));
            File.WriteAllLines(classesFilePath, lines);
        }

        public List<SubjectRegistration> ReadSubjectRegistrations()
        {
            List<SubjectRegistration> registrations = new List<SubjectRegistration>();
            if (!File.Exists(subjectRegistrationsFilePath))
            {
                File.WriteAllLines(subjectRegistrationsFilePath, new[] { "Id,SubjectId,StudentUsername,ClassId,RegistrationDate" });
                return registrations;
            }

            var lines = File.ReadAllLines(subjectRegistrationsFilePath);
            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

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

        public void WriteSubjectRegistrations(List<SubjectRegistration> registrations)
        {
            var lines = new List<string> { "Id,SubjectId,StudentUsername,ClassId,RegistrationDate" };
            foreach (var r in registrations)
            {
                lines.Add($"{r.Id},{r.SubjectId},{r.StudentUsername},{r.ClassId},{r.RegistrationDate:yyyy-MM-dd HH:mm:ss}");
            }
            File.WriteAllLines(subjectRegistrationsFilePath, lines);
        }

        public List<Grade> ReadGrades()
        {
            List<Grade> grades = new List<Grade>();
            if (!File.Exists(gradesFilePath))
            {
                File.WriteAllLines(gradesFilePath, new[] { "Id,ClassId,StudentUsername,MidtermScore,FinalScore,TotalScore,Classification" });
                return grades;
            }

            var lines = File.ReadAllLines(gradesFilePath);
            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

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

        public void WriteGrades(List<Grade> grades)
        {
            var lines = new List<string> { "Id,ClassId,StudentUsername,MidtermScore,FinalScore,TotalScore,Classification" };
            foreach (var g in grades)
            {
                lines.Add($"{g.Id},{g.ClassId},{g.StudentUsername},{g.MidtermScore},{g.FinalScore},{g.TotalScore},{g.Classification}");
            }
            File.WriteAllLines(gradesFilePath, lines);
        }

        public List<Notification> ReadNotifications()
        {
            List<Notification> notifications = new List<Notification>();
            if (!File.Exists(notificationsFilePath))
            {
                File.WriteAllLines(notificationsFilePath, new[] { "Id,StudentUsername,Message,NotificationDate" });
                return notifications;
            }

            var lines = File.ReadAllLines(notificationsFilePath);
            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

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

        public void WriteNotifications(List<Notification> notifications)
        {
            var lines = new List<string> { "Id,StudentUsername,Message,NotificationDate" };
            foreach (var n in notifications)
            {
                lines.Add($"{n.Id},{n.StudentUsername},{n.Message},{n.NotificationDate:yyyy-MM-dd HH:mm:ss}");
            }
            File.WriteAllLines(notificationsFilePath, lines);
        }
    }
}