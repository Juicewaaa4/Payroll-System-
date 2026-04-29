using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using PayrollSystem.Helpers;
using PayrollSystem.DataAccess;
using Microsoft.Data.Sqlite;
using PayrollSystem.Models;

namespace PayrollSystem.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private ObservableCollection<UserItem> _users = new();
        private ObservableCollection<DepartmentItem> _departments = new();

        public ObservableCollection<UserItem> Users { get => _users; set => SetProperty(ref _users, value); }
        public ObservableCollection<DepartmentItem> Departments { get => _departments; set => SetProperty(ref _departments, value); }

        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        // --- View states ---
        private bool _isUserFormVisible;
        public bool IsUserFormVisible { get => _isUserFormVisible; set => SetProperty(ref _isUserFormVisible, value); }

        private bool _isDeptFormVisible;
        public bool IsDeptFormVisible { get => _isDeptFormVisible; set => SetProperty(ref _isDeptFormVisible, value); }

        // --- Appearance ---
        public bool IsDarkMode
        {
            get => App.IsDarkMode();
            set
            {
                if (App.IsDarkMode() != value)
                {
                    App.ChangeTheme(value);
                    OnPropertyChanged(nameof(IsDarkMode));
                }
            }
        }

        // --- Current Tab (0=Users, 1=Departments, 2=Backup, 3=13thMonth)
        private int _selectedTabIndex = 0;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set { SetProperty(ref _selectedTabIndex, value); LoadData(); }
        }

        // --- User Form Data ---
        private UserItem? _selectedUser;
        public string FormUsername { get; set; } = "";
        public string FormFullName { get; set; } = "";
        public string FormPassword { get; set; } = "";
        public string FormRole { get; set; } = "Staff";

        // --- Dept Form Data ---
        private DepartmentItem? _selectedDept;
        public string FormDeptName { get; set; } = "";
        public string FormDeptDesc { get; set; } = "";

        // ═══════════════════════════════════════════════════════════
        // BACKUP & RESTORE properties
        // ═══════════════════════════════════════════════════════════
        private string _backupStatusMessage = "Automatic backups are enabled. Database is backed up automatically on exit.";
        public string BackupStatusMessage { get => _backupStatusMessage; set => SetProperty(ref _backupStatusMessage, value); }

        private string _lastBackupInfo = "Backups are stored in the 'Backups' folder beside the executable.";
        public string LastBackupInfo { get => _lastBackupInfo; set => SetProperty(ref _lastBackupInfo, value); }

        private string _dataStats = "";
        public string DataStats { get => _dataStats; set => SetProperty(ref _dataStats, value); }

        private bool _isBackupInProgress;
        public bool IsBackupInProgress { get => _isBackupInProgress; set => SetProperty(ref _isBackupInProgress, value); }

        // ═══════════════════════════════════════════════════════════
        // 13TH-MONTH PAY properties
        // ═══════════════════════════════════════════════════════════
        private int _selectedYear;
        public int SelectedYear
        {
            get => _selectedYear;
            set { SetProperty(ref _selectedYear, value); }
        }

        public ObservableCollection<int> AvailableYears { get; } = new();
        public ObservableCollection<ThirteenthMonthRecord> ThirteenthMonthRecords { get; } = new();

        private string _thirteenthMonthStatus = "";
        public string ThirteenthMonthStatus { get => _thirteenthMonthStatus; set => SetProperty(ref _thirteenthMonthStatus, value); }

        private string _totalThirteenthMonthPay = "₱0.00";
        public string TotalThirteenthMonthPay { get => _totalThirteenthMonthPay; set => SetProperty(ref _totalThirteenthMonthPay, value); }

        private string _totalBasicSalary = "₱0.00";
        public string TotalBasicSalary { get => _totalBasicSalary; set => SetProperty(ref _totalBasicSalary, value); }

        private int _totalEmployeesComputed;
        public int TotalEmployeesComputed { get => _totalEmployeesComputed; set => SetProperty(ref _totalEmployeesComputed, value); }

        private bool _hasThirteenthMonthData;
        public bool HasThirteenthMonthData { get => _hasThirteenthMonthData; set => SetProperty(ref _hasThirteenthMonthData, value); }

        // Commands — Users
        public ICommand AddUserCommand { get; }
        public ICommand EditUserCommand { get; }
        public ICommand SaveUserCommand { get; }
        public ICommand CancelUserCommand { get; }
        public ICommand DeleteUserCommand { get; }

        // Commands — Departments
        public ICommand AddDeptCommand { get; }
        public ICommand EditDeptCommand { get; }
        public ICommand SaveDeptCommand { get; }
        public ICommand CancelDeptCommand { get; }
        public ICommand DeleteDeptCommand { get; }

        // Commands — Backup & Restore
        public ICommand BackupCommand { get; }
        public ICommand RestoreCommand { get; }

        // Commands — 13th-Month Pay
        public ICommand Generate13thMonthCommand { get; }
        public ICommand Export13thMonthCommand { get; }

        public SettingsViewModel()
        {
            // --- User commands ---
            AddUserCommand = new RelayCommand(_ => { 
                _selectedUser = null; FormUsername = ""; FormFullName = ""; FormPassword = ""; FormRole = "Staff"; StatusMessage = ""; IsUserFormVisible = true; 
                OnPropertyChanged(nameof(FormUsername)); OnPropertyChanged(nameof(FormFullName)); OnPropertyChanged(nameof(FormPassword)); OnPropertyChanged(nameof(FormRole)); 
            });
            EditUserCommand = new RelayCommand(p => {
                if (p is UserItem u) {
                    _selectedUser = u; FormUsername = u.Username; FormFullName = u.FullName; FormPassword = ""; FormRole = u.Role; StatusMessage = ""; IsUserFormVisible = true;
                    OnPropertyChanged(nameof(FormUsername)); OnPropertyChanged(nameof(FormFullName)); OnPropertyChanged(nameof(FormPassword)); OnPropertyChanged(nameof(FormRole));
                }
            });
            CancelUserCommand = new RelayCommand(_ => { IsUserFormVisible = false; StatusMessage = ""; });
            SaveUserCommand = new RelayCommand(_ => SaveUser());
            DeleteUserCommand = new RelayCommand(p => DeleteUser(p as UserItem));

            // --- Dept commands ---
            AddDeptCommand = new RelayCommand(_ => { 
                _selectedDept = null; FormDeptName = ""; FormDeptDesc = ""; StatusMessage = ""; IsDeptFormVisible = true; 
                OnPropertyChanged(nameof(FormDeptName)); OnPropertyChanged(nameof(FormDeptDesc)); 
            });
            EditDeptCommand = new RelayCommand(p => {
                if (p is DepartmentItem d) {
                    _selectedDept = d; FormDeptName = d.Name; FormDeptDesc = d.Description; StatusMessage = ""; IsDeptFormVisible = true;
                    OnPropertyChanged(nameof(FormDeptName)); OnPropertyChanged(nameof(FormDeptDesc));
                }
            });
            CancelDeptCommand = new RelayCommand(_ => { IsDeptFormVisible = false; StatusMessage = ""; });
            SaveDeptCommand = new RelayCommand(_ => SaveDepartment());
            DeleteDeptCommand = new RelayCommand(p => DeleteDepartment(p as DepartmentItem));

            // --- Backup & Restore commands ---
            BackupCommand = new RelayCommand(_ => PerformBackup());
            RestoreCommand = new RelayCommand(_ => PerformRestore());

            // --- 13th-Month Pay commands ---
            Generate13thMonthCommand = new RelayCommand(_ => Generate13thMonthPay());
            Export13thMonthCommand = new RelayCommand(_ => Export13thMonthToExcel());

            // Initialize years
            _selectedYear = DateTime.Now.Year;
            for (int y = DateTime.Now.Year; y >= DateTime.Now.Year - 5; y--)
                AvailableYears.Add(y);
        }

        public void LoadData()
        {
            if (SelectedTabIndex == 0) LoadUsers();
            else if (SelectedTabIndex == 1) LoadDepartments();
            else if (SelectedTabIndex == 2) LoadBackupInfo();
            // Tab 3 loads on demand via Generate button
        }

        // ═════════════════ USERS CRUD ═════════════════ //

        private void LoadUsers()
        {
            Users.Clear();
            try
            {
                if (!DatabaseHelper.TestConnection()) return;

                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                using var cmd = new SqliteCommand("SELECT id, username, full_name, role FROM users WHERE is_active=1", conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Users.Add(new UserItem
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("id")),
                        Username = reader.GetString(reader.GetOrdinal("username")),
                        FullName = reader.GetString(reader.GetOrdinal("full_name")),
                        Role = reader.GetString(reader.GetOrdinal("role")),
                        IsActive = true
                    });
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading users: {ex.Message}";
            }
        }

        private void SaveUser()
        {
            if (string.IsNullOrWhiteSpace(FormUsername) || string.IsNullOrWhiteSpace(FormFullName))
            {
                StatusMessage = "Username and Full Name are required.";
                return;
            }

            try
            {
                if (!DatabaseHelper.TestConnection()) return;

                using var conn = DatabaseHelper.GetConnection();
                conn.Open();

                var hashedPwd = string.IsNullOrWhiteSpace(FormPassword) ? PayrollSystem.Utilities.PasswordHelper.HashPassword("password123") : PayrollSystem.Utilities.PasswordHelper.HashPassword(FormPassword);

                if (_selectedUser == null) 
                {
                    using var cmd = new SqliteCommand("INSERT INTO users (username, password_hash, full_name, role) VALUES (@u, @p, @f, @r)", conn);
                    cmd.Parameters.AddWithValue("@u", FormUsername);
                    cmd.Parameters.AddWithValue("@p", hashedPwd);
                    cmd.Parameters.AddWithValue("@f", FormFullName);
                    cmd.Parameters.AddWithValue("@r", FormRole);
                    cmd.ExecuteNonQuery();
                }
                else
                {
                    var updateQuery = "UPDATE users SET username=@u, full_name=@f, role=@r" +
                                      (!string.IsNullOrWhiteSpace(FormPassword) ? ", password_hash=@p" : "") +
                                      " WHERE id=@id";
                    using var cmd = new SqliteCommand(updateQuery, conn);
                    cmd.Parameters.AddWithValue("@u", FormUsername);
                    if (!string.IsNullOrWhiteSpace(FormPassword)) cmd.Parameters.AddWithValue("@p", hashedPwd);
                    cmd.Parameters.AddWithValue("@f", FormFullName);
                    cmd.Parameters.AddWithValue("@r", FormRole);
                    cmd.Parameters.AddWithValue("@id", _selectedUser.Id);
                    cmd.ExecuteNonQuery();
                }

                IsUserFormVisible = false;
                StatusMessage = "User saved successfully.";
                ShowToast("User saved successfully!");
                LoadUsers();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        private void DeleteUser(UserItem? user)
        {
            if (user == null) return;
            if (user.Username.ToLower() == "admin") { StatusMessage = "Cannot delete the default admin account."; return; }

            var result = System.Windows.MessageBox.Show(
                $"Are you sure you want to delete the user account '{user.Username}'?",
                "Confirm Delete",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result != System.Windows.MessageBoxResult.Yes) return;

            try
            {
                if (!DatabaseHelper.TestConnection()) return;

                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                using var cmd = new SqliteCommand("UPDATE users SET is_active=0 WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("@id", user.Id);
                cmd.ExecuteNonQuery();

                StatusMessage = "User deleted.";
                LoadUsers();
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
        }

        // ═════════════════ DEPARTMENTS CRUD ═════════════════ //

        private void LoadDepartments()
        {
            Departments.Clear();
            try
            {
                if (!DatabaseHelper.TestConnection()) return;

                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                using var cmd = new SqliteCommand("SELECT id, name, description FROM departments WHERE is_active=1", conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Departments.Add(new DepartmentItem
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("id")),
                        Name = reader.GetString(reader.GetOrdinal("name")),
                        Description = reader.IsDBNull(2) ? "" : reader.GetString(reader.GetOrdinal("description"))
                    });
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading departments: {ex.Message}";
            }
        }

        private void SaveDepartment()
        {
            if (string.IsNullOrWhiteSpace(FormDeptName))
            {
                StatusMessage = "Department Name is required.";
                return;
            }

            try
            {
                if (!DatabaseHelper.TestConnection()) return;

                using var conn = DatabaseHelper.GetConnection();
                conn.Open();

                if (_selectedDept == null)
                {
                    using var cmd = new SqliteCommand("INSERT INTO departments (name, description) VALUES (@n, @d)", conn);
                    cmd.Parameters.AddWithValue("@n", FormDeptName);
                    cmd.Parameters.AddWithValue("@d", FormDeptDesc);
                    cmd.ExecuteNonQuery();
                }
                else
                {
                    using var cmd = new SqliteCommand("UPDATE departments SET name=@n, description=@d WHERE id=@id", conn);
                    cmd.Parameters.AddWithValue("@n", FormDeptName);
                    cmd.Parameters.AddWithValue("@d", FormDeptDesc);
                    cmd.Parameters.AddWithValue("@id", _selectedDept.Id);
                    cmd.ExecuteNonQuery();
                }

                IsDeptFormVisible = false;
                StatusMessage = "Department saved successfully.";
                LoadDepartments();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        private void DeleteDepartment(DepartmentItem? dept)
        {
            if (dept == null) return;

            var result = System.Windows.MessageBox.Show(
                $"Are you sure you want to delete the department '{dept.Name}'?",
                "Confirm Delete",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result != System.Windows.MessageBoxResult.Yes) return;

            try
            {
                if (!DatabaseHelper.TestConnection()) return;

                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                using var cmd = new SqliteCommand("UPDATE departments SET is_active=0 WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("@id", dept.Id);
                cmd.ExecuteNonQuery();

                StatusMessage = "Department deleted.";
                LoadDepartments();
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
        }

        // ═════════════════════════════════════════════════════════════
        // BACKUP & RESTORE
        // ═════════════════════════════════════════════════════════════

        private void LoadBackupInfo()
        {
            try
            {
                if (!DatabaseHelper.TestConnection()) return;
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();

                using var cmdEmp = new SqliteCommand("SELECT COUNT(*) FROM employees WHERE is_active=1", conn);
                int empCount = Convert.ToInt32(cmdEmp.ExecuteScalar());

                using var cmdPay = new SqliteCommand("SELECT COUNT(*) FROM payroll", conn);
                int payrollCount = Convert.ToInt32(cmdPay.ExecuteScalar());

                using var cmdUsr = new SqliteCommand("SELECT COUNT(*) FROM users WHERE is_active=1", conn);
                int userCount = Convert.ToInt32(cmdUsr.ExecuteScalar());

                using var cmdDept = new SqliteCommand("SELECT COUNT(*) FROM departments WHERE is_active=1", conn);
                int deptCount = Convert.ToInt32(cmdDept.ExecuteScalar());

                DataStats = $"👥 {empCount} employees  •  💵 {payrollCount} payroll records  •  👤 {userCount} users  •  🏢 {deptCount} departments";
            }
            catch
            {
                DataStats = "Unable to fetch stats.";
            }
        }

        private void PerformBackup()
        {
            try 
            {
                DataAccess.DatabaseHelper.BackupDatabase();
                BackupStatusMessage = "✓ Database backed up successfully to the 'Backups' folder.";
                ShowToast("Manual backup completed!");
            }
            catch (Exception ex)
            {
                BackupStatusMessage = $"Error: {ex.Message}";
            }
        }

        private void PerformRestore()
        {
            BackupStatusMessage = "To restore a backup, please close the application, navigate to the 'Backups' folder, and copy the desired backup over 'payroll.db'.";
            ShowToast("Please check instructions on screen", "ℹ️");
        }

        // ═════════════════════════════════════════════════════════════
        // 13TH-MONTH PAY GENERATOR
        // ═════════════════════════════════════════════════════════════

        private void Generate13thMonthPay()
        {
            ThirteenthMonthRecords.Clear();
            ThirteenthMonthStatus = "";
            HasThirteenthMonthData = false;

            if (!DatabaseHelper.TestConnection())
            {
                ThirteenthMonthStatus = "Database connection required.";
                return;
            }

            try
            {
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();

                // Group by employee and compute
                using var cmd = new SqliteCommand(@"
                    SELECT e.emp_number, e.first_name || ' ' || e.last_name as employee_name, 
                           COUNT(p.id) as payroll_count, 
                           SUM(p.basic_salary * p.work_days) as total_basic 
                    FROM payroll p 
                    JOIN employees e ON p.employee_id = e.id 
                    WHERE strftime('%Y', p.payroll_date) = @y 
                    GROUP BY e.emp_number, employee_name 
                    ORDER BY e.emp_number", conn);
                cmd.Parameters.AddWithValue("@y", SelectedYear.ToString());

                using var reader = cmd.ExecuteReader();
                decimal grandTotalBasic = 0;
                decimal grandTotal13th = 0;

                while (reader.Read())
                {
                    decimal totalBasic = reader.GetDecimal(reader.GetOrdinal("total_basic"));
                    decimal thirteenthMonth = Math.Round(totalBasic / 12m, 2);

                    ThirteenthMonthRecords.Add(new ThirteenthMonthRecord
                    {
                        EmpNumber = reader.GetString(reader.GetOrdinal("emp_number")),
                        EmployeeName = reader.GetString(reader.GetOrdinal("employee_name")),
                        PayrollCount = reader.GetInt32(reader.GetOrdinal("payroll_count")),
                        TotalBasicSalary = totalBasic,
                        TotalBasicSalaryFormatted = $"₱{totalBasic:N2}",
                        ThirteenthMonthPay = thirteenthMonth,
                        ThirteenthMonthPayFormatted = $"₱{thirteenthMonth:N2}"
                    });

                    grandTotalBasic += totalBasic;
                    grandTotal13th += thirteenthMonth;
                }

                TotalBasicSalary = $"₱{grandTotalBasic:N2}";
                TotalThirteenthMonthPay = $"₱{grandTotal13th:N2}";
                TotalEmployeesComputed = ThirteenthMonthRecords.Count;
                HasThirteenthMonthData = ThirteenthMonthRecords.Count > 0;

                if (HasThirteenthMonthData)
                    ThirteenthMonthStatus = $"✅ Generated 13th-month pay for {ThirteenthMonthRecords.Count} employee(s) — Year {SelectedYear}";
                else
                    ThirteenthMonthStatus = $"No payroll records found for year {SelectedYear}.";

            }
            catch (Exception ex)
            {
                ThirteenthMonthStatus = $"Error: {ex.Message}";
            }
        }

        private void Export13thMonthToExcel()
        {
            if (ThirteenthMonthRecords.Count == 0)
            {
                ThirteenthMonthStatus = "No data to export. Generate 13th-month pay first.";
                return;
            }

            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Title = "Export 13th-Month Pay Report",
                    Filter = "Excel XML (*.xlsx)|*.xlsx|CSV Files (*.csv)|*.csv",
                    FileName = $"13thMonth_Pay_{SelectedYear}_{DateTime.Now:yyyyMMdd}",
                    DefaultExt = ".xlsx"
                };

                if (saveDialog.ShowDialog() != true) return;

                var filePath = saveDialog.FileName;

                if (filePath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                    Export13thMonthAsXlsx(filePath);
                else
                    Export13thMonthAsCsv(filePath);

                ThirteenthMonthStatus = $"✅ Exported successfully: {Path.GetFileName(filePath)}";
            }
            catch (Exception ex)
            {
                ThirteenthMonthStatus = $"❌ Export error: {ex.Message}";
            }
        }

        private void Export13thMonthAsCsv(string filePath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Zoey's Billiard House - 13th-Month Pay Report");
            sb.AppendLine($"Year: {SelectedYear}");
            sb.AppendLine($"Generated: {DateTime.Now:MMM dd, yyyy  hh:mm tt}");
            sb.AppendLine();
            sb.AppendLine("EMP #,Employee Name,Payroll Count,Total Basic Salary,13th-Month Pay");

            foreach (var rec in ThirteenthMonthRecords)
            {
                sb.AppendLine($"{rec.EmpNumber},\"{rec.EmployeeName}\",{rec.PayrollCount},{rec.TotalBasicSalary:F2},{rec.ThirteenthMonthPay:F2}");
            }

            sb.AppendLine();
            sb.AppendLine($",,TOTALS,{ThirteenthMonthRecords.Sum(r => r.TotalBasicSalary):F2},{ThirteenthMonthRecords.Sum(r => r.ThirteenthMonthPay):F2}");

            File.WriteAllText(filePath, sb.ToString());
        }

        private void Export13thMonthAsXlsx(string filePath)
        {
            var sharedStrings = new List<string>();
            int SharedStr(string s) { if (!sharedStrings.Contains(s)) sharedStrings.Add(s); return sharedStrings.IndexOf(s); }

            var rows = new StringBuilder();

            rows.Append("<row r=\"1\"><c r=\"A1\" t=\"s\" s=\"1\"><v>" + SharedStr("Zoey's Billiard House - 13th-Month Pay Report") + "</v></c></row>");
            rows.Append("<row r=\"2\"><c r=\"A2\" t=\"s\" s=\"2\"><v>" + SharedStr($"Year: {SelectedYear}") + "</v></c></row>");
            rows.Append("<row r=\"3\"><c r=\"A3\" t=\"s\" s=\"2\"><v>" + SharedStr($"Generated: {DateTime.Now:MMM dd, yyyy hh:mm tt}") + "</v></c></row>");
            rows.Append("<row r=\"4\"><c r=\"A4\" t=\"s\" s=\"2\"><v>" + SharedStr("Formula: Total Basic Salary Earned ÷ 12") + "</v></c></row>");
            rows.Append("<row r=\"5\"></row>");

            string[] headers = { "EMP #", "Employee Name", "Payroll Count", "Total Basic Salary", "13th-Month Pay" };
            rows.Append("<row r=\"6\">");
            for (int i = 0; i < headers.Length; i++)
            {
                var col = GetColLetter(i);
                rows.Append($"<c r=\"{col}6\" t=\"s\" s=\"3\"><v>{SharedStr(headers[i])}</v></c>");
            }
            rows.Append("</row>");

            int rowNum = 7;
            int dataIdx = 0;
            foreach (var rec in ThirteenthMonthRecords)
            {
                string sText = (dataIdx % 2 == 1) ? "8" : "4";
                string sNum = (dataIdx % 2 == 1) ? "9" : "5";

                rows.Append($"<row r=\"{rowNum}\">");
                rows.Append($"<c r=\"A{rowNum}\" t=\"s\" s=\"{sText}\"><v>{SharedStr(rec.EmpNumber)}</v></c>");
                rows.Append($"<c r=\"B{rowNum}\" t=\"s\" s=\"{sText}\"><v>{SharedStr(rec.EmployeeName)}</v></c>");
                rows.Append($"<c r=\"C{rowNum}\" s=\"{sText}\"><v>{rec.PayrollCount}</v></c>");
                rows.Append($"<c r=\"D{rowNum}\" s=\"{sNum}\"><v>{rec.TotalBasicSalary}</v></c>");
                rows.Append($"<c r=\"E{rowNum}\" s=\"{sNum}\"><v>{rec.ThirteenthMonthPay}</v></c>");
                rows.Append("</row>");
                rowNum++;
                dataIdx++;
            }

            // Totals row
            rows.Append($"<row r=\"{rowNum}\">");
            rows.Append($"<c r=\"B{rowNum}\" t=\"s\" s=\"6\"><v>{SharedStr("TOTALS")}</v></c>");
            rows.Append($"<c r=\"C{rowNum}\" s=\"6\"><v>{ThirteenthMonthRecords.Count}</v></c>");
            rows.Append($"<c r=\"D{rowNum}\" s=\"7\"><v>{ThirteenthMonthRecords.Sum(r => r.TotalBasicSalary)}</v></c>");
            rows.Append($"<c r=\"E{rowNum}\" s=\"7\"><v>{ThirteenthMonthRecords.Sum(r => r.ThirteenthMonthPay)}</v></c>");
            rows.Append("</row>");

            var sheetData = $"<sheetData>{rows}</sheetData>";
            var cols = "<cols><col min=\"1\" max=\"1\" width=\"15\" customWidth=\"1\"/><col min=\"2\" max=\"2\" width=\"30\" customWidth=\"1\"/><col min=\"3\" max=\"3\" width=\"18\" customWidth=\"1\"/><col min=\"4\" max=\"5\" width=\"22\" customWidth=\"1\"/></cols>";

            BuildXlsxPackage(filePath, sheetData, cols, sharedStrings, "13th-Month Pay");
        }

        private static string GetColLetter(int idx)
        {
            if (idx < 26) return ((char)('A' + idx)).ToString();
            return ((char)('A' + idx / 26 - 1)).ToString() + ((char)('A' + idx % 26)).ToString();
        }

        private void BuildXlsxPackage(string filePath, string sheetData, string cols, List<string> sharedStrings, string sheetName)
        {
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
    <numFmt numFmtId=""164"" formatCode=""_(""₱""* #,##0.00_);_(""₱""* \(#,##0.00\);_(""₱""* ""-""??_);_(@_)""/>
  </numFmts>
  <fonts count=""4"">
    <font><sz val=""11""/><name val=""Calibri""/></font>
    <font><b/><sz val=""14""/><color rgb=""FF1B5E20""/><name val=""Calibri""/></font>
    <font><b/><sz val=""12""/><name val=""Calibri""/></font>
    <font><b/><sz val=""11""/><color rgb=""FFFFFFFF""/><name val=""Calibri""/></font>
  </fonts>
  <fills count=""5"">
    <fill><patternFill patternType=""none""/></fill>
    <fill><patternFill patternType=""gray125""/></fill>
    <fill><patternFill patternType=""solid""><fgColor rgb=""FF2E7D32""/><bgColor indexed=""64""/></patternFill></fill>
    <fill><patternFill patternType=""solid""><fgColor rgb=""FFF2F2F2""/><bgColor indexed=""64""/></patternFill></fill>
    <fill><patternFill patternType=""solid""><fgColor rgb=""FFE8F5E9""/><bgColor indexed=""64""/></patternFill></fill>
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
  <cellXfs count=""10"">
    <xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""0"" xfId=""0""/>
    <xf numFmtId=""0"" fontId=""1"" fillId=""0"" borderId=""0"" xfId=""0"" applyFont=""1""/>
    <xf numFmtId=""0"" fontId=""2"" fillId=""0"" borderId=""0"" xfId=""0"" applyFont=""1""/>
    <xf numFmtId=""0"" fontId=""3"" fillId=""2"" borderId=""1"" xfId=""0"" applyFont=""1"" applyFill=""1"" applyBorder=""1""/>
    <xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""1"" xfId=""0"" applyBorder=""1""/>
    <xf numFmtId=""164"" fontId=""0"" fillId=""0"" borderId=""1"" xfId=""0"" applyNumberFormat=""1"" applyBorder=""1""/>
    <xf numFmtId=""0"" fontId=""2"" fillId=""3"" borderId=""1"" xfId=""0"" applyFont=""1"" applyFill=""1"" applyBorder=""1""/>
    <xf numFmtId=""164"" fontId=""2"" fillId=""3"" borderId=""1"" xfId=""0"" applyNumberFormat=""1"" applyFont=""1"" applyFill=""1"" applyBorder=""1""/>
    <xf numFmtId=""0"" fontId=""0"" fillId=""4"" borderId=""1"" xfId=""0"" applyFill=""1"" applyBorder=""1""/>
    <xf numFmtId=""164"" fontId=""0"" fillId=""4"" borderId=""1"" xfId=""0"" applyNumberFormat=""1"" applyFill=""1"" applyBorder=""1""/>
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

            var escaped_name = System.Security.SecurityElement.Escape(sheetName) ?? "Sheet1";
            var workbook = $@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<workbook xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"" xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships"">
<sheets><sheet name=""{escaped_name}"" sheetId=""1"" r:id=""rId1""/></sheets></workbook>";

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
    }

    public class ThirteenthMonthRecord
    {
        public string EmpNumber { get; set; } = "";
        public string EmployeeName { get; set; } = "";
        public int PayrollCount { get; set; }
        public decimal TotalBasicSalary { get; set; }
        public string TotalBasicSalaryFormatted { get; set; } = "";
        public decimal ThirteenthMonthPay { get; set; }
        public string ThirteenthMonthPayFormatted { get; set; } = "";
    }
}
