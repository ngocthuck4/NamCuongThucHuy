
using AuthCsvApp.Managers;
using AuthCsvApp.Models;
using AuthCsvApp.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AuthCsvApp.Services
{
    public class AdminClassService
    {
        private readonly AdminClassManager _adminClassManager;
        private readonly CsvRepository _repository;

        public AdminClassService(AdminClassManager adminClassManager, CsvRepository repository)
        {
            _adminClassManager = adminClassManager;
            _repository = repository;
        }

        public List<Class> GetClasses()
        {
            return _adminClassManager.GetClasses();
        }

        public bool AddClass(Class model, out string errorMessage)
        {
            var subjects = _repository.ReadSubjects();
            var semesters = _repository.ReadSemesters();
            var teachers = _repository.ReadUsers().Where(u => u.Role == UserRole.Teacher).ToList();
            var classes = _repository.ReadClasses();

            // Validate SubjectId, SemesterId, TeacherUsername
            if (!subjects.Any(s => s.Id == model.SubjectId))
            {
                errorMessage = "Invalid subject selected.";
                return false;
            }

            if (!semesters.Any(s => s.Id == model.SemesterId))
            {
                errorMessage = "Invalid semester selected.";
                return false;
            }

            if (!teachers.Any(t => t.Username == model.TeacherUsername))
            {
                errorMessage = "Invalid teacher selected.";
                return false;
            }

            // Check for schedule conflicts for the teacher
            var teacherClasses = classes.Where(c => c.TeacherUsername == model.TeacherUsername && c.SemesterId == model.SemesterId).ToList();
            if (teacherClasses.Any(c => c.Schedule == model.Schedule))
            {
                errorMessage = "The teacher already has a class scheduled at this time.";
                return false;
            }

            // Generate a new Id for the class
            model.Id = classes.Any() ? classes.Max(c => c.Id) + 1 : 1;
            _adminClassManager.CreateClass(model);
            errorMessage = null;
            return true;
        }

        public Class GetClassById(int id)
        {
            return _adminClassManager.GetClasses().FirstOrDefault(c => c.Id == id);
        }

        public bool UpdateClass(int id, Class model, out string errorMessage)
        {
            var subjects = _repository.ReadSubjects();
            var semesters = _repository.ReadSemesters();
            var teachers = _repository.ReadUsers().Where(u => u.Role == UserRole.Teacher).ToList();
            var classes = _repository.ReadClasses();

            // Validate SubjectId, SemesterId, TeacherUsername
            if (!subjects.Any(s => s.Id == model.SubjectId))
            {
                errorMessage = "Invalid subject selected.";
                return false;
            }

            if (!semesters.Any(s => s.Id == model.SemesterId))
            {
                errorMessage = "Invalid semester selected.";
                return false;
            }

            if (!teachers.Any(t => t.Username == model.TeacherUsername))
            {
                errorMessage = "Invalid teacher selected.";
                return false;
            }

            // Check for schedule conflicts for the teacher (excluding the current class being updated)
            var teacherClasses = classes.Where(c => c.TeacherUsername == model.TeacherUsername && c.SemesterId == model.SemesterId && c.Id != id).ToList();
            if (teacherClasses.Any(c => c.Schedule == model.Schedule))
            {
                errorMessage = "The teacher already has a class scheduled at this time.";
                return false;
            }

            // Ensure the class exists
            var classToUpdate = classes.FirstOrDefault(c => c.Id == id);
            if (classToUpdate == null)
            {
                errorMessage = "Class not found.";
                return false;
            }

            _adminClassManager.UpdateClass(id, model);
            errorMessage = null;
            return true;
        }

        public bool DeleteClass(int id, out string errorMessage)
        {
            var classToDelete = _adminClassManager.GetClasses().FirstOrDefault(c => c.Id == id);
            if (classToDelete == null)
            {
                errorMessage = "Class not found.";
                return false;
            }

            _adminClassManager.DeleteClass(id);
            errorMessage = null;
            return true;
        }

        public List<Subject> GetSubjects()
        {
            return _repository.ReadSubjects();
        }

        public List<Semester> GetSemesters()
        {
            return _repository.ReadSemesters();
        }

        public List<User> GetTeachers()
        {
            return _repository.ReadUsers().Where(u => u.Role == UserRole.Teacher).ToList();
        }
    }
}