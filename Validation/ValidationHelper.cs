using System;
using System.Text.RegularExpressions;
using PayrollSystem.Exceptions;
using PayrollSystem.Models;

namespace PayrollSystem.Validation
{
    /// <summary>
    /// Helper class for validating data in the payroll system
    /// </summary>
    public static class ValidationHelper
    {
        /// <summary>
        /// Validates an employee object
        /// </summary>
        /// <param name="employee">The employee to validate</param>
        /// <exception cref="EmployeeValidationException">Thrown when validation fails</exception>
        public static void ValidateEmployee(Employee employee)
        {
            if (employee == null)
                throw new EmployeeValidationException("Employee cannot be null");

            if (string.IsNullOrWhiteSpace(employee.FirstName))
                throw new EmployeeValidationException("First name is required");

            if (string.IsNullOrWhiteSpace(employee.LastName))
                throw new EmployeeValidationException("Last name is required");

            if (string.IsNullOrWhiteSpace(employee.Position))
                throw new EmployeeValidationException("Position is required");

            if (employee.DailyRate <= 0)
                throw new EmployeeValidationException("Daily rate must be greater than 0");

            if (employee.HireDate > DateTime.Now)
                throw new EmployeeValidationException("Hire date cannot be in the future");

            if (!IsValidName(employee.FirstName))
                throw new EmployeeValidationException("First name contains invalid characters");

            if (!IsValidName(employee.LastName))
                throw new EmployeeValidationException("Last name contains invalid characters");
        }

        /// <summary>
        /// Validates a payroll object
        /// </summary>
        /// <param name="payroll">The payroll to validate</param>
        /// <exception cref="PayrollCalculationException">Thrown when validation fails</exception>
        public static void ValidatePayroll(Payroll payroll)
        {
            if (payroll == null)
                throw new PayrollCalculationException("Payroll cannot be null");

            if (payroll.Employee == null)
                throw new PayrollCalculationException("Employee is required for payroll");

            if (payroll.WorkDays < 0)
                throw new PayrollCalculationException("Work days cannot be negative");

            if (payroll.WorkDays > 31)
                throw new PayrollCalculationException("Work days cannot exceed 31");

            if (payroll.OvertimeHours < 0)
                throw new PayrollCalculationException("Overtime hours cannot be negative");

            if (payroll.HolidayHours < 0)
                throw new PayrollCalculationException("Holiday hours cannot be negative");

            if (payroll.Allowance < 0)
                throw new PayrollCalculationException("Allowance cannot be negative");

            if (payroll.Bonus < 0)
                throw new PayrollCalculationException("Bonus cannot be negative");
        }

        /// <summary>
        /// Validates a deduction object
        /// </summary>
        /// <param name="deduction">The deduction to validate</param>
        /// <exception cref="DeductionException">Thrown when validation fails</exception>
        public static void ValidateDeduction(Deduction deduction)
        {
            if (deduction == null)
                throw new DeductionException("Deduction cannot be null");

            if (string.IsNullOrWhiteSpace(deduction.Name))
                throw new DeductionException("Deduction name is required");

            if (deduction.IsPercentage)
            {
                if (deduction.PercentageRate <= 0 || deduction.PercentageRate > 100)
                    throw new DeductionException("Percentage rate must be between 0 and 100");
            }
            else
            {
                if (deduction.Amount < 0)
                    throw new DeductionException("Deduction amount cannot be negative");
            }
        }

        /// <summary>
        /// Validates that a string contains only valid name characters
        /// </summary>
        /// <param name="name">The name to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        private static bool IsValidName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            // Allow letters, spaces, hyphens, and apostrophes
            return Regex.IsMatch(name, @"^[a-zA-Z\s\-']+$");
        }

        /// <summary>
        /// Validates email format
        /// </summary>
        /// <param name="email">The email to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        /// <summary>
        /// Validates phone number format
        /// </summary>
        /// <param name="phone">The phone number to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValidPhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            // Allow digits, spaces, hyphens, and parentheses
            return Regex.IsMatch(phone, @"^[\d\s\-\(\)]+$");
        }

        /// <summary>
        /// Validates that a date is within a reasonable range
        /// </summary>
        /// <param name="date">The date to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValidDateRange(DateTime date)
        {
            // Date should be between 1900 and 10 years from now
            var minDate = new DateTime(1900, 1, 1);
            var maxDate = DateTime.Now.AddYears(10);
            
            return date >= minDate && date <= maxDate;
        }

        /// <summary>
        /// Validates that a monetary amount is reasonable
        /// </summary>
        /// <param name="amount">The amount to validate</param>
        /// <param name="maxAmount">Maximum allowed amount (default: 1,000,000)</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValidMonetaryAmount(decimal amount, decimal maxAmount = 1000000m)
        {
            return amount >= 0 && amount <= maxAmount;
        }
    }
}
