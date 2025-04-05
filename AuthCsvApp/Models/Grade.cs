namespace AuthCsvApp.Models
{
    public class Grade
    {
        public int Id { get; set; }
        public int ClassId { get; set; }
        public string StudentUsername { get; set; }
        public double MidtermScore { get; set; }
        public double FinalScore { get; set; }
        public double TotalScore { get; set; }
        public string Classification { get; set; }
    }
}