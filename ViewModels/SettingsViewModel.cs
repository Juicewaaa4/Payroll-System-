using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using PayrollSystem.Helpers;
using PayrollSystem.DataAccess;
using MySql.Data.MySqlClient;

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
        private string _backupStatusMessage = "";
        public string BackupStatusMessage { get => _backupStatusMessage; set => SetProperty(ref _backupStatusMessage, value); }

        private string _lastBackupInfo = "No backup created yet";
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
                if (!DatabaseHelper.TestConnection()) { LoadDemoUsers(); return; }

                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                using var cmd = new MySqlCommand("SELECT id, username, full_name, role FROM users WHERE is_active=1", conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Users.Add(new UserItem
                    {
                        Id = reader.GetInt32("id"),
                        Username = reader.GetString("username"),
                        FullName = reader.GetString("full_name"),
                        Role = reader.GetString("role"),
                        IsActive = true
                    });
                }

                // --- MAGIC SYNC ---
                DemoDatabase.Initialize();
                DemoDatabase.Users.Clear();
                foreach(var u in Users) { DemoDatabase.Users.Add(u); }
                DemoDatabase.SaveChanges();
            }
            catch { LoadDemoUsers(); }
        }

        private void LoadDemoUsers()
        {
            DemoDatabase.Initialize();
            foreach (var u in DemoDatabase.Users) if (u.IsActive) Users.Add(u);
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
                if (DatabaseHelper.TestConnection())
                {
                    using var conn = DatabaseHelper.GetConnection();
                    conn.Open();

                    if (_selectedUser == null) 
                    {
                        if (string.IsNullOrWhiteSpace(FormPassword)) FormPassword = "password123"; // default fallback
                        using var cmd = new MySqlCommand("INSERT INTO users (username, password_hash, full_name, role) VALUES (@u, @p, @f, @r)", conn);
                        cmd.Parameters.AddWithValue("@u", FormUsername);
                        cmd.Parameters.AddWithValue("@p", FormPassword);
                        cmd.Parameters.AddWithValue("@f", FormFullName);
                        cmd.Parameters.AddWithValue("@r", FormRole);
                        cmd.ExecuteNonQuery();
                    }
                    else
                    {
                        var updateQuery = "UPDATE users SET username=@u, full_name=@f, role=@r" +
                                        (!string.IsNullOrWhiteSpace(FormPassword) ? ", password_hash=@p" : "") +
                                        " WHERE id=@id";
                        using var cmd = new MySqlCommand(updateQuery, conn);
                        cmd.Parameters.AddWithValue("@u", FormUsername);
                        if (!string.IsNullOrWhiteSpace(FormPassword)) cmd.Parameters.AddWithValue("@p", FormPassword);
                        cmd.Parameters.AddWithValue("@f", FormFullName);
                        cmd.Parameters.AddWithValue("@r", FormRole);
                        cmd.Parameters.AddWithValue("@id", _selectedUser.Id);
                        cmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    if (_selectedUser == null)
                    {
                        var newId = DemoDatabase.Users.Count > 0 ? DemoDatabase.Users.Max(u => u.Id) + 1 : 1;
                        if (string.IsNullOrWhiteSpace(FormPassword)) FormPassword = "password123";
                        DemoDatabase.Users.Add(new UserItem
                        {
                            Id = newId, Username = FormUsername, PasswordHash = FormPassword,
                            FullName = FormFullName, Role = FormRole, IsActive = true
                        });
                    }
                    else
                    {
                        var existing = DemoDatabase.Users.FirstOrDefault(u => u.Id == _selectedUser.Id);
                        if (existing != null)
                        {
                            existing.Username = FormUsername;
                            existing.FullName = FormFullName;
                            existing.Role = FormRole;
                            if (!string.IsNullOrWhiteSpace(FormPassword)) existing.PasswordHash = FormPassword;
                        }
                    }
                    DemoDatabase.SaveChanges();
                }

                IsUserFormVisible = false;
                StatusMessage = "User saved successfully.";
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
                if (DatabaseHelper.TestConnection())
                {
                    using var conn = DatabaseHelper.GetConnection();
                    conn.Open();
                    using var cmd = new MySqlCommand("UPDATE users SET is_active=0 WHERE id=@id", conn);
                    cmd.Parameters.AddWithValue("@id", user.Id);
                    cmd.ExecuteNonQuery();
                }
                else
                {
                    var demoUser = DemoDatabase.Users.FirstOrDefault(u => u.Id == user.Id);
                    if (demoUser != null) { DemoDatabase.Users.Remove(demoUser); DemoDatabase.SaveChanges(); }
                }
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
                if (!DatabaseHelper.TestConnection()) { LoadDemoDepartments(); return; }

                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                using var cmd = new MySqlCommand("SELECT id, name, description FROM departments WHERE is_active=1", conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Departments.Add(new DepartmentItem
                    {
                        Id = reader.GetInt32("id"),
                        Name = reader.GetString("name"),
                        Description = reader.IsDBNull(2) ? "" : reader.GetString("description")
                    });
                }

                // --- MAGIC SYNC ---
                DemoDatabase.Initialize();
                DemoDatabase.Departments.Clear();
                foreach(var d in Departments) { DemoDatabase.Departments.Add(d); }
                DemoDatabase.SaveChanges();
            }
            catch { LoadDemoDepartments(); }
        }

        private void LoadDemoDepartments()
        {
            DemoDatabase.Initialize();
            foreach (var d in DemoDatabase.Departments) Departments.Add(d);
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
                if (DatabaseHelper.TestConnection())
                {
                    using var conn = DatabaseHelper.GetConnection();
                    conn.Open();

                    if (_selectedDept == null)
                    {
                        using var cmd = new MySqlCommand("INSERT INTO departments (name, description) VALUES (@n, @d)", conn);
                        cmd.Parameters.AddWithValue("@n", FormDeptName);
                        cmd.Parameters.AddWithValue("@d", FormDeptDesc);
                        cmd.ExecuteNonQuery();
                    }
                    else
                    {
                        using var cmd = new MySqlCommand("UPDATE departments SET name=@n, description=@d WHERE id=@id", conn);
                        cmd.Parameters.AddWithValue("@n", FormDeptName);
                        cmd.Parameters.AddWithValue("@d", FormDeptDesc);
                        cmd.Parameters.AddWithValue("@id", _selectedDept.Id);
                        cmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    if (_selectedDept == null)
                    {
                        var newId = DemoDatabase.Departments.Count > 0 ? DemoDatabase.Departments.Max(d => d.Id) + 1 : 1;
                        DemoDatabase.Departments.Add(new DepartmentItem
                        {
                            Id = newId, Name = FormDeptName, Description = FormDeptDesc
                        });
                    }
                    else
                    {
                        var existing = DemoDatabase.Departments.FirstOrDefault(d => d.Id == _selectedDept.Id);
                        if (existing != null)
                        {
                            existing.Name = FormDeptName;
                            existing.Description = FormDeptDesc;
                        }
                    }
                    DemoDatabase.SaveChanges();
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
                if (DatabaseHelper.TestConnection())
                {
                    using var conn = DatabaseHelper.GetConnection();
                    conn.Open();
                    using var cmd = new MySqlCommand("UPDATE departments SET is_active=0 WHERE id=@id", conn);
                    cmd.Parameters.AddWithValue("@id", dept.Id);
                    cmd.ExecuteNonQuery();
                }
                else
                {
                    var demoDept = DemoDatabase.Departments.FirstOrDefault(d => d.Id == dept.Id);
                    if (demoDept != null) { DemoDatabase.Departments.Remove(demoDept); DemoDatabase.SaveChanges(); }
                }
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
            DemoDatabase.Initialize();

            int empCount = DemoDatabase.Employees.Count;
            int payrollCount = DemoDatabase.PayrollHistory.Count;
            int userCount = DemoDatabase.Users.Count;
            int deptCount = DemoDatabase.Departments.Count;
            int bioCount = DemoDatabase.BiometricsImports.Count;

            DataStats = $"👥 {empCount} employees  •  💵 {payrollCount} payroll records  •  👤 {userCount} users  •  🏢 {deptCount} departments  •  📅 {bioCount} biometric imports";

            // Check for most recent backup in default location
            try
            {
                var backupFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PayrollSystem Backups");
                if (Directory.Exists(backupFolder))
                {
                    var latestBackup = Directory.GetFiles(backupFolder, "*.json")
                        .OrderByDescending(f => File.GetLastWriteTime(f))
                        .FirstOrDefault();

                    if (latestBackup != null)
                    {
                        var fileInfo = new FileInfo(latestBackup);
                        LastBackupInfo = $"📁 {fileInfo.Name}\n📅 {fileInfo.LastWriteTime:MMM dd, yyyy  h:mm tt}\n💾 {FormatFileSize(fileInfo.Length)}";
                    }
                    else
                    {
                        LastBackupInfo = "No backup files found in default location.";
                    }
                }
                else
                {
                    LastBackupInfo = "No backup files found. Create your first backup!";
                }
            }
            catch
            {
                LastBackupInfo = "Unable to check backup history.";
            }
        }

        private static string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / (1024.0 * 1024.0):F1} MB";
        }

        private void PerformBackup()
        {
            try
            {
                IsBackupInProgress = true;
                BackupStatusMessage = "Preparing backup...";

                DemoDatabase.Initialize();

                // Build the backup data object
                var backupData = new BackupData
                {
                    BackupTimestamp = DateTime.Now,
                    AppVersion = "1.0",
                    Employees = DemoDatabase.Employees.ToList(),
                    PayrollHistory = DemoDatabase.PayrollHistory.ToList(),
                    Users = DemoDatabase.Users.ToList(),
                    Departments = DemoDatabase.Departments.ToList(),
                    BiometricsImports = DemoDatabase.BiometricsImports.ToList()
                };

                // Default folder
                var defaultFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PayrollSystem Backups");
                if (!Directory.Exists(defaultFolder)) Directory.CreateDirectory(defaultFolder);

                var saveDialog = new SaveFileDialog
                {
                    Title = "Save Payroll System Backup",
                    Filter = "JSON Backup (*.json)|*.json",
                    FileName = $"PayrollBackup_{DateTime.Now:yyyyMMdd_HHmmss}",
                    DefaultExt = ".json",
                    InitialDirectory = defaultFolder
                };

                if (saveDialog.ShowDialog() != true)
                {
                    IsBackupInProgress = false;
                    BackupStatusMessage = "";
                    return;
                }

                var opts = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(backupData, opts);
                File.WriteAllText(saveDialog.FileName, json);

                var fileInfo = new FileInfo(saveDialog.FileName);
                BackupStatusMessage = $"✅ Backup saved successfully!\n📁 {fileInfo.Name}  •  💾 {FormatFileSize(fileInfo.Length)}";
                LastBackupInfo = $"📁 {fileInfo.Name}\n📅 {fileInfo.LastWriteTime:MMM dd, yyyy  h:mm tt}\n💾 {FormatFileSize(fileInfo.Length)}";

                IsBackupInProgress = false;
            }
            catch (Exception ex)
            {
                BackupStatusMessage = $"❌ Backup failed: {ex.Message}";
                IsBackupInProgress = false;
            }
        }

        private void PerformRestore()
        {
            try
            {
                var defaultFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PayrollSystem Backups");

                var openDialog = new OpenFileDialog
                {
                    Title = "Select Payroll System Backup File",
                    Filter = "JSON Backup (*.json)|*.json",
                    DefaultExt = ".json",
                    InitialDirectory = Directory.Exists(defaultFolder) ? defaultFolder : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                };

                if (openDialog.ShowDialog() != true) return;

                // Read and validate
                var json = File.ReadAllText(openDialog.FileName);
                var backupData = JsonSerializer.Deserialize<BackupData>(json);

                if (backupData == null)
                {
                    BackupStatusMessage = "❌ Invalid backup file — could not parse data.";
                    return;
                }

                // Build summary for confirmation
                int empCount = backupData.Employees?.Count ?? 0;
                int payCount = backupData.PayrollHistory?.Count ?? 0;
                int usrCount = backupData.Users?.Count ?? 0;
                int dptCount = backupData.Departments?.Count ?? 0;
                int bioCount = backupData.BiometricsImports?.Count ?? 0;
                var backupDate = backupData.BackupTimestamp.ToString("MMM dd, yyyy  h:mm tt");

                var confirmResult = MessageBox.Show(
                    $"🔄 Restore from backup?\n\n" +
                    $"📅 Backup Date: {backupDate}\n" +
                    $"👥 Employees: {empCount}\n" +
                    $"💵 Payroll Records: {payCount}\n" +
                    $"👤 Users: {usrCount}\n" +
                    $"🏢 Departments: {dptCount}\n" +
                    $"📅 Biometric Imports: {bioCount}\n\n" +
                    $"⚠️ This will REPLACE all current data. Continue?",
                    "Confirm Restore",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirmResult != MessageBoxResult.Yes) return;

                IsBackupInProgress = true;
                BackupStatusMessage = "Restoring data...";

                // Restore all collections
                DemoDatabase.Initialize();

                DemoDatabase.Employees.Clear();
                if (backupData.Employees != null)
                    foreach (var emp in backupData.Employees) DemoDatabase.Employees.Add(emp);

                DemoDatabase.PayrollHistory.Clear();
                if (backupData.PayrollHistory != null)
                    foreach (var rec in backupData.PayrollHistory) DemoDatabase.PayrollHistory.Add(rec);

                DemoDatabase.Users.Clear();
                if (backupData.Users != null)
                    foreach (var usr in backupData.Users) DemoDatabase.Users.Add(usr);

                DemoDatabase.Departments.Clear();
                if (backupData.Departments != null)
                    foreach (var dept in backupData.Departments) DemoDatabase.Departments.Add(dept);

                DemoDatabase.BiometricsImports.Clear();
                if (backupData.BiometricsImports != null)
                    foreach (var bio in backupData.BiometricsImports) DemoDatabase.BiometricsImports.Add(bio);

                DemoDatabase.SaveChanges();

                BackupStatusMessage = $"✅ Restore completed successfully!\n📅 Restored from: {backupDate}\n👥 {empCount} employees  •  💵 {payCount} payroll records";
                LoadBackupInfo();

                IsBackupInProgress = false;
            }
            catch (Exception ex)
            {
                BackupStatusMessage = $"❌ Restore failed: {ex.Message}";
                IsBackupInProgress = false;
            }
        }

        // ═════════════════════════════════════════════════════════════
        // 13TH-MONTH PAY GENERATOR
        // ═════════════════════════════════════════════════════════════

        private void Generate13thMonthPay()
        {
            ThirteenthMonthRecords.Clear();
            ThirteenthMonthStatus = "";
            HasThirteenthMonthData = false;

            DemoDatabase.Initialize();

            // Get all payroll records for the selected year
            var yearRecords = DemoDatabase.PayrollHistory
                .Where(r => r.PayrollDate.Year == SelectedYear)
                .ToList();

            if (yearRecords.Count == 0)
            {
                ThirteenthMonthStatus = $"No payroll records found for year {SelectedYear}. Process payroll first.";
                TotalThirteenthMonthPay = "₱0.00";
                TotalBasicSalary = "₱0.00";
                TotalEmployeesComputed = 0;
                return;
            }

            // Group by employee and compute
            var grouped = yearRecords
                .GroupBy(r => r.EmpNumber)
                .Select(g =>
                {
                    var first = g.First();
                    // Total basic salary = sum of (DailyRate × WorkDays) for all records
                    // We use GrossRaw minus extras (OT, Holiday, Allowance, Bonus) to get base salary
                    decimal totalBasic = 0;
                    foreach (var rec in g)
                    {
                        // Find the employee's daily rate from DemoDatabase
                        var emp = DemoDatabase.Employees.FirstOrDefault(e => e.EmpNumber == rec.EmpNumber);
                        decimal dailyRate = emp?.DailyRate ?? 0;
                        decimal basicForPeriod = dailyRate * rec.WorkDays;
                        totalBasic += basicForPeriod;
                    }

                    decimal thirteenthMonth = Math.Round(totalBasic / 12m, 2);

                    return new ThirteenthMonthRecord
                    {
                        EmpNumber = first.EmpNumber,
                        EmployeeName = first.EmployeeName,
                        PayrollCount = g.Count(),
                        TotalBasicSalary = totalBasic,
                        TotalBasicSalaryFormatted = $"₱{totalBasic:N2}",
                        ThirteenthMonthPay = thirteenthMonth,
                        ThirteenthMonthPayFormatted = $"₱{thirteenthMonth:N2}"
                    };
                })
                .OrderBy(r => r.EmpNumber)
                .ToList();

            foreach (var rec in grouped)
                ThirteenthMonthRecords.Add(rec);

            decimal grandTotalBasic = grouped.Sum(r => r.TotalBasicSalary);
            decimal grandTotal13th = grouped.Sum(r => r.ThirteenthMonthPay);

            TotalBasicSalary = $"₱{grandTotalBasic:N2}";
            TotalThirteenthMonthPay = $"₱{grandTotal13th:N2}";
            TotalEmployeesComputed = grouped.Count;
            HasThirteenthMonthData = grouped.Count > 0;

            ThirteenthMonthStatus = $"✅ Generated 13th-month pay for {grouped.Count} employee(s) — Year {SelectedYear}";
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

    // ═══════════════════════════════════════════════════════════
    // Supporting Models
    // ═══════════════════════════════════════════════════════════

    public class BackupData
    {
        public DateTime BackupTimestamp { get; set; }
        public string AppVersion { get; set; } = "1.0";
        public List<EmployeeItem>? Employees { get; set; }
        public List<PayrollHistoryRecord>? PayrollHistory { get; set; }
        public List<UserItem>? Users { get; set; }
        public List<DepartmentItem>? Departments { get; set; }
        public List<BiometricsImportRecord>? BiometricsImports { get; set; }
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
