using System;
using System.IO;
using System.Text.Json;
using System.Collections.ObjectModel;
using Microsoft.Data.Sqlite;
using PayrollSystem.Models;
using PayrollSystem.Utilities;
using PayrollSystem.ViewModels;

namespace PayrollSystem.DataAccess
{
    /// <summary>
    /// Centralized database connection helper for SQLite
    /// The database file (payroll.db) is stored next to the executable.
    /// </summary>
    public static class DatabaseHelper
    {
        private static string _dbPath = GetSafeDatabasePath();

        private static string GetSafeDatabasePath()
        {
            // Store in the exact same folder as the .exe so it can be easily ZIPPED and moved around via Google Drive
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "payroll.db");
        }
        private static string _connectionString = $"Data Source={_dbPath}";
        private static bool? _connectionAvailable = null;
        private static DateTime _lastCheckTime = DateTime.MinValue;
        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(2);

        public static void SetConnectionString(string connectionString)
        {
            _connectionString = connectionString;
            _connectionAvailable = null;
        }

        public static SqliteConnection GetConnection()
        {
            return new SqliteConnection(_connectionString);
        }

        /// <summary>
        /// Tests DB connection with caching
        /// </summary>
        public static bool TestConnection()
        {
            if (_connectionAvailable.HasValue && (DateTime.Now - _lastCheckTime) < CheckInterval)
                return _connectionAvailable.Value;

            try
            {
                using var conn = new SqliteConnection(_connectionString);
                conn.Open();
                _connectionAvailable = true;
                _lastCheckTime = DateTime.Now;
                return true;
            }
            catch
            {
                _connectionAvailable = false;
                _lastCheckTime = DateTime.Now;
                return false;
            }
        }

        /// <summary>
        /// Force re-check connection
        /// </summary>
        public static void ResetConnectionCache()
        {
            _connectionAvailable = null;
        }

        /// <summary>
        /// Creates a backup of the local database to prevent data loss.
        /// Keeps the last 5 backups.
        /// </summary>
        public static void BackupDatabase()
        {
            try
            {
                if (!File.Exists(_dbPath)) return;

                string backupDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
                if (!Directory.Exists(backupDir)) Directory.CreateDirectory(backupDir);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupPath = Path.Combine(backupDir, $"payroll_backup_{timestamp}.db");

                File.Copy(_dbPath, backupPath, true);

                // Optional: Clean up old backups to save space (keep last 5)
                var oldBackups = new DirectoryInfo(backupDir)
                    .GetFiles("payroll_backup_*.db")
                    .OrderByDescending(f => f.CreationTime)
                    .Skip(5)
                    .ToList();

                foreach (var old in oldBackups)
                {
                    try { old.Delete(); } catch { }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to backup database: {ex.Message}");
            }
        }

        public static void InitializeDatabase()
        {
            try
            {
                using var conn = GetConnection();
                conn.Open();

                // Enable WAL mode for better concurrency
                using (var walCmd = new SqliteCommand("PRAGMA journal_mode=WAL;", conn))
                    walCmd.ExecuteNonQuery();

                // Enable foreign keys
                using (var fkCmd = new SqliteCommand("PRAGMA foreign_keys=ON;", conn))
                    fkCmd.ExecuteNonQuery();

                var sql = @"
                CREATE TABLE IF NOT EXISTS users (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    username TEXT NOT NULL UNIQUE,
                    password_hash TEXT NOT NULL,
                    full_name TEXT NOT NULL,
                    role TEXT NOT NULL DEFAULT 'Staff' CHECK(role IN ('Admin', 'Staff')),
                    is_active INTEGER NOT NULL DEFAULT 1,
                    created_at TEXT DEFAULT (datetime('now', 'localtime')),
                    updated_at TEXT DEFAULT (datetime('now', 'localtime'))
                );

                CREATE TABLE IF NOT EXISTS departments (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL UNIQUE,
                    description TEXT,
                    location TEXT,
                    budget REAL DEFAULT 0,
                    manager_name TEXT,
                    is_active INTEGER NOT NULL DEFAULT 1,
                    created_at TEXT DEFAULT (datetime('now', 'localtime'))
                );

                CREATE TABLE IF NOT EXISTS employees (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    emp_number TEXT NOT NULL UNIQUE,
                    first_name TEXT NOT NULL,
                    last_name TEXT NOT NULL,
                    position TEXT NOT NULL,
                    department_id INTEGER,
                    daily_rate REAL NOT NULL,
                    hire_date TEXT NOT NULL,
                    is_active INTEGER NOT NULL DEFAULT 1,
                    created_at TEXT DEFAULT (datetime('now', 'localtime')),
                    updated_at TEXT DEFAULT (datetime('now', 'localtime')),
                    FOREIGN KEY (department_id) REFERENCES departments(id)
                );

                CREATE TABLE IF NOT EXISTS payroll (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    employee_id INTEGER NOT NULL,
                    payroll_date TEXT NOT NULL,
                    period_start TEXT NOT NULL,
                    period_end TEXT NOT NULL,
                    work_days INTEGER NOT NULL DEFAULT 0,
                    overtime_hours REAL DEFAULT 0,
                    holiday_hours REAL DEFAULT 0,
                    basic_salary REAL NOT NULL,
                    overtime_pay REAL DEFAULT 0,
                    holiday_pay REAL DEFAULT 0,
                    allowance REAL DEFAULT 0,
                    bonus REAL DEFAULT 0,
                    gross_salary REAL NOT NULL,
                    total_deductions REAL DEFAULT 0,
                    net_pay REAL NOT NULL,
                    status TEXT DEFAULT 'Draft' CHECK(status IN ('Draft', 'Processed', 'Paid', 'Pending')),
                    created_at TEXT DEFAULT (datetime('now', 'localtime')),
                    updated_at TEXT DEFAULT (datetime('now', 'localtime')),
                    FOREIGN KEY (employee_id) REFERENCES employees(id)
                );

                CREATE TABLE IF NOT EXISTS deductions (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    payroll_id INTEGER NOT NULL,
                    name TEXT NOT NULL,
                    type TEXT NOT NULL CHECK(type IN ('SSS', 'PAGIBIG', 'PhilHealth', 'Tax', 'Loan', 'Other')),
                    amount REAL NOT NULL,
                    created_at TEXT DEFAULT (datetime('now', 'localtime')),
                    FOREIGN KEY (payroll_id) REFERENCES payroll(id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS biometrics_imports (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    file_name TEXT NOT NULL,
                    file_path TEXT NOT NULL,
                    file_hash TEXT,
                    imported_at TEXT DEFAULT (datetime('now', 'localtime')),
                    period_start TEXT,
                    period_end TEXT,
                    employee_count INTEGER DEFAULT 0
                );";

                // SQLite supports running multiple statements in one command
                using var tableCmd = new SqliteCommand(sql, conn);
                tableCmd.ExecuteNonQuery();

                SeedDefaultData(conn);
                MigrateJsonToSqlite(conn);

                _connectionAvailable = true;
                _lastCheckTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database initialization error: {ex.Message}");
            }
        }

        private static void SeedDefaultData(SqliteConnection conn)
        {
            using var checkCmd = new SqliteCommand("SELECT COUNT(*) FROM users", conn);
            var userCount = Convert.ToInt32(checkCmd.ExecuteScalar());

            if (userCount == 0)
            {
                var adminHash = PasswordHelper.HashPassword("admin123");
                var staffHash = PasswordHelper.HashPassword("staff123");

                using var seedCmd = new SqliteCommand(
                    @"INSERT INTO users (username, password_hash, full_name, role) VALUES (@u1, @p1, 'System Administrator', 'Admin');
                      INSERT INTO users (username, password_hash, full_name, role) VALUES (@u2, @p2, 'Staff User', 'Staff');", conn);
                seedCmd.Parameters.AddWithValue("@u1", "admin");
                seedCmd.Parameters.AddWithValue("@p1", adminHash);
                seedCmd.Parameters.AddWithValue("@u2", "staff");
                seedCmd.Parameters.AddWithValue("@p2", staffHash);
                seedCmd.ExecuteNonQuery();
            }

            using var checkDeptCmd = new SqliteCommand("SELECT COUNT(*) FROM departments", conn);
            if (Convert.ToInt32(checkDeptCmd.ExecuteScalar()) == 0)
            {
                using var deptCmd = new SqliteCommand(
                    @"INSERT INTO departments (name, description, location, budget) VALUES ('ADMIN', 'Administrative Department', 'Main Office', 50000);
                      INSERT INTO departments (name, description, location, budget) VALUES ('Zoey''s Eatery', 'Food Service Department', 'Food Court', 50000);
                      INSERT INTO departments (name, description, location, budget) VALUES ('Billiard Tenant', 'Recreation Department', 'Game Area', 50000);", conn);
                deptCmd.ExecuteNonQuery();
            }
        }

        private static void MigrateJsonToSqlite(SqliteConnection conn)
        {
            string dataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PayrollData");
            if (!Directory.Exists(dataFolder)) return;

            // Migrate Users
            string usersFile = Path.Combine(dataFolder, "users.json");
            if (File.Exists(usersFile))
            {
                try
                {
                    var users = JsonSerializer.Deserialize<ObservableCollection<UserItem>>(File.ReadAllText(usersFile));
                    if (users != null)
                    {
                        foreach (var u in users)
                        {
                            using var checkCmd = new SqliteCommand("SELECT COUNT(*) FROM users WHERE username=@u", conn);
                            checkCmd.Parameters.AddWithValue("@u", u.Username);
                            if (Convert.ToInt32(checkCmd.ExecuteScalar()) == 0)
                            {
                                using var insCmd = new SqliteCommand("INSERT INTO users (username, password_hash, full_name, role, is_active) VALUES (@u, @p, @f, @r, @a)", conn);
                                insCmd.Parameters.AddWithValue("@u", u.Username);
                                insCmd.Parameters.AddWithValue("@p", PasswordHelper.HashPassword(u.PasswordHash));
                                insCmd.Parameters.AddWithValue("@f", u.FullName);
                                insCmd.Parameters.AddWithValue("@r", u.Role);
                                insCmd.Parameters.AddWithValue("@a", u.IsActive ? 1 : 0);
                                insCmd.ExecuteNonQuery();
                            }
                        }
                    }
                    File.Move(usersFile, usersFile + ".migrated", true);
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Migration error (users): {ex.Message}"); }
            }

            // Migrate Departments
            string deptsFile = Path.Combine(dataFolder, "departments.json");
            if (File.Exists(deptsFile))
            {
                try
                {
                    var depts = JsonSerializer.Deserialize<ObservableCollection<DepartmentItem>>(File.ReadAllText(deptsFile));
                    if (depts != null)
                    {
                        foreach (var d in depts)
                        {
                            using var checkCmd = new SqliteCommand("SELECT COUNT(*) FROM departments WHERE name=@n", conn);
                            checkCmd.Parameters.AddWithValue("@n", d.Name);
                            if (Convert.ToInt32(checkCmd.ExecuteScalar()) == 0)
                            {
                                using var insCmd = new SqliteCommand("INSERT INTO departments (name, description) VALUES (@n, @d)", conn);
                                insCmd.Parameters.AddWithValue("@n", d.Name);
                                insCmd.Parameters.AddWithValue("@d", d.Description);
                                insCmd.ExecuteNonQuery();
                            }
                        }
                    }
                    File.Move(deptsFile, deptsFile + ".migrated", true);
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Migration error (depts): {ex.Message}"); }
            }

            // Migrate Employees
            string empsFile = Path.Combine(dataFolder, "employees.json");
            if (File.Exists(empsFile))
            {
                try
                {
                    var emps = JsonSerializer.Deserialize<ObservableCollection<EmployeeItem>>(File.ReadAllText(empsFile));
                    if (emps != null)
                    {
                        foreach (var e in emps)
                        {
                            using var checkCmd = new SqliteCommand("SELECT COUNT(*) FROM employees WHERE emp_number=@n", conn);
                            checkCmd.Parameters.AddWithValue("@n", e.EmpNumber);
                            if (Convert.ToInt32(checkCmd.ExecuteScalar()) == 0)
                            {
                                using var insCmd = new SqliteCommand(
                                    @"INSERT INTO employees (emp_number, first_name, last_name, position, daily_rate, department_id, hire_date, is_active) 
                                      VALUES (@en, @fn, @ln, @pos, @rate, (SELECT id FROM departments WHERE name=@dept LIMIT 1), @hd, @act)", conn);
                                insCmd.Parameters.AddWithValue("@en", e.EmpNumber);
                                insCmd.Parameters.AddWithValue("@fn", e.FirstName);
                                insCmd.Parameters.AddWithValue("@ln", e.LastName);
                                insCmd.Parameters.AddWithValue("@pos", e.Position);
                                insCmd.Parameters.AddWithValue("@rate", e.DailyRate);
                                insCmd.Parameters.AddWithValue("@dept", e.Department);
                                insCmd.Parameters.AddWithValue("@hd", e.HireDate.ToString("yyyy-MM-dd"));
                                insCmd.Parameters.AddWithValue("@act", e.IsActive ? 1 : 0);
                                insCmd.ExecuteNonQuery();
                            }
                        }
                    }
                    File.Move(empsFile, empsFile + ".migrated", true);
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Migration error (emps): {ex.Message}"); }
            }

            // Once everything is migrated, rename the folder so it doesn't run again
            try
            {
                if (Directory.GetFiles(dataFolder, "*.json").Length == 0)
                {
                    Directory.Move(dataFolder, dataFolder + "_Migrated");
                }
            }
            catch { }
        }
    }
}
