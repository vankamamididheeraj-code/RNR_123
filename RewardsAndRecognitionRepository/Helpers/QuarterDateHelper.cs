using RewardsAndRecognitionRepository.Enums;

namespace RewardsAndRecognitionRepository.Helpers
{
    public static class QuarterDateHelper
    {
        /// <summary>
        /// Calculates the start and end dates for a given year and quarter.
        /// </summary>
        /// <param name="year">The year (e.g., 2024)</param>
        /// <param name="quarter">The quarter (Q1, Q2, Q3, or Q4)</param>
        /// <returns>A tuple containing the start and end dates for the quarter</returns>
        /// <exception cref="ArgumentException">Thrown when the quarter is invalid</exception>
        public static (DateTime StartDate, DateTime EndDate) GetQuarterDateRange(int year, Quarter quarter)
        {
            return quarter switch
            {
                Quarter.Q1 => (new DateTime(year, 1, 1), new DateTime(year, 3, 31)),
                Quarter.Q2 => (new DateTime(year, 4, 1), new DateTime(year, 6, 30)),
                Quarter.Q3 => (new DateTime(year, 7, 1), new DateTime(year, 9, 30)),
                Quarter.Q4 => (new DateTime(year, 10, 1), new DateTime(year, 12, 31)),
                _ => throw new ArgumentException($"Invalid quarter: {quarter}", nameof(quarter))
            };
        }
    }
}
