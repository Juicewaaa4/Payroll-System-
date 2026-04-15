using System;
using System.Collections.Generic;
using System.Linq;
using PayrollSystem.Models;
using PayrollSystem.Services;

namespace PayrollSystem.Services
{
    /// <summary>
    /// Service for initializing predefined employees and departments
    /// </summary>
    public class EmployeeInitializationService
    {
        private readonly EmployeeService _employeeService;
        private readonly DepartmentService _departmentService;

        public EmployeeInitializationService(EmployeeService employeeService, DepartmentService departmentService)
        {
            _employeeService = employeeService;
            _departmentService = departmentService;
        }

        /// <summary>
        /// Initializes all predefined departments and employees
        /// </summary>
        public void InitializePredefinedData()
        {
            InitializeDepartments();
            InitializeEmployees();
        }

        /// <summary>
        /// Creates predefined departments
        /// </summary>
        private void InitializeDepartments()
        {
            var departments = new List<(string name, string description, string location)>
            {
                ("ADMIN", "Administrative Department", "Main Office"),
                ("Zoey's Eatery", "Food Service Department", "Food Court"),
                ("Billiard Tenant", "Recreation Department", "Game Area")
            };

            foreach (var dept in departments)
            {
                try
                {
                    var createdDept = _departmentService.CreateDepartment(dept.name, dept.description, dept.location, 50000m);
                    
                    // Create positions for each department
                    CreatePositionsForDepartment(createdDept.Id, dept.name);
                }
                catch (Exception)
                {
                    // Department might already exist, continue
                }
            }
        }

        /// <summary>
        /// Creates positions for a specific department
        /// </summary>
        /// <param name="departmentId">Department ID</param>
        /// <param name="departmentName">Department name</param>
        private void CreatePositionsForDepartment(int departmentId, string departmentName)
        {
            switch (departmentName)
            {
                case "ADMIN":
                    _departmentService.CreatePosition("Administrator", "Overall system administrator", departmentId, PositionLevel.Manager, 1000m, 1500m);
                    _departmentService.CreatePosition("HR Manager", "Human Resources management", departmentId, PositionLevel.Manager, 1200m, 1800m);
                    _departmentService.CreatePosition("Office Administrator", "Office administration tasks", departmentId, PositionLevel.Regular, 900m, 1300m);
                    break;

                case "Zoey's Eatery":
                    _departmentService.CreatePosition("Restaurant Manager", "Restaurant operations management", departmentId, PositionLevel.Manager, 800m, 1200m);
                    _departmentService.CreatePosition("Head Chef", "Kitchen management and food preparation", departmentId, PositionLevel.Supervisor, 700m, 1000m);
                    _departmentService.CreatePosition("Service Staff", "Customer service and food serving", departmentId, PositionLevel.Regular, 500m, 700m);
                    _departmentService.CreatePosition("Kitchen Staff", "Food preparation and kitchen duties", departmentId, PositionLevel.Regular, 450m, 650m);
                    _departmentService.CreatePosition("Cashier", "Payment processing and customer service", departmentId, PositionLevel.Regular, 400m, 600m);
                    break;

                case "Billiard Tenant":
                    _departmentService.CreatePosition("Billiard Manager", "Billiard area management", departmentId, PositionLevel.Supervisor, 600m, 900m);
                    _departmentService.CreatePosition("Game Attendant", "Game area assistance and maintenance", departmentId, PositionLevel.Regular, 400m, 600m);
                    break;
            }
        }

        /// <summary>
        /// Creates predefined employees with their roles
        /// </summary>
        private void InitializeEmployees()
        {
            var employees = new List<(string firstName, string lastName, string position, string department, decimal dailyRate)>
            {
                // ADMIN Department
                ("Kenneth Ariel", "Francisco", "Administrator", "ADMIN", 1200m),
                ("Judy", "Peralta", "HR Manager", "ADMIN", 1500m),
                ("Trecia", "De Jesus", "Office Administrator", "ADMIN", 1100m),

                // Zoey's Eatery Department
                ("Alyssa Marie", "Zamudio", "Restaurant Manager", "Zoey's Eatery", 1000m),
                ("Alliyah", "Lobendino", "Head Chef", "Zoey's Eatery", 950m),
                ("Cristel Khaye", "Sevilla", "Service Staff", "Zoey's Eatery", 650m),
                ("Michael", "Villaseñor", "Kitchen Staff", "Zoey's Eatery", 600m),
                ("Beverly", "Gabriel", "Cashier", "Zoey's Eatery", 550m),
                ("Charmine", "Resus", "Cashier", "Zoey's Eatery", 550m),
                ("Kiven", "Paez", "Service Staff", "Zoey's Eatery", 600m),

                // Billiard Tenant Department
                ("Lucky", "Flores", "Billiard Manager", "Billiard Tenant", 800m),
                ("Romez", "Bautista", "Game Attendant", "Billiard Tenant", 500m),
                ("Jerryco", "Viador", "Game Attendant", "Billiard Tenant", 500m)
            };

            var allDepartments = _departmentService.GetAllDepartments();

            foreach (var emp in employees)
            {
                try
                {
                    var department = allDepartments.FirstOrDefault(d => d.Name.Equals(emp.department, StringComparison.OrdinalIgnoreCase));
                    if (department != null)
                    {
                        var employee = new Employee
                        {
                            FirstName = emp.firstName,
                            LastName = emp.lastName,
                            Position = emp.position,
                            DailyRate = emp.dailyRate,
                            HireDate = DateTime.Now.AddMonths(-new Random().Next(1, 12)),
                            IsActive = true
                        };

                        _employeeService.AddEmployee(employee);
                        
                        // Update department employee count
                        _departmentService.UpdateDepartmentEmployeeCount(department.Id, 
                            _departmentService.GetDepartment(department.Id)!.EmployeeCount + 1);
                    }
                }
                catch (Exception)
                {
                    // Employee might already exist, continue
                }
            }
        }

        /// <summary>
        /// Gets all predefined employees by department
        /// </summary>
        /// <returns>Dictionary of employees by department</returns>
        public Dictionary<string, List<Employee>> GetEmployeesByDepartment()
        {
            var result = new Dictionary<string, List<Employee>>();
            var allEmployees = _employeeService.GetAllEmployees();
            var allDepartments = _departmentService.GetAllDepartments();

            foreach (var department in allDepartments)
            {
                var departmentEmployees = allEmployees.Where(e => 
                    e.Position.Contains(department.Name, StringComparison.OrdinalIgnoreCase) || 
                    department.Name.Equals("ADMIN", StringComparison.OrdinalIgnoreCase) && 
                    (e.Position.Contains("Administrator") || e.Position.Contains("HR") || e.Position.Contains("Office")))
                    .ToList();

                result[department.Name] = departmentEmployees;
            }

            return result;
        }
    }
}
