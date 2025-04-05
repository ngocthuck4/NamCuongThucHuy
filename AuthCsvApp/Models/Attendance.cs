namespace AuthCsvApp.Models
{
    public class Attendance
    {
        public int Id { get; set; }
        public int ClassId { get; set; }
        public string StudentUsername { get; set; }
        public DateTime AttendanceDate { get; set; }
        public bool IsPresent { get; set; } // true: Có mặt, false: Vắng mặt
    }
}