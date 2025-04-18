using AuthCsvApp.Interfaces;
using AuthCsvApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AuthCsvApp.Managers
{
    public class AdminManager
    {
        private readonly ICsvRepository _repository;

        public AdminManager(ICsvRepository repository)
        {
            _repository = repository;
        }

        public void AddClass(int classId, int subjectId, int semesterId, string schedule)
        {
            var classes = _repository.ReadClasses();
            if (classes.Any(c => c.Id == classId))
            {
                throw new InvalidOperationException("Class already exists.");
            }

            classes.Add(new Class
            {
                Id = classId,
                SubjectId = subjectId,
                SemesterId = semesterId,
                Schedule = schedule
            });

            _repository.WriteClasses(classes);
        }

        public void AssignTeacherToClass(int classId, string teacherUsername)
        {
            var classes = _repository.ReadClasses();
            var classToUpdate = classes.FirstOrDefault(c => c.Id == classId);

            if (classToUpdate == null)
            {
                throw new InvalidOperationException("Class not found.");
            }

            // Giả định có phương thức ReadTeachers để kiểm tra giáo viên tồn tại
            var teachers = _repository.ReadTeachers();
            if (!teachers.Any(t => t.Username == teacherUsername))
            {
                throw new InvalidOperationException("Teacher not found.");
            }

            classToUpdate.TeacherUsername = teacherUsername;
            _repository.WriteClasses(classes);
        }
    }
}