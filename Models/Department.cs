using System.Collections.Generic;

namespace PayrollSystem.Models
{
    /// <summary>
    /// Represents a department in the organization
    /// </summary>
    public class Department
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ManagerName { get; set; }
        public int? ManagerId { get; set; }
        public bool IsActive { get; set; } = true;
        public string Location { get; set; } = string.Empty;
        public decimal Budget { get; set; }
        public int EmployeeCount { get; set; }

        /// <summary>
        /// Gets a formatted string representation of the department
        /// </summary>
        /// <returns>Formatted department string</returns>
        public override string ToString()
        {
            return $"{Name} ({EmployeeCount} employees) - {Location}";
        }
    }

    /// <summary>
    /// Represents a job position in the organization
    /// </summary>
    public class Position
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public Department? Department { get; set; }
        public PositionLevel Level { get; set; }
        public decimal MinSalary { get; set; }
        public decimal MaxSalary { get; set; }
        public bool IsActive { get; set; } = true;
        public List<string> RequiredSkills { get; set; } = new List<string>();
        public List<string> Responsibilities { get; set; } = new List<string>();

        /// <summary>
        /// Gets the salary range as a formatted string
        /// </summary>
        /// <returns>Formatted salary range</returns>
        public string SalaryRange => $"{MinSalary:C} - {MaxSalary:C}";

        /// <summary>
        /// Gets a formatted string representation of the position
        /// </summary>
        /// <returns>Formatted position string</returns>
        public override string ToString()
        {
            return $"{Title} ({Level}) - {Department?.Name ?? "Unassigned"}";
        }
    }

    /// <summary>
    /// Enumeration of position levels
    /// </summary>
    public enum PositionLevel
    {
        Intern,
        Junior,
        Regular,
        Senior,
        Lead,
        Supervisor,
        Manager,
        Director,
        VicePresident,
        Executive
    }
}
