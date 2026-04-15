using System;

namespace PayrollSystem.Models
{
    /// <summary>
    /// Represents a deduction from an employee's salary
    /// </summary>
    public class Deduction
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DeductionType Type { get; set; }
        public bool IsPercentage { get; set; }
        public decimal PercentageRate { get; set; }
        public DateTime DateApplied { get; set; }

        /// <summary>
        /// Calculates the deduction amount based on gross salary
        /// </summary>
        /// <param name="grossSalary">The gross salary to calculate from</param>
        /// <returns>The calculated deduction amount</returns>
        public decimal CalculateDeduction(decimal grossSalary)
        {
            if (IsPercentage)
            {
                return grossSalary * (PercentageRate / 100);
            }
            return Amount;
        }
    }

    /// <summary>
    /// Enumeration of deduction types
    /// </summary>
    public enum DeductionType
    {
        SSS,
        PAGIBIG,
        PhilHealth,
        Tax,
        Loan,
        Other
    }
}
