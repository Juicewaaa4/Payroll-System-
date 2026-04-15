using System;

namespace PayrollSystem.Exceptions
{
    /// <summary>
    /// Custom exception for payroll-related errors
    /// </summary>
    public class PayrollException : Exception
    {
        public PayrollException() : base() { }
        public PayrollException(string message) : base(message) { }
        public PayrollException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception for employee-related validation errors
    /// </summary>
    public class EmployeeValidationException : PayrollException
    {
        public EmployeeValidationException() : base() { }
        public EmployeeValidationException(string message) : base(message) { }
        public EmployeeValidationException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception for payroll calculation errors
    /// </summary>
    public class PayrollCalculationException : PayrollException
    {
        public PayrollCalculationException() : base() { }
        public PayrollCalculationException(string message) : base(message) { }
        public PayrollCalculationException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception for deduction-related errors
    /// </summary>
    public class DeductionException : PayrollException
    {
        public DeductionException() : base() { }
        public DeductionException(string message) : base(message) { }
        public DeductionException(string message, Exception innerException) : base(message, innerException) { }
    }
}
