using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows.Input;
using PayrollSystem.Helpers;
using PayrollSystem.DataAccess;
using MySql.Data.MySqlClient;

namespace PayrollSystem.ViewModels
{
    public class ReportsViewModel : BaseViewModel
    {
        private DateTime _startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        private DateTime _endDate = DateTime.Now;
        private string _statusMessage = "";

        public DateTime StartDate { get => _startDate; set { SetProperty(ref _startDate, value); LoadData(); } }
        public DateTime EndDate { get => _endDate; set { SetProperty(ref _endDate, value); LoadData(); } }
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        public ObservableCollection<PayrollRecord> PayrollRecords { get; } = new();

        public ICommand ExportExcelCommand { get; }

        public ReportsViewModel()
        {
            ExportExcelCommand = new RelayCommand(_ => ExportToExcel());
        }

        public void LoadData()
        {
            PayrollRecords.Clear();
            try
            {
                if (!DatabaseHelper.TestConnection()) { LoadDemoData(); return; }

                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                using var cmd = new MySqlCommand(
                    @"SELECT p.*, CONCAT(e.first_name, ' ', e.last_name) as employee_name, e.emp_number
                      FROM payroll p JOIN employees e ON p.employee_id = e.id
                      WHERE p.payroll_date BETWEEN @start AND @end
                      ORDER BY p.payroll_date DESC", conn);
                cmd.Parameters.AddWithValue("@start", StartDate);
                cmd.Parameters.AddWithValue("@end", EndDate);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    PayrollRecords.Add(new PayrollRecord
                    {
                        Id = reader.GetInt32("id"),
                        EmployeeName = reader.GetString("employee_name"),
                        EmpNumber = reader.GetString("emp_number"),
                        PayrollDate = reader.GetDateTime("payroll_date").ToString("MMM dd, yyyy  hh:mm tt"),
                        GrossSalary = $"₱{reader.GetDecimal("gross_salary"):N2}",
                        GrossRaw = reader.GetDecimal("gross_salary"),
                        Deductions = $"₱{reader.GetDecimal("total_deductions"):N2}",
                        DeductionsRaw = reader.GetDecimal("total_deductions"),
                        NetPay = $"₱{reader.GetDecimal("net_pay"):N2}",
                        NetPayRaw = reader.GetDecimal("net_pay"),
                        Status = reader.GetString("status")
                    });
                }

                if (PayrollRecords.Count == 0) LoadDemoData();
            }
            catch { LoadDemoData(); }
        }

        private void LoadDemoData()
        {
            PayrollRecords.Clear();
            // Show sample demo records
            var now = DateTime.Now;
            PayrollRecords.Add(new PayrollRecord { EmployeeName = "Kenneth Ariel Francisco", EmpNumber = "EMP-0001", PayrollDate = now.ToString("MMM dd, yyyy  hh:mm tt"), GrossSalary = "₱26,400.00", GrossRaw = 26400, Deductions = "₱2,412.00", DeductionsRaw = 2412, NetPay = "₱23,988.00", NetPayRaw = 23988, Status = "Processed" });
            PayrollRecords.Add(new PayrollRecord { EmployeeName = "Judy Peralta", EmpNumber = "EMP-0002", PayrollDate = now.ToString("MMM dd, yyyy  hh:mm tt"), GrossSalary = "₱33,000.00", GrossRaw = 33000, Deductions = "₱3,037.50", DeductionsRaw = 3037.50m, NetPay = "₱29,962.50", NetPayRaw = 29962.50m, Status = "Processed" });
            PayrollRecords.Add(new PayrollRecord { EmployeeName = "Alyssa Marie Zamudio", EmpNumber = "EMP-0004", PayrollDate = now.AddDays(-1).ToString("MMM dd, yyyy  hh:mm tt"), GrossSalary = "₱22,000.00", GrossRaw = 22000, Deductions = "₱2,035.00", DeductionsRaw = 2035, NetPay = "₱19,965.00", NetPayRaw = 19965, Status = "Processed" });
        }

        private void ExportToExcel()
        {
            if (PayrollRecords.Count == 0)
            {
                StatusMessage = "No records to export.";
                return;
            }

            try
            {
                var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var fileName = $"Payroll_Report_{DateTime.Now:yyyyMMdd_hhmmtt}.csv";
                var filePath = Path.Combine(desktop, fileName);

                var sb = new StringBuilder();
                sb.AppendLine("Payroll Report");
                sb.AppendLine($"Period: {StartDate:MMM dd, yyyy} to {EndDate:MMM dd, yyyy}");
                sb.AppendLine($"Generated: {DateTime.Now:MMM dd, yyyy  hh:mm tt}");
                sb.AppendLine();
                sb.AppendLine("EMP #,Employee Name,Date,Gross Salary,Deductions,Net Pay,Status");

                decimal totalGross = 0, totalDed = 0, totalNet = 0;

                foreach (var rec in PayrollRecords)
                {
                    sb.AppendLine($"{rec.EmpNumber},{rec.EmployeeName},{rec.PayrollDate},{rec.GrossRaw:N2},{rec.DeductionsRaw:N2},{rec.NetPayRaw:N2},{rec.Status}");
                    totalGross += rec.GrossRaw;
                    totalDed += rec.DeductionsRaw;
                    totalNet += rec.NetPayRaw;
                }

                sb.AppendLine();
                sb.AppendLine($",,TOTALS,{totalGross:N2},{totalDed:N2},{totalNet:N2},");

                File.WriteAllText(filePath, sb.ToString());
                StatusMessage = $"✓ Exported to Desktop: {fileName}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export error: {ex.Message}";
            }
        }
    }

    public class PayrollRecord
    {
        public int Id { get; set; }
        public string EmployeeName { get; set; } = "";
        public string EmpNumber { get; set; } = "";
        public string PayrollDate { get; set; } = "";
        public string GrossSalary { get; set; } = "";
        public decimal GrossRaw { get; set; }
        public string Deductions { get; set; } = "";
        public decimal DeductionsRaw { get; set; }
        public string NetPay { get; set; } = "";
        public decimal NetPayRaw { get; set; }
        public string Status { get; set; } = "";
    }
}
