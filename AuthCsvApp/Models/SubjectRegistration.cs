namespace AuthCsvApp.Models
{
    public class SubjectRegistration
    {
        public int Id { get; set; }
        public int SubjectId { get; set; }
        public string StudentUsername { get; set; }
        public int ClassId { get; set; } // Lớp học được gán sau khi đăng ký môn học
        public DateTime RegistrationDate { get; set; }
    }
}
