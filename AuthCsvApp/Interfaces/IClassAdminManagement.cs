using AuthCsvApp.Models;

namespace AuthCsvApp.Interfaces
{
    public interface IClassAdminManagement
    {
        void CreateClass(Class classModel);
        void UpdateClass(int classId, Class classModel);
        void DeleteClass(int classId);
        void AssignTeacher(int classId, string teacherUsername);
    }
}
