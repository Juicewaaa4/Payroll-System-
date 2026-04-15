using System;
using System.Collections.Generic;
using System.Linq;
using PayrollSystem.Models;
using PayrollSystem.Exceptions;
using PayrollSystem.Validation;

namespace PayrollSystem.Services
{
    /// <summary>
    /// Service for managing employee attendance and leave requests
    /// </summary>
    public class AttendanceService
    {
        private static List<Attendance> _attendanceRecords = new List<Attendance>();
        private static List<LeaveRequest> _leaveRequests = new List<LeaveRequest>();
        private static int _nextAttendanceId = 1;
        private static int _nextLeaveId = 1;

        #region Attendance Management

        /// <summary>
        /// Records attendance for an employee
        /// </summary>
        /// <param name="employeeId">Employee ID</param>
        /// <param name="date">Attendance date</param>
        /// <param name="timeIn">Time in</param>
        /// <param name="timeOut">Time out</param>
        /// <param name="status">Attendance status</param>
        /// <param name="notes">Notes</param>
        /// <returns>The created attendance record</returns>
        /// <exception cref="PayrollException">Thrown when validation fails</exception>
        public Attendance RecordAttendance(int employeeId, DateTime date, DateTime? timeIn, 
                                          DateTime? timeOut, AttendanceStatus status, string notes = "")
        {
            ValidateAttendance(employeeId, date, timeIn, timeOut, status);

            // Check if attendance already exists for this date
            if (_attendanceRecords.Any(a => a.EmployeeId == employeeId && a.Date.Date == date.Date))
            {
                throw new PayrollException($"Attendance already recorded for employee {employeeId} on {date:MMM dd, yyyy}");
            }

            var attendance = new Attendance
            {
                Id = _nextAttendanceId++,
                EmployeeId = employeeId,
                Date = date,
                TimeIn = timeIn,
                TimeOut = timeOut,
                Status = status,
                Notes = notes,
                CreatedDate = DateTime.Now,
                IsHoliday = IsHolidayDate(date),
                IsWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday
            };

            attendance.CalculateHoursWorked();

            _attendanceRecords.Add(attendance);
            return attendance;
        }

        /// <summary>
        /// Gets attendance records for an employee
        /// </summary>
        /// <param name="employeeId">Employee ID</param>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>List of attendance records</returns>
        public List<Attendance> GetEmployeeAttendance(int employeeId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _attendanceRecords.Where(a => a.EmployeeId == employeeId);

            if (startDate.HasValue)
                query = query.Where(a => a.Date >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.Date <= endDate.Value);

            return query.OrderByDescending(a => a.Date).ToList();
        }

        /// <summary>
        /// Gets attendance records for all employees
        /// </summary>
        /// <param name="date">Specific date</param>
        /// <returns>List of attendance records</returns>
        public List<Attendance> GetAttendanceByDate(DateTime date)
        {
            return _attendanceRecords.Where(a => a.Date.Date == date.Date)
                                   .OrderBy(a => a.EmployeeId)
                                   .ToList();
        }

        /// <summary>
        /// Updates an attendance record
        /// </summary>
        /// <param name="attendance">The attendance record with updated information</param>
        /// <returns>True if updated, false if not found</returns>
        public bool UpdateAttendance(Attendance attendance)
        {
            var existingAttendance = _attendanceRecords.FirstOrDefault(a => a.Id == attendance.Id);
            if (existingAttendance != null)
            {
                existingAttendance.TimeIn = attendance.TimeIn;
                existingAttendance.TimeOut = attendance.TimeOut;
                existingAttendance.Status = attendance.Status;
                existingAttendance.Notes = attendance.Notes;
                existingAttendance.ModifiedDate = DateTime.Now;
                existingAttendance.CalculateHoursWorked();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets attendance summary for an employee in a period
        /// </summary>
        /// <param name="employeeId">Employee ID</param>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>Dictionary with attendance summary</returns>
        public Dictionary<string, object> GetAttendanceSummary(int employeeId, DateTime startDate, DateTime endDate)
        {
            var records = GetEmployeeAttendance(employeeId, startDate, endDate);

            var summary = new Dictionary<string, object>
            {
                ["TotalDays"] = records.Count,
                ["PresentDays"] = records.Count(a => a.Status == AttendanceStatus.Present),
                ["AbsentDays"] = records.Count(a => a.Status == AttendanceStatus.Absent),
                ["LateDays"] = records.Count(a => a.Status == AttendanceStatus.Late),
                ["HalfDays"] = records.Count(a => a.Status == AttendanceStatus.HalfDay),
                ["LeaveDays"] = records.Count(a => a.Status == AttendanceStatus.Leave),
                ["TotalHoursWorked"] = records.Sum(a => a.HoursWorked),
                ["TotalOvertimeHours"] = records.Sum(a => a.OvertimeHours),
                ["AverageHoursPerDay"] = records.Any() ? records.Average(a => a.HoursWorked) : 0m
            };

            return summary;
        }

        /// <summary>
        /// Gets employees with attendance issues
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>List of employees with attendance issues</returns>
        public List<int> GetEmployeesWithAttendanceIssues(DateTime startDate, DateTime endDate)
        {
            var employeeIds = _attendanceRecords.Where(a => a.Date >= startDate && a.Date <= endDate)
                                              .Select(a => a.EmployeeId)
                                              .Distinct()
                                              .ToList();

            var problematicEmployees = new List<int>();

            foreach (var employeeId in employeeIds)
            {
                var records = GetEmployeeAttendance(employeeId, startDate, endDate);
                var absentDays = records.Count(a => a.Status == AttendanceStatus.Absent);
                var lateDays = records.Count(a => a.Status == AttendanceStatus.Late);

                // Consider problematic if absent more than 3 days or late more than 5 days
                if (absentDays > 3 || lateDays > 5)
                {
                    problematicEmployees.Add(employeeId);
                }
            }

            return problematicEmployees;
        }

        #endregion

        #region Leave Management

        /// <summary>
        /// Creates a leave request
        /// </summary>
        /// <param name="employeeId">Employee ID</param>
        /// <param name="leaveType">Type of leave</param>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <param name="reason">Reason for leave</param>
        /// <returns>The created leave request</returns>
        /// <exception cref="PayrollException">Thrown when validation fails</exception>
        public LeaveRequest CreateLeaveRequest(int employeeId, LeaveType leaveType, 
                                             DateTime startDate, DateTime endDate, string reason)
        {
            ValidateLeaveRequest(employeeId, leaveType, startDate, endDate, reason);

            var leaveRequest = new LeaveRequest
            {
                Id = _nextLeaveId++,
                EmployeeId = employeeId,
                LeaveType = leaveType,
                StartDate = startDate,
                EndDate = endDate,
                DaysRequested = (decimal)(endDate - startDate).TotalDays + 1,
                Reason = reason,
                Status = LeaveStatus.Pending,
                RequestDate = DateTime.Now
            };

            _leaveRequests.Add(leaveRequest);
            return leaveRequest;
        }

        /// <summary>
        /// Approves a leave request
        /// </summary>
        /// <param name="leaveId">Leave request ID</param>
        /// <param name="approvedBy">Approver employee ID</param>
        /// <param name="approverName">Approver name</param>
        /// <param name="remarks">Approval remarks</param>
        /// <returns>True if approved successfully</returns>
        public bool ApproveLeaveRequest(int leaveId, int approvedBy, string approverName, string remarks = "")
        {
            var leaveRequest = _leaveRequests.FirstOrDefault(l => l.Id == leaveId);
            if (leaveRequest != null && leaveRequest.Status == LeaveStatus.Pending)
            {
                leaveRequest.Status = LeaveStatus.Approved;
                leaveRequest.ApprovedDate = DateTime.Now;
                leaveRequest.ApprovedBy = approvedBy;
                leaveRequest.ApproverName = approverName;
                leaveRequest.Remarks = remarks;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Rejects a leave request
        /// </summary>
        /// <param name="leaveId">Leave request ID</param>
        /// <param name="rejectedBy">Rejector employee ID</param>
        /// <param name="rejectorName">Rejector name</param>
        /// <param name="remarks">Rejection remarks</param>
        /// <returns>True if rejected successfully</returns>
        public bool RejectLeaveRequest(int leaveId, int rejectedBy, string rejectorName, string remarks = "")
        {
            var leaveRequest = _leaveRequests.FirstOrDefault(l => l.Id == leaveId);
            if (leaveRequest != null && leaveRequest.Status == LeaveStatus.Pending)
            {
                leaveRequest.Status = LeaveStatus.Rejected;
                leaveRequest.Remarks = remarks;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets leave requests for an employee
        /// </summary>
        /// <param name="employeeId">Employee ID</param>
        /// <returns>List of leave requests</returns>
        public List<LeaveRequest> GetEmployeeLeaveRequests(int employeeId)
        {
            return _leaveRequests.Where(l => l.EmployeeId == employeeId)
                                .OrderByDescending(l => l.RequestDate)
                                .ToList();
        }

        /// <summary>
        /// Gets all leave requests
        /// </summary>
        /// <param name="status">Filter by status (optional)</param>
        /// <returns>List of leave requests</returns>
        public List<LeaveRequest> GetAllLeaveRequests(LeaveStatus? status = null)
        {
            var query = _leaveRequests.AsQueryable();
            
            if (status.HasValue)
                query = query.Where(l => l.Status == status.Value);

            return query.OrderByDescending(l => l.RequestDate).ToList();
        }

        /// <summary>
        /// Gets pending leave requests
        /// </summary>
        /// <returns>List of pending leave requests</returns>
        public List<LeaveRequest> GetPendingLeaveRequests()
        {
            return _leaveRequests.Where(l => l.Status == LeaveStatus.Pending)
                                .OrderBy(l => l.StartDate)
                                .ToList();
        }

        /// <summary>
        /// Marks leave as used
        /// </summary>
        /// <param name="leaveId">Leave request ID</param>
        /// <returns>True if marked successfully</returns>
        public bool MarkLeaveAsUsed(int leaveId)
        {
            var leaveRequest = _leaveRequests.FirstOrDefault(l => l.Id == leaveId);
            if (leaveRequest != null && leaveRequest.Status == LeaveStatus.Approved)
            {
                leaveRequest.Status = LeaveStatus.Used;
                return true;
            }
            return false;
        }

        #endregion

        #region Private Methods

        private void ValidateAttendance(int employeeId, DateTime date, DateTime? timeIn, 
                                      DateTime? timeOut, AttendanceStatus status)
        {
            if (date > DateTime.Now)
                throw new PayrollException("Cannot record attendance for future dates");

            if (timeIn.HasValue && timeOut.HasValue)
            {
                if (timeIn.Value >= timeOut.Value)
                    throw new PayrollException("Time in must be before time out");

                if (timeIn.Value.Date != date.Date || timeOut.Value.Date != date.Date)
                    throw new PayrollException("Time in and time out must be on the attendance date");
            }

            if (status == AttendanceStatus.Present && (!timeIn.HasValue || !timeOut.HasValue))
                throw new PayrollException("Time in and time out are required for present attendance");
        }

        private void ValidateLeaveRequest(int employeeId, LeaveType leaveType, DateTime startDate, 
                                        DateTime endDate, string reason)
        {
            if (startDate > endDate)
                throw new PayrollException("Start date must be before end date");

            if (startDate < DateTime.Now.Date)
                throw new PayrollException("Cannot request leave for past dates");

            var daysRequested = (endDate - startDate).TotalDays + 1;
            if (daysRequested > 30) // Max 30 days
                throw new PayrollException("Leave cannot exceed 30 days");

            // Check for overlapping leave requests
            if (_leaveRequests.Any(l => l.EmployeeId == employeeId && 
                                       l.Status == LeaveStatus.Approved &&
                                       ((startDate >= l.StartDate && startDate <= l.EndDate) ||
                                        (endDate >= l.StartDate && endDate <= l.EndDate) ||
                                        (startDate <= l.StartDate && endDate >= l.EndDate))))
            {
                throw new PayrollException("Leave dates overlap with an existing approved leave");
            }

            // Check for pending leave requests
            if (_leaveRequests.Any(l => l.EmployeeId == employeeId && l.Status == LeaveStatus.Pending))
            {
                throw new PayrollException("Cannot request leave while having pending leave requests");
            }
        }

        private bool IsHolidayDate(DateTime date)
        {
            // This is a simplified holiday check
            // In a real system, this would check against a holiday calendar
            var holidays = new[]
            {
                new DateTime(date.Year, 1, 1),   // New Year
                new DateTime(date.Year, 4, 9),   // Day of Valor
                new DateTime(date.Year, 5, 1),   // Labor Day
                new DateTime(date.Year, 6, 12),  // Independence Day
                new DateTime(date.Year, 12, 25), // Christmas
                new DateTime(date.Year, 12, 30),  // Rizal Day
            };

            return holidays.Contains(date.Date);
        }

        #endregion
    }
}
