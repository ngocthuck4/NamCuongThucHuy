using AuthCsvApp.Models;

namespace AuthCsvApp.Interfaces
{
    public interface IClassGrading
    {
        void GradeStudent(int classId, string studentUsername, Grade grade);
    }
}
