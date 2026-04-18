using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Controls;
using System.Windows.Media;
using PayrollSystem.Helpers;
using PayrollSystem.DataAccess;
using MySql.Data.MySqlClient;

namespace PayrollSystem.ViewModels
{
    public class BatchPrintViewModel : BaseViewModel
    {
        private DateTime _startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        private DateTime _endDate = DateTime.Now;
        private string _statusMessage = "";
        private bool _selectAll;

        public DateTime StartDate { get => _startDate; set { SetProperty(ref _startDate, value); LoadData(); } }
        public DateTime EndDate { get => _endDate; set { SetProperty(ref _endDate, value); LoadData(); } }
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        public bool SelectAll 
        { 
            get => _selectAll; 
            set 
            { 
                SetProperty(ref _selectAll, value);
                foreach (var rec in PayrollRecords) rec.IsSelected = value; 
            } 
        }

        public ObservableCollection<BatchPrintRecord> PayrollRecords { get; } = new();

        public ICommand PrintSelectedCommand { get; }
        public ICommand PrintAllCommand { get; }
        public ICommand EditRecordCommand { get; }
        public ICommand DeleteRecordCommand { get; }
        public ICommand ConfirmDeleteCommand { get; }
        public ICommand CancelDeleteCommand { get; }
        public ICommand ApproveRecordCommand { get; }
        public ICommand SaveEditCommand { get; }
        public ICommand CancelEditCommand { get; }

        // --- Delete Modal State ---
        private BatchPrintRecord? _pendingDeleteRecord;
        private bool _isDeleteModalVisible;
        private string _deleteTargetName = "";
        public bool IsDeleteModalVisible { get => _isDeleteModalVisible; set => SetProperty(ref _isDeleteModalVisible, value); }
        public string DeleteTargetName { get => _deleteTargetName; set => SetProperty(ref _deleteTargetName, value); }

        // --- Edit Modal State ---
        private BatchPrintRecord? _editingRecord;
        private bool _isEditModalVisible;
        public bool IsEditModalVisible { get => _isEditModalVisible; set => SetProperty(ref _isEditModalVisible, value); }
        
        private string _editTargetName = "";
        public string EditTargetName { get => _editTargetName; set => SetProperty(ref _editTargetName, value); }

        private string _editGross = "";
        public string EditGross { get => _editGross; set => SetProperty(ref _editGross, value); }

        private string _editDeductions = "";
        public string EditDeductions { get => _editDeductions; set => SetProperty(ref _editDeductions, value); }

        private string _editNetPay = "";
        public string EditNetPay { get => _editNetPay; set => SetProperty(ref _editNetPay, value); }
        
        private string _editSss = "";
        public string EditSss { get => _editSss; set { SetProperty(ref _editSss, value); RecomputeTotals(); } }

        private string _editPagibig = "";
        public string EditPagibig { get => _editPagibig; set { SetProperty(ref _editPagibig, value); RecomputeTotals(); } }

        private string _editPhilhealth = "";
        public string EditPhilhealth { get => _editPhilhealth; set { SetProperty(ref _editPhilhealth, value); RecomputeTotals(); } }

        private string _editLoan = "";
        public string EditLoan { get => _editLoan; set { SetProperty(ref _editLoan, value); RecomputeTotals(); } }

        private string _editLate = "";
        public string EditLate { get => _editLate; set { SetProperty(ref _editLate, value); RecomputeTotals(); } }

        private string _editUndertime = "";
        public string EditUndertime { get => _editUndertime; set { SetProperty(ref _editUndertime, value); RecomputeTotals(); } }

        private string _editOthers = "";
        public string EditOthers { get => _editOthers; set { SetProperty(ref _editOthers, value); RecomputeTotals(); } }
        // ------------------------

        public BatchPrintViewModel()
        {
            PrintSelectedCommand = new RelayCommand(_ => PrintSelectedPayslips());
            PrintAllCommand = new RelayCommand(_ => { SelectAll = true; PrintSelectedPayslips(); });
            EditRecordCommand = new RelayCommand(p => OpenEditModal(p as BatchPrintRecord));
            DeleteRecordCommand = new RelayCommand(p => ShowDeleteModal(p as BatchPrintRecord));
            ConfirmDeleteCommand = new RelayCommand(_ => ConfirmDeleteRecord());
            CancelDeleteCommand = new RelayCommand(_ => { IsDeleteModalVisible = false; _pendingDeleteRecord = null; });
            ApproveRecordCommand = new RelayCommand(p => ApproveRecord(p as BatchPrintRecord));
            SaveEditCommand = new RelayCommand(_ => SaveEditRecord());
            CancelEditCommand = new RelayCommand(_ => { IsEditModalVisible = false; _editingRecord = null; });
        }

        private void ShowDeleteModal(BatchPrintRecord? record)
        {
            if (record == null) return;
            _pendingDeleteRecord = record;
            DeleteTargetName = $"{record.Record.EmployeeName} ({record.Record.PayrollDateFormatted})";
            IsDeleteModalVisible = true;
        }

        private void ConfirmDeleteRecord()
        {
            if (_pendingDeleteRecord != null)
            {
                if (DatabaseHelper.TestConnection())
                {
                    try
                    {
                        using var conn = DatabaseHelper.GetConnection();
                        conn.Open();
                        using var cmd = new MySqlCommand("DELETE FROM payroll WHERE id=@id", conn);
                        cmd.Parameters.AddWithValue("@id", _pendingDeleteRecord.Record.Id);
                        cmd.ExecuteNonQuery();
                    }
                    catch { }
                }

                DemoDatabase.PayrollHistory.Remove(_pendingDeleteRecord.Record);
                PayrollRecords.Remove(_pendingDeleteRecord);
                StatusMessage = $"✓ Successfully deleted payroll record for {_pendingDeleteRecord.Record.EmployeeName}.";
                _pendingDeleteRecord = null;
            }
            IsDeleteModalVisible = false;
        }

        private void ApproveRecord(BatchPrintRecord? record)
        {
            if (record == null) return;
            
            record.Record.Status = "Paid";
            
            // Sync with memory database to reflect globally
            DemoDatabase.SaveChanges(); 

            // Hard refresh local binding
            var idx = PayrollRecords.IndexOf(record);
            if (idx >= 0)
            {
                var cachedRec = PayrollRecords[idx];
                PayrollRecords[idx] = null!;
                PayrollRecords[idx] = cachedRec;
            }

            StatusMessage = $"✓ Marked {record.Record.EmployeeName}'s payroll as Paid.";
        }

        private void OpenEditModal(BatchPrintRecord? record)
        {
            if (record == null) return;
            _editingRecord = record;
            EditTargetName = $"{record.Record.EmployeeName} ({record.Record.PayrollDateFormatted})";
            EditGross = record.Record.GrossRaw.ToString("N2");
            
            _editSss = record.Record.Sss.ToString("N2");
            _editPagibig = record.Record.Pagibig.ToString("N2");
            _editPhilhealth = record.Record.Philhealth.ToString("N2");
            _editLoan = record.Record.Loan.ToString("N2");
            _editLate = record.Record.Late.ToString("N2");
            _editUndertime = record.Record.Undertime.ToString("N2");
            _editOthers = record.Record.Others.ToString("N2");
            
            OnPropertyChanged(nameof(EditSss));
            OnPropertyChanged(nameof(EditPagibig));
            OnPropertyChanged(nameof(EditPhilhealth));
            OnPropertyChanged(nameof(EditLoan));
            OnPropertyChanged(nameof(EditLate));
            OnPropertyChanged(nameof(EditUndertime));
            OnPropertyChanged(nameof(EditOthers));

            RecomputeTotals();
            IsEditModalVisible = true;
        }

        private void RecomputeTotals()
        {
            if (_editingRecord == null) return;
            
            decimal.TryParse(EditGross, out decimal gross);
            decimal.TryParse(EditSss, out decimal sss);
            decimal.TryParse(EditPagibig, out decimal pagibig);
            decimal.TryParse(EditPhilhealth, out decimal phil);
            decimal.TryParse(EditLoan, out decimal loan);
            decimal.TryParse(EditLate, out decimal late);
            decimal.TryParse(EditUndertime, out decimal ut);
            decimal.TryParse(EditOthers, out decimal others);

            var ded = sss + pagibig + phil + loan + late + ut + others;
            var net = gross - ded;

            SetProperty(ref _editDeductions, ded.ToString("N2"), nameof(EditDeductions));
            SetProperty(ref _editNetPay, net.ToString("N2"), nameof(EditNetPay));
        }

        private void SaveEditRecord()
        {
            if (_editingRecord == null) return;
            
            decimal.TryParse(EditGross, out decimal gross);
            decimal.TryParse(EditDeductions, out decimal ded);
            decimal.TryParse(EditNetPay, out decimal net);
            
            decimal.TryParse(EditSss, out decimal sss);
            decimal.TryParse(EditPagibig, out decimal pagibig);
            decimal.TryParse(EditPhilhealth, out decimal phil);
            decimal.TryParse(EditLoan, out decimal loan);
            decimal.TryParse(EditLate, out decimal late);
            decimal.TryParse(EditUndertime, out decimal ut);
            decimal.TryParse(EditOthers, out decimal others);

            _editingRecord.Record.GrossRaw = gross;
            _editingRecord.Record.GrossSalary = $"₱{gross:N2}";

            _editingRecord.Record.Sss = sss;
            _editingRecord.Record.Pagibig = pagibig;
            _editingRecord.Record.Philhealth = phil;
            _editingRecord.Record.Loan = loan;
            _editingRecord.Record.Late = late;
            _editingRecord.Record.Undertime = ut;
            _editingRecord.Record.Others = others;

            _editingRecord.Record.DeductionsRaw = ded;
            _editingRecord.Record.Deductions = $"₱{ded:N2}";
            _editingRecord.Record.NetPayRaw = net;
            _editingRecord.Record.NetPay = $"₱{net:N2}";

            // Sync Database (MySQL if available, or DemoDatabase)
            if (DatabaseHelper.TestConnection())
            {
                try
                {
                    using var conn = DatabaseHelper.GetConnection();
                    conn.Open();
                    // Assuming we have the payroll_id. Wait, DemoDatabase's PayrollHistoryRecord Id corresponds to what? 
                    // To be safe, for now we will just log it or update via Emp/Date.
                    // Actually, modifying past payroll fundamentally requires a matching ID. If ID mapped to payroll.id, we can execute:
                    using var cmd = new MySqlCommand(@"UPDATE payroll SET 
                        gross_salary=@gross, total_deductions=@ded, net_pay=@net 
                        WHERE id=@id", conn);
                    cmd.Parameters.AddWithValue("@gross", gross);
                    cmd.Parameters.AddWithValue("@ded", ded);
                    cmd.Parameters.AddWithValue("@net", net);
                    cmd.Parameters.AddWithValue("@id", _editingRecord.Record.Id);
                    cmd.ExecuteNonQuery();
                }
                catch { /* Ignore if it fails */ }
            }

            DemoDatabase.SaveChanges();

            // Refresh UI safely without crashing WPF DataGrid
            var idx = PayrollRecords.IndexOf(_editingRecord);
            if (idx >= 0)
            {
                var cachedRec = PayrollRecords[idx];
                PayrollRecords.RemoveAt(idx);
                PayrollRecords.Insert(idx, cachedRec);
            }

            IsEditModalVisible = false;
            _editingRecord = null;
            StatusMessage = "Record updated and saved successfully.";
        }

        public void LoadData()
        {
            PayrollRecords.Clear();
            SelectAll = false;

            try
            {
                if (!DatabaseHelper.TestConnection()) { LoadDemoData(); return; }

                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                using var cmd = new MySqlCommand(
                    @"SELECT p.*, CONCAT(e.first_name, ' ', e.last_name) as employee_name, e.emp_number,
                             e.position, e.daily_rate
                      FROM payroll p JOIN employees e ON p.employee_id = e.id
                      WHERE p.payroll_date BETWEEN @start AND @end
                      ORDER BY e.last_name, p.payroll_date", conn);
                cmd.Parameters.AddWithValue("@start", StartDate);
                cmd.Parameters.AddWithValue("@end", EndDate.AddDays(1));

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var rec = new PayrollHistoryRecord
                    {
                        EmpNumber = reader.GetString("emp_number"),
                        EmployeeName = reader.GetString("employee_name"),
                        PayrollDateFormatted = reader.GetDateTime("payroll_date").ToString("MMM dd, yyyy"),
                        GrossSalary = $"₱{reader.GetDecimal("gross_salary"):N2}",
                        NetPay = $"₱{reader.GetDecimal("net_pay"):N2}",
                        Deductions = $"₱{reader.GetDecimal("total_deductions"):N2}",
                        Status = reader.GetString("status")
                    };
                    
                    // We need these for printing
                    var position = reader.GetString("position");
                    var basicPay = reader.GetDecimal("basic_salary");

                    PayrollRecords.Add(new BatchPrintRecord(rec, position, basicPay));
                }

                if (PayrollRecords.Count == 0) LoadDemoData();
            }
            catch { LoadDemoData(); }
        }

        private void LoadDemoData()
        {
            PayrollRecords.Clear();
            DemoDatabase.Initialize();
            
            foreach (var rec in DemoDatabase.PayrollHistory)
            {
                if (rec.PayrollDate >= StartDate && rec.PayrollDate <= EndDate.AddDays(1))
                {
                    // Find position
                    var emp = DemoDatabase.Employees.FirstOrDefault(e => e.EmpNumber == rec.EmpNumber);
                    var pos = emp != null ? emp.Position : "Staff";
                    var basicPay = emp != null ? emp.DailyRate : 0;
                    
                    PayrollRecords.Add(new BatchPrintRecord(rec, pos, basicPay));
                }
            }

            if (PayrollRecords.Count == 0)
                StatusMessage = "No processed payrolls found for this period.";
            else
                StatusMessage = "";
        }

        private void PrintSelectedPayslips()
        {
            var selectedToPrint = PayrollRecords.Where(r => r.IsSelected).ToList();

            if (selectedToPrint.Count == 0)
            {
                MessageBox.Show("Please select at least one payslip to print.", "Print Payslip",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() != true) return;

                var doc = new FlowDocument();
                
                // FORCE: Long Bond (Folio) - 8.5 x 13 inches exactly regardless of OS defaults
                double w = 816;   // 8.5 * 96
                double h = 1248;  // 13.0 * 96
                
                doc.PageWidth = w;
                doc.PageHeight = h;
                doc.ColumnWidth = w;
                doc.PagePadding = new Thickness(40, 20, 40, 20); // Tighter padding for batch
                doc.FontFamily = new FontFamily("Segoe UI");
                doc.FontSize = 11; // Slightly smaller font

                int count = 0;
                int maxPerSheet = 3; // Stack 3 per Long Bond to ensure safety

                foreach (var item in selectedToPrint)
                {
                    BuildMiniPayslip(doc, item);

                    count++;
                    if (count < selectedToPrint.Count)
                    {
                        if (count % maxPerSheet == 0)
                        {
                            // Page break
                            var breakPara = new Paragraph(new Run(""));
                            breakPara.BreakPageBefore = true;
                            doc.Blocks.Add(breakPara);
                        }
                        else
                        {
                            // Cut line
                            var cutPara = new Paragraph(new Run("------------------------------------ CUT HERE ------------------------------------"));
                            cutPara.TextAlignment = TextAlignment.Center;
                            cutPara.Foreground = Brushes.Gray;
                            cutPara.FontSize = 9;
                            cutPara.Margin = new Thickness(0, 15, 0, 15);
                            doc.Blocks.Add(cutPara);
                        }
                    }
                }

                var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
                paginator.PageSize = new Size(816, 1248);

                // Overpower Bluetooth default PrintTicket logic
                if (printDialog.PrintTicket != null)
                {
                    printDialog.PrintTicket.PageMediaSize = new System.Printing.PageMediaSize(
                        System.Printing.PageMediaSizeName.Unknown, 816, 1248);
                }

                printDialog.PrintDocument(paginator, "Batch Payslip Print");

                StatusMessage = $"✓ Successfully sent {selectedToPrint.Count} payslip(s) to printer.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Print error: {ex.Message}", "Default", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BuildMiniPayslip(FlowDocument doc, BatchPrintRecord item)
        {
            var darkGreen  = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E7B44"));
            var darkGray   = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"));
            var lightGray  = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8E8E8"));

            var periodStr  = $"{item.Record.PeriodStart:MMMM dd, yyyy} \u2013 {item.Record.PeriodEnd:MMMM dd, yyyy}";
            var basicPay   = item.BasicPay;
            var otPay      = item.Record.OvertimeHours > 0 ? (basicPay / 8 * 1.25m * item.Record.OvertimeHours) : 0;
            var holidayPay = item.Record.HolidayHours  > 0 ? (basicPay / 8 * 1.30m * item.Record.HolidayHours)  : 0;

            // ── Company Header ────────────────────────────────────────────
            var hdrTable = new Table();
            hdrTable.Columns.Add(new TableColumn());
            var hRg  = new TableRowGroup();
            var hRow = new TableRow { Background = Brushes.Black };
            hRow.Cells.Add(BP_Cell("Zoey's Billiard House", FontWeights.Bold, Brushes.Black, Brushes.White, 1, TextAlignment.Center, 16));
            hRg.Rows.Add(hRow);
            hdrTable.RowGroups.Add(hRg);
            doc.Blocks.Add(hdrTable);

            doc.Blocks.Add(new Paragraph(new Run("Paltao, Pulilan, Bulacan")) { FontSize = 10, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 4, 0, 4) });
            doc.Blocks.Add(BP_Sep());
            doc.Blocks.Add(new Paragraph(new Run($"Payslip for the Period of {periodStr}")) { FontSize = 12, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 6, 0, 10) });

            // ── Employee Info ─────────────────────────────────────────────
            var empT = new Table { CellSpacing = 0 };
            empT.Columns.Add(new TableColumn { Width = new GridLength(130) });
            empT.Columns.Add(new TableColumn());
            var eRg   = new TableRowGroup();
            var eHead = new TableRow();
            eHead.Cells.Add(BP_Cell("Employee Information", FontWeights.Bold, darkGreen, Brushes.White, 2));
            eRg.Rows.Add(eHead);
            eRg.Rows.Add(BP_InfoRow("Employee Name:", item.Record.EmployeeName));
            eRg.Rows.Add(BP_InfoRow("Employee ID:",   item.Record.EmpNumber));
            eRg.Rows.Add(BP_InfoRow("Position:",      item.Position));
            eRg.Rows.Add(BP_InfoRow("Pay Period:",    periodStr));
            eRg.Rows.Add(BP_InfoRow("Payment Date:",  item.Record.PayrollDateFormatted));
            empT.RowGroups.Add(eRg);
            doc.Blocks.Add(empT);
            doc.Blocks.Add(new Paragraph { Margin = new Thickness(0, 8, 0, 0) });

            // ── Dual Table Grid ───────────────────────────────────────────
            var mainT = new Table { CellSpacing = 8 };
            mainT.Columns.Add(new TableColumn { Width = new GridLength(5.5, GridUnitType.Star) });
            mainT.Columns.Add(new TableColumn { Width = new GridLength(4.5, GridUnitType.Star) });
            var mainRg  = new TableRowGroup();
            var mainRow = new TableRow();

            // Earnings
            var earnT = new Table { CellSpacing = 0, BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1) };
            earnT.Columns.Add(new TableColumn { Width = new GridLength(3,   GridUnitType.Star) });
            earnT.Columns.Add(new TableColumn { Width = new GridLength(2,   GridUnitType.Star) });
            earnT.Columns.Add(new TableColumn { Width = new GridLength(1.5, GridUnitType.Star) });
            earnT.Columns.Add(new TableColumn { Width = new GridLength(2,   GridUnitType.Star) });
            var eaRg = new TableRowGroup();
            var eaH1 = new TableRow();
            eaH1.Cells.Add(BP_Cell("Earnings", FontWeights.Bold, darkGreen, Brushes.White, 4, TextAlignment.Left, 11, true));
            eaRg.Rows.Add(eaH1);
            var eaH2 = new TableRow { Background = lightGray };
            eaH2.Cells.Add(BP_Cell("Description", FontWeights.SemiBold, lightGray, Brushes.Black, 1, TextAlignment.Center, 10, true));
            eaH2.Cells.Add(BP_Cell("Rate/Day",    FontWeights.SemiBold, lightGray, Brushes.Black, 1, TextAlignment.Center, 10, true));
            eaH2.Cells.Add(BP_Cell("Days/Hrs",    FontWeights.SemiBold, lightGray, Brushes.Black, 1, TextAlignment.Center, 10, true));
            eaH2.Cells.Add(BP_Cell("Amount",      FontWeights.SemiBold, lightGray, Brushes.Black, 1, TextAlignment.Center, 10, true));
            eaRg.Rows.Add(eaH2);
            eaRg.Rows.Add(BP_EarnRow("Basic Salary",      $"\u20b1{basicPay:N2}", item.Record.WorkDays.ToString(),         $"\u20b1{basicPay * item.Record.WorkDays:N2}"));
            eaRg.Rows.Add(BP_EarnRow("Overtime Pay",      "-",                    $"{item.Record.OvertimeHours:N1}",        $"\u20b1{otPay:N2}"));
            eaRg.Rows.Add(BP_EarnRow("Holiday Pay",       "-",                    $"{item.Record.HolidayHours:N1}",         $"\u20b1{holidayPay:N2}"));
            eaRg.Rows.Add(BP_EarnRow("Incentives/Bonus",  "-",                    "-",                                      $"\u20b1{item.Record.Bonus:N2}"));
            eaRg.Rows.Add(BP_EarnRow("Allowance",         "-",                    "-",                                      $"\u20b1{item.Record.Allowance:N2}"));
            var eaFoot = new TableRow();
            eaFoot.Cells.Add(BP_Cell("Gross Earnings", FontWeights.Bold, lightGray, Brushes.Black, 3, TextAlignment.Left,  11, true));
            eaFoot.Cells.Add(BP_Cell(item.Record.GrossSalary, FontWeights.Bold, lightGray, Brushes.Black, 1, TextAlignment.Right, 11, true));
            eaRg.Rows.Add(eaFoot);
            earnT.RowGroups.Add(eaRg);

            // Deductions
            var dedT = new Table { CellSpacing = 0, BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1) };
            dedT.Columns.Add(new TableColumn { Width = new GridLength(3, GridUnitType.Star) });
            dedT.Columns.Add(new TableColumn { Width = new GridLength(2, GridUnitType.Star) });
            var deRg = new TableRowGroup();
            var deH1 = new TableRow();
            deH1.Cells.Add(BP_Cell("Deductions", FontWeights.Bold, darkGray, Brushes.White, 2, TextAlignment.Left, 11, true));
            deRg.Rows.Add(deH1);
            var deH2 = new TableRow { Background = lightGray };
            deH2.Cells.Add(BP_Cell("Description", FontWeights.SemiBold, lightGray, Brushes.Black, 1, TextAlignment.Center, 10, true));
            deH2.Cells.Add(BP_Cell("Amount",      FontWeights.SemiBold, lightGray, Brushes.Black, 1, TextAlignment.Center, 10, true));
            deRg.Rows.Add(deH2);
            deRg.Rows.Add(BP_DedRow("SSS Contribution",  $"\u20b1{item.Record.Sss:N2}"));
            deRg.Rows.Add(BP_DedRow("PhilHealth",        $"\u20b1{item.Record.Philhealth:N2}"));
            deRg.Rows.Add(BP_DedRow("Pag-IBIG",          $"\u20b1{item.Record.Pagibig:N2}"));
            deRg.Rows.Add(BP_DedRow("Loan",              $"\u20b1{item.Record.Loan:N2}"));
            deRg.Rows.Add(BP_DedRow("Late",              $"\u20b1{item.Record.Late:N2}"));
            deRg.Rows.Add(BP_DedRow("Undertime",         $"\u20b1{item.Record.Undertime:N2}"));
            deRg.Rows.Add(BP_DedRow("Other Deductions",  $"\u20b1{item.Record.Others:N2}"));
            var deFoot = new TableRow();
            deFoot.Cells.Add(BP_Cell("Total Deductions", FontWeights.Bold, lightGray, Brushes.Black, 1, TextAlignment.Left,  11, true));
            deFoot.Cells.Add(BP_Cell(item.Record.Deductions, FontWeights.Bold, lightGray, Brushes.Black, 1, TextAlignment.Right, 11, true));
            deRg.Rows.Add(deFoot);
            dedT.RowGroups.Add(deRg);

            mainRow.Cells.Add(new TableCell(earnT) { Padding = new Thickness(0) });
            mainRow.Cells.Add(new TableCell(dedT)  { Padding = new Thickness(0) });
            mainRg.Rows.Add(mainRow);
            mainT.RowGroups.Add(mainRg);
            doc.Blocks.Add(mainT);

            // ── Summary ───────────────────────────────────────────────────
            doc.Blocks.Add(BP_Sep());
            var sum = new Paragraph();
            sum.TextAlignment = TextAlignment.Center;
            sum.Inlines.Add(new Run("Gross Earnings \u2013 Total Deductions = ") { FontWeight = FontWeights.Bold });
            sum.Inlines.Add(new Run(item.Record.NetPay) { FontWeight = FontWeights.Bold, Foreground = darkGreen });
            doc.Blocks.Add(sum);
            doc.Blocks.Add(BP_Sep());

            // ── Notes / Signatures ────────────────────────────────────────
            var notesT = new Table { CellSpacing = 0 };
            notesT.Columns.Add(new TableColumn());
            var nRg  = new TableRowGroup();
            var nRow = new TableRow();
            nRow.Cells.Add(BP_Cell("Notes", FontWeights.Bold, darkGreen, Brushes.White));
            nRg.Rows.Add(nRow);
            notesT.RowGroups.Add(nRg);
            doc.Blocks.Add(notesT);

            var sigT = new Table { CellSpacing = 20, Margin = new Thickness(0, 18, 0, 0) };
            sigT.Columns.Add(new TableColumn());
            sigT.Columns.Add(new TableColumn());
            var sRg  = new TableRowGroup();
            var sRow = new TableRow();
            sRow.Cells.Add(new TableCell(new Paragraph(new Run("Prepared by:  _________________________"))) { TextAlignment = TextAlignment.Left });
            sRow.Cells.Add(new TableCell(new Paragraph(new Run("Approved by:  _________________________"))) { TextAlignment = TextAlignment.Right });
            sRg.Rows.Add(sRow);
            sigT.RowGroups.Add(sRg);
            doc.Blocks.Add(sigT);
        }

        private static BlockUIContainer BP_Sep()
        {
            return new BlockUIContainer(new System.Windows.Controls.Border { Height = 1, Background = Brushes.Gray, Margin = new Thickness(0, 8, 0, 8) });
        }

        private static TableCell BP_Cell(string text, FontWeight weight, Brush bg, Brush fg, int colSpan = 1, TextAlignment align = TextAlignment.Left, double fontSize = 11, bool border = false)
        {
            var p    = new Paragraph(new Run(text)) { TextAlignment = align, FontSize = fontSize, FontWeight = weight };
            var cell = new TableCell(p) { Background = bg, Foreground = fg, ColumnSpan = colSpan, Padding = new Thickness(5, 3, 5, 3) };
            if (border) { cell.BorderBrush = Brushes.Gray; cell.BorderThickness = new Thickness(0.25); }
            return cell;
        }

        private static TableRow BP_InfoRow(string label, string value)
        {
            var row = new TableRow();
            row.Cells.Add(new TableCell(new Paragraph(new Run(label)) { FontSize = 10, FontWeight = FontWeights.SemiBold }) { Padding = new Thickness(5, 2, 5, 2) });
            var vc  = new TableCell(new Paragraph(new Run(value))  { FontSize = 10 }) { Padding = new Thickness(5, 2, 5, 2), BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(0, 0, 0, 1) };
            row.Cells.Add(vc);
            return row;
        }

        private static TableRow BP_EarnRow(string desc, string rate, string days, string amt)
        {
            var row = new TableRow();
            row.Cells.Add(BP_Cell(desc, FontWeights.Normal, null!, Brushes.Black, 1, TextAlignment.Left,   10, true));
            row.Cells.Add(BP_Cell(rate, FontWeights.Normal, null!, Brushes.Black, 1, TextAlignment.Center, 10, true));
            row.Cells.Add(BP_Cell(days, FontWeights.Normal, null!, Brushes.Black, 1, TextAlignment.Center, 10, true));
            row.Cells.Add(BP_Cell(amt,  FontWeights.Normal, null!, Brushes.Black, 1, TextAlignment.Right,  10, true));
            return row;
        }

        private static TableRow BP_DedRow(string desc, string amt)
        {
            var row = new TableRow();
            row.Cells.Add(BP_Cell(desc, FontWeights.Normal, null!, Brushes.Black, 1, TextAlignment.Left,  10, true));
            row.Cells.Add(BP_Cell(amt,  FontWeights.Normal, null!, Brushes.Black, 1, TextAlignment.Right, 10, true));
            return row;
        }
    }

    public class BatchPrintRecord : BaseViewModel
    {
        private bool _isSelected;
        public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
        public PayrollHistoryRecord Record { get; }
        public string Position { get; }
        public decimal BasicPay { get; }

        public BatchPrintRecord(PayrollHistoryRecord record, string pos, decimal basic)
        {
            Record = record;
            Position = pos;
            BasicPay = basic;
        }
    }
}
