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
        private static readonly string DataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PayrollSystem");
        private static readonly string EmployeesFile = Path.Combine(DataFolder, "employees.json");
        private static readonly string PayrollFile = Path.Combine(DataFolder, "payroll_history.json");
        private static readonly string UsersFile = Path.Combine(DataFolder, "users.json");
        private static readonly string DepartmentsFile = Path.Combine(DataFolder, "departments.json");
        private static readonly string BiometricsFile = Path.Combine(DataFolder, "biometrics_imports.json");

        public static ObservableCollection<EmployeeItem> Employees { get; private set; } = new();
        public static ObservableCollection<PayrollHistoryRecord> PayrollHistory { get; private set; } = new();
        public static ObservableCollection<UserItem> Users { get; private set; } = new();
        public static ObservableCollection<DepartmentItem> Departments { get; private set; } = new();
        public static ObservableCollection<BiometricsImportRecord> BiometricsImports { get; private set; } = new();

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                if (!Directory.Exists(DataFolder)) Directory.CreateDirectory(DataFolder);

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
                else SeedDefaultEmployees();

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

                SaveChanges(); // Write initial files if they didn't exist
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load offline data: {ex.Message}");
                if (Employees.Count == 0) SeedDefaultEmployees();
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

        private static void SeedDefaultEmployees()
        {
            Employees.Clear();
            var demoEmployees = new[]
            {
                ("EMP-0001", "Kenneth Ariel", "Francisco", "Administrator", "ADMIN", 1200m),
                ("EMP-0002", "Judy", "Peralta", "HR Manager", "ADMIN", 1500m),
                ("EMP-0003", "Trecia", "De Jesus", "Office Administrator", "ADMIN", 1100m),
                ("EMP-0004", "Alyssa Marie", "Zamudio", "Restaurant Manager", "Zoey's Eatery", 1000m),
                ("EMP-0005", "Alliyah", "Lobendino", "Head Chef", "Zoey's Eatery", 950m),
                ("EMP-0006", "Cristel Khaye", "Sevilla", "Service Staff", "Zoey's Eatery", 650m),
                ("EMP-0007", "Michael", "Villasenor", "Kitchen Staff", "Zoey's Eatery", 600m),
                ("EMP-0008", "Beverly", "Gabriel", "Cashier", "Zoey's Eatery", 550m),
                ("EMP-0009", "Charmine", "Resus", "Cashier", "Zoey's Eatery", 550m),
                ("EMP-0010", "Kiven", "Paez", "Service Staff", "Zoey's Eatery", 600m),
                ("EMP-0011", "Lucky", "Flores", "Billiard Manager", "Billiard Tenant", 800m),
                ("EMP-0012", "Romez", "Bautista", "Game Attendant", "Billiard Tenant", 500m),
                ("EMP-0013", "Jerryco", "Viador", "Game Attendant", "Billiard Tenant", 500m),
            };

            int id = 1;
            foreach (var (num, fn, ln, pos, dept, rate) in demoEmployees)
            {
                Employees.Add(new EmployeeItem
                {
                    Id = id++, EmpNumber = num, FirstName = fn, LastName = ln,
                    FullName = $"{fn} {ln}", Position = pos, Department = dept,
                    DailyRate = rate, DailyRateFormatted = $"₱{rate:N2}",
                    HireDate = DateTime.Now.AddMonths(-id), IsActive = true, Status = "Active"
                });
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
    }
}
