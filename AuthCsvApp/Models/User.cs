namespace AuthCsvApp.Models
{
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Address { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        // Thay đổi từ string sang Enum để kiểm soát role chặt chẽ hơn
        public UserRole Role { get; set; }
    }

    // Định nghĩa Enum để tránh nhập sai giá trị Role
    public enum UserRole
    {
        Admin,
        Student,
        Teacher
    }
}
