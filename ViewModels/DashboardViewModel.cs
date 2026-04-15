using System;
using System.Collections.ObjectModel;
using PayrollSystem.DataAccess;
using MySql.Data.MySqlClient;

namespace PayrollSystem.ViewModels
{
    public class DashboardViewModel : BaseViewModel
    {
        private string _totalEmployees = "0";
        private string _totalPayroll = "₱0.00";
        private string _activeDepartments = "0";
        private string _pendingPayroll = "0";
        private string _currentDate = DateTime.Now.ToString("dddd, MMMM dd, yyyy");
        private string _greeting = "Good Morning";

        public string TotalEmployees { get => _totalEmployees; set => SetProperty(ref _totalEmployees, value); }
        public string TotalPayroll { get => _totalPayroll; set => SetProperty(ref _totalPayroll, value); }
        public string ActiveDepartments { get => _activeDepartments; set => SetProperty(ref _activeDepartments, value); }
        public string PendingPayroll { get => _pendingPayroll; set => SetProperty(ref _pendingPayroll, value); }
        public string CurrentDate { get => _currentDate; set => SetProperty(ref _currentDate, value); }
        public string Greeting { get => _greeting; set => SetProperty(ref _greeting, value); }

        public ObservableCollection<RecentActivity> RecentActivities { get; } = new();

        public DashboardViewModel()
        {
            var hour = DateTime.Now.Hour;
            Greeting = hour < 12 ? "Good Morning" : hour < 17 ? "Good Afternoon" : "Good Evening";
        }

        public void LoadData()
        {
            CurrentDate = DateTime.Now.ToString("dddd, MMMM dd, yyyy");

            try
            {
                if (!DatabaseHelper.TestConnection())
                {
                    // Fallback demo data
                    TotalEmployees = "13";
                    ActiveDepartments = "3";
                    TotalPayroll = "₱0.00";
                    PendingPayroll = "0";
                    LoadDemoActivities();
                    return;
                }

                using var conn = DatabaseHelper.GetConnection();
                conn.Open();

                // Total employees
                using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM employees WHERE is_active = 1", conn))
                    TotalEmployees = Convert.ToInt32(cmd.ExecuteScalar()).ToString();

                // Active departments
                using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM departments WHERE is_active = 1", conn))
                    ActiveDepartments = Convert.ToInt32(cmd.ExecuteScalar()).ToString();

                // Total payroll this month
                using (var cmd = new MySqlCommand("SELECT COALESCE(SUM(net_pay), 0) FROM payroll WHERE MONTH(payroll_date) = MONTH(NOW()) AND YEAR(payroll_date) = YEAR(NOW())", conn))
                    TotalPayroll = $"₱{Convert.ToDecimal(cmd.ExecuteScalar()):N2}";

                // Pending payroll
                using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM payroll WHERE status = 'Draft'", conn))
                    PendingPayroll = Convert.ToInt32(cmd.ExecuteScalar()).ToString();

                // Recent activities
                LoadRecentActivities(conn);
            }
            catch
            {
                TotalEmployees = "13";
                ActiveDepartments = "3";
                TotalPayroll = "₱0.00";
                PendingPayroll = "0";
                LoadDemoActivities();
            }
        }

        private void LoadRecentActivities(MySqlConnection conn)
        {
            RecentActivities.Clear();
            try
            {
                using var cmd = new MySqlCommand(
                    @"SELECT CONCAT(e.first_name, ' ', e.last_name) as name, p.net_pay, p.payroll_date, p.status
                      FROM payroll p JOIN employees e ON p.employee_id = e.id
                      ORDER BY p.created_at DESC LIMIT 5", conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    RecentActivities.Add(new RecentActivity
                    {
                        Description = $"Payroll processed for {reader.GetString("name")}",
                        Amount = $"₱{reader.GetDecimal("net_pay"):N2}",
                        Date = reader.GetDateTime("payroll_date").ToString("MMM dd, yyyy"),
                        Status = reader.GetString("status")
                    });
                }
            }
            catch { LoadDemoActivities(); }
        }

        private void LoadDemoActivities()
        {
            RecentActivities.Clear();
            RecentActivities.Add(new RecentActivity { Description = "System initialized", Amount = "—", Date = DateTime.Now.ToString("MMM dd, yyyy"), Status = "Info" });
            RecentActivities.Add(new RecentActivity { Description = "13 employees loaded", Amount = "—", Date = DateTime.Now.ToString("MMM dd, yyyy"), Status = "Info" });
            RecentActivities.Add(new RecentActivity { Description = "3 departments active", Amount = "—", Date = DateTime.Now.ToString("MMM dd, yyyy"), Status = "Info" });
        }
    }

    public class RecentActivity
    {
        public string Description { get; set; } = "";
        public string Amount { get; set; } = "";
        public string Date { get; set; } = "";
        public string Status { get; set; } = "";
    }
}
