using System;
using System.ComponentModel;

namespace PayrollSystem.Models
{
    public class UserItem
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Role { get; set; } = "Staff";
        public bool IsActive { get; set; } = true;
    }

    public class DepartmentItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public class BiometricsImportRecord
    {
        public int Id { get; set; }
        public string FileName { get; set; } = "";
        public string FilePath { get; set; } = "";
        public DateTime ImportedAt { get; set; } = DateTime.Now;
        public string ImportedAtFormatted => ImportedAt.ToString("MMM dd, yyyy hh:mm tt");
        public DateTime? PeriodStart { get; set; }
        public DateTime? PeriodEnd { get; set; }
        public string PeriodRange => PeriodStart.HasValue && PeriodEnd.HasValue
            ? $"{PeriodStart.Value:MMM dd} - {PeriodEnd.Value:MMM dd, yyyy}"
            : "Unknown";
        public int EmployeeCount { get; set; }
        public string FileHash { get; set; } = ""; // For duplicate detection
    }

    public class PayrollHistoryRecord : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public int Id { get; set; }
        public string EmployeeName { get; set; } = "";
        public string EmpNumber { get; set; } = "";
        public DateTime PayrollDate { get; set; }
        public string PayrollDateFormatted { get; set; } = "";
        public decimal GrossRaw { get; set; }
        public string GrossSalary { get; set; } = "";
        public decimal DeductionsRaw { get; set; }
        public string Deductions { get; set; } = "";
        public decimal NetPayRaw { get; set; }
        public string NetPay { get; set; } = "";

        private string _status = "Processed";
        public string Status 
        { 
            get => _status; 
            set 
            { 
                if (_status != value) 
                { 
                    _status = value; 
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status))); 
                } 
            } 
        }
        
        // Processing Parameters
        public int WorkDays { get; set; }
        public DateTime PeriodStart { get; set; } = DateTime.Now;
        public DateTime PeriodEnd { get; set; } = DateTime.Now;
        
        // Earnings variables
        public decimal OvertimeHours { get; set; }
        public decimal HolidayHours { get; set; }
        public decimal Allowance { get; set; }
        public decimal Bonus { get; set; }

        // Deduction breakdown
        public decimal Sss { get; set; }
        public decimal Pagibig { get; set; }
        public decimal Philhealth { get; set; }
        public decimal Loan { get; set; }
        public decimal Late { get; set; }
        public decimal Undertime { get; set; }
        public decimal CashAdvance { get; set; }
        public decimal Others { get; set; }
        public string OthersName { get; set; } = "Others";
    }

    public class AuditLogRecord
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string TimestampFormatted => Timestamp.ToString("MMM dd, yyyy  h:mm:ss tt");
        public string Action { get; set; } = "";
        public string Description { get; set; } = "";
    }
}
