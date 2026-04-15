using System;
using System.Collections.Generic;

namespace PayrollSystem.Models
{
    /// <summary>
    /// Represents a payroll record for an employee
    /// </summary>
    public class Payroll
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public Employee Employee { get; set; } = null!;
        public DateTime PayrollDate { get; set; }
        public int WorkDays { get; set; }
        public decimal OvertimeHours { get; set; }
        public decimal HolidayHours { get; set; }
        public decimal Allowance { get; set; }
        public decimal Bonus { get; set; }
        public List<Deduction> Deductions { get; set; } = new List<Deduction>();

        // Computed Properties
        public decimal BasicSalary => Employee.DailyRate * WorkDays;
        public decimal OvertimePay => OvertimeHours * (Employee.DailyRate / 8) * 1.25m; // 1.25x rate for OT
        public decimal HolidayPay => HolidayHours * (Employee.DailyRate / 8) * 2.0m; // 2x rate for holiday
        public decimal GrossSalary => BasicSalary + OvertimePay + HolidayPay + Allowance + Bonus;
        public decimal TotalDeductions => Deductions.Sum(d => d.CalculateDeduction(GrossSalary));
        public decimal NetPay => GrossSalary - TotalDeductions;

        /// <summary>
        /// Adds a deduction to this payroll
        /// </summary>
        /// <param name="deduction">The deduction to add</param>
        public void AddDeduction(Deduction deduction)
        {
            Deductions.Add(deduction);
        }

        /// <summary>
        /// Removes a deduction by ID
        /// </summary>
        /// <param name="deductionId">The ID of the deduction to remove</param>
        /// <returns>True if removed, false if not found</returns>
        public bool RemoveDeduction(int deductionId)
        {
            var deduction = Deductions.FirstOrDefault(d => d.Id == deductionId);
            if (deduction != null)
            {
                return Deductions.Remove(deduction);
            }
            return false;
        }
    }
}
