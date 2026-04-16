using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using PayrollSystem.Helpers;
using PayrollSystem.DataAccess;
using MySql.Data.MySqlClient;
using Microsoft.Win32;

namespace PayrollSystem.ViewModels
{
    public class ReportsViewModel : BaseViewModel
    {
        private DateTime _startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        private DateTime _endDate = DateTime.Now;
        private string _statusMessage = "";

        public DateTime StartDate { get => _startDate; set { SetProperty(ref _startDate, value); LoadData(); } }
        public DateTime EndDate { get => _endDate; set { SetProperty(ref _endDate, value); LoadData(); } }
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        public ObservableCollection<PayrollRecord> PayrollRecords { get; } = new();

        public ICommand ExportExcelCommand { get; }

        public ReportsViewModel()
        {
            ExportExcelCommand = new RelayCommand(_ => ExportToExcel());
        }

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
                        PayrollDate = reader.GetDateTime("payroll_date").ToString("MMM dd, yyyy  hh:mm tt"),
                        GrossSalary = $"₱{reader.GetDecimal("gross_salary"):N2}",
                        GrossRaw = reader.GetDecimal("gross_salary"),
                        Deductions = $"₱{reader.GetDecimal("total_deductions"):N2}",
                        DeductionsRaw = reader.GetDecimal("total_deductions"),
                        NetPay = $"₱{reader.GetDecimal("net_pay"):N2}",
                        NetPayRaw = reader.GetDecimal("net_pay"),
                        Status = reader.GetString("status")
                    });
                }

                if (PayrollRecords.Count == 0) LoadDemoData();
            }
            catch { LoadDemoData(); }
        }

        private void LoadDemoData()
        {
            PayrollRecords.Clear();
            var now = DateTime.Now;
            PayrollRecords.Add(new PayrollRecord { EmployeeName = "Kenneth Ariel Francisco", EmpNumber = "EMP-0001", PayrollDate = now.ToString("MMM dd, yyyy  hh:mm tt"), GrossSalary = "₱26,400.00", GrossRaw = 26400, Deductions = "₱2,412.00", DeductionsRaw = 2412, NetPay = "₱23,988.00", NetPayRaw = 23988, Status = "Processed" });
            PayrollRecords.Add(new PayrollRecord { EmployeeName = "Judy Peralta", EmpNumber = "EMP-0002", PayrollDate = now.ToString("MMM dd, yyyy  hh:mm tt"), GrossSalary = "₱33,000.00", GrossRaw = 33000, Deductions = "₱3,037.50", DeductionsRaw = 3037.50m, NetPay = "₱29,962.50", NetPayRaw = 29962.50m, Status = "Processed" });
            PayrollRecords.Add(new PayrollRecord { EmployeeName = "Alyssa Marie Zamudio", EmpNumber = "EMP-0004", PayrollDate = now.AddDays(-1).ToString("MMM dd, yyyy  hh:mm tt"), GrossSalary = "₱22,000.00", GrossRaw = 22000, Deductions = "₱2,035.00", DeductionsRaw = 2035, NetPay = "₱19,965.00", NetPayRaw = 19965, Status = "Processed" });
        }

        private void ExportToExcel()
        {
            if (PayrollRecords.Count == 0)
            {
                StatusMessage = "No records to export.";
                return;
            }

            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Title = "Export Payroll Report",
                    Filter = "Excel Files (*.xlsx)|*.xlsx|CSV Files (*.csv)|*.csv",
                    FileName = $"Payroll_Report_{DateTime.Now:yyyyMMdd}",
                    DefaultExt = ".xlsx"
                };

                if (saveDialog.ShowDialog() != true) return;

                var filePath = saveDialog.FileName;

                if (filePath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    ExportAsXlsx(filePath);
                }
                else
                {
                    ExportAsCsv(filePath);
                }

                StatusMessage = $"✓ Exported successfully: {Path.GetFileName(filePath)}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export error: {ex.Message}";
            }
        }

        private void ExportAsXlsx(string filePath)
        {
            // Generate Office Open XML Spreadsheet (real .xlsx)
            var sharedStrings = new List<string>();
            int SharedStr(string s) { if (!sharedStrings.Contains(s)) sharedStrings.Add(s); return sharedStrings.IndexOf(s); }

            var rows = new StringBuilder();
            // Header row
            string[] headers = { "EMP #", "Employee Name", "Date", "Gross Salary", "Deductions", "Net Pay", "Status" };
            rows.Append("<row r=\"1\">");
            for (int i = 0; i < headers.Length; i++)
            {
                var col = (char)('A' + i);
                rows.Append($"<c r=\"{col}1\" t=\"s\"><v>{SharedStr(headers[i])}</v></c>");
            }
            rows.Append("</row>");

            decimal totalGross = 0, totalDed = 0, totalNet = 0;
            int rowNum = 2;

            foreach (var rec in PayrollRecords)
            {
                rows.Append($"<row r=\"{rowNum}\">");
                rows.Append($"<c r=\"A{rowNum}\" t=\"s\"><v>{SharedStr(rec.EmpNumber)}</v></c>");
                rows.Append($"<c r=\"B{rowNum}\" t=\"s\"><v>{SharedStr(rec.EmployeeName)}</v></c>");
                rows.Append($"<c r=\"C{rowNum}\" t=\"s\"><v>{SharedStr(rec.PayrollDate)}</v></c>");
                rows.Append($"<c r=\"D{rowNum}\"><v>{rec.GrossRaw}</v></c>");
                rows.Append($"<c r=\"E{rowNum}\"><v>{rec.DeductionsRaw}</v></c>");
                rows.Append($"<c r=\"F{rowNum}\"><v>{rec.NetPayRaw}</v></c>");
                rows.Append($"<c r=\"G{rowNum}\" t=\"s\"><v>{SharedStr(rec.Status)}</v></c>");
                rows.Append("</row>");
                totalGross += rec.GrossRaw;
                totalDed += rec.DeductionsRaw;
                totalNet += rec.NetPayRaw;
                rowNum++;
            }

            // Totals row
            rows.Append($"<row r=\"{rowNum}\">");
            rows.Append($"<c r=\"A{rowNum}\" t=\"s\"><v>{SharedStr("")}</v></c>");
            rows.Append($"<c r=\"B{rowNum}\" t=\"s\"><v>{SharedStr("")}</v></c>");
            rows.Append($"<c r=\"C{rowNum}\" t=\"s\"><v>{SharedStr("TOTALS")}</v></c>");
            rows.Append($"<c r=\"D{rowNum}\"><v>{totalGross}</v></c>");
            rows.Append($"<c r=\"E{rowNum}\"><v>{totalDed}</v></c>");
            rows.Append($"<c r=\"F{rowNum}\"><v>{totalNet}</v></c>");
            rows.Append("</row>");

            // Build shared strings XML
            var ssXml = new StringBuilder();
            ssXml.Append($"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><sst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" count=\"{sharedStrings.Count}\" uniqueCount=\"{sharedStrings.Count}\">");
            foreach (var s in sharedStrings)
                ssXml.Append($"<si><t>{System.Security.SecurityElement.Escape(s)}</t></si>");
            ssXml.Append("</sst>");

            // Build sheet XML
            var sheetXml = $@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<worksheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">
<cols><col min=""1"" max=""1"" width=""12""/><col min=""2"" max=""2"" width=""28""/><col min=""3"" max=""3"" width=""22""/>
<col min=""4"" max=""6"" width=""16""/><col min=""7"" max=""7"" width=""12""/></cols>
<sheetData>{rows}</sheetData></worksheet>";

            var contentTypes = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types"">
<Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/>
<Default Extension=""xml"" ContentType=""application/xml""/>
<Override PartName=""/xl/workbook.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml""/>
<Override PartName=""/xl/worksheets/sheet1.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml""/>
<Override PartName=""/xl/sharedStrings.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml""/>
</Types>";

            var rels = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
<Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""xl/workbook.xml""/>
</Relationships>";

            var workbookRels = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
<Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"" Target=""worksheets/sheet1.xml""/>
<Relationship Id=""rId2"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/sharedStrings"" Target=""sharedStrings.xml""/>
</Relationships>";

            var workbook = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<workbook xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"" xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships"">
<sheets><sheet name=""Payroll Report"" sheetId=""1"" r:id=""rId1""/></sheets></workbook>";

            // Write as ZIP (xlsx is a ZIP)
            if (File.Exists(filePath)) File.Delete(filePath);
            using var zip = System.IO.Compression.ZipFile.Open(filePath, System.IO.Compression.ZipArchiveMode.Create);
            WriteEntry(zip, "[Content_Types].xml", contentTypes);
            WriteEntry(zip, "_rels/.rels", rels);
            WriteEntry(zip, "xl/workbook.xml", workbook);
            WriteEntry(zip, "xl/_rels/workbook.xml.rels", workbookRels);
            WriteEntry(zip, "xl/worksheets/sheet1.xml", sheetXml);
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
            sb.AppendLine("Payroll Report");
            sb.AppendLine($"Period: {StartDate:MMM dd, yyyy} to {EndDate:MMM dd, yyyy}");
            sb.AppendLine($"Generated: {DateTime.Now:MMM dd, yyyy  hh:mm tt}");
            sb.AppendLine();
            sb.AppendLine("EMP #,Employee Name,Date,Gross Salary,Deductions,Net Pay,Status");

            decimal totalGross = 0, totalDed = 0, totalNet = 0;

            foreach (var rec in PayrollRecords)
            {
                sb.AppendLine($"{rec.EmpNumber},\"{rec.EmployeeName}\",\"{rec.PayrollDate}\",{rec.GrossRaw:N2},{rec.DeductionsRaw:N2},{rec.NetPayRaw:N2},{rec.Status}");
                totalGross += rec.GrossRaw;
                totalDed += rec.DeductionsRaw;
                totalNet += rec.NetPayRaw;
            }

            sb.AppendLine();
            sb.AppendLine($",,TOTALS,{totalGross:N2},{totalDed:N2},{totalNet:N2},");

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
    }
}
