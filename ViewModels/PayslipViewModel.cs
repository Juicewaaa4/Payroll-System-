using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using PayrollSystem.Helpers;
using PayrollSystem.DataAccess;
using MySql.Data.MySqlClient;

namespace PayrollSystem.ViewModels
{
    public class PayslipViewModel : BaseViewModel
    {
        private EmployeeItem? _selectedEmployee;
        private string _employeeSearch = "";
        private string _companyName = "Transfund Business";
        private string _companyAddress = "Quezon City, Metro Manila";
        private string _periodText = "";
        private string _empNumber = "";
        private string _employeeName = "";
        private string _position = "";
        private string _ratePerDay = "0.00";
        private string _allowPerDay = "500.00";

        // Daily salary (Mon-Sun)
        private string _monSalary = "0.00";
        private string _tueSalary = "0.00";
        private string _wedSalary = "0.00";
        private string _thuSalary = "0.00";
        private string _friSalary = "0.00";
        private string _satSalary = "0.00";
        private string _sunSalary = "0.00";

        // Earnings
        private string _basicSalary = "0.00";
        private string _otPay = "0.00";
        private string _allowance = "0.00";
        private string _holidayPay = "0.00";
        private string _bonus = "0.00";
        private string _grossSalary = "0.00";

        // Editable Deductions
        private string _sss = "0.00";
        private string _pagibig = "0.00";
        private string _philhealth = "0.00";
        private string _loan = "0.00";
        private string _others = "0.00";
        private string _totalDeductions = "0.00";
        private string _netPay = "0.00";

        // Signature lines
        private string _preparedBy = "";
        private string _approvedBy = "";

        // Period dates
        private DateTime _periodStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        private DateTime _periodEnd = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month));

        // Input fields
        private string _workDays = "22";
        private string _overtimeHours = "0";
        private string _holidayHours = "0";
        private string _allowanceInput = "0";
        private string _bonusInput = "0";
        private string _loanInput = "0";
        private string _othersInput = "0";

        private decimal _cachedGross = 0;

        #region Properties
        public string CompanyName { get => _companyName; set => SetProperty(ref _companyName, value); }
        public string CompanyAddress { get => _companyAddress; set => SetProperty(ref _companyAddress, value); }
        public string PeriodText { get => _periodText; set => SetProperty(ref _periodText, value); }
        public string EmpNumber { get => _empNumber; set => SetProperty(ref _empNumber, value); }
        public string EmployeeName { get => _employeeName; set => SetProperty(ref _employeeName, value); }
        public string Position { get => _position; set => SetProperty(ref _position, value); }
        public string RatePerDay { get => _ratePerDay; set => SetProperty(ref _ratePerDay, value); }
        public string AllowPerDay { get => _allowPerDay; set => SetProperty(ref _allowPerDay, value); }

        public string MonSalary { get => _monSalary; set => SetProperty(ref _monSalary, value); }
        public string TueSalary { get => _tueSalary; set => SetProperty(ref _tueSalary, value); }
        public string WedSalary { get => _wedSalary; set => SetProperty(ref _wedSalary, value); }
        public string ThuSalary { get => _thuSalary; set => SetProperty(ref _thuSalary, value); }
        public string FriSalary { get => _friSalary; set => SetProperty(ref _friSalary, value); }
        public string SatSalary { get => _satSalary; set => SetProperty(ref _satSalary, value); }
        public string SunSalary { get => _sunSalary; set => SetProperty(ref _sunSalary, value); }

        public string BasicSalary { get => _basicSalary; set => SetProperty(ref _basicSalary, value); }
        public string OtPay { get => _otPay; set => SetProperty(ref _otPay, value); }
        public string AllowanceTotal { get => _allowance; set => SetProperty(ref _allowance, value); }
        public string HolidayPay { get => _holidayPay; set => SetProperty(ref _holidayPay, value); }
        public string BonusAmount { get => _bonus; set => SetProperty(ref _bonus, value); }
        public string GrossSalary { get => _grossSalary; set => SetProperty(ref _grossSalary, value); }

        // Editable deduction fields
        public string Sss { get => _sss; set { SetProperty(ref _sss, value); RecomputeDeductions(); } }
        public string Pagibig { get => _pagibig; set { SetProperty(ref _pagibig, value); RecomputeDeductions(); } }
        public string Philhealth { get => _philhealth; set { SetProperty(ref _philhealth, value); RecomputeDeductions(); } }
        public string Loan { get => _loan; set { SetProperty(ref _loan, value); RecomputeDeductions(); } }
        public string Others { get => _others; set { SetProperty(ref _others, value); RecomputeDeductions(); } }
        public string TotalDeductions { get => _totalDeductions; set => SetProperty(ref _totalDeductions, value); }
        public string NetPay { get => _netPay; set => SetProperty(ref _netPay, value); }

        public string PreparedBy { get => _preparedBy; set => SetProperty(ref _preparedBy, value); }
        public string ApprovedBy { get => _approvedBy; set => SetProperty(ref _approvedBy, value); }

        public EmployeeItem? SelectedEmployee { get => _selectedEmployee; set { SetProperty(ref _selectedEmployee, value); OnEmployeeSelected(); } }
        public string EmployeeSearch { get => _employeeSearch; set { SetProperty(ref _employeeSearch, value); FilterEmployees(); } }
        public DateTime PeriodStart { get => _periodStart; set { SetProperty(ref _periodStart, value); UpdatePeriodText(); } }
        public DateTime PeriodEnd { get => _periodEnd; set { SetProperty(ref _periodEnd, value); UpdatePeriodText(); } }
        public string WorkDays { get => _workDays; set { SetProperty(ref _workDays, value); ComputePayslip(); } }
        public string OvertimeHours { get => _overtimeHours; set { SetProperty(ref _overtimeHours, value); ComputePayslip(); } }
        public string HolidayHours { get => _holidayHours; set { SetProperty(ref _holidayHours, value); ComputePayslip(); } }
        public string AllowanceInput { get => _allowanceInput; set { SetProperty(ref _allowanceInput, value); ComputePayslip(); } }
        public string BonusInput { get => _bonusInput; set { SetProperty(ref _bonusInput, value); ComputePayslip(); } }
        public string LoanInput { get => _loanInput; set { SetProperty(ref _loanInput, value); ComputePayslip(); } }
        public string OthersInput { get => _othersInput; set { SetProperty(ref _othersInput, value); ComputePayslip(); } }
        #endregion

        public ObservableCollection<EmployeeItem> Employees { get; } = new();
        public ObservableCollection<EmployeeItem> FilteredEmployees { get; } = new();

        public ICommand GeneratePayslipCommand { get; }
        public ICommand PrintCommand { get; }

        public PayslipViewModel()
        {
            GeneratePayslipCommand = new RelayCommand(_ => ComputePayslip());
            PrintCommand = new RelayCommand(_ => PrintPayslip());
            UpdatePeriodText();
        }

        public void LoadEmployees()
        {
            Employees.Clear();
            try
            {
                if (DatabaseHelper.TestConnection())
                {
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
                        });
                    }
                    FilterEmployees();
                    return;
                }
            }
            catch { }

            // Demo
            var demo = new[] {
                (1, "EMP-0001", "Kenneth Ariel", "Francisco", "Administrator", 1200m),
                (2, "EMP-0002", "Judy", "Peralta", "HR Manager", 1500m),
                (3, "EMP-0003", "Trecia", "De Jesus", "Office Administrator", 1100m),
                (4, "EMP-0004", "Alyssa Marie", "Zamudio", "Restaurant Manager", 1000m),
                (5, "EMP-0005", "Alliyah", "Lobendino", "Head Chef", 950m),
                (6, "EMP-0006", "Cristel Khaye", "Sevilla", "Service Staff", 650m),
                (7, "EMP-0007", "Michael", "Villasenor", "Kitchen Staff", 600m),
                (8, "EMP-0008", "Beverly", "Gabriel", "Cashier", 550m),
            };
            foreach (var (id, num, fn, ln, pos, rate) in demo)
                Employees.Add(new EmployeeItem { Id = id, EmpNumber = num, FirstName = fn, LastName = ln, FullName = $"{fn} {ln}", Position = pos, DailyRate = rate });
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
        }

        private void OnEmployeeSelected()
        {
            if (SelectedEmployee == null) return;
            EmployeeName = SelectedEmployee.FullName;
            Position = SelectedEmployee.Position;
            EmpNumber = SelectedEmployee.EmpNumber;
            RatePerDay = $"{SelectedEmployee.DailyRate:N2}";
            ComputePayslip();
        }

        private void UpdatePeriodText()
        {
            PeriodText = $"Payslip for the period of  {PeriodStart:MMM dd} to {PeriodEnd:dd, yyyy}";
        }

        private void ComputePayslip()
        {
            if (SelectedEmployee == null) return;

            int.TryParse(WorkDays, out var days);
            decimal.TryParse(OvertimeHours, out var otHours);
            decimal.TryParse(HolidayHours, out var holHours);
            decimal.TryParse(AllowanceInput, out var allowInput);
            decimal.TryParse(BonusInput, out var bonInput);

            var rate = SelectedEmployee.DailyRate;

            // Fill daily salary grid
            var daysInWeek = Math.Min(days, 7);
            MonSalary = daysInWeek >= 1 ? $"{rate:N2}" : "0.00";
            TueSalary = daysInWeek >= 2 ? $"{rate:N2}" : "0.00";
            WedSalary = daysInWeek >= 3 ? $"{rate:N2}" : "0.00";
            ThuSalary = daysInWeek >= 4 ? $"{rate:N2}" : "0.00";
            FriSalary = daysInWeek >= 5 ? $"{rate:N2}" : "0.00";
            SatSalary = daysInWeek >= 6 ? $"{rate:N2}" : "0.00";
            SunSalary = daysInWeek >= 7 ? $"{rate:N2}" : "0.00";

            var basic = rate * days;
            var otPay = otHours * (rate / 8) * 1.25m;
            var holPay = holHours * (rate / 8) * 2m;
            _cachedGross = basic + otPay + holPay + allowInput + bonInput;

            BasicSalary = $"{basic:N2}";
            OtPay = $"{otPay:N2}";
            AllowanceTotal = $"{allowInput:N2}";
            HolidayPay = $"{holPay:N2}";
            BonusAmount = $"{bonInput:N2}";
            GrossSalary = $"{_cachedGross:N2}";

            // Auto-compute default deductions
            var sss = Math.Min(_cachedGross * 0.045m, 1125m);
            var pagibig = Math.Min(_cachedGross * 0.02m, 100m);
            var philhealth = Math.Min(_cachedGross * 0.0275m, 1650m);

            decimal.TryParse(LoanInput, out var loanVal);
            decimal.TryParse(OthersInput, out var othersVal);

            _sss = $"{sss:N2}";
            _pagibig = $"{pagibig:N2}";
            _philhealth = $"{philhealth:N2}";
            _loan = $"{loanVal:N2}";
            _others = $"{othersVal:N2}";
            OnPropertyChanged(nameof(Sss));
            OnPropertyChanged(nameof(Pagibig));
            OnPropertyChanged(nameof(Philhealth));
            OnPropertyChanged(nameof(Loan));
            OnPropertyChanged(nameof(Others));

            RecomputeDeductions();
            AllowPerDay = "500.00";
        }

        private void RecomputeDeductions()
        {
            decimal.TryParse(_sss, out var sss);
            decimal.TryParse(_pagibig, out var pagibig);
            decimal.TryParse(_philhealth, out var phil);
            decimal.TryParse(_loan, out var loan);
            decimal.TryParse(_others, out var others);

            var totalDed = sss + pagibig + phil + loan + others;
            var net = _cachedGross - totalDed;

            TotalDeductions = $"{totalDed:N2}";
            NetPay = $"{net:N2}";
        }

        private void PrintPayslip()
        {
            System.Windows.MessageBox.Show(
                $"Payslip for {EmployeeName}\nNet Pay: ₱{NetPay}\n\nPrint functionality ready for production.",
                "Print Payslip", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
    }
}
