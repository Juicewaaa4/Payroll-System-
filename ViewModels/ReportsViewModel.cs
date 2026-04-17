using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using PayrollSystem.Helpers;
using PayrollSystem.DataAccess;
using PayrollSystem.ViewModels;
using MySql.Data.MySqlClient;
using Microsoft.Win32;

namespace PayrollSystem.ViewModels
{
    public class ReportsViewModel : BaseViewModel
    {
        private DateTime _startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        private DateTime _endDate = DateTime.Now;
        private string _statusMessage = "";
        private string _deductionSearchText = "";
        private EmployeeItem? _selectedDeductionEmployee;
        private bool _hasDeductionRecords;
        private decimal _totalSss, _totalPagibig, _totalPhilhealth, _totalAllDeductions;

        private List<PayrollRecord> _allPayrollRecords = new();
        private int _currentPage = 1;
        private int _totalPages = 1;
        private int _pageSize = 20;

        private string _sortColumn = "Date";
        private bool _sortAscending = false;

        public DateTime StartDate { get => _startDate; set { SetProperty(ref _startDate, value); LoadData(); } }
        public DateTime EndDate { get => _endDate; set { SetProperty(ref _endDate, value); LoadData(); } }
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        // Deduction history
        public string DeductionSearchText
        {
            get => _deductionSearchText;
            set { SetProperty(ref _deductionSearchText, value); FilterDeductionEmployees(); }
        }
        public EmployeeItem? SelectedDeductionEmployee
        {
            get => _selectedDeductionEmployee;
            set { SetProperty(ref _selectedDeductionEmployee, value); LoadEmployeeDeductions(); }
        }
        public bool HasDeductionRecords { get => _hasDeductionRecords; set => SetProperty(ref _hasDeductionRecords, value); }
        public decimal TotalSss { get => _totalSss; set => SetProperty(ref _totalSss, value); }
        public decimal TotalPagibig { get => _totalPagibig; set => SetProperty(ref _totalPagibig, value); }
        public decimal TotalPhilhealth { get => _totalPhilhealth; set => SetProperty(ref _totalPhilhealth, value); }
        public decimal TotalAllDeductions { get => _totalAllDeductions; set => SetProperty(ref _totalAllDeductions, value); }
        private decimal _totalLoan, _totalLate, _totalUndertime, _totalOthers;
        public decimal TotalLoan { get => _totalLoan; set => SetProperty(ref _totalLoan, value); }
        public decimal TotalLate { get => _totalLate; set => SetProperty(ref _totalLate, value); }
        public decimal TotalUndertime { get => _totalUndertime; set => SetProperty(ref _totalUndertime, value); }
        public decimal TotalOthers { get => _totalOthers; set => SetProperty(ref _totalOthers, value); }

        public int CurrentPage { get => _currentPage; set { SetProperty(ref _currentPage, value); UpdatePagedData(); } }
        public int TotalPages { get => _totalPages; set => SetProperty(ref _totalPages, value); }
        public int PageSize { get => _pageSize; set { SetProperty(ref _pageSize, value); CurrentPage = 1; UpdatePagedData(); } }

        public ObservableCollection<PayrollRecord> PayrollRecords { get; } = new();
        public ObservableCollection<EmployeeItem> AllEmployees { get; } = new();
        public ObservableCollection<EmployeeItem> FilteredDeductionEmployees { get; } = new();
        public ObservableCollection<PayrollRecord> EmployeeDeductionRecords { get; } = new();

        public ICommand ExportExcelCommand { get; }
        public ICommand ExportDeductionsExcelCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand SortCommand { get; }

        public ReportsViewModel()
        {
            ExportExcelCommand = new RelayCommand(_ => ExportToExcel());
            ExportDeductionsExcelCommand = new RelayCommand(_ => ExportDeductionsToExcel());
            NextPageCommand = new RelayCommand(_ => { if (CurrentPage < TotalPages) CurrentPage++; });
            PreviousPageCommand = new RelayCommand(_ => { if (CurrentPage > 1) CurrentPage--; });
            SortCommand = new RelayCommand(p => SortData(p?.ToString() ?? "Date"));
        }

        public void LoadData()
        {
            _allPayrollRecords.Clear();

            // Load employees for deduction search
            DemoDatabase.Initialize();
            AllEmployees.Clear();
            foreach (var emp in DemoDatabase.Employees) AllEmployees.Add(emp);
            FilterDeductionEmployees();

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
                    _allPayrollRecords.Add(new PayrollRecord
                    {
                        Id = reader.GetInt32("id"),
                        EmployeeName = reader.GetString("employee_name"),
                        EmpNumber = reader.GetString("emp_number"),
                        PayrollDate = reader.GetDateTime("payroll_date").ToString("MMM dd, yyyy  h:mm tt"),
                        GrossSalary = $"₱{reader.GetDecimal("gross_salary"):N2}",
                        GrossRaw = reader.GetDecimal("gross_salary"),
                        Deductions = $"₱{reader.GetDecimal("total_deductions"):N2}",
                        DeductionsRaw = reader.GetDecimal("total_deductions"),
                        NetPay = $"₱{reader.GetDecimal("net_pay"):N2}",
                        NetPayRaw = reader.GetDecimal("net_pay"),
                        Status = reader.GetString("status")
                    });
                }

                if (_allPayrollRecords.Count == 0) LoadDemoData();
                else UpdatePagedData();
            }
            catch { LoadDemoData(); }
        }

        private void LoadDemoData()
        {
            _allPayrollRecords.Clear();

            // Pull from DemoDatabase payroll history (real processed records)
            foreach (var rec in DemoDatabase.PayrollHistory)
            {
                if (rec.PayrollDate >= StartDate && rec.PayrollDate <= EndDate.AddDays(1))
                {
                    _allPayrollRecords.Add(new PayrollRecord
                    {
                        Id = rec.Id,
                        EmployeeName = rec.EmployeeName,
                        EmpNumber = rec.EmpNumber,
                        PayrollDate = rec.PayrollDateFormatted,
                        GrossSalary = rec.GrossSalary,
                        GrossRaw = rec.GrossRaw,
                        Deductions = rec.Deductions,
                        DeductionsRaw = rec.DeductionsRaw,
                        NetPay = rec.NetPay,
                        NetPayRaw = rec.NetPayRaw,
                        Status = rec.Status,
                        Sss = rec.Sss,
                        Pagibig = rec.Pagibig,
                        Philhealth = rec.Philhealth
                    });
                }
            }

            if (_allPayrollRecords.Count == 0)
                StatusMessage = "No payroll records found. Process payroll first to see records here.";
            else
                StatusMessage = "";

            UpdatePagedData();
        }

        public void SortData(string column)
        {
            if (_sortColumn == column) _sortAscending = !_sortAscending;
            else { _sortColumn = column; _sortAscending = false; }
            _currentPage = 1;
            OnPropertyChanged(nameof(CurrentPage));
            UpdatePagedData();
        }

        private void UpdatePagedData()
        {
            if (_allPayrollRecords == null) return;
            
            IEnumerable<PayrollRecord> query = _allPayrollRecords;

            switch (_sortColumn)
            {
                case "EMP #":
                    query = _sortAscending ? query.OrderBy(x => x.EmpNumber) : query.OrderByDescending(x => x.EmpNumber);
                    break;
                case "Employee":
                    query = _sortAscending ? query.OrderBy(x => x.EmployeeName) : query.OrderByDescending(x => x.EmployeeName);
                    break;
                case "Gross":
                    query = _sortAscending ? query.OrderBy(x => x.GrossRaw) : query.OrderByDescending(x => x.GrossRaw);
                    break;
                case "Net Pay":
                    query = _sortAscending ? query.OrderBy(x => x.NetPayRaw) : query.OrderByDescending(x => x.NetPayRaw);
                    break;
                case "Status":
                    query = _sortAscending ? query.OrderBy(x => x.Status) : query.OrderByDescending(x => x.Status);
                    break;
                case "Date":
                default:
                    // ID is sequential and gives proper date sorting
                    query = _sortAscending ? query.OrderBy(x => x.Id) : query.OrderByDescending(x => x.Id);
                    break;
            }

            TotalPages = Math.Max(1, (int)Math.Ceiling(query.Count() / (double)PageSize));
            if (_currentPage > TotalPages) { _currentPage = TotalPages; OnPropertyChanged(nameof(CurrentPage)); }

            var paged = query.Skip((_currentPage - 1) * PageSize).Take(PageSize).ToList();
            
            PayrollRecords.Clear();
            foreach (var item in paged) PayrollRecords.Add(item);
        }

        private void FilterDeductionEmployees()
        {
            FilteredDeductionEmployees.Clear();
            var filtered = string.IsNullOrWhiteSpace(DeductionSearchText)
                ? AllEmployees
                : new ObservableCollection<EmployeeItem>(AllEmployees.Where(e =>
                    e.FullName.Contains(DeductionSearchText, StringComparison.OrdinalIgnoreCase) ||
                    e.EmpNumber.Contains(DeductionSearchText, StringComparison.OrdinalIgnoreCase)));
            foreach (var emp in filtered) FilteredDeductionEmployees.Add(emp);

            if (!string.IsNullOrWhiteSpace(DeductionSearchText) && FilteredDeductionEmployees.Count > 0)
                SelectedDeductionEmployee = FilteredDeductionEmployees[0];
        }

        private void LoadEmployeeDeductions()
        {
            EmployeeDeductionRecords.Clear();
            TotalSss = 0; TotalPagibig = 0; TotalPhilhealth = 0; 
            TotalLoan = 0; TotalLate = 0; TotalUndertime = 0; TotalOthers = 0;
            TotalAllDeductions = 0;

            if (SelectedDeductionEmployee == null) { HasDeductionRecords = false; return; }

            var empNum = SelectedDeductionEmployee.EmpNumber;

            foreach (var rec in DemoDatabase.PayrollHistory)
            {
                if (rec.EmpNumber == empNum)
                {
                    EmployeeDeductionRecords.Add(new PayrollRecord
                    {
                        PayrollDate = rec.PayrollDateFormatted,
                        GrossSalary = rec.GrossSalary,
                        GrossRaw = rec.GrossRaw,
                        Deductions = rec.Deductions,
                        DeductionsRaw = rec.DeductionsRaw,
                        NetPay = rec.NetPay,
                        NetPayRaw = rec.NetPayRaw,
                        Sss = rec.Sss,
                        Pagibig = rec.Pagibig,
                        Philhealth = rec.Philhealth,
                        Loan = rec.Loan,
                        Late = rec.Late,
                        Undertime = rec.Undertime,
                        Others = rec.Others
                    });

                    TotalSss += rec.Sss;
                    TotalPagibig += rec.Pagibig;
                    TotalPhilhealth += rec.Philhealth;
                    TotalLoan += rec.Loan;
                    TotalLate += rec.Late;
                    TotalUndertime += rec.Undertime;
                    TotalOthers += rec.Others;
                    TotalAllDeductions += rec.DeductionsRaw;
                }
            }

            HasDeductionRecords = EmployeeDeductionRecords.Count > 0;
        }

        private void ExportToExcel()
        {
            if (_allPayrollRecords.Count == 0)
            {
                StatusMessage = "No records to export.";
                return;
            }

            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Title = "Export Payroll Report",
                    Filter = "CSV Files (*.csv)|*.csv|Excel XML (*.xlsx)|*.xlsx",
                    FileName = $"Payroll_Report_{DateTime.Now:yyyyMMdd}",
                    DefaultExt = ".csv"
                };

                if (saveDialog.ShowDialog() != true) return;

                var filePath = saveDialog.FileName;

                if (filePath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                    ExportAsXlsx(filePath);
                else
                    ExportAsCsv(filePath);

                StatusMessage = $"✓ Exported successfully: {Path.GetFileName(filePath)}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export error: {ex.Message}";
            }
        }

        private void ExportAsXlsx(string filePath)
        {
            var sharedStrings = new List<string>();
            int SharedStr(string s) { if (!sharedStrings.Contains(s)) sharedStrings.Add(s); return sharedStrings.IndexOf(s); }

            var rows = new StringBuilder();
            
            // Header Info for Professional Look
            rows.Append("<row r=\"1\"><c r=\"A1\" t=\"s\" s=\"1\"><v>" + SharedStr("Zoey's Billiard House - Payroll Report") + "</v></c></row>");
            rows.Append("<row r=\"2\">");
            rows.Append("<c r=\"A2\" t=\"s\" s=\"2\"><v>" + SharedStr($"Period: {StartDate:MMM dd yyyy} to {EndDate:MMM dd yyyy}") + "</v></c>");
            rows.Append("</row>");
            rows.Append("<row r=\"3\"></row>");

            string[] headers = { "EMP #", "Employee Name", "Date", "Gross Salary", "SSS", "PAG-IBIG", "PhilHealth", "Total Deductions", "Net Pay", "Status" };
            rows.Append("<row r=\"4\">");
            for (int i = 0; i < headers.Length; i++)
            {
                var col = (char)('A' + i);
                rows.Append($"<c r=\"{col}4\" t=\"s\" s=\"3\"><v>{SharedStr(headers[i])}</v></c>");
            }
            rows.Append("</row>");

            decimal totalGross = 0, totalSss = 0, totalPag = 0, totalPhil = 0, totalDed = 0, totalNet = 0;
            int rowNum = 5;

            foreach (var rec in _allPayrollRecords)
            {
                rows.Append($"<row r=\"{rowNum}\">");
                rows.Append($"<c r=\"A{rowNum}\" t=\"s\" s=\"4\"><v>{SharedStr(rec.EmpNumber)}</v></c>");
                rows.Append($"<c r=\"B{rowNum}\" t=\"s\" s=\"4\"><v>{SharedStr(rec.EmployeeName)}</v></c>");
                rows.Append($"<c r=\"C{rowNum}\" t=\"s\" s=\"4\"><v>{SharedStr(rec.PayrollDate)}</v></c>");
                rows.Append($"<c r=\"D{rowNum}\" s=\"5\"><v>{rec.GrossRaw}</v></c>");
                rows.Append($"<c r=\"E{rowNum}\" s=\"5\"><v>{rec.Sss}</v></c>");
                rows.Append($"<c r=\"F{rowNum}\" s=\"5\"><v>{rec.Pagibig}</v></c>");
                rows.Append($"<c r=\"G{rowNum}\" s=\"5\"><v>{rec.Philhealth}</v></c>");
                rows.Append($"<c r=\"H{rowNum}\" s=\"5\"><v>{rec.DeductionsRaw}</v></c>");
                rows.Append($"<c r=\"I{rowNum}\" s=\"5\"><v>{rec.NetPayRaw}</v></c>");
                rows.Append($"<c r=\"J{rowNum}\" t=\"s\" s=\"4\"><v>{SharedStr(rec.Status)}</v></c>");
                rows.Append("</row>");
                totalGross += rec.GrossRaw; totalSss += rec.Sss; totalPag += rec.Pagibig;
                totalPhil += rec.Philhealth; totalDed += rec.DeductionsRaw; totalNet += rec.NetPayRaw;
                rowNum++;
            }

            rows.Append($"<row r=\"{rowNum}\">");
            rows.Append($"<c r=\"C{rowNum}\" t=\"s\" s=\"6\"><v>{SharedStr("TOTALS")}</v></c>");
            rows.Append($"<c r=\"D{rowNum}\" s=\"7\"><v>{totalGross}</v></c>");
            rows.Append($"<c r=\"E{rowNum}\" s=\"7\"><v>{totalSss}</v></c>");
            rows.Append($"<c r=\"F{rowNum}\" s=\"7\"><v>{totalPag}</v></c>");
            rows.Append($"<c r=\"G{rowNum}\" s=\"7\"><v>{totalPhil}</v></c>");
            rows.Append($"<c r=\"H{rowNum}\" s=\"7\"><v>{totalDed}</v></c>");
            rows.Append($"<c r=\"I{rowNum}\" s=\"7\"><v>{totalNet}</v></c>");
            rows.Append("</row>");

            var sheetData = $"<sheetData>{rows}</sheetData>";
            var cols = "<cols><col min=\"1\" max=\"1\" width=\"15\" customWidth=\"1\"/><col min=\"2\" max=\"2\" width=\"28\" customWidth=\"1\"/><col min=\"3\" max=\"3\" width=\"20\" customWidth=\"1\"/><col min=\"4\" max=\"9\" width=\"15\" customWidth=\"1\"/><col min=\"10\" max=\"10\" width=\"12\" customWidth=\"1\"/></cols>";
            var sheetXml = $@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<worksheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">{cols}{sheetData}</worksheet>";

            var ssXml = new StringBuilder();
            ssXml.Append(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?><sst xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"" count=""" + sharedStrings.Count + @""" uniqueCount=""" + sharedStrings.Count + @""">");
            foreach (var s in sharedStrings)
            {
                var escaped = System.Security.SecurityElement.Escape(s) ?? "";
                ssXml.Append($"<si><t>{escaped}</t></si>");
            }
            ssXml.Append("</sst>");

            var styles = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<styleSheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">
  <numFmts count=""1"">
    <numFmt numFmtId=""164"" formatCode=""_(&quot;₱&quot;* #,##0.00_);_(&quot;₱&quot;* \(#,##0.00\);_(&quot;₱&quot;* &quot;-&quot;??_);_(@_)""/>
  </numFmts>
  <fonts count=""4"">
    <font><sz val=""11""/><name val=""Calibri""/></font>
    <font><b/><sz val=""14""/><color rgb=""FF1B5E20""/><name val=""Calibri""/></font>
    <font><b/><sz val=""12""/><name val=""Calibri""/></font>
    <font><b/><sz val=""11""/><color rgb=""FFFFFFFF""/><name val=""Calibri""/></font>
  </fonts>
  <fills count=""4"">
    <fill><patternFill patternType=""none""/></fill>
    <fill><patternFill patternType=""gray125""/></fill>
    <fill><patternFill patternType=""solid""><fgColor rgb=""FF2E7D32""/><bgColor indexed=""64""/></patternFill></fill>
    <fill><patternFill patternType=""solid""><fgColor rgb=""FFF2F2F2""/><bgColor indexed=""64""/></patternFill></fill>
  </fills>
  <borders count=""2"">
    <border><left/><right/><top/><bottom/><diagonal/></border>
    <border>
      <left style=""thin""><color rgb=""FFDDDDDD""/></left>
      <right style=""thin""><color rgb=""FFDDDDDD""/></right>
      <top style=""thin""><color rgb=""FFDDDDDD""/></top>
      <bottom style=""thin""><color rgb=""FFDDDDDD""/></bottom>
      <diagonal/>
    </border>
  </borders>
  <cellXfs count=""8"">
    <xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""0"" xfId=""0""/>
    <xf numFmtId=""0"" fontId=""1"" fillId=""0"" borderId=""0"" xfId=""0"" applyFont=""1""/>
    <xf numFmtId=""0"" fontId=""2"" fillId=""0"" borderId=""0"" xfId=""0"" applyFont=""1""/>
    <xf numFmtId=""0"" fontId=""3"" fillId=""2"" borderId=""1"" xfId=""0"" applyFont=""1"" applyFill=""1"" applyBorder=""1""/>
    <xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""1"" xfId=""0"" applyBorder=""1""/>
    <xf numFmtId=""164"" fontId=""0"" fillId=""0"" borderId=""1"" xfId=""0"" applyNumberFormat=""1"" applyBorder=""1""/>
    <xf numFmtId=""0"" fontId=""2"" fillId=""3"" borderId=""1"" xfId=""0"" applyFont=""1"" applyFill=""1"" applyBorder=""1""/>
    <xf numFmtId=""164"" fontId=""2"" fillId=""3"" borderId=""1"" xfId=""0"" applyNumberFormat=""1"" applyFont=""1"" applyFill=""1"" applyBorder=""1""/>
  </cellXfs>
</styleSheet>";

            var contentTypes = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types"">
<Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/>
<Default Extension=""xml"" ContentType=""application/xml""/>
<Override PartName=""/xl/workbook.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml""/>
<Override PartName=""/xl/worksheets/sheet1.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml""/>
<Override PartName=""/xl/sharedStrings.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml""/>
<Override PartName=""/xl/styles.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml""/>
</Types>";

            var rels = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
<Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""xl/workbook.xml""/>
</Relationships>";

            var workbookRels = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
<Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"" Target=""worksheets/sheet1.xml""/>
<Relationship Id=""rId2"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/sharedStrings"" Target=""sharedStrings.xml""/>
<Relationship Id=""rId3"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles"" Target=""styles.xml""/>
</Relationships>";

            var workbook = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<workbook xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"" xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships"">
<sheets><sheet name=""Payroll Report"" sheetId=""1"" r:id=""rId1""/></sheets></workbook>";

            if (File.Exists(filePath)) File.Delete(filePath);
            using var zip = System.IO.Compression.ZipFile.Open(filePath, System.IO.Compression.ZipArchiveMode.Create);
            WriteEntry(zip, "[Content_Types].xml", contentTypes);
            WriteEntry(zip, "_rels/.rels", rels);
            WriteEntry(zip, "xl/workbook.xml", workbook);
            WriteEntry(zip, "xl/_rels/workbook.xml.rels", workbookRels);
            WriteEntry(zip, "xl/worksheets/sheet1.xml", sheetXml);
            WriteEntry(zip, "xl/styles.xml", styles);
            WriteEntry(zip, "xl/sharedStrings.xml", ssXml.ToString());
        }

        private static void WriteEntry(System.IO.Compression.ZipArchive zip, string path, string content)
        {
            var entry = zip.CreateEntry(path);
            using var stream = entry.Open();
            using var writer = new StreamWriter(stream, Encoding.UTF8);
            writer.Write(content);
        }

        private void ExportAsCsv(string filePath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Payroll Report - Zoey's Billiard House");
            sb.AppendLine($"Period: {StartDate:MMM dd yyyy} to {EndDate:MMM dd yyyy}");
            sb.AppendLine($"Generated: {DateTime.Now:MMM dd yyyy  hh:mm tt}");
            sb.AppendLine();
            sb.AppendLine("EMP #,Employee Name,Date,Gross Salary,SSS,PAG-IBIG,PhilHealth,Total Deductions,Net Pay,Status");

            decimal totalGross = 0, totalSss = 0, totalPag = 0, totalPhil = 0, totalDed = 0, totalNet = 0;

            foreach (var rec in _allPayrollRecords)
            {
                sb.AppendLine($"{rec.EmpNumber},\"{rec.EmployeeName}\",\"{rec.PayrollDate}\",{rec.GrossRaw:F2},{rec.Sss:F2},{rec.Pagibig:F2},{rec.Philhealth:F2},{rec.DeductionsRaw:F2},{rec.NetPayRaw:F2},{rec.Status}");
                totalGross += rec.GrossRaw; totalSss += rec.Sss; totalPag += rec.Pagibig;
                totalPhil += rec.Philhealth; totalDed += rec.DeductionsRaw; totalNet += rec.NetPayRaw;
            }

            sb.AppendLine();
            sb.AppendLine($",,TOTALS,{totalGross:F2},{totalSss:F2},{totalPag:F2},{totalPhil:F2},{totalDed:F2},{totalNet:F2},");

            File.WriteAllText(filePath, sb.ToString());
        }

        private void ExportDeductionsToExcel()
        {
            if (EmployeeDeductionRecords.Count == 0 || SelectedDeductionEmployee == null)
            {
                StatusMessage = "No deduction records to export.";
                return;
            }

            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Title = "Export Deduction History",
                    Filter = "Excel XML (*.xlsx)|*.xlsx|CSV Files (*.csv)|*.csv",
                    FileName = $"Deductions_{SelectedDeductionEmployee.EmpNumber}_{DateTime.Now:yyyyMMdd}",
                    DefaultExt = ".xlsx"
                };

                if (saveDialog.ShowDialog() != true) return;

                var filePath = saveDialog.FileName;

                if (filePath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                    ExportDeductionsAsXlsx(filePath);
                else
                    ExportDeductionsAsCsv(filePath);

                StatusMessage = $"✓ Exported deductions successfully: {Path.GetFileName(filePath)}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export error: {ex.Message}";
            }
        }

        private void ExportDeductionsAsXlsx(string filePath)
        {
            var sharedStrings = new List<string>();
            int SharedStr(string s) { if (!sharedStrings.Contains(s)) sharedStrings.Add(s); return sharedStrings.IndexOf(s); }

            var rows = new StringBuilder();
            
            // Header Info for Professional Look
            rows.Append("<row r=\"1\"><c r=\"A1\" t=\"s\" s=\"1\"><v>" + SharedStr("Zoey's Billiard House - Employee Deduction History") + "</v></c></row>");
            rows.Append("<row r=\"2\">");
            rows.Append("<c r=\"A2\" t=\"s\" s=\"2\"><v>" + SharedStr($"Employee: {SelectedDeductionEmployee?.FullName ?? ""} ({SelectedDeductionEmployee?.EmpNumber ?? ""})") + "</v></c>");
            rows.Append("</row>");
            rows.Append("<row r=\"3\"></row>");

            string[] headers = { "Date", "Gross Salary", "SSS", "PAG-IBIG", "PhilHealth", "Loan", "Late", "Undertime", "Others", "Total Deductions", "Net Pay" };
            rows.Append("<row r=\"4\">");
            for (int i = 0; i < headers.Length; i++)
            {
                var col = (char)('A' + i);
                rows.Append($"<c r=\"{col}4\" t=\"s\" s=\"3\"><v>{SharedStr(headers[i])}</v></c>");
            }
            rows.Append("</row>");

            int rowNum = 5;
            foreach (var rec in EmployeeDeductionRecords)
            {
                rows.Append($"<row r=\"{rowNum}\">");
                rows.Append($"<c r=\"A{rowNum}\" t=\"s\" s=\"4\"><v>{SharedStr(rec.PayrollDate)}</v></c>");
                rows.Append($"<c r=\"B{rowNum}\" s=\"5\"><v>{rec.GrossRaw}</v></c>");
                rows.Append($"<c r=\"C{rowNum}\" s=\"5\"><v>{rec.Sss}</v></c>");
                rows.Append($"<c r=\"D{rowNum}\" s=\"5\"><v>{rec.Pagibig}</v></c>");
                rows.Append($"<c r=\"E{rowNum}\" s=\"5\"><v>{rec.Philhealth}</v></c>");
                rows.Append($"<c r=\"F{rowNum}\" s=\"5\"><v>{rec.Loan}</v></c>");
                rows.Append($"<c r=\"G{rowNum}\" s=\"5\"><v>{rec.Late}</v></c>");
                rows.Append($"<c r=\"H{rowNum}\" s=\"5\"><v>{rec.Undertime}</v></c>");
                rows.Append($"<c r=\"I{rowNum}\" s=\"5\"><v>{rec.Others}</v></c>");
                rows.Append($"<c r=\"J{rowNum}\" s=\"5\"><v>{rec.DeductionsRaw}</v></c>");
                rows.Append($"<c r=\"K{rowNum}\" s=\"5\"><v>{rec.NetPayRaw}</v></c>");
                rows.Append("</row>");
                rowNum++;
            }

            rows.Append($"<row r=\"{rowNum}\">");
            rows.Append($"<c r=\"A{rowNum}\" t=\"s\" s=\"6\"><v>{SharedStr("TOTALS")}</v></c>");
            rows.Append($"<c r=\"B{rowNum}\" s=\"7\"><v>{EmployeeDeductionRecords.Sum(r => r.GrossRaw)}</v></c>");
            rows.Append($"<c r=\"C{rowNum}\" s=\"7\"><v>{TotalSss}</v></c>");
            rows.Append($"<c r=\"D{rowNum}\" s=\"7\"><v>{TotalPagibig}</v></c>");
            rows.Append($"<c r=\"E{rowNum}\" s=\"7\"><v>{TotalPhilhealth}</v></c>");
            rows.Append($"<c r=\"F{rowNum}\" s=\"7\"><v>{TotalLoan}</v></c>");
            rows.Append($"<c r=\"G{rowNum}\" s=\"7\"><v>{TotalLate}</v></c>");
            rows.Append($"<c r=\"H{rowNum}\" s=\"7\"><v>{TotalUndertime}</v></c>");
            rows.Append($"<c r=\"I{rowNum}\" s=\"7\"><v>{TotalOthers}</v></c>");
            rows.Append($"<c r=\"J{rowNum}\" s=\"7\"><v>{TotalAllDeductions}</v></c>");
            rows.Append($"<c r=\"K{rowNum}\" s=\"7\"><v>{EmployeeDeductionRecords.Sum(r => r.NetPayRaw)}</v></c>");
            rows.Append("</row>");

            var sheetData = $"<sheetData>{rows}</sheetData>";
            var cols = "<cols><col min=\"1\" max=\"1\" width=\"25\" customWidth=\"1\"/><col min=\"2\" max=\"11\" width=\"15\" customWidth=\"1\"/></cols>";
            var sheetXml = $@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<worksheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">{cols}{sheetData}</worksheet>";

            var ssXml = new StringBuilder();
            ssXml.Append(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?><sst xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"" count=""" + sharedStrings.Count + @""" uniqueCount=""" + sharedStrings.Count + @""">");
            foreach (var s in sharedStrings)
            {
                var escaped = System.Security.SecurityElement.Escape(s) ?? "";
                ssXml.Append($"<si><t>{escaped}</t></si>");
            }
            ssXml.Append("</sst>");

            var styles = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<styleSheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">
  <numFmts count=""1"">
    <numFmt numFmtId=""164"" formatCode=""_(&quot;₱&quot;* #,##0.00_);_(&quot;₱&quot;* \(#,##0.00\);_(&quot;₱&quot;* &quot;-&quot;??_);_(@_)""/>
  </numFmts>
  <fonts count=""4"">
    <font><sz val=""11""/><name val=""Calibri""/></font>
    <font><b/><sz val=""14""/><color rgb=""FF1B5E20""/><name val=""Calibri""/></font>
    <font><b/><sz val=""12""/><name val=""Calibri""/></font>
    <font><b/><sz val=""11""/><color rgb=""FFFFFFFF""/><name val=""Calibri""/></font>
  </fonts>
  <fills count=""4"">
    <fill><patternFill patternType=""none""/></fill>
    <fill><patternFill patternType=""gray125""/></fill>
    <fill><patternFill patternType=""solid""><fgColor rgb=""FF2E7D32""/><bgColor indexed=""64""/></patternFill></fill>
    <fill><patternFill patternType=""solid""><fgColor rgb=""FFF2F2F2""/><bgColor indexed=""64""/></patternFill></fill>
  </fills>
  <borders count=""2"">
    <border><left/><right/><top/><bottom/><diagonal/></border>
    <border>
      <left style=""thin""><color rgb=""FFDDDDDD""/></left>
      <right style=""thin""><color rgb=""FFDDDDDD""/></right>
      <top style=""thin""><color rgb=""FFDDDDDD""/></top>
      <bottom style=""thin""><color rgb=""FFDDDDDD""/></bottom>
      <diagonal/>
    </border>
  </borders>
  <cellXfs count=""8"">
    <xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""0"" xfId=""0""/>
    <xf numFmtId=""0"" fontId=""1"" fillId=""0"" borderId=""0"" xfId=""0"" applyFont=""1""/>
    <xf numFmtId=""0"" fontId=""2"" fillId=""0"" borderId=""0"" xfId=""0"" applyFont=""1""/>
    <xf numFmtId=""0"" fontId=""3"" fillId=""2"" borderId=""1"" xfId=""0"" applyFont=""1"" applyFill=""1"" applyBorder=""1""/>
    <xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""1"" xfId=""0"" applyBorder=""1""/>
    <xf numFmtId=""164"" fontId=""0"" fillId=""0"" borderId=""1"" xfId=""0"" applyNumberFormat=""1"" applyBorder=""1""/>
    <xf numFmtId=""0"" fontId=""2"" fillId=""3"" borderId=""1"" xfId=""0"" applyFont=""1"" applyFill=""1"" applyBorder=""1""/>
    <xf numFmtId=""164"" fontId=""2"" fillId=""3"" borderId=""1"" xfId=""0"" applyNumberFormat=""1"" applyFont=""1"" applyFill=""1"" applyBorder=""1""/>
  </cellXfs>
</styleSheet>";

            var contentTypes = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types"">
<Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/>
<Default Extension=""xml"" ContentType=""application/xml""/>
<Override PartName=""/xl/workbook.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml""/>
<Override PartName=""/xl/worksheets/sheet1.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml""/>
<Override PartName=""/xl/sharedStrings.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml""/>
<Override PartName=""/xl/styles.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml""/>
</Types>";

            var rels = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
<Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""xl/workbook.xml""/>
</Relationships>";

            var workbookRels = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
<Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"" Target=""worksheets/sheet1.xml""/>
<Relationship Id=""rId2"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/sharedStrings"" Target=""sharedStrings.xml""/>
<Relationship Id=""rId3"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles"" Target=""styles.xml""/>
</Relationships>";

            var workbook = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<workbook xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"" xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships"">
<sheets><sheet name=""Deduction History"" sheetId=""1"" r:id=""rId1""/></sheets></workbook>";

            if (File.Exists(filePath)) File.Delete(filePath);
            using var zip = System.IO.Compression.ZipFile.Open(filePath, System.IO.Compression.ZipArchiveMode.Create);
            WriteEntry(zip, "[Content_Types].xml", contentTypes);
            WriteEntry(zip, "_rels/.rels", rels);
            WriteEntry(zip, "xl/workbook.xml", workbook);
            WriteEntry(zip, "xl/_rels/workbook.xml.rels", workbookRels);
            WriteEntry(zip, "xl/worksheets/sheet1.xml", sheetXml);
            WriteEntry(zip, "xl/styles.xml", styles);
            WriteEntry(zip, "xl/sharedStrings.xml", ssXml.ToString());
        }

        private void ExportDeductionsAsCsv(string filePath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Zoey's Billiard House - Employee Deduction History");
            sb.AppendLine($"Employee: {SelectedDeductionEmployee.FullName} ({SelectedDeductionEmployee.EmpNumber})");
            sb.AppendLine($"Generated: {DateTime.Now:MMM dd yyyy  hh:mm tt}");
            sb.AppendLine();
            sb.AppendLine("Date,Gross Salary,SSS,PAG-IBIG,PhilHealth,Loan,Late,Undertime,Others,Total Deductions,Net Pay");

            foreach (var rec in EmployeeDeductionRecords)
            {
                sb.AppendLine($"\"{rec.PayrollDate}\",{rec.GrossRaw:F2},{rec.Sss:F2},{rec.Pagibig:F2},{rec.Philhealth:F2},{rec.Loan:F2},{rec.Late:F2},{rec.Undertime:F2},{rec.Others:F2},{rec.DeductionsRaw:F2},{rec.NetPayRaw:F2}");
            }

            sb.AppendLine();
            sb.AppendLine($"TOTALS,{EmployeeDeductionRecords.Sum(r => r.GrossRaw):F2},{TotalSss:F2},{TotalPagibig:F2},{TotalPhilhealth:F2},{TotalLoan:F2},{TotalLate:F2},{TotalUndertime:F2},{TotalOthers:F2},{TotalAllDeductions:F2},{EmployeeDeductionRecords.Sum(r => r.NetPayRaw):F2}");

            File.WriteAllText(filePath, sb.ToString());
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
        public decimal Sss { get; set; }
        public decimal Pagibig { get; set; }
        public decimal Philhealth { get; set; }
        public decimal Loan { get; set; }
        public decimal Late { get; set; }
        public decimal Undertime { get; set; }
        public decimal Others { get; set; }
    }
}
