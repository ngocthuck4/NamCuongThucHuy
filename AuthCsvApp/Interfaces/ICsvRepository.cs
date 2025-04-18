using AuthCsvApp.Models;
using System.Collections.Generic;

namespace AuthCsvApp.Interfaces
{
    public interface ICsvRepository
    {
        List<Class> ReadClasses();
        List<SubjectRegistration> ReadSubjectRegistrations();
        void WriteSubjectRegistrations(List<SubjectRegistration> registrations);
        List<Grade> ReadGrades();
        void WriteClasses(List<Class> classes); // Thêm phương thức này
        List<User> ReadTeachers(); // Thêm phương thức này
        void WriteGrades(List<Grade> grades); // Thêm phương thức này
    }
}