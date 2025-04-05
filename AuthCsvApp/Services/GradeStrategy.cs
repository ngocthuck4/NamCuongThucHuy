
using AuthCsvApp.Models;

namespace AuthCsvApp.Services
{
    // Interface cho Strategy Pattern
    public interface IGradeStrategy
    {
        double CalculateTotalScore(Grade grade);
        string CalculateClassification(double totalScore);
    }

    // Strategy 1: Tính điểm trung bình cộng (Midterm 50%, Final 50%)
    public class AverageGradeStrategy : IGradeStrategy
    {
        public double CalculateTotalScore(Grade grade)
        {
            return (grade.MidtermScore * 0.5) + (grade.FinalScore * 0.5);
        }

        public string CalculateClassification(double totalScore)
        {
            if (totalScore >= 9.0) return "A";
            if (totalScore >= 8.0) return "B";
            if (totalScore >= 7.0) return "C";
            if (totalScore >= 5.0) return "D";
            return "F";
        }
    }

    // Strategy 2: Tính điểm với trọng số khác (Midterm 40%, Final 60%)
    public class WeightedGradeStrategy : IGradeStrategy
    {
        public double CalculateTotalScore(Grade grade)
        {
            return (grade.MidtermScore * 0.4) + (grade.FinalScore * 0.6);
        }

        public string CalculateClassification(double totalScore)
        {
            if (totalScore >= 9.0) return "A";
            if (totalScore >= 8.0) return "B";
            if (totalScore >= 7.0) return "C";
            if (totalScore >= 5.0) return "D";
            return "F";
        }
    }
}