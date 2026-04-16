using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using PayrollSystem.Helpers;
using PayrollSystem.DataAccess;
using MySql.Data.MySqlClient;

namespace PayrollSystem.ViewModels
{
    public class PayrollViewModel : BaseViewModel
    {
        private EmployeeItem? _selectedEmployee;
        private string _employeeSearch = "";
        private string _workDays = "22";
        private string _overtimeHours = "0";
        private string _holidayHours = "0";
        private string _allowance = "0";
        private string _bonus = "0";
        private string _basicSalary = "₱0.00";
        private string _overtimePay = "₱0.00";
        private string _holidayPay = "₱0.00";
        private string _grossSalary = "₱0.00";
        private string _sssDeduction = "0";
        private string _pagibigDeduction = "0";
        private string _philhealthDeduction = "0";
        private string _totalDeductions = "₱0.00";
        private string _netPay = "₱0.00";
        private DateTime _periodStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        private DateTime _periodEnd = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month));
        private string _statusMessage = "";

        public EmployeeItem? SelectedEmployee { get => _selectedEmployee; set { SetProperty(ref _selectedEmployee, value); ComputeSalary(); } }
        public string EmployeeSearch { get => _employeeSearch; set { SetProperty(ref _employeeSearch, value); FilterEmployees(); } }
        public string WorkDays { get => _workDays; set { SetProperty(ref _workDays, value); ComputeSalary(); } }
        public string OvertimeHours { get => _overtimeHours; set { SetProperty(ref _overtimeHours, value); ComputeSalary(); } }
        public string HolidayHours { get => _holidayHours; set { SetProperty(ref _holidayHours, value); ComputeSalary(); } }
        public string Allowance { get => _allowance; set { SetProperty(ref _allowance, value); ComputeSalary(); } }
        public string Bonus { get => _bonus; set { SetProperty(ref _bonus, value); ComputeSalary(); } }
        public string BasicSalary { get => _basicSalary; set => SetProperty(ref _basicSalary, value); }
        public string OvertimePay { get => _overtimePay; set => SetProperty(ref _overtimePay, value); }
        public string HolidayPay { get => _holidayPay; set => SetProperty(ref _holidayPay, value); }
        public string GrossSalary { get => _grossSalary; set => SetProperty(ref _grossSalary, value); }
        // Editable deductions
        public string SssDeduction { get => _sssDeduction; set { SetProperty(ref _sssDeduction, value); ComputeDeductions(); } }
        public string PagibigDeduction { get => _pagibigDeduction; set { SetProperty(ref _pagibigDeduction, value); ComputeDeductions(); } }
        public string PhilhealthDeduction { get => _philhealthDeduction; set { SetProperty(ref _philhealthDeduction, value); ComputeDeductions(); } }
        public string TotalDeductions { get => _totalDeductions; set => SetProperty(ref _totalDeductions, value); }
        public string NetPay { get => _netPay; set => SetProperty(ref _netPay, value); }
        public DateTime PeriodStart { get => _periodStart; set => SetProperty(ref _periodStart, value); }
        public DateTime PeriodEnd { get => _periodEnd; set => SetProperty(ref _periodEnd, value); }
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        public ObservableCollection<EmployeeItem> Employees { get; } = new();
        public ObservableCollection<EmployeeItem> FilteredEmployees { get; } = new();

        public ICommand ProcessPayrollCommand { get; }

        private decimal _cachedGross = 0;

        public PayrollViewModel()
        {
            ProcessPayrollCommand = new RelayCommand(_ => ProcessPayroll());
        }

        public void LoadData()
        {
            try
            {
                if (!DatabaseHelper.TestConnection())
                {
                    if (DemoDatabase.Employees == null) DemoDatabase.Initialize();
                    
                    Employees.Clear();
                    foreach (var emp in DemoDatabase.Employees) Employees.Add(emp);
                    
                    FilterEmployees();
                    return;
                }

                Employees.Clear();
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                using var cmd = new MySqlCommand("SELECT id, emp_number, first_name, last_name, position, daily_rate FROM employees WHERE is_active = 1 ORDER BY last_name", conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Employees.Add(new EmployeeItem
                    {
                        Id = reader.GetInt32("id"),
                        EmpNumber = reader.GetString("emp_number"),
                        FirstName = reader.GetString("first_name"),
                        LastName = reader.GetString("last_name"),
                        FullName = $"{reader.GetString("first_name")} {reader.GetString("last_name")}",
                        Position = reader.GetString("position"),
                        DailyRate = reader.GetDecimal("daily_rate"),
                        DailyRateFormatted = $"₱{reader.GetDecimal("daily_rate"):N2}"
                    });
                }
            }
            catch 
            { 
                if (DemoDatabase.Employees == null) DemoDatabase.Initialize();
                Employees.Clear();
                foreach (var emp in DemoDatabase.Employees) Employees.Add(emp);
            }

            FilterEmployees();
        }

        private void FilterEmployees()
        {
            FilteredEmployees.Clear();
            var filtered = string.IsNullOrWhiteSpace(EmployeeSearch)
                ? Employees
                : new ObservableCollection<EmployeeItem>(Employees.Where(e =>
                    e.FullName.Contains(EmployeeSearch, StringComparison.OrdinalIgnoreCase) ||
                    e.EmpNumber.Contains(EmployeeSearch, StringComparison.OrdinalIgnoreCase) ||
                    e.Position.Contains(EmployeeSearch, StringComparison.OrdinalIgnoreCase)));
            foreach (var emp in filtered) FilteredEmployees.Add(emp);

            // Auto-select first match for instant feedback
            if (!string.IsNullOrWhiteSpace(EmployeeSearch) && FilteredEmployees.Count > 0)
                SelectedEmployee = FilteredEmployees[0];
        }

        private void ComputeSalary()
        {
            if (SelectedEmployee == null) return;

            int.TryParse(WorkDays, out var days);
            decimal.TryParse(OvertimeHours, out var ot);
            decimal.TryParse(HolidayHours, out var hol);
            decimal.TryParse(Allowance, out var allow);
            decimal.TryParse(Bonus, out var bon);

            var rate = SelectedEmployee.DailyRate;
            var basic = rate * days;
            var otPay = ot * (rate / 8) * 1.25m;
            var holPay = hol * (rate / 8) * 2m;
            _cachedGross = basic + otPay + holPay + allow + bon;

            BasicSalary = $"₱{basic:N2}";
            OvertimePay = $"₱{otPay:N2}";
            HolidayPay = $"₱{holPay:N2}";
            GrossSalary = $"₱{_cachedGross:N2}";

            // Auto-compute default deductions based on gross
            var sss = Math.Min(_cachedGross * 0.045m, 1125m);
            var pagibig = Math.Min(_cachedGross * 0.02m, 100m);
            var philhealth = Math.Min(_cachedGross * 0.0275m, 1650m);

            // Update editable deduction fields (user can override)
            _sssDeduction = $"{sss:N2}";
            _pagibigDeduction = $"{pagibig:N2}";
            _philhealthDeduction = $"{philhealth:N2}";
            OnPropertyChanged(nameof(SssDeduction));
            OnPropertyChanged(nameof(PagibigDeduction));
            OnPropertyChanged(nameof(PhilhealthDeduction));

            ComputeDeductions();
        }

        private void ComputeDeductions()
        {
            decimal.TryParse(SssDeduction, out var sss);
            decimal.TryParse(PagibigDeduction, out var pagibig);
            decimal.TryParse(PhilhealthDeduction, out var phil);

            var totalDed = sss + pagibig + phil;
            var net = _cachedGross - totalDed;

            TotalDeductions = $"₱{totalDed:N2}";
            NetPay = $"₱{net:N2}";
        }

        private void ProcessPayroll()
        {
            if (SelectedEmployee == null) { StatusMessage = "Please select an employee."; return; }

            try
            {
                int.TryParse(WorkDays, out var days);
                decimal.TryParse(OvertimeHours, out var ot);
                decimal.TryParse(HolidayHours, out var hol);
                decimal.TryParse(Allowance, out var allow);
                decimal.TryParse(Bonus, out var bon);
                decimal.TryParse(SssDeduction, out var sss);
                decimal.TryParse(PagibigDeduction, out var pagibig);
                decimal.TryParse(PhilhealthDeduction, out var phil);

                var rate = SelectedEmployee.DailyRate;
                var basic = rate * days;
                var otPay = ot * (rate / 8) * 1.25m;
                var holPay = hol * (rate / 8) * 2m;
                var gross = basic + otPay + holPay + allow + bon;
                var totalDed = sss + pagibig + phil;
                var net = gross - totalDed;

                if (DatabaseHelper.TestConnection())
                {
                    using var conn = DatabaseHelper.GetConnection();
                    conn.Open();

                    using var cmd = new MySqlCommand(@"INSERT INTO payroll
                        (employee_id, payroll_date, period_start, period_end, work_days, overtime_hours, holiday_hours,
                         basic_salary, overtime_pay, holiday_pay, allowance, bonus, gross_salary, total_deductions, net_pay, status)
                        VALUES (@eid, NOW(), @ps, @pe, @wd, @ot, @hol, @basic, @otpay, @holpay, @allow, @bon, @gross, @ded, @net, 'Processed')", conn);

                    cmd.Parameters.AddWithValue("@eid", SelectedEmployee.Id);
                    cmd.Parameters.AddWithValue("@ps", PeriodStart);
                    cmd.Parameters.AddWithValue("@pe", PeriodEnd);
                    cmd.Parameters.AddWithValue("@wd", days);
                    cmd.Parameters.AddWithValue("@ot", ot);
                    cmd.Parameters.AddWithValue("@hol", hol);
                    cmd.Parameters.AddWithValue("@basic", basic);
                    cmd.Parameters.AddWithValue("@otpay", otPay);
                    cmd.Parameters.AddWithValue("@holpay", holPay);
                    cmd.Parameters.AddWithValue("@allow", allow);
                    cmd.Parameters.AddWithValue("@bon", bon);
                    cmd.Parameters.AddWithValue("@gross", gross);
                    cmd.Parameters.AddWithValue("@ded", totalDed);
                    cmd.Parameters.AddWithValue("@net", net);
                    cmd.ExecuteNonQuery();

                    var payrollId = cmd.LastInsertedId;
                    SaveDeduction(conn, payrollId, "SSS", "SSS", sss);
                    SaveDeduction(conn, payrollId, "PAG-IBIG", "PAGIBIG", pagibig);
                    SaveDeduction(conn, payrollId, "PhilHealth", "PhilHealth", phil);
                }

                StatusMessage = $"✓ Payroll processed for {SelectedEmployee.FullName} — Net Pay: ₱{net:N2}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        private void SaveDeduction(MySqlConnection conn, long payrollId, string name, string type, decimal amount)
        {
            using var cmd = new MySqlCommand("INSERT INTO deductions (payroll_id, name, type, amount) VALUES (@pid, @n, @t, @a)", conn);
            cmd.Parameters.AddWithValue("@pid", payrollId);
            cmd.Parameters.AddWithValue("@n", name);
            cmd.Parameters.AddWithValue("@t", type);
            cmd.Parameters.AddWithValue("@a", amount);
            cmd.ExecuteNonQuery();
        }
    }
}
