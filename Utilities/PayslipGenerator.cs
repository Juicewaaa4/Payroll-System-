using System;
using System.Globalization;
using PayrollSystem.Models;
using PayrollSystem.UI;
using PayrollSystem.Configuration;

namespace PayrollSystem.Utilities
{
    /// <summary>
    /// Professional payslip generator with formatted display
    /// </summary>
    public static class PayslipGenerator
    {
        /// <summary>
        /// Displays a professionally formatted payslip
        /// </summary>
        /// <param name="payroll">The payroll to display</param>
        public static void DisplayPayslip(Payroll payroll)
        {
            var width = ConsoleUIHelper.DefaultWidth;

            Console.WriteLine();

            // ─── Payslip Header ─────────────────────────────────────
            ConsoleUIHelper.DrawTopBorder(width);
            ConsoleUIHelper.DrawEmptyLine(width);
            ConsoleUIHelper.DrawCenteredLine("P  A  Y  S  L  I  P", ConsoleUIHelper.PrimaryColor, width);
            ConsoleUIHelper.DrawCenteredLine("Payroll Management System", ConsoleUIHelper.MutedColor, width);
            ConsoleUIHelper.DrawEmptyLine(width);
            ConsoleUIHelper.DrawSeparator(width);

            // ─── Employee Information ───────────────────────────────
            ConsoleUIHelper.DrawEmptyLine(width);
            ConsoleUIHelper.DrawBoxLine("EMPLOYEE DETAILS", ConsoleUIHelper.SubtleColor, width);
            ConsoleUIHelper.DrawEmptyLine(width);
            ConsoleUIHelper.DrawKeyValue("Employee Name:", payroll.Employee.FullName, 20, width);
            ConsoleUIHelper.DrawKeyValue("Position:", payroll.Employee.Position, 20, width);
            ConsoleUIHelper.DrawKeyValueColored("Employee ID:", payroll.EmployeeId.ToString("D4"), ConsoleUIHelper.PrimaryColor, 20, width);
            ConsoleUIHelper.DrawKeyValue("Payroll Date:", payroll.PayrollDate.ToString("dd MMMM yyyy"), 20, width);
            ConsoleUIHelper.DrawEmptyLine(width);

            // ─── Earnings Section ───────────────────────────────────
            ConsoleUIHelper.DrawSeparator(width);
            ConsoleUIHelper.DrawEmptyLine(width);
            ConsoleUIHelper.DrawBoxLine("EARNINGS", ConsoleUIHelper.SuccessColor, width);
            ConsoleUIHelper.DrawEmptyLine(width);

            // Basic Salary
            var basicLabel = $"Basic Salary ({payroll.WorkDays} days × {payroll.Employee.DailyRate:C})";
            ConsoleUIHelper.DrawKeyValueColored(
                basicLabel.Length > 42 ? basicLabel.Substring(0, 42) : basicLabel,
                payroll.BasicSalary.ToString("C"),
                ConsoleUIHelper.SuccessColor, 44, width);

            // Overtime Pay
            var otRate = (payroll.Employee.DailyRate / 8) * 1.25m;
            ConsoleUIHelper.DrawKeyValueColored(
                $"Overtime ({payroll.OvertimeHours}h × {otRate:C})",
                payroll.OvertimePay.ToString("C"),
                ConsoleUIHelper.SuccessColor, 44, width);

            // Holiday Pay
            var holRate = (payroll.Employee.DailyRate / 8) * 2m;
            ConsoleUIHelper.DrawKeyValueColored(
                $"Holiday Pay ({payroll.HolidayHours}h × {holRate:C})",
                payroll.HolidayPay.ToString("C"),
                ConsoleUIHelper.SuccessColor, 44, width);

            // Allowance
            ConsoleUIHelper.DrawKeyValueColored(
                "Allowance",
                payroll.Allowance.ToString("C"),
                ConsoleUIHelper.SuccessColor, 44, width);

            // Bonus
            ConsoleUIHelper.DrawKeyValueColored(
                "Bonus",
                payroll.Bonus.ToString("C"),
                ConsoleUIHelper.SuccessColor, 44, width);

            ConsoleUIHelper.DrawEmptyLine(width);
            ConsoleUIHelper.DrawThinSeparator(width);
            ConsoleUIHelper.DrawKeyValueColored(
                "GROSS SALARY",
                payroll.GrossSalary.ToString("C"),
                ConsoleUIHelper.AccentColor, 44, width);
            ConsoleUIHelper.DrawEmptyLine(width);

            // ─── Deductions Section ─────────────────────────────────
            ConsoleUIHelper.DrawSeparator(width);
            ConsoleUIHelper.DrawEmptyLine(width);
            ConsoleUIHelper.DrawBoxLine("DEDUCTIONS", ConsoleUIHelper.ErrorColor, width);
            ConsoleUIHelper.DrawEmptyLine(width);

            foreach (var deduction in payroll.Deductions)
            {
                string deductionLabel = deduction.IsPercentage
                    ? $"{deduction.Name} ({deduction.PercentageRate}%)"
                    : deduction.Name;

                ConsoleUIHelper.DrawKeyValueColored(
                    deductionLabel,
                    $"-{deduction.CalculateDeduction(payroll.GrossSalary):C}",
                    ConsoleUIHelper.ErrorColor, 44, width);
            }

            ConsoleUIHelper.DrawEmptyLine(width);
            ConsoleUIHelper.DrawThinSeparator(width);
            ConsoleUIHelper.DrawKeyValueColored(
                "TOTAL DEDUCTIONS",
                $"-{payroll.TotalDeductions:C}",
                ConsoleUIHelper.ErrorColor, 44, width);
            ConsoleUIHelper.DrawEmptyLine(width);

            // ─── Net Pay Section ────────────────────────────────────
            ConsoleUIHelper.DrawSeparator(width);
            ConsoleUIHelper.DrawEmptyLine(width);
            ConsoleUIHelper.DrawEmptyLine(width);

            // Prominent net pay display
            ConsoleUIHelper.Write($"{ConsoleUIHelper.Vertical}", ConsoleUIHelper.MutedColor);
            var netPayText = $"NET PAY:  {payroll.NetPay:C}";
            var totalPad = width - 2 - netPayText.Length;
            var leftPad = totalPad / 2;
            var rightPad = totalPad - leftPad;
            ConsoleUIHelper.Write(new string(' ', leftPad));
            ConsoleUIHelper.Write("NET PAY:  ", ConsoleUIHelper.SubtleColor);
            ConsoleUIHelper.Write(payroll.NetPay.ToString("C"), ConsoleUIHelper.PrimaryColor);
            ConsoleUIHelper.Write(new string(' ', rightPad));
            ConsoleUIHelper.WriteLine($"{ConsoleUIHelper.Vertical}", ConsoleUIHelper.MutedColor);

            ConsoleUIHelper.DrawEmptyLine(width);
            ConsoleUIHelper.DrawEmptyLine(width);
            ConsoleUIHelper.DrawSeparator(width);
            ConsoleUIHelper.DrawCenteredLine("This is a system-generated payslip.", ConsoleUIHelper.MutedColor, width);
            ConsoleUIHelper.DrawBottomBorder(width);
        }

        /// <summary>
        /// Displays a summary of payroll calculations
        /// </summary>
        /// <param name="payroll">The payroll to summarize</param>
        public static void DisplayPayrollSummary(Payroll payroll)
        {
            var width = ConsoleUIHelper.DefaultWidth;

            Console.WriteLine();
            ConsoleUIHelper.DrawTopBorder(width);
            ConsoleUIHelper.DrawEmptyLine(width);
            ConsoleUIHelper.DrawCenteredLine("PAYROLL SUMMARY", ConsoleUIHelper.PrimaryColor, width);
            ConsoleUIHelper.DrawEmptyLine(width);
            ConsoleUIHelper.DrawSeparator(width);
            ConsoleUIHelper.DrawKeyValue("Employee:", payroll.Employee.FullName, 20, width);
            ConsoleUIHelper.DrawKeyValueColored("Basic Salary:", payroll.BasicSalary.ToString("C"), ConsoleUIHelper.SuccessColor, 20, width);
            ConsoleUIHelper.DrawKeyValueColored("Overtime:", payroll.OvertimePay.ToString("C"), ConsoleUIHelper.SuccessColor, 20, width);
            ConsoleUIHelper.DrawKeyValueColored("Holiday Pay:", payroll.HolidayPay.ToString("C"), ConsoleUIHelper.SuccessColor, 20, width);
            ConsoleUIHelper.DrawKeyValueColored("Allowance:", payroll.Allowance.ToString("C"), ConsoleUIHelper.SuccessColor, 20, width);
            ConsoleUIHelper.DrawKeyValueColored("Bonus:", payroll.Bonus.ToString("C"), ConsoleUIHelper.SuccessColor, 20, width);
            ConsoleUIHelper.DrawThinSeparator(width);
            ConsoleUIHelper.DrawKeyValueColored("Gross Salary:", payroll.GrossSalary.ToString("C"), ConsoleUIHelper.AccentColor, 20, width);
            ConsoleUIHelper.DrawKeyValueColored("Deductions:", $"-{payroll.TotalDeductions:C}", ConsoleUIHelper.ErrorColor, 20, width);
            ConsoleUIHelper.DrawSeparator(width);
            ConsoleUIHelper.DrawKeyValueColored("Net Pay:", payroll.NetPay.ToString("C"), ConsoleUIHelper.PrimaryColor, 20, width);
            ConsoleUIHelper.DrawEmptyLine(width);
            ConsoleUIHelper.DrawBottomBorder(width);
        }
    }
}
