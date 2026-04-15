using System;
using System.Collections.Generic;
using System.Linq;
using PayrollSystem.Models;
using PayrollSystem.Exceptions;
using PayrollSystem.Validation;

namespace PayrollSystem.Services
{
    /// <summary>
    /// Service for managing employee operations
    /// </summary>
    public class EmployeeService
    {
        private static List<Employee> _employees = new List<Employee>();
        private static int _nextId = 1;

        /// <summary>
        /// Adds a new employee to the system
        /// </summary>
        /// <param name="employee">The employee to add</param>
        /// <returns>The added employee with assigned ID</returns>
        /// <exception cref="EmployeeValidationException">Thrown when employee validation fails</exception>
        public Employee AddEmployee(Employee employee)
        {
            ValidationHelper.ValidateEmployee(employee);
            
            // Check for duplicate employees
            if (_employees.Any(e => e.FirstName.Equals(employee.FirstName, StringComparison.OrdinalIgnoreCase) && 
                                   e.LastName.Equals(employee.LastName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new EmployeeValidationException($"Employee {employee.FullName} already exists");
            }
            
            employee.Id = _nextId++;
            _employees.Add(employee);
            return employee;
        }

        /// <summary>
        /// Gets an employee by ID
        /// </summary>
        /// <param name="id">The employee ID</param>
        /// <returns>The employee if found, null otherwise</returns>
        public Employee? GetEmployee(int id)
        {
            return _employees.FirstOrDefault(e => e.Id == id);
        }

        /// <summary>
        /// Gets all employees
        /// </summary>
        /// <returns>List of all employees</returns>
        public List<Employee> GetAllEmployees()
        {
            return _employees.ToList();
        }

        /// <summary>
        /// Updates an existing employee
        /// </summary>
        /// <param name="employee">The employee with updated information</param>
        /// <returns>True if updated, false if not found</returns>
        /// <exception cref="EmployeeValidationException">Thrown when employee validation fails</exception>
        public bool UpdateEmployee(Employee employee)
        {
            ValidationHelper.ValidateEmployee(employee);
            
            var existingEmployee = _employees.FirstOrDefault(e => e.Id == employee.Id);
            if (existingEmployee != null)
            {
                // Check for duplicate employees (excluding current employee)
                if (_employees.Any(e => e.Id != employee.Id && 
                                       e.FirstName.Equals(employee.FirstName, StringComparison.OrdinalIgnoreCase) && 
                                       e.LastName.Equals(employee.LastName, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new EmployeeValidationException($"Employee {employee.FullName} already exists");
                }
                
                existingEmployee.FirstName = employee.FirstName;
                existingEmployee.LastName = employee.LastName;
                existingEmployee.Position = employee.Position;
                existingEmployee.DailyRate = employee.DailyRate;
                existingEmployee.HireDate = employee.HireDate;
                existingEmployee.IsActive = employee.IsActive;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Deletes an employee by ID
        /// </summary>
        /// <param name="id">The employee ID to delete</param>
        /// <returns>True if deleted, false if not found</returns>
        public bool DeleteEmployee(int id)
        {
            var employee = _employees.FirstOrDefault(e => e.Id == id);
            if (employee != null)
            {
                return _employees.Remove(employee);
            }
            return false;
        }

        /// <summary>
        /// Gets active employees only
        /// </summary>
        /// <returns>List of active employees</returns>
        public List<Employee> GetActiveEmployees()
        {
            return _employees.Where(e => e.IsActive).ToList();
        }

        /// <summary>
        /// Gets the total number of employees
        /// </summary>
        /// <returns>Employee count</returns>
        public int GetEmployeeCount()
        {
            return _employees.Count;
        }
    }
}
