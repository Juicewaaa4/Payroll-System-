using System;
using System.Collections.Generic;
using System.Linq;
using PayrollSystem.Models;
using PayrollSystem.Exceptions;
using PayrollSystem.Validation;

namespace PayrollSystem.Services
{
    /// <summary>
    /// Service for managing payroll periods
    /// </summary>
    public class PayrollPeriodService
    {
        private static List<PayrollPeriod> _payrollPeriods = new List<PayrollPeriod>();
        private static int _nextId = 1;

        /// <summary>
        /// Creates a new payroll period
        /// </summary>
        /// <param name="name">Period name</param>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <param name="payDate">Payment date</param>
        /// <param name="periodType">Type of period</param>
        /// <returns>The created payroll period</returns>
        /// <exception cref="PayrollException">Thrown when validation fails</exception>
        public PayrollPeriod CreatePayrollPeriod(string name, DateTime startDate, DateTime endDate, 
                                               DateTime payDate, PayrollPeriodType periodType)
        {
            ValidatePayrollPeriod(name, startDate, endDate, payDate);

            var payrollPeriod = new PayrollPeriod
            {
                Id = _nextId++,
                Name = name,
                StartDate = startDate,
                EndDate = endDate,
                PayDate = payDate,
                PeriodType = periodType,
                IsProcessed = false,
                CreatedDate = DateTime.Now
            };

            _payrollPeriods.Add(payrollPeriod);
            return payrollPeriod;
        }

        /// <summary>
        /// Gets a payroll period by ID
        /// </summary>
        /// <param name="id">The period ID</param>
        /// <returns>The payroll period if found, null otherwise</returns>
        public PayrollPeriod? GetPayrollPeriod(int id)
        {
            return _payrollPeriods.FirstOrDefault(p => p.Id == id);
        }

        /// <summary>
        /// Gets all payroll periods
        /// </summary>
        /// <returns>List of all payroll periods</returns>
        public List<PayrollPeriod> GetAllPayrollPeriods()
        {
            return _payrollPeriods.OrderByDescending(p => p.StartDate).ToList();
        }

        /// <summary>
        /// Gets active (unprocessed) payroll periods
        /// </summary>
        /// <returns>List of active payroll periods</returns>
        public List<PayrollPeriod> GetActivePayrollPeriods()
        {
            return _payrollPeriods.Where(p => !p.IsProcessed).OrderByDescending(p => p.StartDate).ToList();
        }

        /// <summary>
        /// Gets processed payroll periods
        /// </summary>
        /// <returns>List of processed payroll periods</returns>
        public List<PayrollPeriod> GetProcessedPayrollPeriods()
        {
            return _payrollPeriods.Where(p => p.IsProcessed).OrderByDescending(p => p.StartDate).ToList();
        }

        /// <summary>
        /// Marks a payroll period as processed
        /// </summary>
        /// <param name="id">The period ID</param>
        /// <returns>True if marked successfully, false if not found</returns>
        public bool MarkAsProcessed(int id)
        {
            var period = _payrollPeriods.FirstOrDefault(p => p.Id == id);
            if (period != null)
            {
                period.IsProcessed = true;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Deletes a payroll period
        /// </summary>
        /// <param name="id">The period ID</param>
        /// <returns>True if deleted, false if not found or already processed</returns>
        public bool DeletePayrollPeriod(int id)
        {
            var period = _payrollPeriods.FirstOrDefault(p => p.Id == id);
            if (period != null)
            {
                if (period.IsProcessed)
                {
                    throw new PayrollException("Cannot delete a processed payroll period");
                }
                return _payrollPeriods.Remove(period);
            }
            return false;
        }

        /// <summary>
        /// Creates standard payroll periods for a year
        /// </summary>
        /// <param name="year">The year to create periods for</param>
        /// <param name="periodType">Type of periods to create</param>
        /// <returns>List of created payroll periods</returns>
        public List<PayrollPeriod> CreateStandardPeriods(int year, PayrollPeriodType periodType)
        {
            var periods = new List<PayrollPeriod>();
            
            switch (periodType)
            {
                case PayrollPeriodType.SemiMonthly:
                    periods = CreateSemiMonthlyPeriods(year);
                    break;
                case PayrollPeriodType.Monthly:
                    periods = CreateMonthlyPeriods(year);
                    break;
                case PayrollPeriodType.Weekly:
                    periods = CreateWeeklyPeriods(year);
                    break;
                case PayrollPeriodType.Quarterly:
                    periods = CreateQuarterlyPeriods(year);
                    break;
                default:
                    throw new PayrollException($"Standard periods not supported for {periodType}");
            }

            return periods;
        }

        /// <summary>
        /// Gets the payroll period that contains a specific date
        /// </summary>
        /// <param name="date">The date to search for</param>
        /// <returns>The payroll period if found, null otherwise</returns>
        public PayrollPeriod? GetPeriodForDate(DateTime date)
        {
            return _payrollPeriods.FirstOrDefault(p => p.ContainsDate(date));
        }

        #region Private Methods

        private void ValidatePayrollPeriod(string name, DateTime startDate, DateTime endDate, DateTime payDate)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new PayrollException("Payroll period name is required");

            if (startDate >= endDate)
                throw new PayrollException("Start date must be before end date");

            if (payDate <= endDate)
                throw new PayrollException("Pay date must be after end date");

            if (!ValidationHelper.IsValidDateRange(startDate))
                throw new PayrollException("Start date is out of valid range");

            if (!ValidationHelper.IsValidDateRange(endDate))
                throw new PayrollException("End date is out of valid range");

            if (!ValidationHelper.IsValidDateRange(payDate))
                throw new PayrollException("Pay date is out of valid range");

            // Check for overlapping periods
            if (_payrollPeriods.Any(p => 
                (startDate >= p.StartDate && startDate <= p.EndDate) ||
                (endDate >= p.StartDate && endDate <= p.EndDate) ||
                (startDate <= p.StartDate && endDate >= p.EndDate)))
            {
                throw new PayrollException("Payroll period overlaps with an existing period");
            }
        }

        private List<PayrollPeriod> CreateSemiMonthlyPeriods(int year)
        {
            var periods = new List<PayrollPeriod>();
            
            for (int month = 1; month <= 12; month++)
            {
                // First period: 1st to 15th
                var startDate1 = new DateTime(year, month, 1);
                var endDate1 = new DateTime(year, month, 15);
                var payDate1 = new DateTime(year, month, 20);
                
                periods.Add(CreatePayrollPeriod(
                    $"{year} - {GetMonthName(month)} (1-15)",
                    startDate1, endDate1, payDate1, PayrollPeriodType.SemiMonthly));

                // Second period: 16th to end of month
                var startDate2 = new DateTime(year, month, 16);
                var endDate2 = new DateTime(year, month, DateTime.DaysInMonth(year, month));
                var payDate2 = new DateTime(year, month, DateTime.DaysInMonth(year, month));
                
                periods.Add(CreatePayrollPeriod(
                    $"{year} - {GetMonthName(month)} (16-{DateTime.DaysInMonth(year, month)})",
                    startDate2, endDate2, payDate2, PayrollPeriodType.SemiMonthly));
            }

            return periods;
        }

        private List<PayrollPeriod> CreateMonthlyPeriods(int year)
        {
            var periods = new List<PayrollPeriod>();
            
            for (int month = 1; month <= 12; month++)
            {
                var startDate = new DateTime(year, month, 1);
                var endDate = new DateTime(year, month, DateTime.DaysInMonth(year, month));
                var payDate = new DateTime(year, month, DateTime.DaysInMonth(year, month));
                
                periods.Add(CreatePayrollPeriod(
                    $"{year} - {GetMonthName(month)}",
                    startDate, endDate, payDate, PayrollPeriodType.Monthly));
            }

            return periods;
        }

        private List<PayrollPeriod> CreateWeeklyPeriods(int year)
        {
            var periods = new List<PayrollPeriod>();
            var startDate = new DateTime(year, 1, 1);
            
            // Find first Monday
            while (startDate.DayOfWeek != DayOfWeek.Monday)
            {
                startDate = startDate.AddDays(1);
            }

            var currentStart = startDate;
            var currentEnd = startDate.AddDays(6);
            
            while (currentStart.Year == year)
            {
                var payDate = currentEnd.AddDays(1); // Pay on Monday after the week
                
                periods.Add(CreatePayrollPeriod(
                    $"Week {currentStart:MMM dd}-{currentEnd:MMM dd}",
                    currentStart, currentEnd, payDate, PayrollPeriodType.Weekly));

                currentStart = currentStart.AddDays(7);
                currentEnd = currentEnd.AddDays(7);
            }

            return periods;
        }

        private List<PayrollPeriod> CreateQuarterlyPeriods(int year)
        {
            var periods = new List<PayrollPeriod>();
            
            for (int quarter = 1; quarter <= 4; quarter++)
            {
                var startMonth = (quarter - 1) * 3 + 1;
                var endMonth = quarter * 3;
                
                var startDate = new DateTime(year, startMonth, 1);
                var endDate = new DateTime(year, endMonth, DateTime.DaysInMonth(year, endMonth));
                var payDate = new DateTime(year, endMonth, DateTime.DaysInMonth(year, endMonth));
                
                periods.Add(CreatePayrollPeriod(
                    $"Q{quarter} {year}",
                    startDate, endDate, payDate, PayrollPeriodType.Quarterly));
            }

            return periods;
        }

        private string GetMonthName(int month)
        {
            return new DateTime(2023, month, 1).ToString("MMMM");
        }

        #endregion
    }
}
