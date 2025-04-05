namespace AuthCsvApp.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public string StudentUsername { get; set; }
        public string Message { get; set; }
        public DateTime NotificationDate { get; set; }
    }
}
