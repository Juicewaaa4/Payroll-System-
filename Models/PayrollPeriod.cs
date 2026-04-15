using System;

namespace PayrollSystem.Models
{
    /// <summary>
    /// Represents a payroll period for processing employee salaries
    /// </summary>
    public class PayrollPeriod
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime PayDate { get; set; }
        public PayrollPeriodType PeriodType { get; set; }
        public bool IsProcessed { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? Description { get; set; }

        /// <summary>
        /// Gets the number of days in the payroll period
        /// </summary>
        public int NumberOfDays => (EndDate - StartDate).Days + 1;

        /// <summary>
        /// Gets the number of working days (excluding weekends)
        /// </summary>
        public int WorkingDays
        {
            get
            {
                int workingDays = 0;
                DateTime current = StartDate;
                
                while (current <= EndDate)
                {
                    if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
                    {
                        workingDays++;
                    }
                    current = current.AddDays(1);
                }
                
                return workingDays;
            }
        }

        /// <summary>
        /// Checks if a date falls within this payroll period
        /// </summary>
        /// <param name="date">The date to check</param>
        /// <returns>True if the date is within the period</returns>
        public bool ContainsDate(DateTime date)
        {
            return date >= StartDate && date <= EndDate;
        }

        /// <summary>
        /// Gets a formatted string representation of the period
        /// </summary>
        /// <returns>Formatted period string</returns>
        public override string ToString()
        {
            return $"{Name}: {StartDate:MMM dd} - {EndDate:MMM dd, yyyy} (Pay: {PayDate:MMM dd})";
        }
    }

    /// <summary>
    /// Enumeration of payroll period types
    /// </summary>
    public enum PayrollPeriodType
    {
        Weekly,
        SemiMonthly,
        Monthly,
        Quarterly,
        Custom
    }
}
