using System;
using System.Collections.Generic;
using System.Linq;
using PayrollSystem.Models;
using PayrollSystem.Services;

namespace PayrollSystem.UI
{
    /// <summary>
    /// Professional user interface for employee management
    /// </summary>
    public class EmployeeManagementUI
    {
        private readonly EmployeeService _employeeService;
        private readonly DepartmentService _departmentService;
        private readonly PayrollService _payrollService;

        public EmployeeManagementUI(EmployeeService employeeService, DepartmentService departmentService, PayrollService payrollService)
        {
            _employeeService = employeeService;
            _departmentService = departmentService;
            _payrollService = payrollService;
        }

        /// <summary>
        /// Displays the main employee management menu
        /// </summary>
        public void ShowEmployeeManagementMenu()
        {
            while (true)
            {
                ConsoleUIHelper.DrawPageHeader("Employees", "Main Menu › Employee Management");

                var width = ConsoleUIHelper.DefaultWidth;

                Console.WriteLine();
                ConsoleUIHelper.DrawTopBorder(width);
                ConsoleUIHelper.DrawEmptyLine(width);
                ConsoleUIHelper.DrawCenteredLine("EMPLOYEE MANAGEMENT", ConsoleUIHelper.PrimaryColor, width);
                ConsoleUIHelper.DrawEmptyLine(width);
                ConsoleUIHelper.DrawSeparator(width);
                ConsoleUIHelper.DrawEmptyLine(width);
                ConsoleUIHelper.DrawMenuOption("1", "View All Employees", width);
                ConsoleUIHelper.DrawMenuOption("2", "Add New Employee", width);
                ConsoleUIHelper.DrawMenuOption("3", "Update Employee", width);
                ConsoleUIHelper.DrawMenuOption("4", "Delete Employee", width);
                ConsoleUIHelper.DrawMenuOption("5", "View Employees by Department", width);
                ConsoleUIHelper.DrawMenuOption("6", "Generate Payroll for Employee", width);
                ConsoleUIHelper.DrawEmptyLine(width);
                ConsoleUIHelper.DrawSeparator(width);
                ConsoleUIHelper.DrawEmptyLine(width);
                ConsoleUIHelper.DrawMenuOption("7", "Back to Main Menu", width);
                ConsoleUIHelper.DrawEmptyLine(width);
                ConsoleUIHelper.DrawBottomBorder(width);

                ConsoleUIHelper.DrawPrompt("Select an option (1-7): ");

                var choice = Console.ReadLine()?.Trim();
                Console.WriteLine();

                switch (choice)
                {
                    case "1":
                        DisplayAllEmployees();
                        break;
                    case "2":
                        AddNewEmployee();
                        break;
                    case "3":
                        UpdateEmployee();
                        break;
                    case "4":
                        DeleteEmployee();
                        break;
                    case "5":
                        ViewEmployeesByDepartment();
                        break;
                    case "6":
                        GeneratePayrollForEmployee();
                        break;
                    case "7":
                        return;
                    default:
                        ConsoleUIHelper.DrawError("Invalid option. Please select 1-7.");
                        break;
                }

                if (choice != "7")
                {
                    ConsoleUIHelper.DrawPressAnyKey();
                }
            }
        }

        /// <summary>
        /// Displays all employees in a professionally formatted table
        /// </summary>
        private void DisplayAllEmployees()
        {
            ConsoleUIHelper.DrawPageHeader("Employees", "Employee Management › View All");

            var employees = _employeeService.GetAllEmployees();

            if (!employees.Any())
            {
                ConsoleUIHelper.DrawWarning("No employees found in the system.");
                return;
            }

            ConsoleUIHelper.DrawMiniHeader("Employee Directory");

            var headers = new string[] { "ID", "First Name", "Last Name", "Position", "Daily Rate", "Status" };
            var columnWidths = new int[] { 4, 16, 14, 18, 12, 8 };
            var rows = new List<string[]>();

            foreach (var employee in employees.OrderBy(e => e.LastName).ThenBy(e => e.FirstName))
            {
                rows.Add(new string[] {
                    employee.Id.ToString(),
                    employee.FirstName,
                    employee.LastName,
                    employee.Position,
                    employee.DailyRate.ToString("C"),
                    employee.IsActive ? "Active" : "Inactive"
                });
            }

            ConsoleUIHelper.DrawTable(headers, rows, columnWidths);
            ConsoleUIHelper.DrawFooter("Total Employees", employees.Count.ToString());
        }

        /// <summary>
        /// Adds a new employee with professional form interface
        /// </summary>
        private void AddNewEmployee()
        {
            ConsoleUIHelper.DrawPageHeader("Employees", "Employee Management › Add New");

            var width = ConsoleUIHelper.DefaultWidth;

            Console.WriteLine();
            ConsoleUIHelper.DrawTopBorder(width);
            ConsoleUIHelper.DrawEmptyLine(width);
            ConsoleUIHelper.DrawCenteredLine("NEW EMPLOYEE FORM", ConsoleUIHelper.PrimaryColor, width);
            ConsoleUIHelper.DrawEmptyLine(width);
            ConsoleUIHelper.DrawBottomBorder(width);

            try
            {
                Console.WriteLine();
                var firstName = GetValidInput("First Name", x => !string.IsNullOrWhiteSpace(x));
                var lastName = GetValidInput("Last Name", x => !string.IsNullOrWhiteSpace(x));

                string position;
                decimal dailyRate;

                // Show available departments
                var departments = _departmentService.GetAllDepartments();
                if (departments.Any())
                {
                    ConsoleUIHelper.DrawMiniHeader("Select Department");

                    for (int i = 0; i < departments.Count; i++)
                    {
                        ConsoleUIHelper.Write("    ", ConsoleColor.DarkGray);
                        ConsoleUIHelper.Write($"[", ConsoleUIHelper.MutedColor);
                        ConsoleUIHelper.Write($"{(i + 1)}", ConsoleUIHelper.PrimaryColor);
                        ConsoleUIHelper.Write($"]", ConsoleUIHelper.MutedColor);
                        ConsoleUIHelper.WriteLine($"  {departments[i].Name}", ConsoleColor.White);
                    }

                    var deptChoice = GetValidNumber("Department", 1, departments.Count);
                    var selectedDepartment = departments[deptChoice - 1];

                    // Show positions for the selected department
                    ConsoleUIHelper.DrawMiniHeader($"Positions in {selectedDepartment.Name}");

                    var positions = _departmentService.GetPositionsByDepartment(selectedDepartment.Id);

                    if (positions.Any())
                    {
                        for (int i = 0; i < positions.Count; i++)
                        {
                            ConsoleUIHelper.Write("    ", ConsoleColor.DarkGray);
                            ConsoleUIHelper.Write($"[", ConsoleUIHelper.MutedColor);
                            ConsoleUIHelper.Write($"{(i + 1)}", ConsoleUIHelper.PrimaryColor);
                            ConsoleUIHelper.Write($"]", ConsoleUIHelper.MutedColor);
                            ConsoleUIHelper.Write($"  {positions[i].Title}", ConsoleColor.White);
                            ConsoleUIHelper.Write($"  ·  ", ConsoleUIHelper.MutedColor);
                            ConsoleUIHelper.WriteLine(positions[i].SalaryRange, ConsoleUIHelper.AccentColor);
                        }

                        var posChoice = GetValidNumber("Position", 1, positions.Count);
                        var selectedPosition = positions[posChoice - 1];

                        Console.WriteLine();
                        ConsoleUIHelper.DrawInfo($"Selected: {selectedPosition.Title} ({selectedPosition.SalaryRange})");

                        position = selectedPosition.Title;
                        dailyRate = GetValidDecimal($"Daily Rate ({selectedPosition.MinSalary:C}-{selectedPosition.MaxSalary:C})",
                            selectedPosition.MinSalary, selectedPosition.MaxSalary);
                    }
                    else
                    {
                        position = GetValidInput("Position", x => !string.IsNullOrWhiteSpace(x));
                        dailyRate = GetValidDecimal("Daily Rate", 0, 10000);
                    }
                }
                else
                {
                    position = GetValidInput("Position", x => !string.IsNullOrWhiteSpace(x));
                    dailyRate = GetValidDecimal("Daily Rate", 0, 10000);
                }

                var hireDate = GetValidDate("Hire Date (MM/DD/YYYY)", DateTime.Now.AddYears(-5), DateTime.Now);

                var employee = new Employee
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Position = position,
                    DailyRate = dailyRate,
                    HireDate = hireDate,
                    IsActive = true
                };

                var addedEmployee = _employeeService.AddEmployee(employee);

                ConsoleUIHelper.DrawSuccess($"Employee '{addedEmployee.FullName}' added successfully!");

                // Show summary card
                Console.WriteLine();
                ConsoleUIHelper.DrawTopBorder(width);
                ConsoleUIHelper.DrawEmptyLine(width);
                ConsoleUIHelper.DrawBoxLine("EMPLOYEE ADDED", ConsoleUIHelper.SuccessColor, width);
                ConsoleUIHelper.DrawEmptyLine(width);
                ConsoleUIHelper.DrawThinSeparator(width);
                ConsoleUIHelper.DrawKeyValueColored("Employee ID:", addedEmployee.Id.ToString("D4"), ConsoleUIHelper.PrimaryColor, 18, width);
                ConsoleUIHelper.DrawKeyValue("Full Name:", addedEmployee.FullName, 18, width);
                ConsoleUIHelper.DrawKeyValue("Position:", addedEmployee.Position, 18, width);
                ConsoleUIHelper.DrawKeyValueColored("Daily Rate:", addedEmployee.DailyRate.ToString("C"), ConsoleUIHelper.AccentColor, 18, width);
                ConsoleUIHelper.DrawKeyValueColored("Status:", "Active", ConsoleUIHelper.SuccessColor, 18, width);
                ConsoleUIHelper.DrawEmptyLine(width);
                ConsoleUIHelper.DrawBottomBorder(width);
            }
            catch (Exception ex)
            {
                ConsoleUIHelper.DrawError($"Error adding employee: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates an existing employee with professional form interface
        /// </summary>
        private void UpdateEmployee()
        {
            ConsoleUIHelper.DrawPageHeader("Employees", "Employee Management › Update");

            var employees = _employeeService.GetAllEmployees();
            if (!employees.Any())
            {
                ConsoleUIHelper.DrawWarning("No employees found in the system.");
                return;
            }

            ConsoleUIHelper.DrawMiniHeader("Select Employee to Update");

            for (int i = 0; i < employees.Count; i++)
            {
                ConsoleUIHelper.Write("    ", ConsoleColor.DarkGray);
                ConsoleUIHelper.Write($"[", ConsoleUIHelper.MutedColor);
                ConsoleUIHelper.Write($"{(i + 1).ToString().PadLeft(2)}", ConsoleUIHelper.PrimaryColor);
                ConsoleUIHelper.Write($"]", ConsoleUIHelper.MutedColor);
                ConsoleUIHelper.Write($"  {employees[i].FullName}", ConsoleColor.White);
                ConsoleUIHelper.Write($"  ·  ", ConsoleUIHelper.MutedColor);
                ConsoleUIHelper.WriteLine(employees[i].Position, ConsoleUIHelper.SubtleColor);
            }

            var empChoice = GetValidNumber("Employee #", 1, employees.Count);
            var selectedEmployee = employees[empChoice - 1];

            // Display current info
            var width = ConsoleUIHelper.DefaultWidth;
            Console.WriteLine();
            ConsoleUIHelper.DrawTopBorder(width);
            ConsoleUIHelper.DrawEmptyLine(width);
            ConsoleUIHelper.DrawBoxLine("CURRENT INFORMATION", ConsoleUIHelper.SubtleColor, width);
            ConsoleUIHelper.DrawEmptyLine(width);
            ConsoleUIHelper.DrawThinSeparator(width);
            ConsoleUIHelper.DrawKeyValue("First Name:", selectedEmployee.FirstName, 16, width);
            ConsoleUIHelper.DrawKeyValue("Last Name:", selectedEmployee.LastName, 16, width);
            ConsoleUIHelper.DrawKeyValue("Position:", selectedEmployee.Position, 16, width);
            ConsoleUIHelper.DrawKeyValueColored("Daily Rate:", selectedEmployee.DailyRate.ToString("C"), ConsoleUIHelper.AccentColor, 16, width);
            ConsoleUIHelper.DrawKeyValueColored("Status:", selectedEmployee.IsActive ? "Active" : "Inactive",
                selectedEmployee.IsActive ? ConsoleUIHelper.SuccessColor : ConsoleUIHelper.ErrorColor, 16, width);
            ConsoleUIHelper.DrawEmptyLine(width);
            ConsoleUIHelper.DrawBottomBorder(width);

            Console.WriteLine();
            ConsoleUIHelper.DrawInfo("Press Enter to keep current value.");
            Console.WriteLine();

            var newFirstName = GetOptionalInput("First Name", selectedEmployee.FirstName);
            var newLastName = GetOptionalInput("Last Name", selectedEmployee.LastName);
            var newPosition = GetOptionalInput("Position", selectedEmployee.Position);

            decimal newDailyRate = selectedEmployee.DailyRate;
            ConsoleUIHelper.DrawInputPrompt($"Daily Rate [{selectedEmployee.DailyRate:C}]: ");
            var dailyRateInput = Console.ReadLine()?.Trim();
            if (!string.IsNullOrWhiteSpace(dailyRateInput) && decimal.TryParse(dailyRateInput, out var rate))
            {
                newDailyRate = rate;
            }

            var newStatus = selectedEmployee.IsActive;
            ConsoleUIHelper.DrawInputPrompt($"Status [{(selectedEmployee.IsActive ? "Active" : "Inactive")}]: ");
            var statusInput = Console.ReadLine()?.Trim();
            if (!string.IsNullOrWhiteSpace(statusInput))
            {
                newStatus = statusInput.Equals("Active", StringComparison.OrdinalIgnoreCase);
            }

            try
            {
                selectedEmployee.FirstName = newFirstName ?? selectedEmployee.FirstName;
                selectedEmployee.LastName = newLastName ?? selectedEmployee.LastName;
                selectedEmployee.Position = newPosition ?? selectedEmployee.Position;
                selectedEmployee.DailyRate = newDailyRate;
                selectedEmployee.IsActive = newStatus;

                if (_employeeService.UpdateEmployee(selectedEmployee))
                {
                    ConsoleUIHelper.DrawSuccess($"Employee '{selectedEmployee.FullName}' updated successfully!");
                }
                else
                {
                    ConsoleUIHelper.DrawError("Failed to update employee.");
                }
            }
            catch (Exception ex)
            {
                ConsoleUIHelper.DrawError($"Error updating employee: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes an employee with confirmation
        /// </summary>
        private void DeleteEmployee()
        {
            ConsoleUIHelper.DrawPageHeader("Employees", "Employee Management › Delete");

            var employees = _employeeService.GetAllEmployees();
            if (!employees.Any())
            {
                ConsoleUIHelper.DrawWarning("No employees found in the system.");
                return;
            }

            ConsoleUIHelper.DrawMiniHeader("Select Employee to Delete");

            for (int i = 0; i < employees.Count; i++)
            {
                ConsoleUIHelper.Write("    ", ConsoleColor.DarkGray);
                ConsoleUIHelper.Write($"[", ConsoleUIHelper.MutedColor);
                ConsoleUIHelper.Write($"{(i + 1).ToString().PadLeft(2)}", ConsoleUIHelper.PrimaryColor);
                ConsoleUIHelper.Write($"]", ConsoleUIHelper.MutedColor);
                ConsoleUIHelper.Write($"  {employees[i].FullName}", ConsoleColor.White);
                ConsoleUIHelper.Write($"  ·  ", ConsoleUIHelper.MutedColor);
                ConsoleUIHelper.WriteLine(employees[i].Position, ConsoleUIHelper.SubtleColor);
            }

            var empChoice = GetValidNumber("Employee #", 1, employees.Count);
            var selectedEmployee = employees[empChoice - 1];

            // Show employee details
            var width = ConsoleUIHelper.DefaultWidth;
            Console.WriteLine();
            ConsoleUIHelper.DrawTopBorder(width);
            ConsoleUIHelper.DrawEmptyLine(width);
            ConsoleUIHelper.DrawBoxLine("EMPLOYEE TO DELETE", ConsoleUIHelper.ErrorColor, width);
            ConsoleUIHelper.DrawEmptyLine(width);
            ConsoleUIHelper.DrawThinSeparator(width);
            ConsoleUIHelper.DrawKeyValue("Name:", selectedEmployee.FullName, 14, width);
            ConsoleUIHelper.DrawKeyValue("Position:", selectedEmployee.Position, 14, width);
            ConsoleUIHelper.DrawKeyValueColored("Daily Rate:", selectedEmployee.DailyRate.ToString("C"), ConsoleUIHelper.AccentColor, 14, width);
            ConsoleUIHelper.DrawEmptyLine(width);
            ConsoleUIHelper.DrawBottomBorder(width);

            if (ConsoleUIHelper.DrawConfirmation("Are you sure you want to delete this employee?"))
            {
                try
                {
                    if (_employeeService.DeleteEmployee(selectedEmployee.Id))
                    {
                        ConsoleUIHelper.DrawSuccess($"Employee '{selectedEmployee.FullName}' has been deleted.");
                    }
                    else
                    {
                        ConsoleUIHelper.DrawError("Failed to delete employee.");
                    }
                }
                catch (Exception ex)
                {
                    ConsoleUIHelper.DrawError($"Error deleting employee: {ex.Message}");
                }
            }
            else
            {
                ConsoleUIHelper.DrawInfo("Deletion cancelled.");
            }
        }

        /// <summary>
        /// Displays employees grouped by department
        /// </summary>
        private void ViewEmployeesByDepartment()
        {
            ConsoleUIHelper.DrawPageHeader("Employees", "Employee Management › By Department");

            var departments = _departmentService.GetAllDepartments();
            var allEmployees = _employeeService.GetAllEmployees();

            if (!departments.Any())
            {
                ConsoleUIHelper.DrawWarning("No departments found in the system.");
                return;
            }

            foreach (var department in departments.OrderBy(d => d.Name))
            {
                var deptEmployees = allEmployees.Where(e =>
                    e.Position.Contains(department.Name, StringComparison.OrdinalIgnoreCase) ||
                    (department.Name.Equals("ADMIN", StringComparison.OrdinalIgnoreCase) &&
                     (e.Position.Contains("Administrator") || e.Position.Contains("HR") || e.Position.Contains("Office"))))
                    .ToList();

                ConsoleUIHelper.DrawSectionHeader(department.Name);

                if (deptEmployees.Any())
                {
                    var headers = new string[] { "Name", "Position", "Daily Rate" };
                    var columnWidths = new int[] { 24, 22, 14 };
                    var rows = new List<string[]>();

                    foreach (var employee in deptEmployees.OrderBy(e => e.LastName).ThenBy(e => e.FirstName))
                    {
                        rows.Add(new string[] {
                            employee.FullName,
                            employee.Position,
                            employee.DailyRate.ToString("C")
                        });
                    }

                    ConsoleUIHelper.DrawTable(headers, rows, columnWidths);
                    ConsoleUIHelper.Write("    ", ConsoleColor.DarkGray);
                    ConsoleUIHelper.Write($"Subtotal: ", ConsoleUIHelper.SubtleColor);
                    ConsoleUIHelper.WriteLine($"{deptEmployees.Count} employees", ConsoleUIHelper.PrimaryColor);
                }
                else
                {
                    ConsoleUIHelper.DrawInfo("No employees in this department");
                }
            }

            ConsoleUIHelper.DrawFooter("Total Departments", departments.Count.ToString());
        }

        /// <summary>
        /// Generates payroll for a specific employee
        /// </summary>
        private void GeneratePayrollForEmployee()
        {
            ConsoleUIHelper.DrawPageHeader("Payroll", "Employee Management › Generate Payroll");

            var employees = _employeeService.GetAllEmployees();
            if (!employees.Any())
            {
                ConsoleUIHelper.DrawWarning("No employees found in the system.");
                return;
            }

            ConsoleUIHelper.DrawMiniHeader("Select Employee");

            for (int i = 0; i < employees.Count; i++)
            {
                ConsoleUIHelper.Write("    ", ConsoleColor.DarkGray);
                ConsoleUIHelper.Write($"[", ConsoleUIHelper.MutedColor);
                ConsoleUIHelper.Write($"{(i + 1).ToString().PadLeft(2)}", ConsoleUIHelper.PrimaryColor);
                ConsoleUIHelper.Write($"]", ConsoleUIHelper.MutedColor);
                ConsoleUIHelper.Write($"  {employees[i].FullName}", ConsoleColor.White);
                ConsoleUIHelper.Write($"  ·  ", ConsoleUIHelper.MutedColor);
                ConsoleUIHelper.WriteLine(employees[i].Position, ConsoleUIHelper.SubtleColor);
            }

            var empChoice = GetValidNumber("Employee #", 1, employees.Count);
            var selectedEmployee = employees[empChoice - 1];

            try
            {
                ConsoleUIHelper.DrawMiniHeader("Payroll Parameters");

                var workDays = GetValidNumber("Work Days (1-31)", 1, 31);
                var overtimeHours = GetValidDecimal("Overtime Hours", 0, 100);
                var holidayHours = GetValidDecimal("Holiday Hours", 0, 100);
                var allowance = GetValidDecimal("Allowance", 0, 10000);
                var bonus = GetValidDecimal("Bonus", 0, 50000);

                var payroll = _payrollService.GenerateCompletePayroll(
                    selectedEmployee, workDays, overtimeHours, holidayHours, allowance, bonus);

                ConsoleUIHelper.DrawSuccess("Payroll generated successfully!");
                Utilities.PayslipGenerator.DisplayPayslip(payroll);
            }
            catch (Exception ex)
            {
                ConsoleUIHelper.DrawError($"Error generating payroll: {ex.Message}");
            }
        }

        #region Helper Methods

        private string GetValidInput(string label, Func<string, bool> validator)
        {
            while (true)
            {
                ConsoleUIHelper.DrawInputPrompt($"{label}: ");
                var input = Console.ReadLine()?.Trim();

                if (!string.IsNullOrEmpty(input) && validator(input))
                {
                    return input;
                }

                ConsoleUIHelper.DrawError($"{label} is required.");
            }
        }

        private string? GetOptionalInput(string label, string currentValue)
        {
            ConsoleUIHelper.DrawInputPrompt($"{label} [{currentValue}]: ");
            var input = Console.ReadLine()?.Trim();
            return string.IsNullOrWhiteSpace(input) ? null : input;
        }

        private int GetValidNumber(string label, int min, int max)
        {
            while (true)
            {
                ConsoleUIHelper.DrawInputPrompt($"{label}: ");
                var input = Console.ReadLine()?.Trim();

                if (int.TryParse(input, out var number) && number >= min && number <= max)
                {
                    return number;
                }

                ConsoleUIHelper.DrawError($"Please enter a number between {min} and {max}.");
            }
        }

        private decimal GetValidDecimal(string label, decimal min, decimal max)
        {
            while (true)
            {
                ConsoleUIHelper.DrawInputPrompt($"{label}: ");
                var input = Console.ReadLine()?.Trim();

                if (decimal.TryParse(input, out var number) && number >= min && number <= max)
                {
                    return number;
                }

                ConsoleUIHelper.DrawError($"Please enter a value between {min:C} and {max:C}.");
            }
        }

        private DateTime GetValidDate(string label, DateTime minDate, DateTime maxDate)
        {
            while (true)
            {
                ConsoleUIHelper.DrawInputPrompt($"{label}: ");
                var input = Console.ReadLine()?.Trim();

                if (DateTime.TryParse(input, out var date) && date >= minDate && date <= maxDate)
                {
                    return date;
                }

                ConsoleUIHelper.DrawError($"Please enter a valid date between {minDate:MM/dd/yyyy} and {maxDate:MM/dd/yyyy}.");
            }
        }

        #endregion
    }
}
