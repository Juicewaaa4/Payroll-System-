-- ═══════════════════════════════════════════════════════════
-- PAYROLL MANAGEMENT SYSTEM — MySQL Database Schema
-- ═══════════════════════════════════════════════════════════

CREATE DATABASE IF NOT EXISTS payroll_system;
USE payroll_system;

-- ─── Users Table (Login) ─────────────────────────────────
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

-- ─── Departments Table ───────────────────────────────────
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

-- ─── Employees Table ─────────────────────────────────────
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

-- ─── Payroll Table ───────────────────────────────────────
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

-- ─── Deductions Table ────────────────────────────────────
CREATE TABLE IF NOT EXISTS deductions (
    id INT AUTO_INCREMENT PRIMARY KEY,
    payroll_id INT NOT NULL,
    name VARCHAR(50) NOT NULL,
    type ENUM('SSS', 'PAGIBIG', 'PhilHealth', 'Tax', 'Loan', 'Other') NOT NULL,
    amount DECIMAL(10,2) NOT NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (payroll_id) REFERENCES payroll(id) ON DELETE CASCADE
);

-- ─── Insert Default Data ─────────────────────────────────

-- Default admin user (password: admin123)
INSERT IGNORE INTO users (username, password_hash, full_name, role)
VALUES ('admin', 'admin123', 'System Administrator', 'Admin');

-- Default staff user (password: staff123)
INSERT IGNORE INTO users (username, password_hash, full_name, role)
VALUES ('staff', 'staff123', 'Staff User', 'Staff');

-- Default departments
INSERT IGNORE INTO departments (name, description, location, budget)
VALUES 
    ('ADMIN', 'Administrative Department', 'Main Office', 50000),
    ('Zoey''s Eatery', 'Food Service Department', 'Food Court', 50000),
    ('Billiard Tenant', 'Recreation Department', 'Game Area', 50000);

-- Default employees
INSERT IGNORE INTO employees (emp_number, first_name, last_name, position, department_id, daily_rate, hire_date)
VALUES
    ('EMP-0001', 'Kenneth Ariel', 'Francisco', 'Administrator', 1, 1200, '2025-06-15'),
    ('EMP-0002', 'Judy', 'Peralta', 'HR Manager', 1, 1500, '2025-07-01'),
    ('EMP-0003', 'Trecia', 'De Jesus', 'Office Administrator', 1, 1100, '2025-08-10'),
    ('EMP-0004', 'Alyssa Marie', 'Zamudio', 'Restaurant Manager', 2, 1000, '2025-05-20'),
    ('EMP-0005', 'Alliyah', 'Lobendino', 'Head Chef', 2, 950, '2025-06-01'),
    ('EMP-0006', 'Cristel Khaye', 'Sevilla', 'Service Staff', 2, 650, '2025-09-15'),
    ('EMP-0007', 'Michael', 'Villasenor', 'Kitchen Staff', 2, 600, '2025-10-01'),
    ('EMP-0008', 'Beverly', 'Gabriel', 'Cashier', 2, 550, '2025-11-10'),
    ('EMP-0009', 'Charmine', 'Resus', 'Cashier', 2, 550, '2025-07-20'),
    ('EMP-0010', 'Kiven', 'Paez', 'Service Staff', 2, 600, '2025-08-01'),
    ('EMP-0011', 'Lucky', 'Flores', 'Billiard Manager', 3, 800, '2025-06-15'),
    ('EMP-0012', 'Romez', 'Bautista', 'Game Attendant', 3, 500, '2025-09-01'),
    ('EMP-0013', 'Jerryco', 'Viador', 'Game Attendant', 3, 500, '2025-10-15');
