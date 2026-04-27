using System;
using System.Collections.ObjectModel;
using System.Linq;
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
                if (!DatabaseHelper.TestConnection())
                {
                    // Fallback demo data
                    DemoDatabase.Initialize();
                    TotalEmployees = DemoDatabase.Employees.Count(e => e.IsActive).ToString();
                    ActiveDepartments = DemoDatabase.Departments.Count.ToString();
                    
                    var totalMonthly = DemoDatabase.PayrollHistory
                        .Where(p => p.PayrollDate.Month == DateTime.Now.Month && p.PayrollDate.Year == DateTime.Now.Year)
                        .Sum(p => p.NetPayRaw);
                    TotalPayroll = $"₱{totalMonthly:N2}";

                    PendingPayroll = DemoDatabase.PayrollHistory.Count(p => p.Status == "Pending").ToString();
                    LoadDemoActivities();
                    LoadMonthlyExpenses();
                    CheckPaydayReminder();
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
                using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM payroll WHERE status = 'Pending'", conn))
                    PendingPayroll = Convert.ToInt32(cmd.ExecuteScalar()).ToString();

                // Recent activities
                LoadRecentActivities(conn);
                LoadMonthlyExpenses();
                CheckPaydayReminder();
            }
            catch
            {
                TotalEmployees = DemoDatabase.Employees.Count(e => e.IsActive).ToString();
                ActiveDepartments = DemoDatabase.Departments.Count.ToString();
                
                var totalMonthly = DemoDatabase.PayrollHistory
                    .Where(p => p.PayrollDate.Month == DateTime.Now.Month && p.PayrollDate.Year == DateTime.Now.Year)
                    .Sum(p => p.NetPayRaw);
                TotalPayroll = $"₱{totalMonthly:N2}";

                PendingPayroll = DemoDatabase.PayrollHistory.Count(p => p.Status == "Pending").ToString();
                LoadDemoActivities();
                LoadMonthlyExpenses();
                CheckPaydayReminder();
            }
        }

        private void LoadMonthlyExpenses()
        {
            MonthlyExpenses.Clear();
            DemoDatabase.Initialize();

            var now = DateTime.Now;
            decimal maxVal = 1; // avoid div by zero

            // Compute totals for each of the last 6 months
            var monthData = new System.Collections.Generic.List<MonthlyExpense>();
            for (int i = 5; i >= 0; i--)
            {
                var targetMonth = now.AddMonths(-i);
                var total = DemoDatabase.PayrollHistory
                    .Where(p => p.PayrollDate.Month == targetMonth.Month && p.PayrollDate.Year == targetMonth.Year)
                    .Sum(p => p.NetPayRaw);

                monthData.Add(new MonthlyExpense
                {
                    MonthLabel = targetMonth.ToString("MMM"),
                    YearLabel = targetMonth.ToString("yyyy"),
                    TotalExpense = total,
                    TotalFormatted = $"₱{total:N0}"
                });

                if (total > maxVal) maxVal = total;
            }

            // Normalize bar heights (max = 180px)
            foreach (var m in monthData)
            {
                m.BarHeight = maxVal > 0 ? (double)(m.TotalExpense / maxVal) * 180.0 : 5;
                if (m.BarHeight < 5) m.BarHeight = 5; // minimum visible bar
                MonthlyExpenses.Add(m);
            }
        }

        private void CheckPaydayReminder()
        {
            var day = DateTime.Now.Day;
            // Near mid-month payday (13th-15th) or end-of-month payday (28th-31st)
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
                // Check if there are pending payrolls
                var pending = DemoDatabase.PayrollHistory.Count(p => p.Status == "Pending");
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
            var recent = DemoDatabase.PayrollHistory.OrderByDescending(p => p.PayrollDate).Take(10);
            foreach (var r in recent)
            {
                RecentActivities.Add(new RecentActivity 
                { 
                    Description = $"Payroll processed for {r.EmployeeName}", 
                    Amount = $"₱{r.NetPayRaw:N2}", 
                    Date = r.PayrollDateFormatted, 
                    Status = r.Status 
                });
            }

            if (RecentActivities.Count == 0)
            {
                RecentActivities.Add(new RecentActivity { Description = "System initialized", Amount = "—", Date = DateTime.Now.ToString("MMM dd, yyyy"), Status = "Info" });
            }
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
