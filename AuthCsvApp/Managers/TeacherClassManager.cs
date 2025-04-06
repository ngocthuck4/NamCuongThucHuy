
using AuthCsvApp.Interfaces;
using AuthCsvApp.Models;
using AuthCsvApp.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AuthCsvApp.Managers
{
    public class TeacherClassManager : IClassViewer, IClassGrading
    {
        private readonly CsvRepository _repository;
        private readonly string _teacherUsername;

        public TeacherClassManager(CsvRepository repository, string teacherUsername)
        {
            _repository = repository;
            _teacherUsername = teacherUsername;
        }

        public List<Class> GetClasses()
        {
            return _repository.ReadClasses().Where(c => c.TeacherUsername == _teacherUsername).ToList();
        }

        public void GradeStudent(int classId, string studentUsername, Grade grade)
        {
            var classes = _repository.ReadClasses();
            var classToGrade = classes.FirstOrDefault(c => c.Id == classId && c.TeacherUsername == _teacherUsername);
            if (classToGrade == null)
            {
                throw new InvalidOperationException("Class not found or not assigned to this teacher.");
            }

            var grades = _repository.ReadGrades();
            var existingGrade = grades.FirstOrDefault(g => g.ClassId == classId && g.StudentUsername == studentUsername);
            if (existingGrade != null)
            {
                existingGrade.MidtermScore = grade.MidtermScore;
                existingGrade.FinalScore = grade.FinalScore;
                existingGrade.TotalScore = grade.TotalScore;
                existingGrade.Classification = grade.Classification;
            }
            else
            {
                grade.Id = grades.Any() ? grades.Max(g => g.Id) + 1 : 1;
                grade.ClassId = classId;
                grade.StudentUsername = studentUsername;
                grades.Add(grade);
            }

            _repository.WriteGrades(grades);

            // Gửi thông báo cho sinh viên
            var notifications = _repository.ReadNotifications();
            notifications.Add(new Notification
            {
                Id = notifications.Any() ? notifications.Max(n => n.Id) + 1 : 1,
                StudentUsername = studentUsername,
                Message = $"Your grade for class {classId} has been updated: {grade.TotalScore} ({grade.Classification})",
                NotificationDate = DateTime.Now
            });
            _repository.WriteNotifications(notifications);
        }
    }
}