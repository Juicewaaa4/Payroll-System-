using System;

namespace PayrollSystem.Models
{
    /// <summary>
    /// Represents attendance record for an employee
    /// </summary>
    public class Attendance
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public Employee? Employee { get; set; }
        public DateTime Date { get; set; }
        public DateTime? TimeIn { get; set; }
        public DateTime? TimeOut { get; set; }
        public AttendanceStatus Status { get; set; }
        public decimal HoursWorked { get; set; }
        public decimal OvertimeHours { get; set; }
        public bool IsHoliday { get; set; }
        public bool IsWeekend { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }

        /// <summary>
        /// Gets the total hours worked including overtime
        /// </summary>
        public decimal TotalHours => HoursWorked + OvertimeHours;

        /// <summary>
        /// Gets whether the attendance is complete (has time in and time out)
        /// </summary>
        public bool IsComplete => TimeIn.HasValue && TimeOut.HasValue;

        /// <summary>
        /// Gets whether the employee was late
        /// </summary>
        public bool IsLate => TimeIn.HasValue && TimeIn.Value.TimeOfDay > new TimeSpan(9, 0, 0); // Assuming 9 AM start

        /// <summary>
        /// Gets whether the employee had undertime
        /// </summary>
        public bool HasUndertime => TimeOut.HasValue && TimeOut.Value.TimeOfDay < new TimeSpan(18, 0, 0); // Assuming 6 PM end

        /// <summary>
        /// Calculates the hours worked based on time in and time out
        /// </summary>
        public void CalculateHoursWorked()
        {
            if (TimeIn.HasValue && TimeOut.HasValue)
            {
                var totalHours = (TimeOut.Value - TimeIn.Value).TotalHours;
                HoursWorked = (decimal)totalHours;

                // Calculate overtime (hours beyond 8 hours)
                if (HoursWorked > 8m)
                {
                    OvertimeHours = HoursWorked - 8m;
                    HoursWorked = 8m; // Regular hours capped at 8
                }
                else
                {
                    OvertimeHours = 0m;
                }
            }
        }

        /// <summary>
        /// Gets a formatted string representation of the attendance
        /// </summary>
        /// <returns>Formatted attendance string</returns>
        public override string ToString()
        {
            return $"{Date:MMM dd, yyyy}: {Status} - {HoursWorked:F1}h ({TimeIn:HH:mm} - {TimeOut:HH:mm})";
        }
    }

    /// <summary>
    /// Enumeration of attendance statuses
    /// </summary>
    public enum AttendanceStatus
    {
        Present,
        Absent,
        Late,
        HalfDay,
        Leave,
        Holiday,
        Weekend,
        Undertime
    }

    /// <summary>
    /// Represents a leave request
    /// </summary>
    public class LeaveRequest
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public Employee? Employee { get; set; }
        public LeaveType LeaveType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal DaysRequested { get; set; }
        public string Reason { get; set; } = string.Empty;
        public LeaveStatus Status { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public int? ApprovedBy { get; set; }
        public string? ApproverName { get; set; }
        public string? Remarks { get; set; }

        /// <summary>
        /// Gets the number of days for the leave request
        /// </summary>
        public int DaysCount => (int)Math.Ceiling((EndDate - StartDate).TotalDays) + 1;

        /// <summary>
        /// Gets whether the leave is approved
        /// </summary>
        public bool IsApproved => Status == LeaveStatus.Approved;

        /// <summary>
        /// Gets whether the leave is pending
        /// </summary>
        public bool IsPending => Status == LeaveStatus.Pending;

        /// <summary>
        /// Gets a formatted string representation of the leave request
        /// </summary>
        /// <returns>Formatted leave request string</returns>
        public override string ToString()
        {
            return $"{LeaveType}: {StartDate:MMM dd} - {EndDate:MMM dd} ({DaysCount} days) - {Status}";
        }
    }

    /// <summary>
    /// Enumeration of leave types
    /// </summary>
    public enum LeaveType
    {
        SickLeave,
        VacationLeave,
        MaternityLeave,
        PaternityLeave,
        EmergencyLeave,
        BereavementLeave,
        StudyLeave,
        UnpaidLeave,
        SpecialLeave
    }

    /// <summary>
    /// Enumeration of leave statuses
    /// </summary>
    public enum LeaveStatus
    {
        Pending,
        Approved,
        Rejected,
        Cancelled,
        Used
    }
}
