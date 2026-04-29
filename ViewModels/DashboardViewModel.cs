using System;
using System.Collections.ObjectModel;
using System.Linq;
using PayrollSystem.DataAccess;
using Microsoft.Data.Sqlite;
using PayrollSystem.Models;

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
        public ObservableCollection<MonthlyExpense> MonthlyExpenses { get; } = new();

        // Notification / Reminder
        private string _notificationMessage = "";
        private bool _isNotificationVisible;
        public string NotificationMessage { get => _notificationMessage; set => SetProperty(ref _notificationMessage, value); }
        public bool IsNotificationVisible { get => _isNotificationVisible; set => SetProperty(ref _isNotificationVisible, value); }

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
                if (!DatabaseHelper.TestConnection()) return;

                using var conn = DatabaseHelper.GetConnection();
                conn.Open();

                // Total employees
                using (var cmd = new SqliteCommand("SELECT COUNT(*) FROM employees WHERE is_active = 1", conn))
                    TotalEmployees = Convert.ToInt32(cmd.ExecuteScalar()).ToString();

                // Active departments
                using (var cmd = new SqliteCommand("SELECT COUNT(*) FROM departments WHERE is_active = 1", conn))
                    ActiveDepartments = Convert.ToInt32(cmd.ExecuteScalar()).ToString();

                // Total payroll this month
                using (var cmd = new SqliteCommand("SELECT COALESCE(SUM(net_pay), 0) FROM payroll WHERE strftime('%m', payroll_date) = @m AND strftime('%Y', payroll_date) = @y", conn))
                {
                    cmd.Parameters.AddWithValue("@m", DateTime.Now.ToString("MM"));
                    cmd.Parameters.AddWithValue("@y", DateTime.Now.ToString("yyyy"));
                    TotalPayroll = $"₱{Convert.ToDecimal(cmd.ExecuteScalar()):N2}";
                }

                // Pending payroll
                using (var cmd = new SqliteCommand("SELECT COUNT(*) FROM payroll WHERE status = 'Draft'", conn))
                    PendingPayroll = Convert.ToInt32(cmd.ExecuteScalar()).ToString();

                // Recent activities
                LoadRecentActivities(conn);
                LoadMonthlyExpenses(conn);
                CheckPaydayReminder(conn);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Dashboard Error: {ex.Message}");
            }
        }

        private void LoadMonthlyExpenses(SqliteConnection conn)
        {
            MonthlyExpenses.Clear();
            var now = DateTime.Now;
            decimal maxVal = 1;

            var monthData = new System.Collections.Generic.List<MonthlyExpense>();
            for (int i = 5; i >= 0; i--)
            {
                var targetMonth = now.AddMonths(-i);
                
                using var cmd = new SqliteCommand("SELECT COALESCE(SUM(net_pay), 0) FROM payroll WHERE strftime('%m', payroll_date) = @m AND strftime('%Y', payroll_date) = @y", conn);
                cmd.Parameters.AddWithValue("@m", targetMonth.ToString("MM"));
                cmd.Parameters.AddWithValue("@y", targetMonth.ToString("yyyy"));
                
                decimal total = Convert.ToDecimal(cmd.ExecuteScalar());

                monthData.Add(new MonthlyExpense
                {
                    MonthLabel = targetMonth.ToString("MMM"),
                    YearLabel = targetMonth.ToString("yyyy"),
                    TotalExpense = total,
                    TotalFormatted = $"₱{total:N0}"
                });

                if (total > maxVal) maxVal = total;
            }

            foreach (var m in monthData)
            {
                m.BarHeight = maxVal > 0 ? (double)(m.TotalExpense / maxVal) * 180.0 : 5;
                if (m.BarHeight < 5) m.BarHeight = 5;
                MonthlyExpenses.Add(m);
            }
        }

        private void CheckPaydayReminder(SqliteConnection conn)
        {
            var day = DateTime.Now.Day;
            if (day >= 13 && day <= 15)
            {
                NotificationMessage = "📋 Reminder: Mid-month payday is approaching! Make sure to import biometrics and process payroll.";
                IsNotificationVisible = true;
            }
            else if (day >= 28)
            {
                NotificationMessage = "📋 Reminder: End-of-month payday is approaching! Don't forget to finalize payroll processing.";
                IsNotificationVisible = true;
            }
            else
            {
                using var cmd = new SqliteCommand("SELECT COUNT(*) FROM payroll WHERE status = 'Draft'", conn);
                var pending = Convert.ToInt32(cmd.ExecuteScalar());
                if (pending > 0)
                {
                    NotificationMessage = $"⚠️ You have {pending} pending payroll record(s) awaiting approval. Go to Batch Print to approve.";
                    IsNotificationVisible = true;
                }
                else
                {
                    IsNotificationVisible = false;
                }
            }
        }

        public void DismissNotification()
        {
            IsNotificationVisible = false;
        }

        private void LoadRecentActivities(SqliteConnection conn)
        {
            RecentActivities.Clear();
            try
            {
                using var cmd = new SqliteCommand(
                    @"SELECT e.first_name || ' ' || e.last_name as name, p.net_pay, p.payroll_date, p.status
                      FROM payroll p JOIN employees e ON p.employee_id = e.id
                      ORDER BY p.created_at DESC LIMIT 5", conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    RecentActivities.Add(new RecentActivity
                    {
                        Description = $"Payroll processed for {reader.GetString(reader.GetOrdinal("name"))}",
                        Amount = $"₱{reader.GetDecimal(reader.GetOrdinal("net_pay")):N2}",
                        Date = DateTime.Parse(reader.GetString(reader.GetOrdinal("payroll_date"))).ToString("MMM dd, yyyy"),
                        Status = reader.GetString(reader.GetOrdinal("status"))
                    });
                }

                if (RecentActivities.Count == 0)
                {
                    RecentActivities.Add(new RecentActivity { Description = "System initialized", Amount = "—", Date = DateTime.Now.ToString("MMM dd, yyyy"), Status = "Info" });
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
        }
    }

    public class RecentActivity
    {
        public string Description { get; set; } = "";
        public string Amount { get; set; } = "";
        public string Date { get; set; } = "";
        public string Status { get; set; } = "";
    }

    public class MonthlyExpense
    {
        public string MonthLabel { get; set; } = "";
        public string YearLabel { get; set; } = "";
        public decimal TotalExpense { get; set; }
        public string TotalFormatted { get; set; } = "₱0";
        public double BarHeight { get; set; } = 5;
    }
}
