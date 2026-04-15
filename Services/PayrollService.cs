using System;
using System.Collections.Generic;
using System.Linq;
using PayrollSystem.Models;
using PayrollSystem.Exceptions;
using PayrollSystem.Validation;

namespace PayrollSystem.Services
{
    /// <summary>
    /// Service for handling payroll computations and operations
    /// </summary>
    public class PayrollService
    {
        private static List<Payroll> _payrolls = new List<Payroll>();
        private static int _nextPayrollId = 1;
        private static int _nextDeductionId = 1;

        /// <summary>
        /// Creates a new payroll record for an employee
        /// </summary>
        /// <param name="employee">The employee</param>
        /// <param name="workDays">Number of work days</param>
        /// <param name="overtimeHours">Overtime hours</param>
        /// <param name="holidayHours">Holiday hours</param>
        /// <param name="allowance">Allowance amount</param>
        /// <param name="bonus">Bonus amount</param>
        /// <returns>The created payroll record</returns>
        /// <exception cref="PayrollCalculationException">Thrown when payroll validation fails</exception>
        public Payroll CreatePayroll(Employee employee, int workDays, decimal overtimeHours, 
                                   decimal holidayHours, decimal allowance, decimal bonus)
        {
            if (employee == null)
                throw new PayrollCalculationException("Employee is required for payroll creation");

            var payroll = new Payroll
            {
                Id = _nextPayrollId++,
                EmployeeId = employee.Id,
                Employee = employee,
                PayrollDate = DateTime.Now,
                WorkDays = workDays,
                OvertimeHours = overtimeHours,
                HolidayHours = holidayHours,
                Allowance = allowance,
                Bonus = bonus
            };

            ValidationHelper.ValidatePayroll(payroll);
            _payrolls.Add(payroll);
            return payroll;
        }

        /// <summary>
        /// Calculates standard government deductions based on gross salary
        /// </summary>
        /// <param name="grossSalary">The gross salary</param>
        /// <returns>List of standard deductions</returns>
        public List<Deduction> CalculateStandardDeductions(decimal grossSalary)
        {
            var deductions = new List<Deduction>();

            // SSS Contribution (4.5% of gross salary, max 1,125)
            var sssContribution = Math.Min(grossSalary * 0.045m, 1125m);
            deductions.Add(new Deduction
            {
                Id = _nextDeductionId++,
                Name = "SSS",
                Amount = sssContribution,
                Type = DeductionType.SSS,
                IsPercentage = false,
                PercentageRate = 4.5m,
                DateApplied = DateTime.Now
            });

            // PAG-IBIG Contribution (2% of gross salary, max 100)
            var pagibigContribution = Math.Min(grossSalary * 0.02m, 100m);
            deductions.Add(new Deduction
            {
                Id = _nextDeductionId++,
                Name = "PAG-IBIG",
                Amount = pagibigContribution,
                Type = DeductionType.PAGIBIG,
                IsPercentage = false,
                PercentageRate = 2m,
                DateApplied = DateTime.Now
            });

            // PhilHealth Contribution (2.75% of gross salary, max 1,650)
            var philhealthContribution = Math.Min(grossSalary * 0.0275m, 1650m);
            deductions.Add(new Deduction
            {
                Id = _nextDeductionId++,
                Name = "PhilHealth",
                Amount = philhealthContribution,
                Type = DeductionType.PhilHealth,
                IsPercentage = false,
                PercentageRate = 2.75m,
                DateApplied = DateTime.Now
            });

            // Tax Calculation (Progressive tax system)
            var tax = CalculateTax(grossSalary);
            deductions.Add(new Deduction
            {
                Id = _nextDeductionId++,
                Name = "Income Tax",
                Amount = tax,
                Type = DeductionType.Tax,
                IsPercentage = false,
                PercentageRate = 0m,
                DateApplied = DateTime.Now
            });

            return deductions;
        }

        /// <summary>
        /// Calculates income tax based on Philippine tax brackets
        /// </summary>
        /// <param name="grossSalary">Monthly gross salary</param>
        /// <returns>Tax amount</returns>
        private decimal CalculateTax(decimal grossSalary)
        {
            // Philippine Tax Brackets (Monthly)
            // 0 - 20,833: 0%
            // 20,834 - 33,333: 15% of excess over 20,833
            // 33,334 - 66,667: 2,500 + 20% of excess over 33,333
            // 66,668 - 166,667: 9,167 + 25% of excess over 66,667
            // 166,668 - 666,667: 34,167 + 30% of excess over 166,667
            // Above 666,667: 184,167 + 32% of excess over 666,667

            if (grossSalary <= 20833m)
                return 0m;
            else if (grossSalary <= 33333m)
                return (grossSalary - 20833m) * 0.15m;
            else if (grossSalary <= 66667m)
                return 2500m + (grossSalary - 33333m) * 0.20m;
            else if (grossSalary <= 166667m)
                return 9167m + (grossSalary - 66667m) * 0.25m;
            else if (grossSalary <= 666667m)
                return 34167m + (grossSalary - 166667m) * 0.30m;
            else
                return 184167m + (grossSalary - 666667m) * 0.32m;
        }

        /// <summary>
        /// Adds a custom deduction to a payroll
        /// </summary>
        /// <param name="payrollId">The payroll ID</param>
        /// <param name="deduction">The deduction to add</param>
        /// <returns>True if added successfully</returns>
        public bool AddDeductionToPayroll(int payrollId, Deduction deduction)
        {
            var payroll = _payrolls.FirstOrDefault(p => p.Id == payrollId);
            if (payroll != null)
            {
                deduction.Id = _nextDeductionId++;
                deduction.DateApplied = DateTime.Now;
                payroll.AddDeduction(deduction);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets a payroll by ID
        /// </summary>
        /// <param name="id">The payroll ID</param>
        /// <returns>The payroll if found, null otherwise</returns>
        public Payroll? GetPayroll(int id)
        {
            return _payrolls.FirstOrDefault(p => p.Id == id);
        }

        /// <summary>
        /// Gets all payrolls
        /// </summary>
        /// <returns>List of all payrolls</returns>
        public List<Payroll> GetAllPayrolls()
        {
            return _payrolls.ToList();
        }

        /// <summary>
        /// Gets payrolls for a specific employee
        /// </summary>
        /// <param name="employeeId">The employee ID</param>
        /// <returns>List of employee payrolls</returns>
        public List<Payroll> GetEmployeePayrolls(int employeeId)
        {
            return _payrolls.Where(p => p.EmployeeId == employeeId).ToList();
        }

        /// <summary>
        /// Generates a complete payroll with standard deductions
        /// </summary>
        /// <param name="employee">The employee</param>
        /// <param name="workDays">Number of work days</param>
        /// <param name="overtimeHours">Overtime hours</param>
        /// <param name="holidayHours">Holiday hours</param>
        /// <param name="allowance">Allowance amount</param>
        /// <param name="bonus">Bonus amount</param>
        /// <returns>The complete payroll with deductions</returns>
        public Payroll GenerateCompletePayroll(Employee employee, int workDays, decimal overtimeHours,
                                            decimal holidayHours, decimal allowance, decimal bonus)
        {
            var payroll = CreatePayroll(employee, workDays, overtimeHours, holidayHours, allowance, bonus);
            var standardDeductions = CalculateStandardDeductions(payroll.GrossSalary);
            
            foreach (var deduction in standardDeductions)
            {
                payroll.AddDeduction(deduction);
            }

            return payroll;
        }
    }
}
