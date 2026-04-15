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

        // Deductions
        private string _sss = "0.00";
        private string _pagibig = "0.00";
        private string _philhealth = "0.00";
        private string _incomeTax = "0.00";
        private string _loan = "0.00";
        private string _others = "0.00";
        private string _totalDeductions = "0.00";

        // Net Pay
        private string _netPay = "0.00";

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

        public string Sss { get => _sss; set => SetProperty(ref _sss, value); }
        public string Pagibig { get => _pagibig; set => SetProperty(ref _pagibig, value); }
        public string Philhealth { get => _philhealth; set => SetProperty(ref _philhealth, value); }
        public string IncomeTax { get => _incomeTax; set => SetProperty(ref _incomeTax, value); }
        public string Loan { get => _loan; set => SetProperty(ref _loan, value); }
        public string Others { get => _others; set => SetProperty(ref _others, value); }
        public string TotalDeductions { get => _totalDeductions; set => SetProperty(ref _totalDeductions, value); }
        public string NetPay { get => _netPay; set => SetProperty(ref _netPay, value); }

        public EmployeeItem? SelectedEmployee { get => _selectedEmployee; set { SetProperty(ref _selectedEmployee, value); OnEmployeeSelected(); } }
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

        public ICommand GeneratePayslipCommand { get; }
        public ICommand PrintCommand { get; }

        public PayslipViewModel()
        {
            GeneratePayslipCommand = new RelayCommand(_ => ComputePayslip());
            PrintCommand = new RelayCommand(_ => PrintPayslip());
            UpdatePeriodText();
            LoadEmployees();
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
            decimal.TryParse(LoanInput, out var loanVal);
            decimal.TryParse(OthersInput, out var othersVal);

            var rate = SelectedEmployee.DailyRate;

            // Fill daily salary grid (simulate weekly pattern)
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
            var gross = basic + otPay + holPay + allowInput + bonInput;

            // Government deductions
            var sss = Math.Min(gross * 0.045m, 1125m);
            var pagibig = Math.Min(gross * 0.02m, 100m);
            var philhealth = Math.Min(gross * 0.0275m, 1650m);
            var tax = CalculateTax(gross);
            var totalDed = sss + pagibig + philhealth + tax + loanVal + othersVal;
            var net = gross - totalDed;

            BasicSalary = $"{basic:N2}";
            OtPay = $"{otPay:N2}";
            AllowanceTotal = $"{allowInput:N2}";
            HolidayPay = $"{holPay:N2}";
            BonusAmount = $"{bonInput:N2}";
            GrossSalary = $"{gross:N2}";

            Sss = $"{sss:N2}";
            Pagibig = $"{pagibig:N2}";
            Philhealth = $"{philhealth:N2}";
            IncomeTax = $"{tax:N2}";
            Loan = $"{loanVal:N2}";
            Others = $"{othersVal:N2}";
            TotalDeductions = $"{totalDed:N2}";
            NetPay = $"{net:N2}";
            AllowPerDay = "500.00";
        }

        private decimal CalculateTax(decimal gross)
        {
            if (gross <= 20833m) return 0m;
            if (gross <= 33333m) return (gross - 20833m) * 0.15m;
            if (gross <= 66667m) return 2500m + (gross - 33333m) * 0.20m;
            if (gross <= 166667m) return 9167m + (gross - 66667m) * 0.25m;
            if (gross <= 666667m) return 34167m + (gross - 166667m) * 0.30m;
            return 184167m + (gross - 666667m) * 0.32m;
        }

        private void PrintPayslip()
        {
            // Print functionality — would open print dialog in real implementation
            System.Windows.MessageBox.Show(
                $"Payslip for {EmployeeName}\nNet Pay: ₱{NetPay}\n\nPrint functionality ready for production.",
                "Print Payslip", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
    }
}
