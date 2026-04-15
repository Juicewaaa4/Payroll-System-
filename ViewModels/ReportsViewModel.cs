using System;
using System.Collections.ObjectModel;
using PayrollSystem.DataAccess;
using MySql.Data.MySqlClient;

namespace PayrollSystem.ViewModels
{
    public class ReportsViewModel : BaseViewModel
    {
        private DateTime _startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        private DateTime _endDate = DateTime.Now;

        public DateTime StartDate { get => _startDate; set { SetProperty(ref _startDate, value); LoadData(); } }
        public DateTime EndDate { get => _endDate; set { SetProperty(ref _endDate, value); LoadData(); } }

        public ObservableCollection<PayrollRecord> PayrollRecords { get; } = new();

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
                        PayrollDate = reader.GetDateTime("payroll_date").ToString("MMM dd, yyyy"),
                        GrossSalary = $"₱{reader.GetDecimal("gross_salary"):N2}",
                        Deductions = $"₱{reader.GetDecimal("total_deductions"):N2}",
                        NetPay = $"₱{reader.GetDecimal("net_pay"):N2}",
                        Status = reader.GetString("status")
                    });
                }
            }
            catch { LoadDemoData(); }
        }

        private void LoadDemoData()
        {
            PayrollRecords.Add(new PayrollRecord { EmployeeName = "No records found", EmpNumber = "—", PayrollDate = "—", GrossSalary = "—", Deductions = "—", NetPay = "—", Status = "—" });
        }
    }

    public class PayrollRecord
    {
        public int Id { get; set; }
        public string EmployeeName { get; set; } = "";
        public string EmpNumber { get; set; } = "";
        public string PayrollDate { get; set; } = "";
        public string GrossSalary { get; set; } = "";
        public string Deductions { get; set; } = "";
        public string NetPay { get; set; } = "";
        public string Status { get; set; } = "";
    }
}
