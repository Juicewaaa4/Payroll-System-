using System;

namespace PayrollSystem.Models
{
    /// <summary>
    /// Represents an employee in the payroll system
    /// </summary>
    public class Employee
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public decimal DailyRate { get; set; }
        public DateTime HireDate { get; set; }
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets the full name of the employee
        /// </summary>
        public string FullName => $"{FirstName} {LastName}";

        /// <summary>
        /// Gets the basic monthly salary (assuming 22 working days)
        /// </summary>
        public decimal BasicSalary => DailyRate * 22;
    }
}
