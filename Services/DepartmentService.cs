using System;
using System.Collections.Generic;
using System.Linq;
using PayrollSystem.Models;
using PayrollSystem.Exceptions;
using PayrollSystem.Validation;

namespace PayrollSystem.Services
{
    /// <summary>
    /// Service for managing departments and positions
    /// </summary>
    public class DepartmentService
    {
        private static List<Department> _departments = new List<Department>();
        private static List<Position> _positions = new List<Position>();
        private static int _nextDepartmentId = 1;
        private static int _nextPositionId = 1;

        #region Department Management

        /// <summary>
        /// Creates a new department
        /// </summary>
        /// <param name="name">Department name</param>
        /// <param name="description">Department description</param>
        /// <param name="location">Department location</param>
        /// <param name="budget">Department budget</param>
        /// <returns>The created department</returns>
        /// <exception cref="PayrollException">Thrown when validation fails</exception>
        public Department CreateDepartment(string name, string description, string location, decimal budget)
        {
            ValidateDepartment(name, budget);

            var department = new Department
            {
                Id = _nextDepartmentId++,
                Name = name,
                Description = description,
                Location = location,
                Budget = budget,
                EmployeeCount = 0
            };

            _departments.Add(department);
            return department;
        }

        /// <summary>
        /// Gets a department by ID
        /// </summary>
        /// <param name="id">The department ID</param>
        /// <returns>The department if found, null otherwise</returns>
        public Department? GetDepartment(int id)
        {
            return _departments.FirstOrDefault(d => d.Id == id);
        }

        /// <summary>
        /// Gets all departments
        /// </summary>
        /// <returns>List of all departments</returns>
        public List<Department> GetAllDepartments()
        {
            return _departments.OrderBy(d => d.Name).ToList();
        }

        /// <summary>
        /// Gets active departments only
        /// </summary>
        /// <returns>List of active departments</returns>
        public List<Department> GetActiveDepartments()
        {
            return _departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToList();
        }

        /// <summary>
        /// Updates a department
        /// </summary>
        /// <param name="department">The department with updated information</param>
        /// <returns>True if updated, false if not found</returns>
        /// <exception cref="PayrollException">Thrown when validation fails</exception>
        public bool UpdateDepartment(Department department)
        {
            ValidateDepartment(department.Name, department.Budget);

            var existingDepartment = _departments.FirstOrDefault(d => d.Id == department.Id);
            if (existingDepartment != null)
            {
                existingDepartment.Name = department.Name;
                existingDepartment.Description = department.Description;
                existingDepartment.ManagerName = department.ManagerName;
                existingDepartment.ManagerId = department.ManagerId;
                existingDepartment.Location = department.Location;
                existingDepartment.Budget = department.Budget;
                existingDepartment.IsActive = department.IsActive;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Deletes a department
        /// </summary>
        /// <param name="id">The department ID to delete</param>
        /// <returns>True if deleted, false if not found or has employees</returns>
        public bool DeleteDepartment(int id)
        {
            var department = _departments.FirstOrDefault(d => d.Id == id);
            if (department != null)
            {
                if (department.EmployeeCount > 0)
                {
                    throw new PayrollException("Cannot delete department with assigned employees");
                }

                // Check for positions in this department
                if (_positions.Any(p => p.DepartmentId == id))
                {
                    throw new PayrollException("Cannot delete department with assigned positions");
                }

                return _departments.Remove(department);
            }
            return false;
        }

        /// <summary>
        /// Sets a department manager
        /// </summary>
        /// <param name="departmentId">The department ID</param>
        /// <param name="managerId">The manager employee ID</param>
        /// <param name="managerName">The manager name</param>
        /// <returns>True if set successfully</returns>
        public bool SetDepartmentManager(int departmentId, int managerId, string managerName)
        {
            var department = _departments.FirstOrDefault(d => d.Id == departmentId);
            if (department != null)
            {
                department.ManagerId = managerId;
                department.ManagerName = managerName;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Updates department employee count
        /// </summary>
        /// <param name="departmentId">The department ID</param>
        /// <param name="count">The new employee count</param>
        public void UpdateDepartmentEmployeeCount(int departmentId, int count)
        {
            var department = _departments.FirstOrDefault(d => d.Id == departmentId);
            if (department != null)
            {
                department.EmployeeCount = count;
            }
        }

        #endregion

        #region Position Management

        /// <summary>
        /// Creates a new position
        /// </summary>
        /// <param name="title">Position title</param>
        /// <param name="description">Position description</param>
        /// <param name="departmentId">Department ID</param>
        /// <param name="level">Position level</param>
        /// <param name="minSalary">Minimum salary</param>
        /// <param name="maxSalary">Maximum salary</param>
        /// <returns>The created position</returns>
        /// <exception cref="PayrollException">Thrown when validation fails</exception>
        public Position CreatePosition(string title, string description, int departmentId, 
                                     PositionLevel level, decimal minSalary, decimal maxSalary)
        {
            ValidatePosition(title, departmentId, minSalary, maxSalary);

            var position = new Position
            {
                Id = _nextPositionId++,
                Title = title,
                Description = description,
                DepartmentId = departmentId,
                Level = level,
                MinSalary = minSalary,
                MaxSalary = maxSalary
            };

            _positions.Add(position);
            return position;
        }

        /// <summary>
        /// Gets a position by ID
        /// </summary>
        /// <param name="id">The position ID</param>
        /// <returns>The position if found, null otherwise</returns>
        public Position? GetPosition(int id)
        {
            var position = _positions.FirstOrDefault(p => p.Id == id);
            if (position != null)
            {
                position.Department = _departments.FirstOrDefault(d => d.Id == position.DepartmentId);
            }
            return position;
        }

        /// <summary>
        /// Gets all positions
        /// </summary>
        /// <returns>List of all positions</returns>
        public List<Position> GetAllPositions()
        {
            var positions = _positions.ToList();
            foreach (var position in positions)
            {
                position.Department = _departments.FirstOrDefault(d => d.Id == position.DepartmentId);
            }
            return positions.OrderBy(p => p.Title).ToList();
        }

        /// <summary>
        /// Gets positions by department
        /// </summary>
        /// <param name="departmentId">The department ID</param>
        /// <returns>List of positions in the department</returns>
        public List<Position> GetPositionsByDepartment(int departmentId)
        {
            var positions = _positions.Where(p => p.DepartmentId == departmentId).ToList();
            foreach (var position in positions)
            {
                position.Department = _departments.FirstOrDefault(d => d.Id == position.DepartmentId);
            }
            return positions.OrderBy(p => p.Level).ThenBy(p => p.Title).ToList();
        }

        /// <summary>
        /// Gets active positions only
        /// </summary>
        /// <returns>List of active positions</returns>
        public List<Position> GetActivePositions()
        {
            var positions = _positions.Where(p => p.IsActive).ToList();
            foreach (var position in positions)
            {
                position.Department = _departments.FirstOrDefault(d => d.Id == position.DepartmentId);
            }
            return positions.OrderBy(p => p.Title).ToList();
        }

        /// <summary>
        /// Updates a position
        /// </summary>
        /// <param name="position">The position with updated information</param>
        /// <returns>True if updated, false if not found</returns>
        /// <exception cref="PayrollException">Thrown when validation fails</exception>
        public bool UpdatePosition(Position position)
        {
            ValidatePosition(position.Title, position.DepartmentId, position.MinSalary, position.MaxSalary);

            var existingPosition = _positions.FirstOrDefault(p => p.Id == position.Id);
            if (existingPosition != null)
            {
                existingPosition.Title = position.Title;
                existingPosition.Description = position.Description;
                existingPosition.DepartmentId = position.DepartmentId;
                existingPosition.Level = position.Level;
                existingPosition.MinSalary = position.MinSalary;
                existingPosition.MaxSalary = position.MaxSalary;
                existingPosition.IsActive = position.IsActive;
                existingPosition.RequiredSkills = position.RequiredSkills;
                existingPosition.Responsibilities = position.Responsibilities;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Deletes a position
        /// </summary>
        /// <param name="id">The position ID to delete</param>
        /// <returns>True if deleted, false if not found</returns>
        public bool DeletePosition(int id)
        {
            var position = _positions.FirstOrDefault(p => p.Id == id);
            if (position != null)
            {
                return _positions.Remove(position);
            }
            return false;
        }

        /// <summary>
        /// Adds a required skill to a position
        /// </summary>
        /// <param name="positionId">The position ID</param>
        /// <param name="skill">The skill to add</param>
        /// <returns>True if added successfully</returns>
        public bool AddRequiredSkill(int positionId, string skill)
        {
            var position = _positions.FirstOrDefault(p => p.Id == positionId);
            if (position != null && !position.RequiredSkills.Contains(skill))
            {
                position.RequiredSkills.Add(skill);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes a required skill from a position
        /// </summary>
        /// <param name="positionId">The position ID</param>
        /// <param name="skill">The skill to remove</param>
        /// <returns>True if removed successfully</returns>
        public bool RemoveRequiredSkill(int positionId, string skill)
        {
            var position = _positions.FirstOrDefault(p => p.Id == positionId);
            if (position != null)
            {
                return position.RequiredSkills.Remove(skill);
            }
            return false;
        }

        #endregion

        #region Private Validation Methods

        private void ValidateDepartment(string name, decimal budget)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new PayrollException("Department name is required");

            if (budget < 0)
                throw new PayrollException("Budget cannot be negative");

            if (_departments.Any(d => d.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                throw new PayrollException($"Department '{name}' already exists");
        }

        private void ValidatePosition(string title, int departmentId, decimal minSalary, decimal maxSalary)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new PayrollException("Position title is required");

            if (minSalary <= 0 || maxSalary <= 0)
                throw new PayrollException("Salaries must be greater than 0");

            if (minSalary > maxSalary)
                throw new PayrollException("Minimum salary cannot be greater than maximum salary");

            if (!_departments.Any(d => d.Id == departmentId))
                throw new PayrollException("Invalid department ID");

            if (_positions.Any(p => p.Title.Equals(title, StringComparison.OrdinalIgnoreCase) && 
                                   p.DepartmentId == departmentId))
                throw new PayrollException($"Position '{title}' already exists in this department");
        }

        #endregion
    }
}
