using AuthCsvApp.Interfaces;
using AuthCsvApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AuthCsvApp.Managers
{
    public class TeacherManager
    {
        private readonly ICsvRepository _repository;
        private readonly string _teacherUsername;

        public TeacherManager(ICsvRepository repository, string teacherUsername)
        {
            _repository = repository;
            _teacherUsername = teacherUsername;
        }

        public List<string> GetStudentsInClass(int classId)
        {
            var classes = _repository.ReadClasses();
            var classToView = classes.FirstOrDefault(c => c.Id == classId);

            if (classToView == null)
            {
                throw new InvalidOperationException("Class not found.");
            }

            if (classToView.TeacherUsername != _teacherUsername)
            {
                throw new InvalidOperationException("You are not authorized to view students in this class.");
            }

            var registrations = _repository.ReadSubjectRegistrations();
            return registrations
                .Where(r => r.ClassId == classId)
                .Select(r => r.StudentUsername)
                .Distinct()
                .ToList();
        }

        public void EnterGrades(int classId, string studentUsername, double midtermScore, double finalScore)
        {
            if (midtermScore < 0 || midtermScore > 10 || finalScore < 0 || finalScore > 10)
            {
                throw new ArgumentException("Scores must be between 0 and 10.");
            }

            var classes = _repository.ReadClasses();
            var classToGrade = classes.FirstOrDefault(c => c.Id == classId);

            if (classToGrade == null)
            {
                throw new InvalidOperationException("Class not found.");
            }

            if (classToGrade.TeacherUsername != _teacherUsername)
            {
                throw new InvalidOperationException("You are not authorized to enter grades for this class.");
            }

            var registrations = _repository.ReadSubjectRegistrations();
            var registration = registrations.FirstOrDefault(r => r.ClassId == classId && r.StudentUsername == studentUsername);

            if (registration == null)
            {
                throw new InvalidOperationException("Student is not registered in this class.");
            }

            var grades = _repository.ReadGrades();
            var existingGrade = grades.FirstOrDefault(g => g.ClassId == classId && g.StudentUsername == studentUsername);

            double totalScore = midtermScore * 0.4 + finalScore * 0.6; // Tính điểm tổng: 40% giữa kỳ, 60% cuối kỳ
            string classification = totalScore >= 4.0 ? "Pass" : "Fail";

            if (existingGrade != null)
            {
                // Cập nhật điểm nếu đã tồn tại
                existingGrade.MidtermScore = midtermScore;
                existingGrade.FinalScore = finalScore;
                existingGrade.TotalScore = totalScore;
                existingGrade.Classification = classification;
            }
            else
            {
                // Thêm điểm mới
                grades.Add(new Grade
                {
                    Id = grades.Any() ? grades.Max(g => g.Id) + 1 : 1,
                    ClassId = classId,
                    StudentUsername = studentUsername,
                    MidtermScore = midtermScore,
                    FinalScore = finalScore,
                    TotalScore = totalScore,
                    Classification = classification
                });
            }

            _repository.WriteGrades(grades);
        }
    }
}