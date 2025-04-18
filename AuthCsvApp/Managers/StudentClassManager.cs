using AuthCsvApp.Interfaces;
using AuthCsvApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AuthCsvApp.Managers
{
    public class StudentClassManager : IClassViewer, IClassRegistration
    {
        private readonly ICsvRepository _repository; // Sửa từ CsvRepository thành ICsvRepository
        private readonly string _studentUsername;

        public StudentClassManager(ICsvRepository repository, string studentUsername)
        {
            _repository = repository;
            _studentUsername = studentUsername;
        }

        public List<Class> GetClasses()
        {
            var registrations = _repository.ReadSubjectRegistrations()
                .Where(r => r.StudentUsername == _studentUsername)
                .Select(r => r.ClassId)
                .ToList();

            return _repository.ReadClasses().Where(c => registrations.Contains(c.Id)).ToList();
        }

        public void RegisterClass(int classId, string studentUsername)
        {
            var classes = _repository.ReadClasses();
            var classToRegister = classes.FirstOrDefault(c => c.Id == classId);

            if (classToRegister == null)
            {
                throw new InvalidOperationException("Class not found.");
            }

            var registrations = _repository.ReadSubjectRegistrations();
            if (registrations.Any(r => r.StudentUsername == studentUsername && r.ClassId == classId))
            {
                throw new InvalidOperationException("You have already registered for this class.");
            }

            registrations.Add(new SubjectRegistration
            {
                Id = registrations.Any() ? registrations.Max(r => r.Id) + 1 : 1,
                SubjectId = classToRegister.SubjectId,
                StudentUsername = studentUsername,
                ClassId = classId,
                RegistrationDate = DateTime.Now
            });

            _repository.WriteSubjectRegistrations(registrations);
        }
    }
}