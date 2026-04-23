using System;
using MySql.Data.MySqlClient;

namespace PayrollSystem.DataAccess
{
    /// <summary>
    /// Centralized database connection helper for MySQL
    /// Caches connection status to prevent repeated timeout delays
    /// </summary>
    public static class DatabaseHelper
    {
        private static string _connectionString = "Server=localhost;Port=3306;Database=payroll_system;Uid=root;Pwd=;Connection Timeout=3;";
        private static bool? _connectionAvailable = null;
        private static DateTime _lastCheckTime = DateTime.MinValue;
        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(2);

        public static void SetConnectionString(string connectionString)
        {
            _connectionString = connectionString;
            _connectionAvailable = null; // Reset cache
        }

        public static MySqlConnection GetConnection()
        {
            return new MySqlConnection(_connectionString);
        }

        /// <summary>
        /// Tests DB connection with caching — avoids repeated timeout delays
        /// </summary>
        public static bool TestConnection()
        {
            // Return cached result if checked recently
            if (_connectionAvailable.HasValue && (DateTime.Now - _lastCheckTime) < CheckInterval)
                return _connectionAvailable.Value;

            try
            {
                using var conn = new MySqlConnection(_connectionString);
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
        /// Force re-check connection (e.g. after user starts MySQL)
        /// </summary>
        public static void ResetConnectionCache()
        {
            _connectionAvailable = null;
        }

        public static void InitializeDatabase()
        {
            if (!TestConnection())
            {
                // Try creating the database with a short-timeout connection
                try
                {
                    var createDbConn = new MySqlConnection("Server=localhost;Port=3306;Uid=root;Pwd=;Connection Timeout=3;");
                    createDbConn.Open();
                    var cmd = new MySqlCommand("CREATE DATABASE IF NOT EXISTS payroll_system;", createDbConn);
                    cmd.ExecuteNonQuery();
                    createDbConn.Close();
                    _connectionAvailable = null; // Reset so next TestConnection re-checks
                }
                catch
                {
                    return; // MySQL not available, use demo mode
                }
            }

            if (!TestConnection()) return;

            try
            {
                using var conn = GetConnection();
                conn.Open();

                var sql = @"
                CREATE TABLE IF NOT EXISTS users (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    username VARCHAR(50) NOT NULL UNIQUE,
                    password_hash VARCHAR(255) NOT NULL,
                    full_name VARCHAR(100) NOT NULL,
                    role ENUM('Admin', 'Staff') NOT NULL DEFAULT 'Staff',
                    is_active TINYINT(1) NOT NULL DEFAULT 1,
                    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
                );

                CREATE TABLE IF NOT EXISTS departments (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(100) NOT NULL UNIQUE,
                    description VARCHAR(255),
                    location VARCHAR(100),
                    budget DECIMAL(12,2) DEFAULT 0,
                    manager_name VARCHAR(100),
                    is_active TINYINT(1) NOT NULL DEFAULT 1,
                    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
                );

                CREATE TABLE IF NOT EXISTS employees (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    emp_number VARCHAR(20) NOT NULL UNIQUE,
                    first_name VARCHAR(50) NOT NULL,
                    last_name VARCHAR(50) NOT NULL,
                    position VARCHAR(100) NOT NULL,
                    department_id INT,
                    daily_rate DECIMAL(10,2) NOT NULL,
                    hire_date DATE NOT NULL,
                    is_active TINYINT(1) NOT NULL DEFAULT 1,
                    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                    FOREIGN KEY (department_id) REFERENCES departments(id)
                );

                CREATE TABLE IF NOT EXISTS payroll (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    employee_id INT NOT NULL,
                    payroll_date DATE NOT NULL,
                    period_start DATE NOT NULL,
                    period_end DATE NOT NULL,
                    work_days INT NOT NULL DEFAULT 0,
                    overtime_hours DECIMAL(6,2) DEFAULT 0,
                    holiday_hours DECIMAL(6,2) DEFAULT 0,
                    basic_salary DECIMAL(12,2) NOT NULL,
                    overtime_pay DECIMAL(12,2) DEFAULT 0,
                    holiday_pay DECIMAL(12,2) DEFAULT 0,
                    allowance DECIMAL(12,2) DEFAULT 0,
                    bonus DECIMAL(12,2) DEFAULT 0,
                    gross_salary DECIMAL(12,2) NOT NULL,
                    total_deductions DECIMAL(12,2) DEFAULT 0,
                    net_pay DECIMAL(12,2) NOT NULL,
                    status ENUM('Draft', 'Processed', 'Paid') DEFAULT 'Draft',
                    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                    FOREIGN KEY (employee_id) REFERENCES employees(id)
                );

                CREATE TABLE IF NOT EXISTS deductions (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    payroll_id INT NOT NULL,
                    name VARCHAR(50) NOT NULL,
                    type ENUM('SSS', 'PAGIBIG', 'PhilHealth', 'Tax', 'Loan', 'Other') NOT NULL,
                    amount DECIMAL(10,2) NOT NULL,
                    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (payroll_id) REFERENCES payroll(id) ON DELETE CASCADE
                );";

                foreach (var statement in sql.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    var trimmed = statement.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmed))
                    {
                        using var tableCmd = new MySqlCommand(trimmed, conn);
                        tableCmd.ExecuteNonQuery();
                    }
                }

                SeedDefaultData(conn);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database initialization error: {ex.Message}");
            }
        }

        private static void SeedDefaultData(MySqlConnection conn)
        {
            using var checkCmd = new MySqlCommand("SELECT COUNT(*) FROM users", conn);
            var userCount = Convert.ToInt32(checkCmd.ExecuteScalar());

            if (userCount == 0)
            {
                using var seedCmd = new MySqlCommand(
                    @"INSERT INTO users (username, password_hash, full_name, role) VALUES ('admin', 'admin123', 'System Administrator', 'Admin');
                      INSERT INTO users (username, password_hash, full_name, role) VALUES ('staff', 'staff123', 'Staff User', 'Staff');", conn);
                seedCmd.ExecuteNonQuery();
            }

            using var checkDeptCmd = new MySqlCommand("SELECT COUNT(*) FROM departments", conn);
            if (Convert.ToInt32(checkDeptCmd.ExecuteScalar()) == 0)
            {
                using var deptCmd = new MySqlCommand(
                    @"INSERT INTO departments (name, description, location, budget) VALUES ('ADMIN', 'Administrative Department', 'Main Office', 50000);
                      INSERT INTO departments (name, description, location, budget) VALUES ('Zoey''s Eatery', 'Food Service Department', 'Food Court', 50000);
                      INSERT INTO departments (name, description, location, budget) VALUES ('Billiard Tenant', 'Recreation Department', 'Game Area', 50000);", conn);
                deptCmd.ExecuteNonQuery();
            }
            // Employees are no longer auto-seeded — they are managed via the app
        }
    }
}
