using System;
using System.Collections.Generic;
using System.Linq;
using PayrollSystem.Models;
using PayrollSystem.Services;

namespace PayrollSystem.Reporting
{
    /// <summary>
    /// Service for generating payroll reports
    /// </summary>
    public class PayrollReportService
    {
        private readonly EmployeeService _employeeService;
        private readonly PayrollService _payrollService;
        private readonly LoanService _loanService;
        private readonly AttendanceService _attendanceService;
        private readonly DepartmentService _departmentService;

        public PayrollReportService(EmployeeService employeeService, PayrollService payrollService,
                                 LoanService loanService, AttendanceService attendanceService,
                                 DepartmentService departmentService)
        {
            _employeeService = employeeService;
            _payrollService = payrollService;
            _loanService = loanService;
            _attendanceService = attendanceService;
            _departmentService = departmentService;
        }

        /// <summary>
        /// Generates a comprehensive payroll summary report
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>Payroll summary report</returns>
        public PayrollSummaryReport GeneratePayrollSummary(DateTime startDate, DateTime endDate)
        {
            var payrolls = _payrollService.GetAllPayrolls()
                                        .Where(p => p.PayrollDate >= startDate && p.PayrollDate <= endDate)
                                        .ToList();

            var report = new PayrollSummaryReport
            {
                StartDate = startDate,
                EndDate = endDate,
                GeneratedDate = DateTime.Now,
                TotalEmployees = _employeeService.GetEmployeeCount(),
                TotalPayrollProcessed = payrolls.Count,
                TotalGrossSalary = payrolls.Sum(p => p.GrossSalary),
                TotalNetPay = payrolls.Sum(p => p.NetPay),
                TotalDeductions = payrolls.Sum(p => p.TotalDeductions),
                AverageGrossSalary = payrolls.Any() ? payrolls.Average(p => p.GrossSalary) : 0m,
                AverageNetPay = payrolls.Any() ? payrolls.Average(p => p.NetPay) : 0m
            };

            // Calculate deduction totals by type
            report.DeductionBreakdown = new Dictionary<string, decimal>();
            foreach (var payroll in payrolls)
            {
                foreach (var deduction in payroll.Deductions)
                {
                    if (report.DeductionBreakdown.ContainsKey(deduction.Name))
                    {
                        report.DeductionBreakdown[deduction.Name] += deduction.CalculateDeduction(payroll.GrossSalary);
                    }
                    else
                    {
                        report.DeductionBreakdown[deduction.Name] = deduction.CalculateDeduction(payroll.GrossSalary);
                    }
                }
            }

            return report;
        }

        /// <summary>
        /// Generates an employee payroll report
        /// </summary>
        /// <param name="employeeId">Employee ID</param>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>Employee payroll report</returns>
        public EmployeePayrollReport GenerateEmployeePayrollReport(int employeeId, DateTime startDate, DateTime endDate)
        {
            var employee = _employeeService.GetEmployee(employeeId);
            if (employee == null)
                throw new PayrollSystem.Exceptions.PayrollException("Employee not found");

            var payrolls = _payrollService.GetEmployeePayrolls(employeeId)
                                        .Where(p => p.PayrollDate >= startDate && p.PayrollDate <= endDate)
                                        .ToList();

            var attendanceSummary = _attendanceService.GetAttendanceSummary(employeeId, startDate, endDate);
            var loans = _loanService.GetEmployeeLoans(employeeId);

            var report = new EmployeePayrollReport
            {
                Employee = employee,
                StartDate = startDate,
                EndDate = endDate,
                GeneratedDate = DateTime.Now,
                PayrollRecords = payrolls,
                TotalGrossSalary = payrolls.Sum(p => p.GrossSalary),
                TotalNetPay = payrolls.Sum(p => p.NetPay),
                TotalDeductions = payrolls.Sum(p => p.TotalDeductions),
                AverageGrossSalary = payrolls.Any() ? payrolls.Average(p => p.GrossSalary) : 0m,
                AverageNetPay = payrolls.Any() ? payrolls.Average(p => p.NetPay) : 0m,
                AttendanceSummary = attendanceSummary,
                ActiveLoans = loans.Where(l => l.Status == LoanStatus.Active).ToList(),
                TotalLoanBalance = loans.Where(l => l.Status == LoanStatus.Active).Sum(l => l.OutstandingBalance)
            };

            return report;
        }

        /// <summary>
        /// Generates a department payroll report
        /// </summary>
        /// <param name="departmentId">Department ID</param>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>Department payroll report</returns>
        public DepartmentPayrollReport GenerateDepartmentPayrollReport(int departmentId, DateTime startDate, DateTime endDate)
        {
            var department = _departmentService.GetDepartment(departmentId);
            if (department == null)
                throw new PayrollSystem.Exceptions.PayrollException("Department not found");

            var employees = _employeeService.GetAllEmployees()
                                          .Where(e => e.Position != null && e.Position.Contains(department.Name))
                                          .ToList();

            var employeeReports = new List<EmployeePayrollReport>();
            decimal totalGross = 0m;
            decimal totalNet = 0m;
            decimal totalDeductions = 0m;

            foreach (var employee in employees)
            {
                var employeeReport = GenerateEmployeePayrollReport(employee.Id, startDate, endDate);
                employeeReports.Add(employeeReport);
                totalGross += employeeReport.TotalGrossSalary;
                totalNet += employeeReport.TotalNetPay;
                totalDeductions += employeeReport.TotalDeductions;
            }

            var report = new DepartmentPayrollReport
            {
                Department = department,
                StartDate = startDate,
                EndDate = endDate,
                GeneratedDate = DateTime.Now,
                EmployeeCount = employees.Count(),
                EmployeeReports = employeeReports,
                TotalGrossSalary = totalGross,
                TotalNetPay = totalNet,
                TotalDeductions = totalDeductions,
                AverageGrossSalary = employees.Any() ? totalGross / employees.Count() : 0m,
                AverageNetPay = employees.Any() ? totalNet / employees.Count() : 0m
            };

            return report;
        }

        /// <summary>
        /// Generates a loan status report
        /// </summary>
        /// <returns>Loan status report</returns>
        public LoanStatusReport GenerateLoanStatusReport()
        {
            var loans = _loanService.GetAllLoans();
            var stats = _loanService.GetLoanStatistics();

            var report = new LoanStatusReport
            {
                GeneratedDate = DateTime.Now,
                TotalLoans = loans.Count,
                ActiveLoans = loans.Count(l => l.Status == LoanStatus.Active),
                CompletedLoans = loans.Count(l => l.Status == LoanStatus.Completed),
                PendingLoans = loans.Count(l => l.Status == LoanStatus.Pending),
                TotalLoanAmount = loans.Sum(l => l.TotalAmount),
                TotalOutstandingBalance = loans.Sum(l => l.OutstandingBalance),
                TotalCollected = loans.Sum(l => l.TotalPaid),
                LoansByType = loans.GroupBy(l => l.LoanType)
                                  .ToDictionary(g => g.Key, g => g.ToList()),
                LoansByStatus = loans.GroupBy(l => l.Status)
                                   .ToDictionary(g => g.Key.ToString(), g => g.ToList())
            };

            return report;
        }

        /// <summary>
        /// Generates an attendance report
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>Attendance report</returns>
        public AttendanceReport GenerateAttendanceReport(DateTime startDate, DateTime endDate)
        {
            var employees = _employeeService.GetAllEmployees();
            var attendanceSummaries = new Dictionary<int, Dictionary<string, object>>();
            var totalAttendanceStats = new Dictionary<string, int>();

            foreach (var employee in employees)
            {
                var summary = _attendanceService.GetAttendanceSummary(employee.Id, startDate, endDate);
                attendanceSummaries[employee.Id] = summary;
            }

            // Calculate overall statistics
            var allAttendance = new List<Models.Attendance>();
            foreach (var employee in employees)
            {
                var attendance = _attendanceService.GetEmployeeAttendance(employee.Id, startDate, endDate);
                allAttendance.AddRange(attendance);
            }

            totalAttendanceStats["TotalDays"] = allAttendance.Count;
            totalAttendanceStats["PresentDays"] = allAttendance.Count(a => a.Status == AttendanceStatus.Present);
            totalAttendanceStats["AbsentDays"] = allAttendance.Count(a => a.Status == AttendanceStatus.Absent);
            totalAttendanceStats["LateDays"] = allAttendance.Count(a => a.Status == AttendanceStatus.Late);
            totalAttendanceStats["HalfDays"] = allAttendance.Count(a => a.Status == AttendanceStatus.HalfDay);
            totalAttendanceStats["LeaveDays"] = allAttendance.Count(a => a.Status == AttendanceStatus.Leave);

            var report = new AttendanceReport
            {
                StartDate = startDate,
                EndDate = endDate,
                GeneratedDate = DateTime.Now,
                TotalEmployees = employees.Count,
                EmployeeSummaries = attendanceSummaries,
                TotalStatistics = totalAttendanceStats,
                EmployeesWithIssues = _attendanceService.GetEmployeesWithAttendanceIssues(startDate, endDate)
            };

            return report;
        }

        /// <summary>
        /// Exports a report to a formatted string
        /// </summary>
        /// <param name="report">The report to export</param>
        /// <returns>Formatted report string</returns>
        public string ExportReportToString(PayrollSummaryReport report)
        {
            var output = new System.Text.StringBuilder();

            output.AppendLine("PAYROLL SUMMARY REPORT");
            output.AppendLine("======================");
            output.AppendLine($"Period: {report.StartDate:MMM dd, yyyy} - {report.EndDate:MMM dd, yyyy}");
            output.AppendLine($"Generated: {report.GeneratedDate:MMM dd, yyyy HH:mm}");
            output.AppendLine();
            output.AppendLine("OVERVIEW:");
            output.AppendLine($"Total Employees: {report.TotalEmployees}");
            output.AppendLine($"Payroll Records: {report.TotalPayrollProcessed}");
            output.AppendLine($"Total Gross Salary: {report.TotalGrossSalary:C}");
            output.AppendLine($"Total Net Pay: {report.TotalNetPay:C}");
            output.AppendLine($"Total Deductions: {report.TotalDeductions:C}");
            output.AppendLine($"Average Gross Salary: {report.AverageGrossSalary:C}");
            output.AppendLine($"Average Net Pay: {report.AverageNetPay:C}");
            output.AppendLine();
            output.AppendLine("DEDUCTION BREAKDOWN:");
            output.AppendLine("--------------------");

            foreach (var deduction in report.DeductionBreakdown.OrderByDescending(d => d.Value))
            {
                output.AppendLine($"{deduction.Key}: {deduction.Value:C}");
            }

            return output.ToString();
        }
    }

    #region Report Models

    /// <summary>
    /// Payroll summary report model
    /// </summary>
    public class PayrollSummaryReport
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime GeneratedDate { get; set; }
        public int TotalEmployees { get; set; }
        public int TotalPayrollProcessed { get; set; }
        public decimal TotalGrossSalary { get; set; }
        public decimal TotalNetPay { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal AverageGrossSalary { get; set; }
        public decimal AverageNetPay { get; set; }
        public Dictionary<string, decimal> DeductionBreakdown { get; set; } = new Dictionary<string, decimal>();
    }

    /// <summary>
    /// Employee payroll report model
    /// </summary>
    public class EmployeePayrollReport
    {
        public Employee Employee { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime GeneratedDate { get; set; }
        public List<Payroll> PayrollRecords { get; set; } = new List<Payroll>();
        public decimal TotalGrossSalary { get; set; }
        public decimal TotalNetPay { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal AverageGrossSalary { get; set; }
        public decimal AverageNetPay { get; set; }
        public Dictionary<string, object> AttendanceSummary { get; set; } = new Dictionary<string, object>();
        public List<Loan> ActiveLoans { get; set; } = new List<Loan>();
        public decimal TotalLoanBalance { get; set; }
    }

    /// <summary>
    /// Department payroll report model
    /// </summary>
    public class DepartmentPayrollReport
    {
        public Department Department { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime GeneratedDate { get; set; }
        public int EmployeeCount { get; set; }
        public List<EmployeePayrollReport> EmployeeReports { get; set; } = new List<EmployeePayrollReport>();
        public decimal TotalGrossSalary { get; set; }
        public decimal TotalNetPay { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal AverageGrossSalary { get; set; }
        public decimal AverageNetPay { get; set; }
    }

    /// <summary>
    /// Loan status report model
    /// </summary>
    public class LoanStatusReport
    {
        public DateTime GeneratedDate { get; set; }
        public int TotalLoans { get; set; }
        public int ActiveLoans { get; set; }
        public int CompletedLoans { get; set; }
        public int PendingLoans { get; set; }
        public decimal TotalLoanAmount { get; set; }
        public decimal TotalOutstandingBalance { get; set; }
        public decimal TotalCollected { get; set; }
        public Dictionary<string, List<Loan>> LoansByType { get; set; } = new Dictionary<string, List<Loan>>();
        public Dictionary<string, List<Loan>> LoansByStatus { get; set; } = new Dictionary<string, List<Loan>>();
    }

    /// <summary>
    /// Attendance report model
    /// </summary>
    public class AttendanceReport
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime GeneratedDate { get; set; }
        public int TotalEmployees { get; set; }
        public Dictionary<int, Dictionary<string, object>> EmployeeSummaries { get; set; } = new Dictionary<int, Dictionary<string, object>>();
        public Dictionary<string, int> TotalStatistics { get; set; } = new Dictionary<string, int>();
        public List<int> EmployeesWithIssues { get; set; } = new List<int>();
    }

    #endregion
}
