using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using PayrollSystem.ViewModels;

namespace PayrollSystem.DataAccess
{
    /// <summary>
    /// Centralized local data store for offline mode (when MySQL is not running).
    /// Now saves permanently to JSON files so your edits survive app restarts!
    /// </summary>
    public static class DemoDatabase
    {
        private static bool _initialized = false;
        private static readonly string DataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PayrollData");
        private static readonly string EmployeesFile = Path.Combine(DataFolder, "employees.json");
        private static readonly string PayrollFile = Path.Combine(DataFolder, "payroll_history.json");
        private static readonly string UsersFile = Path.Combine(DataFolder, "users.json");
        private static readonly string DepartmentsFile = Path.Combine(DataFolder, "departments.json");
        private static readonly string BiometricsFile = Path.Combine(DataFolder, "biometrics_imports.json");
        private static readonly string AuditFile = Path.Combine(DataFolder, "audit_logs.json");

        public static ObservableCollection<EmployeeItem> Employees { get; private set; } = new();
        public static ObservableCollection<PayrollHistoryRecord> PayrollHistory { get; private set; } = new();
        public static ObservableCollection<UserItem> Users { get; private set; } = new();
        public static ObservableCollection<DepartmentItem> Departments { get; private set; } = new();
        public static ObservableCollection<BiometricsImportRecord> BiometricsImports { get; private set; } = new();
        public static ObservableCollection<AuditLogRecord> AuditLogs { get; private set; } = new();

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                if (!Directory.Exists(DataFolder)) Directory.CreateDirectory(DataFolder);

                // Auto-migrate from old AppData location if this is a fresh portable install
                MigrateFromAppDataIfNeeded();

                // Load Users
                if (File.Exists(UsersFile))
                {
                    var loaded = JsonSerializer.Deserialize<ObservableCollection<UserItem>>(File.ReadAllText(UsersFile));
                    if (loaded != null) Users = loaded;
                }
                else SeedDefaultUsers();

                // Load Departments
                if (File.Exists(DepartmentsFile))
                {
                    var loaded = JsonSerializer.Deserialize<ObservableCollection<DepartmentItem>>(File.ReadAllText(DepartmentsFile));
                    if (loaded != null) Departments = loaded;
                }
                else SeedDefaultDepartments();

                // Load Employees
                if (File.Exists(EmployeesFile))
                {
                    var loaded = JsonSerializer.Deserialize<ObservableCollection<EmployeeItem>>(File.ReadAllText(EmployeesFile));
                    if (loaded != null) Employees = loaded;
                }

                // Load Payroll History
                if (File.Exists(PayrollFile))
                {
                    var loaded = JsonSerializer.Deserialize<ObservableCollection<PayrollHistoryRecord>>(File.ReadAllText(PayrollFile));
                    if (loaded != null) PayrollHistory = loaded;
                }

                // Load Biometrics Import History
                if (File.Exists(BiometricsFile))
                {
                    var loaded = JsonSerializer.Deserialize<ObservableCollection<BiometricsImportRecord>>(File.ReadAllText(BiometricsFile));
                    if (loaded != null) BiometricsImports = loaded;
                }

                // Load Audit Logs
                if (File.Exists(AuditFile))
                {
                    var loaded = JsonSerializer.Deserialize<ObservableCollection<AuditLogRecord>>(File.ReadAllText(AuditFile));
                    if (loaded != null) AuditLogs = loaded;
                }

                SaveChanges(); // Write initial files if they didn't exist
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load offline data: {ex.Message}");
                // Employees start empty on fresh install — no demo seeding
                if (Users.Count == 0) SeedDefaultUsers();
                if (Departments.Count == 0) SeedDefaultDepartments();
            }
        }

        private static void SeedDefaultUsers()
        {
            Users.Clear();
            Users.Add(new UserItem { Id = 1, Username = "admin", PasswordHash = "admin123", FullName = "System Administrator", Role = "Admin", IsActive = true });
            Users.Add(new UserItem { Id = 2, Username = "staff", PasswordHash = "staff123", FullName = "Staff User", Role = "Staff", IsActive = true });
        }

        private static void SeedDefaultDepartments()
        {
            Departments.Clear();
            Departments.Add(new DepartmentItem { Id = 1, Name = "ADMIN", Description = "Administrative Department" });
            Departments.Add(new DepartmentItem { Id = 2, Name = "Zoey's Eatery", Description = "Food Service Department" });
            Departments.Add(new DepartmentItem { Id = 3, Name = "Billiard Tenant", Description = "Recreation Department" });
        }

        /// <summary>
        /// One-time migration: copies JSON data from old AppData location to portable PayrollData folder
        /// </summary>
        private static void MigrateFromAppDataIfNeeded()
        {
            try
            {
                var oldFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PayrollSystem");
                if (!Directory.Exists(oldFolder)) return;

                // Only migrate if the new folder doesn't have employees yet
                if (File.Exists(EmployeesFile)) return;

                var jsonFiles = Directory.GetFiles(oldFolder, "*.json");
                if (jsonFiles.Length == 0) return;

                foreach (var file in jsonFiles)
                {
                    var destFile = Path.Combine(DataFolder, Path.GetFileName(file));
                    if (!File.Exists(destFile))
                    {
                        File.Copy(file, destFile, false);
                    }
                }

                System.Diagnostics.Debug.WriteLine("Successfully migrated data from AppData to portable PayrollData folder.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Migration skipped: {ex.Message}");
            }
        }



        public static void SaveChanges()
        {
            try
            {
                if (!Directory.Exists(DataFolder)) Directory.CreateDirectory(DataFolder);
                
                var opts = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(EmployeesFile, JsonSerializer.Serialize(Employees, opts));
                File.WriteAllText(PayrollFile, JsonSerializer.Serialize(PayrollHistory, opts));
                File.WriteAllText(UsersFile, JsonSerializer.Serialize(Users, opts));
                File.WriteAllText(DepartmentsFile, JsonSerializer.Serialize(Departments, opts));
                File.WriteAllText(BiometricsFile, JsonSerializer.Serialize(BiometricsImports, opts));
                File.WriteAllText(AuditFile, JsonSerializer.Serialize(AuditLogs, opts));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save offline data: {ex.Message}");
            }
        }

        public static void AddPayrollRecord(PayrollHistoryRecord record)
        {
            PayrollHistory.Insert(0, record); // newest first
            SaveChanges(); // Auto-save after inserting
        }

        /// <summary>
        /// Logs an action to the Audit Trail for accountability tracking.
        /// </summary>
        public static void LogAction(string action, string description)
        {
            Initialize();
            AuditLogs.Insert(0, new AuditLogRecord
            {
                Id = AuditLogs.Count > 0 ? AuditLogs.Max(a => a.Id) + 1 : 1,
                Timestamp = DateTime.Now,
                Action = action,
                Description = description
            });

            // Keep only the latest 500 entries to avoid bloating
            while (AuditLogs.Count > 500) AuditLogs.RemoveAt(AuditLogs.Count - 1);

            SaveChanges();
        }
    }

    public class UserItem
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Role { get; set; } = "Staff";
        public bool IsActive { get; set; } = true;
    }

    public class DepartmentItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public class BiometricsImportRecord
    {
        public int Id { get; set; }
        public string FileName { get; set; } = "";
        public string FilePath { get; set; } = "";
        public DateTime ImportedAt { get; set; } = DateTime.Now;
        public string ImportedAtFormatted => ImportedAt.ToString("MMM dd, yyyy hh:mm tt");
        public DateTime? PeriodStart { get; set; }
        public DateTime? PeriodEnd { get; set; }
        public string PeriodRange => PeriodStart.HasValue && PeriodEnd.HasValue
            ? $"{PeriodStart.Value:MMM dd} - {PeriodEnd.Value:MMM dd, yyyy}"
            : "Unknown";
        public int EmployeeCount { get; set; }
        public string FileHash { get; set; } = ""; // For duplicate detection
    }

    public class PayrollHistoryRecord : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        public int Id { get; set; }
        public string EmployeeName { get; set; } = "";
        public string EmpNumber { get; set; } = "";
        public DateTime PayrollDate { get; set; }
        public string PayrollDateFormatted { get; set; } = "";
        public decimal GrossRaw { get; set; }
        public string GrossSalary { get; set; } = "";
        public decimal DeductionsRaw { get; set; }
        public string Deductions { get; set; } = "";
        public decimal NetPayRaw { get; set; }
        public string NetPay { get; set; } = "";

        private string _status = "Processed";
        public string Status 
        { 
            get => _status; 
            set 
            { 
                if (_status != value) 
                { 
                    _status = value; 
                    PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Status))); 
                } 
            } 
        }
        
        // Processing Parameters
        public int WorkDays { get; set; }
        public DateTime PeriodStart { get; set; } = DateTime.Now;
        public DateTime PeriodEnd { get; set; } = DateTime.Now;
        
        // Earnings variables
        public decimal OvertimeHours { get; set; }
        public decimal HolidayHours { get; set; }
        public decimal Allowance { get; set; }
        public decimal Bonus { get; set; }

        // Deduction breakdown
        public decimal Sss { get; set; }
        public decimal Pagibig { get; set; }
        public decimal Philhealth { get; set; }
        public decimal Loan { get; set; }
        public decimal Late { get; set; }
        public decimal Undertime { get; set; }
        public decimal Others { get; set; }
        public string OthersName { get; set; } = "Others";
    }

    public class AuditLogRecord
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string TimestampFormatted => Timestamp.ToString("MMM dd, yyyy  h:mm:ss tt");
        public string Action { get; set; } = "";
        public string Description { get; set; } = "";
    }
}
