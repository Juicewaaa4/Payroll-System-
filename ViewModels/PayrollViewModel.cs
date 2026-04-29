using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using PayrollSystem.Helpers;
using PayrollSystem.DataAccess;
using Microsoft.Data.Sqlite;
using PayrollSystem.Models;

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
        private string _loanDeduction = "0";
        private string _lateDeduction = "0";
        private string _undertimeDeduction = "0";
        private string _cashAdvance = "0";
        private string _othersDeduction = "0";
        private string _totalDeductions = "₱0.00";
        private string _netPay = "₱0.00";
        private string _othersDeductionName = "Others";
        private DateTime _periodStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        private DateTime _periodEnd = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month));
        private string _statusMessage = "";

        public EmployeeItem? SelectedEmployee { get => _selectedEmployee; set { SetProperty(ref _selectedEmployee, value); PopulateFromBiometrics(); ComputeSalary(); } }
        public string EmployeeSearch { get => _employeeSearch; set { SetProperty(ref _employeeSearch, value); FilterEmployees(); } }
        public string WorkDays { get => _workDays; set { if(SetProperty(ref _workDays, value)) { ComputeSalary(); UpdatePeriodFromDays(); } } }
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
        public string LoanDeduction { get => _loanDeduction; set { SetProperty(ref _loanDeduction, value); ComputeDeductions(); } }
        public string LateDeduction { get => _lateDeduction; set { SetProperty(ref _lateDeduction, value); ComputeDeductions(); } }
        public string UndertimeDeduction { get => _undertimeDeduction; set { SetProperty(ref _undertimeDeduction, value); ComputeDeductions(); } }
        public string CashAdvance { get => _cashAdvance; set { SetProperty(ref _cashAdvance, value); ComputeDeductions(); } }
        public string OthersDeduction { get => _othersDeduction; set { SetProperty(ref _othersDeduction, value); ComputeDeductions(); } }
        public string OthersDeductionName { get => _othersDeductionName; set => SetProperty(ref _othersDeductionName, value); }
        public string TotalDeductions { get => _totalDeductions; set => SetProperty(ref _totalDeductions, value); }
        public string NetPay { get => _netPay; set => SetProperty(ref _netPay, value); }
        public DateTime PeriodStart { get => _periodStart; set { if(SetProperty(ref _periodStart, value)) { UpdateDaysFromPeriod(); PopulateFromBiometrics(); } } }
        public DateTime PeriodEnd { get => _periodEnd; set { if(SetProperty(ref _periodEnd, value)) { UpdateDaysFromPeriod(); PopulateFromBiometrics(); } } }
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        private bool _isUpdatingDates = false;

        private void UpdatePeriodFromDays()
        {
            if (_isUpdatingDates) return;

            if (int.TryParse(WorkDays, out var days) && days > 0)
            {
                _isUpdatingDates = true;
                PeriodEnd = PeriodStart.AddDays(days - 1);
                _isUpdatingDates = false;
            }
        }

        private void UpdateDaysFromPeriod()
        {
            if (_isUpdatingDates) return;

            if (PeriodEnd >= PeriodStart)
            {
                _isUpdatingDates = true;
                var diff = (PeriodEnd - PeriodStart).Days + 1;
                WorkDays = diff.ToString();
                _isUpdatingDates = false;
            }
        }

        public ObservableCollection<EmployeeItem> Employees { get; } = new();
        public ObservableCollection<EmployeeItem> FilteredEmployees { get; } = new();

        public ICommand ProcessPayrollCommand { get; }
        
        private readonly System.Collections.Generic.Dictionary<string, Utilities.AttendanceSummary> _importedAttendance = new();

        public event Action<EmployeeItem>? PayrollProcessed;

        private decimal _cachedGross = 0;

        public PayrollViewModel()
        {
            ProcessPayrollCommand = new RelayCommand(_ => ProcessPayroll());
        }

        public void LoadData()
        {
            try
            {
                if (!DatabaseHelper.TestConnection()) return;

                Employees.Clear();
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                using var cmd = new SqliteCommand("SELECT id, emp_number, first_name, last_name, position, daily_rate FROM employees WHERE is_active = 1 ORDER BY last_name", conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Employees.Add(new EmployeeItem
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("id")),
                        EmpNumber = reader.GetString(reader.GetOrdinal("emp_number")),
                        FirstName = reader.GetString(reader.GetOrdinal("first_name")),
                        LastName = reader.GetString(reader.GetOrdinal("last_name")),
                        FullName = $"{reader.GetString(reader.GetOrdinal("first_name"))} {reader.GetString(reader.GetOrdinal("last_name"))}",
                        Position = reader.GetString(reader.GetOrdinal("position")),
                        DailyRate = reader.GetDecimal(reader.GetOrdinal("daily_rate")),
                        DailyRateFormatted = $"₱{reader.GetDecimal(reader.GetOrdinal("daily_rate")):N2}"
                    });
                }
            }
            catch 
            { 
            }

            FilterEmployees();
            LoadLatestBiometricsData();
        }

        private void LoadLatestBiometricsData()
        {
            try
            {
                if (!DatabaseHelper.TestConnection()) return;

                using var conn = DatabaseHelper.GetConnection();
                conn.Open();

                using var cmd = new SqliteCommand("SELECT file_path, file_name FROM biometrics_imports ORDER BY imported_at DESC LIMIT 1", conn);
                using var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    var filePath = reader.GetString(reader.GetOrdinal("file_path"));
                    var fileName = reader.GetString(reader.GetOrdinal("file_name"));

                    if (System.IO.File.Exists(filePath))
                    {
                        StatusMessage = "Loading latest biometrics data...";
                        var result = Utilities.BiometricsParser.ParseExcelFile(filePath);

                        _importedAttendance.Clear();
                        foreach (var summary in result.Records)
                        {
                            if (!string.IsNullOrEmpty(summary.EmpNumber))
                                _importedAttendance[summary.EmpNumber] = summary;
                        }

                        if (result.Records.Count > 0)
                        {
                            StatusMessage = $"✓ Auto-loaded {result.Records.Count} biometric records from {fileName}.";
                            if (result.StartDate.HasValue && result.EndDate.HasValue)
                            {
                                _isUpdatingDates = true; 
                                PeriodStart = result.StartDate.Value;
                                PeriodEnd = result.EndDate.Value;
                                _isUpdatingDates = false;
                            }
                        }
                    }
                    else
                    {
                        StatusMessage = "No biometrics data loaded. Please import an Excel file via the Biometrics section.";
                    }
                }
                else
                {
                    StatusMessage = "No biometrics data loaded. Please import an Excel file via the Biometrics section.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Failed to load background biometrics data: " + ex.Message;
            }
        }

        private void PopulateFromBiometrics()
        {
            if (_selectedEmployee == null) return;

            Utilities.AttendanceSummary? summary = null;

            if (!_importedAttendance.TryGetValue(_selectedEmployee.EmpNumber, out summary) &&
                !_importedAttendance.TryGetValue(_selectedEmployee.Id.ToString(), out summary))
            {
                // Fallback: Fuzzy Name Matching
                string empFirstName = _selectedEmployee.FirstName.ToLower().Replace(" ", "");
                string empLastName = _selectedEmployee.LastName.ToLower().Replace(" ", "");
                string empFullName = _selectedEmployee.FullName.ToLower().Replace(" ", "");
                
                foreach (var kvp in _importedAttendance)
                {
                    string importedName = kvp.Value.Name.ToLower().Replace(" ", "");
                    if (importedName.Length > 2 && (importedName.Contains(empFirstName) || importedName.Contains(empLastName) || empFullName.Contains(importedName)))
                    {
                        summary = kvp.Value;
                        break;
                    }
                }
            }

            if (summary != null)
            {
                int defaultDays = 0;
                int totalLate = 0;
                int totalUnder = 0;
                int totalOT = 0;

                if (summary.DailyRecords != null && summary.DailyRecords.Count > 0)
                {
                    // Snap the calendar to match the employee's actual attendance range
                    int minDay = summary.DailyRecords.Min(r => r.DayNumber);
                    int maxDay = summary.DailyRecords.Max(r => r.DayNumber);

                    try 
                    {
                        DateTime newStart = new DateTime(PeriodStart.Year, PeriodStart.Month, minDay);
                        DateTime newEnd = new DateTime(PeriodEnd.Year, PeriodEnd.Month, maxDay);
                        
                        _isUpdatingDates = true; 
                        PeriodStart = newStart; 
                        PeriodEnd = newEnd; 
                        _isUpdatingDates = false;
                    } 
                    catch { }

                    // Dynamically compute based on the SNAP dates
                    for (DateTime d = PeriodStart.Date; d <= PeriodEnd.Date; d = d.AddDays(1))
                    {
                        var rec = summary.DailyRecords.FirstOrDefault(r => r.DayNumber == d.Day);
                        if (rec != null && rec.IsPresent)
                        {
                            defaultDays++;
                            totalLate += rec.LateMinutes;
                            totalUnder += rec.UndertimeMinutes;
                            totalOT += rec.OvertimeHours;
                        }
                    }
                }
                else
                {
                    // Fallback globally if daily isn't available
                    defaultDays = summary.PresentDays;
                    totalLate = summary.LateMinutes;
                    totalUnder = summary.UndertimeMinutes;
                    totalOT = summary.OvertimeHours;
                }

                if (defaultDays > 0) 
                {
                    _isUpdatingDates = true;
                    WorkDays = defaultDays.ToString();
                    _isUpdatingDates = false;
                }
                else if (string.IsNullOrWhiteSpace(WorkDays))
                {
                    _isUpdatingDates = true;
                    WorkDays = ((PeriodEnd - PeriodStart).Days + 1).ToString();
                    _isUpdatingDates = false;
                }
                
                OvertimeHours = totalOT.ToString();

                // Compute exact PHP money
                var rateHourly = _selectedEmployee.DailyRate / 8m;
                var ratePerMinute = rateHourly / 60m;

                var lateMoney = Math.Round(ratePerMinute * totalLate, 2);
                var undertimeMoney = Math.Round(ratePerMinute * totalUnder, 2);

                _lateDeduction = lateMoney.ToString("0.00");
                _undertimeDeduction = undertimeMoney.ToString("0.00");

                OnPropertyChanged(nameof(LateDeduction));
                OnPropertyChanged(nameof(UndertimeDeduction));
            }
            else
            {
                // Reset to defaults if no record exists
                _lateDeduction = "0";
                _undertimeDeduction = "0";
                OnPropertyChanged(nameof(LateDeduction));
                OnPropertyChanged(nameof(UndertimeDeduction));
                
                if (string.IsNullOrWhiteSpace(WorkDays))
                {
                    _isUpdatingDates = true;
                    WorkDays = ((PeriodEnd - PeriodStart).Days + 1).ToString();
                    _isUpdatingDates = false;
                }
            }
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
            decimal.TryParse(LoanDeduction, out var loan);
            decimal.TryParse(LateDeduction, out var late);
            decimal.TryParse(UndertimeDeduction, out var under);
            decimal.TryParse(CashAdvance, out var cashAdv);
            decimal.TryParse(OthersDeduction, out var others);

            var totalDed = sss + pagibig + phil + loan + late + under + cashAdv + others;
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
                decimal.TryParse(LoanDeduction, out var loan);
                decimal.TryParse(LateDeduction, out var late);
                decimal.TryParse(UndertimeDeduction, out var under);
                decimal.TryParse(CashAdvance, out var cashAdv);
                decimal.TryParse(OthersDeduction, out var others);

                var rate = SelectedEmployee.DailyRate;
                var basic = rate * days;
                var otPay = ot * (rate / 8) * 1.25m;
                var holPay = hol * (rate / 8) * 2m;
                var gross = basic + otPay + holPay + allow + bon;
                var totalDed = sss + pagibig + phil + loan + late + under + cashAdv + others;
                var net = gross - totalDed;

                if (DatabaseHelper.TestConnection())
                {
                    using var conn = DatabaseHelper.GetConnection();
                    conn.Open();

                    using var cmd = new SqliteCommand(@"INSERT INTO payroll
                        (employee_id, payroll_date, period_start, period_end, work_days, overtime_hours, holiday_hours,
                         basic_salary, overtime_pay, holiday_pay, allowance, bonus, gross_salary, total_deductions, net_pay, status)
                        VALUES (@eid, datetime('now', 'localtime'), @ps, @pe, @wd, @ot, @hol, @basic, @otpay, @holpay, @allow, @bon, @gross, @ded, @net, 'Pending')", conn);

                    cmd.Parameters.AddWithValue("@eid", SelectedEmployee.Id);
                    cmd.Parameters.AddWithValue("@ps", PeriodStart.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@pe", PeriodEnd.ToString("yyyy-MM-dd"));
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

                    // SQLite uses last_insert_rowid() instead of MySQL's LastInsertedId
                    using var idCmd = new SqliteCommand("SELECT last_insert_rowid()", conn);
                    var payrollId = Convert.ToInt64(idCmd.ExecuteScalar());
                    SaveDeduction(conn, payrollId, "SSS", "SSS", sss);
                    SaveDeduction(conn, payrollId, "PAG-IBIG", "PAGIBIG", pagibig);
                    SaveDeduction(conn, payrollId, "PhilHealth", "PhilHealth", phil);
                    if(loan > 0) SaveDeduction(conn, payrollId, "Loan", "Loan", loan);
                    if(late > 0) SaveDeduction(conn, payrollId, "Late", "Other", late);
                    if(under > 0) SaveDeduction(conn, payrollId, "Undertime", "Other", under);
                    if(cashAdv > 0) SaveDeduction(conn, payrollId, "Cash Advance", "Other", cashAdv);
                    if(others > 0) SaveDeduction(conn, payrollId, string.IsNullOrWhiteSpace(OthersDeductionName) ? "Others" : OthersDeductionName, "Other", others);
                    
                    StatusMessage = $"✓ Payroll processed for {SelectedEmployee.FullName} — Net Pay: ₱{net:N2} (Pending Approval)";
                    ShowToast("Payroll Processed Successfully!");
                    
                    // Signal navigation to Payslip section
                    PayrollProcessed?.Invoke(SelectedEmployee);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        private void SaveDeduction(SqliteConnection conn, long payrollId, string name, string type, decimal amount)
        {
            using var cmd = new SqliteCommand("INSERT INTO deductions (payroll_id, name, type, amount) VALUES (@pid, @n, @t, @a)", conn);
            cmd.Parameters.AddWithValue("@pid", payrollId);
            cmd.Parameters.AddWithValue("@n", name);
            cmd.Parameters.AddWithValue("@t", type);
            cmd.Parameters.AddWithValue("@a", amount);
            cmd.ExecuteNonQuery();
        }
    }
}
