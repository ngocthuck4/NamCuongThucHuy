
using AuthCsvApp.Interfaces;
using AuthCsvApp.Models;
using AuthCsvApp.Repositories;
using System.Collections.Generic;
using System.Linq;

namespace AuthCsvApp.Managers
{
    public class AdminClassManager : IClassAdminManagement, IClassViewer
    {
        private readonly CsvRepository _repository;

        public AdminClassManager(CsvRepository repository)
        {
            _repository = repository;
        }

        public void CreateClass(Class classModel)
        {
            var classes = _repository.ReadClasses();
            classModel.Id = classes.Any() ? classes.Max(c => c.Id) + 1 : 1;
            classes.Add(classModel);
            _repository.WriteClasses(classes);
        }

        public void UpdateClass(int classId, Class classModel)
        {
            var classes = _repository.ReadClasses();
            var classToUpdate = classes.FirstOrDefault(c => c.Id == classId);
            if (classToUpdate != null)
            {
                classToUpdate.SubjectId = classModel.SubjectId;
                classToUpdate.SemesterId = classModel.SemesterId;
                classToUpdate.TeacherUsername = classModel.TeacherUsername;
                classToUpdate.Schedule = classModel.Schedule;
                _repository.WriteClasses(classes);
            }
        }

        public void DeleteClass(int classId)
        {
            var classes = _repository.ReadClasses();
            var classToDelete = classes.FirstOrDefault(c => c.Id == classId);
            if (classToDelete != null)
            {
                classes.Remove(classToDelete);
                _repository.WriteClasses(classes);
            }
        }

        public void AssignTeacher(int classId, string teacherUsername)
        {
            var classes = _repository.ReadClasses();
            var classToUpdate = classes.FirstOrDefault(c => c.Id == classId);
            if (classToUpdate != null)
            {
                classToUpdate.TeacherUsername = teacherUsername;
                _repository.WriteClasses(classes);
            }
        }

        public List<Class> GetClasses()
        {
            return _repository.ReadClasses();
        }
    }
}