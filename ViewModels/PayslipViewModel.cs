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
        private string _companyName = "Zoey's Billiard House";
        private string _companyAddress = "Paltao, Pulilan, Bulacan";
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
        private string _late = "0.00";
        private string _undertime = "0.00";
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
        private string _lateInput = "0";
        private string _undertimeInput = "0";
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
        public string Late { get => _late; set { SetProperty(ref _late, value); RecomputeDeductions(); } }
        public string Undertime { get => _undertime; set { SetProperty(ref _undertime, value); RecomputeDeductions(); } }
        public string Others { get => _others; set { SetProperty(ref _others, value); RecomputeDeductions(); } }
        public string TotalDeductions { get => _totalDeductions; set => SetProperty(ref _totalDeductions, value); }
        public string NetPay { get => _netPay; set => SetProperty(ref _netPay, value); }

        public string PreparedBy { get => _preparedBy; set => SetProperty(ref _preparedBy, value); }
        public string ApprovedBy { get => _approvedBy; set => SetProperty(ref _approvedBy, value); }

        public EmployeeItem? SelectedEmployee { get => _selectedEmployee; set { SetProperty(ref _selectedEmployee, value); OnEmployeeSelected(); } }
        public string EmployeeSearch { get => _employeeSearch; set { SetProperty(ref _employeeSearch, value); FilterEmployees(); } }
        public DateTime PeriodStart { get => _periodStart; set { if(SetProperty(ref _periodStart, value)) UpdateDaysFromPeriod(); } }
        public DateTime PeriodEnd { get => _periodEnd; set { if(SetProperty(ref _periodEnd, value)) UpdateDaysFromPeriod(); } }
        public string WorkDays { get => _workDays; set { if(SetProperty(ref _workDays, value)) { ComputePayslip(); UpdatePeriodFromDays(); } } }
        public string OvertimeHours { get => _overtimeHours; set { SetProperty(ref _overtimeHours, value); ComputePayslip(); } }
        public string HolidayHours { get => _holidayHours; set { SetProperty(ref _holidayHours, value); ComputePayslip(); } }
        public string AllowanceInput { get => _allowanceInput; set { SetProperty(ref _allowanceInput, value); ComputePayslip(); } }
        public string BonusInput { get => _bonusInput; set { SetProperty(ref _bonusInput, value); ComputePayslip(); } }
        public string LoanInput { get => _loanInput; set { SetProperty(ref _loanInput, value); ComputePayslip(); } }
        public string LateInput { get => _lateInput; set { SetProperty(ref _lateInput, value); ComputePayslip(); } }
        public string UndertimeInput { get => _undertimeInput; set { SetProperty(ref _undertimeInput, value); ComputePayslip(); } }
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
            try
            {
                if (!DatabaseHelper.TestConnection())
                {
                    DemoDatabase.Initialize();
                    
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
                    });
                }
                FilterEmployees();
                return;
            }
            catch { }

            DemoDatabase.Initialize();
            Employees.Clear();
            foreach (var emp in DemoDatabase.Employees) Employees.Add(emp);
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

        private void OnEmployeeSelected()
        {
            if (SelectedEmployee == null) return;
            EmployeeName = SelectedEmployee.FullName;
            Position = SelectedEmployee.Position;
            EmpNumber = SelectedEmployee.EmpNumber;
            RatePerDay = $"{SelectedEmployee.DailyRate:N2}";
            
            // Apply last processed payroll inputs to the form automatically
            var lastRecord = DemoDatabase.PayrollHistory.OrderByDescending(r => r.PayrollDate).FirstOrDefault(r => r.EmpNumber == SelectedEmployee.EmpNumber);
            if (lastRecord != null)
            {
                _periodStart = lastRecord.PeriodStart;
                _periodEnd = lastRecord.PeriodEnd;
                _workDays = lastRecord.WorkDays > 0 ? lastRecord.WorkDays.ToString() : "22";
                
                _overtimeHours = lastRecord.OvertimeHours > 0 ? lastRecord.OvertimeHours.ToString("0.##") : "";
                _holidayHours = lastRecord.HolidayHours > 0 ? lastRecord.HolidayHours.ToString("0.##") : "";
                _allowanceInput = lastRecord.Allowance > 0 ? lastRecord.Allowance.ToString("0.##") : "";
                _bonusInput = lastRecord.Bonus > 0 ? lastRecord.Bonus.ToString("0.##") : "";
                _loanInput = lastRecord.Loan > 0 ? lastRecord.Loan.ToString("0.##") : "";
                _lateInput = lastRecord.Late > 0 ? lastRecord.Late.ToString("0.##") : "";
                _undertimeInput = lastRecord.Undertime > 0 ? lastRecord.Undertime.ToString("0.##") : "";
                _othersInput = lastRecord.Others > 0 ? lastRecord.Others.ToString("0.##") : "";

                OnPropertyChanged(nameof(PeriodStart));
                OnPropertyChanged(nameof(PeriodEnd));
                OnPropertyChanged(nameof(WorkDays));
                OnPropertyChanged(nameof(OvertimeHours));
                OnPropertyChanged(nameof(HolidayHours));
                OnPropertyChanged(nameof(AllowanceInput));
                OnPropertyChanged(nameof(BonusInput));
                OnPropertyChanged(nameof(LoanInput));
                OnPropertyChanged(nameof(LateInput));
                OnPropertyChanged(nameof(UndertimeInput));
                OnPropertyChanged(nameof(OthersInput));

                UpdatePeriodText();
            }

            ComputePayslip();

            // Override auto-computed deductions with absolute historical truths if they exist
            if (lastRecord != null)
            {
                _sss = $"{lastRecord.Sss:N2}";
                _pagibig = $"{lastRecord.Pagibig:N2}";
                _philhealth = $"{lastRecord.Philhealth:N2}";
                
                OnPropertyChanged(nameof(Sss));
                OnPropertyChanged(nameof(Pagibig));
                OnPropertyChanged(nameof(Philhealth));
                
                RecomputeDeductions();
            }
        }

        private void UpdatePeriodText()
        {
            PeriodText = $"Payslip for the period of  {PeriodStart:MMMM dd} to {PeriodEnd:MMMM dd, yyyy}";
        }

        private bool _isUpdatingDates = false;

        private void UpdatePeriodFromDays()
        {
            if (_isUpdatingDates) return;

            if (int.TryParse(WorkDays, out var days) && days > 0)
            {
                _isUpdatingDates = true;
                var newEnd = PeriodStart.AddDays(days - 1);
                SetProperty(ref _periodEnd, newEnd, nameof(PeriodEnd));
                _isUpdatingDates = false;
            }
            UpdatePeriodText();
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
            UpdatePeriodText();
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
            decimal.TryParse(LateInput, out var lateVal);
            decimal.TryParse(UndertimeInput, out var underVal);
            decimal.TryParse(OthersInput, out var othersVal);

            _sss = $"{sss:N2}";
            _pagibig = $"{pagibig:N2}";
            _philhealth = $"{philhealth:N2}";
            _loan = $"{loanVal:N2}";
            _late = $"{lateVal:N2}";
            _undertime = $"{underVal:N2}";
            _others = $"{othersVal:N2}";
            OnPropertyChanged(nameof(Sss));
            OnPropertyChanged(nameof(Pagibig));
            OnPropertyChanged(nameof(Philhealth));
            OnPropertyChanged(nameof(Loan));
            OnPropertyChanged(nameof(Late));
            OnPropertyChanged(nameof(Undertime));
            OnPropertyChanged(nameof(Others));

            RecomputeDeductions();
            AllowPerDay = "500.00";
        }

        private void RecomputeDeductions()
        {
            decimal.TryParse(_sss?.Replace("₱", ""), out var sss);
            decimal.TryParse(_pagibig?.Replace("₱", ""), out var pagibig);
            decimal.TryParse(_philhealth?.Replace("₱", ""), out var phil);
            decimal.TryParse(_loan?.Replace("₱", ""), out var loan);
            decimal.TryParse(_late?.Replace("₱", ""), out var late);
            decimal.TryParse(_undertime?.Replace("₱", ""), out var under);
            decimal.TryParse(_others?.Replace("₱", ""), out var others);

            var totalDed = sss + pagibig + phil + loan + late + under + others;
            var net = _cachedGross - totalDed;

            TotalDeductions = $"{totalDed:N2}";
            NetPay = $"{net:N2}";
        }

        private void PrintPayslip()
        {
            if (SelectedEmployee == null)
            {
                System.Windows.MessageBox.Show("Please select an employee first.", "Print Payslip",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            try
            {
                var printDialog = new System.Windows.Controls.PrintDialog();
                if (printDialog.ShowDialog() != true) return;

                // Build a FlowDocument for the payslip
                var doc = new System.Windows.Documents.FlowDocument();
                
                // FORCE: Long Bond (Folio) - 8.5 x 13 inches exactly regardless of OS defaults
                double w = 816;   // 8.5 * 96
                double h = 1248;  // 13.0 * 96
                
                doc.PageWidth = w;
                doc.PageHeight = h;
                doc.ColumnWidth = w;
                
                doc.PagePadding = new System.Windows.Thickness(40, 40, 40, 40);
                doc.FontFamily = new System.Windows.Media.FontFamily("Segoe UI");
                doc.FontSize = 12;

                var darkGreen = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1E7B44"));
                var darkGray = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#333333"));
                var lightGray = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E8E8E8"));

                // Top Header (Logo substitute)
                var headerTable = new System.Windows.Documents.Table();
                headerTable.Columns.Add(new System.Windows.Documents.TableColumn());
                var hRg = new System.Windows.Documents.TableRowGroup();
                var hRow = new System.Windows.Documents.TableRow() { Background = System.Windows.Media.Brushes.Black };
                hRow.Cells.Add(CreateCell(CompanyName, System.Windows.FontWeights.Bold, System.Windows.Media.Brushes.Black, System.Windows.Media.Brushes.White, 1, System.Windows.TextAlignment.Center, 22));
                hRg.Rows.Add(hRow);
                headerTable.RowGroups.Add(hRg);
                doc.Blocks.Add(headerTable);

                // Address
                var addrPara = new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(CompanyAddress));
                addrPara.FontSize = 11;
                addrPara.TextAlignment = System.Windows.TextAlignment.Center;
                addrPara.Margin = new System.Windows.Thickness(0, 5, 0, 5);
                doc.Blocks.Add(addrPara);

                doc.Blocks.Add(CreateSeparator());

                // Period
                var periodPara = new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run($"Payslip for the Period of {PeriodText}"));
                periodPara.FontSize = 13;
                periodPara.FontWeight = System.Windows.FontWeights.Bold;
                periodPara.TextAlignment = System.Windows.TextAlignment.Center;
                periodPara.Margin = new System.Windows.Thickness(0, 10, 0, 15);
                doc.Blocks.Add(periodPara);

                // Employee Info Table
                var empInfoTable = new System.Windows.Documents.Table() { CellSpacing = 0 };
                empInfoTable.Columns.Add(new System.Windows.Documents.TableColumn() { Width = new System.Windows.GridLength(150) });
                empInfoTable.Columns.Add(new System.Windows.Documents.TableColumn());
                var empRg = new System.Windows.Documents.TableRowGroup();
                
                var empHeader = new System.Windows.Documents.TableRow() { Background = darkGreen };
                empHeader.Cells.Add(CreateCell("Employee Information", System.Windows.FontWeights.Bold, darkGreen, System.Windows.Media.Brushes.White, 2, System.Windows.TextAlignment.Left, 12));
                empRg.Rows.Add(empHeader);

                empRg.Rows.Add(CreateLineRow("Employee Name:", EmployeeName));
                empRg.Rows.Add(CreateLineRow("Employee ID:", EmpNumber));
                empRg.Rows.Add(CreateLineRow("Position / Department:", Position));
                empRg.Rows.Add(CreateLineRow("Pay Period:", PeriodText));
                empRg.Rows.Add(CreateLineRow("Payment Date:", DateTime.Today.ToString("MMMM dd, yyyy")));
                
                empInfoTable.RowGroups.Add(empRg);
                doc.Blocks.Add(empInfoTable);
                doc.Blocks.Add(new System.Windows.Documents.Paragraph() { Margin = new System.Windows.Thickness(0, 10, 0, 0) });

                // Main Grids (Earnings & Deductions side by side in a master table)
                var mainTable = new System.Windows.Documents.Table() { CellSpacing = 10 };
                mainTable.Columns.Add(new System.Windows.Documents.TableColumn() { Width = new System.Windows.GridLength(5.5, System.Windows.GridUnitType.Star) }); // Earnings Half
                mainTable.Columns.Add(new System.Windows.Documents.TableColumn() { Width = new System.Windows.GridLength(4.5, System.Windows.GridUnitType.Star) }); // Deductions Half
                
                var mainRg = new System.Windows.Documents.TableRowGroup();
                var mainRow = new System.Windows.Documents.TableRow();
                
                // Earnings Inner Table
                var earnTable = new System.Windows.Documents.Table() { CellSpacing = 0, BorderBrush = System.Windows.Media.Brushes.Gray, BorderThickness = new System.Windows.Thickness(1) };
                earnTable.Columns.Add(new System.Windows.Documents.TableColumn() { Width = new System.Windows.GridLength(3, System.Windows.GridUnitType.Star) });
                earnTable.Columns.Add(new System.Windows.Documents.TableColumn() { Width = new System.Windows.GridLength(2, System.Windows.GridUnitType.Star) });
                earnTable.Columns.Add(new System.Windows.Documents.TableColumn() { Width = new System.Windows.GridLength(1.5, System.Windows.GridUnitType.Star) });
                earnTable.Columns.Add(new System.Windows.Documents.TableColumn() { Width = new System.Windows.GridLength(2, System.Windows.GridUnitType.Star) });

                var eRg = new System.Windows.Documents.TableRowGroup();
                var eh1 = new System.Windows.Documents.TableRow();
                eh1.Cells.Add(CreateCell("Earnings", System.Windows.FontWeights.Bold, darkGreen, System.Windows.Media.Brushes.White, 4, System.Windows.TextAlignment.Left, 12, true));
                eRg.Rows.Add(eh1);
                
                var eh2 = new System.Windows.Documents.TableRow() { Background = lightGray };
                eh2.Cells.Add(CreateCell("Description", System.Windows.FontWeights.SemiBold, lightGray, System.Windows.Media.Brushes.Black, 1, System.Windows.TextAlignment.Center, 11, true));
                eh2.Cells.Add(CreateCell("Rate Per Day", System.Windows.FontWeights.SemiBold, lightGray, System.Windows.Media.Brushes.Black, 1, System.Windows.TextAlignment.Center, 11, true));
                eh2.Cells.Add(CreateCell("Days", System.Windows.FontWeights.SemiBold, lightGray, System.Windows.Media.Brushes.Black, 1, System.Windows.TextAlignment.Center, 11, true));
                eh2.Cells.Add(CreateCell("Amount", System.Windows.FontWeights.SemiBold, lightGray, System.Windows.Media.Brushes.Black, 1, System.Windows.TextAlignment.Center, 11, true));
                eRg.Rows.Add(eh2);

                eRg.Rows.Add(CreateEarningRow("Basic Salary", RatePerDay, WorkDays, BasicSalary));
                eRg.Rows.Add(CreateEarningRow("Overtime Pay", "-", OvertimeHours, OtPay));
                eRg.Rows.Add(CreateEarningRow("Holiday Pay", "-", HolidayHours, HolidayPay));
                eRg.Rows.Add(CreateEarningRow("Incentives / Bonus", "-", "-", BonusAmount));
                eRg.Rows.Add(CreateEarningRow("Allowance", "-", "-", AllowanceTotal));
                eRg.Rows.Add(CreateEarningRow(" ", " ", " ", " ")); // Empty block for padding
                
                var ef = new System.Windows.Documents.TableRow();
                var eGrossCell = CreateCell("Gross Earnings", System.Windows.FontWeights.Bold, lightGray, System.Windows.Media.Brushes.Black, 3, System.Windows.TextAlignment.Left, 11, true);
                var eAmountCell = CreateCell($"₱ {GrossSalary}", System.Windows.FontWeights.Bold, lightGray, System.Windows.Media.Brushes.Black, 1, System.Windows.TextAlignment.Right, 11, true);
                ef.Cells.Add(eGrossCell);
                ef.Cells.Add(eAmountCell);
                eRg.Rows.Add(ef);
                earnTable.RowGroups.Add(eRg);

                // Deductions Inner Table
                var dedTable = new System.Windows.Documents.Table() { CellSpacing = 0, BorderBrush = System.Windows.Media.Brushes.Gray, BorderThickness = new System.Windows.Thickness(1) };
                dedTable.Columns.Add(new System.Windows.Documents.TableColumn() { Width = new System.Windows.GridLength(3, System.Windows.GridUnitType.Star) });
                dedTable.Columns.Add(new System.Windows.Documents.TableColumn() { Width = new System.Windows.GridLength(2, System.Windows.GridUnitType.Star) });
                
                var dRg = new System.Windows.Documents.TableRowGroup();
                var dh1 = new System.Windows.Documents.TableRow();
                dh1.Cells.Add(CreateCell("Deductions", System.Windows.FontWeights.Bold, darkGray, System.Windows.Media.Brushes.White, 2, System.Windows.TextAlignment.Left, 12, true));
                dRg.Rows.Add(dh1);

                var dh2 = new System.Windows.Documents.TableRow() { Background = lightGray };
                dh2.Cells.Add(CreateCell("Description", System.Windows.FontWeights.SemiBold, lightGray, System.Windows.Media.Brushes.Black, 1, System.Windows.TextAlignment.Center, 11, true));
                dh2.Cells.Add(CreateCell("Amount", System.Windows.FontWeights.SemiBold, lightGray, System.Windows.Media.Brushes.Black, 1, System.Windows.TextAlignment.Center, 11, true));
                dRg.Rows.Add(dh2);

                dRg.Rows.Add(CreateDeductionRow("Late", Late));
                dRg.Rows.Add(CreateDeductionRow("Undertime", Undertime));
                dRg.Rows.Add(CreateDeductionRow("Tax (Withholding)", "0.00")); // Assume 0
                dRg.Rows.Add(CreateDeductionRow("SSS Contribution", Sss));
                dRg.Rows.Add(CreateDeductionRow("PhilHealth Contribution", Philhealth));
                dRg.Rows.Add(CreateDeductionRow("Pag-IBIG Contribution", Pagibig));
                dRg.Rows.Add(CreateDeductionRow("Loan", Loan));
                dRg.Rows.Add(CreateDeductionRow("Other Deductions", Others));

                var df = new System.Windows.Documents.TableRow();
                var dTotalCell = CreateCell("Total Deductions", System.Windows.FontWeights.Bold, lightGray, System.Windows.Media.Brushes.Black, 1, System.Windows.TextAlignment.Left, 11, true);
                var dAmountCell = CreateCell($"₱ {TotalDeductions}", System.Windows.FontWeights.Bold, lightGray, System.Windows.Media.Brushes.Black, 1, System.Windows.TextAlignment.Right, 11, true);
                df.Cells.Add(dTotalCell);
                df.Cells.Add(dAmountCell);
                dRg.Rows.Add(df);
                dedTable.RowGroups.Add(dRg);

                mainRow.Cells.Add(new System.Windows.Documents.TableCell(earnTable) { Padding = new System.Windows.Thickness(0) });
                mainRow.Cells.Add(new System.Windows.Documents.TableCell(dedTable) { Padding = new System.Windows.Thickness(0) });
                mainRg.Rows.Add(mainRow);
                mainTable.RowGroups.Add(mainRg);
                doc.Blocks.Add(mainTable);

                // Summary
                doc.Blocks.Add(CreateSeparator());
                var summaryPara = new System.Windows.Documents.Paragraph();
                summaryPara.TextAlignment = System.Windows.TextAlignment.Center;
                summaryPara.Inlines.Add(new System.Windows.Documents.Run("Gross Earnings – Total Deductions = ") { FontWeight = System.Windows.FontWeights.Bold });
                summaryPara.Inlines.Add(new System.Windows.Documents.Run($"₱ {NetPay}") { FontWeight = System.Windows.FontWeights.Bold, Foreground = darkGreen });
                doc.Blocks.Add(summaryPara);
                doc.Blocks.Add(CreateSeparator());

                // Notes Section
                var notesTable = new System.Windows.Documents.Table() { CellSpacing = 0 };
                var notesRg = new System.Windows.Documents.TableRowGroup();
                var notesHeader = new System.Windows.Documents.TableRow() { Background = darkGreen };
                notesHeader.Cells.Add(CreateCell("Notes", System.Windows.FontWeights.Bold, darkGreen, System.Windows.Media.Brushes.White, 2, System.Windows.TextAlignment.Left, 12));
                notesRg.Rows.Add(notesHeader);
                notesTable.RowGroups.Add(notesRg);
                doc.Blocks.Add(notesTable);

                var signTable = new System.Windows.Documents.Table() { CellSpacing = 20, Margin = new System.Windows.Thickness(0, 30, 0, 0) };
                signTable.Columns.Add(new System.Windows.Documents.TableColumn());
                signTable.Columns.Add(new System.Windows.Documents.TableColumn());
                var signRg = new System.Windows.Documents.TableRowGroup();
                var signRow = new System.Windows.Documents.TableRow();
                
                string prep = string.IsNullOrWhiteSpace(PreparedBy) ? "_________________________" : PreparedBy;
                string app = string.IsNullOrWhiteSpace(ApprovedBy) ? "_________________________" : ApprovedBy;

                signRow.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run($"Prepared by:  {prep}"))) { TextAlignment = System.Windows.TextAlignment.Left });
                signRow.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run($"Approved by:  {app}"))) { TextAlignment = System.Windows.TextAlignment.Right });
                
                signRg.Rows.Add(signRow);
                signTable.RowGroups.Add(signRg);
                doc.Blocks.Add(signTable);

                // Print
                var paginator = ((System.Windows.Documents.IDocumentPaginatorSource)doc).DocumentPaginator;
                paginator.PageSize = new System.Windows.Size(816, 1248);

                // Overpower Bluetooth default PrintTicket logic
                if (printDialog.PrintTicket != null)
                {
                    printDialog.PrintTicket.PageMediaSize = new System.Printing.PageMediaSize(
                        System.Printing.PageMediaSizeName.Unknown, 816, 1248);
                }

                printDialog.PrintDocument(paginator, $"Payslip - {EmployeeName}");

                System.Windows.MessageBox.Show("Payslip sent to printer successfully!",
                    "Print Complete", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Print error: {ex.Message}", "Print Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private static System.Windows.Documents.BlockUIContainer CreateSeparator()
        {
            var line = new System.Windows.Controls.Border
            {
                Height = 1,
                Background = System.Windows.Media.Brushes.Gray,
                Margin = new System.Windows.Thickness(0, 15, 0, 15)
            };
            return new System.Windows.Documents.BlockUIContainer(line);
        }

        private static System.Windows.Documents.TableCell CreateCell(string text, System.Windows.FontWeight weight, System.Windows.Media.Brush bg, System.Windows.Media.Brush fg, int colSpan = 1, System.Windows.TextAlignment align = System.Windows.TextAlignment.Left, double fontSize = 11, bool border = false)
        {
            var p = new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(text)) { TextAlignment = align, FontSize = fontSize, FontWeight = weight };
            var cell = new System.Windows.Documents.TableCell(p) { Background = bg, Foreground = fg, ColumnSpan = colSpan, Padding = new System.Windows.Thickness(6,4,6,4) };
            if (border)
            {
                cell.BorderBrush = System.Windows.Media.Brushes.Gray;
                cell.BorderThickness = new System.Windows.Thickness(0.25);
            }
            return cell;
        }

        private static System.Windows.Documents.TableRow CreateLineRow(string label, string value)
        {
            var row = new System.Windows.Documents.TableRow();
            row.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(label)) { FontSize = 11, FontWeight = System.Windows.FontWeights.SemiBold }) { Padding = new System.Windows.Thickness(4) });
            var valP = new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(value)) { FontSize = 11 };
            var valCell = new System.Windows.Documents.TableCell(valP) { Padding = new System.Windows.Thickness(4), BorderBrush = System.Windows.Media.Brushes.Black, BorderThickness = new System.Windows.Thickness(0,0,0,1) };
            row.Cells.Add(valCell);
            return row;
        }

        private static System.Windows.Documents.TableRow CreateEarningRow(string desc, string rate, string days, string amt)
        {
            var row = new System.Windows.Documents.TableRow();
            row.Cells.Add(CreateCell(desc, System.Windows.FontWeights.Normal, null, System.Windows.Media.Brushes.Black, 1, System.Windows.TextAlignment.Left, 11, true));
            row.Cells.Add(CreateCell(rate, System.Windows.FontWeights.Normal, null, System.Windows.Media.Brushes.Black, 1, System.Windows.TextAlignment.Center, 11, true));
            row.Cells.Add(CreateCell(days, System.Windows.FontWeights.Normal, null, System.Windows.Media.Brushes.Black, 1, System.Windows.TextAlignment.Center, 11, true));
            row.Cells.Add(CreateCell(string.IsNullOrWhiteSpace(amt) || amt == " " ? " " : $"₱ {amt}", System.Windows.FontWeights.Normal, null, System.Windows.Media.Brushes.Black, 1, System.Windows.TextAlignment.Right, 11, true));
            return row;
        }

        private static System.Windows.Documents.TableRow CreateDeductionRow(string desc, string amt)
        {
            var row = new System.Windows.Documents.TableRow();
            row.Cells.Add(CreateCell(desc, System.Windows.FontWeights.Normal, null, System.Windows.Media.Brushes.Black, 1, System.Windows.TextAlignment.Left, 11, true));
            row.Cells.Add(CreateCell(string.IsNullOrWhiteSpace(amt) || amt == " " ? " " : $"₱ {amt}", System.Windows.FontWeights.Normal, null, System.Windows.Media.Brushes.Black, 1, System.Windows.TextAlignment.Right, 11, true));
            return row;
        }
    }
}
