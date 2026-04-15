using System;
using System.Collections.Generic;

namespace PayrollSystem.Models
{
    /// <summary>
    /// Represents a loan granted to an employee
    /// </summary>
    public class Loan
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public Employee? Employee { get; set; }
        public string LoanType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal PrincipalAmount { get; set; }
        public decimal InterestRate { get; set; }
        public decimal TotalAmount { get; set; }
        public int TermMonths { get; set; }
        public decimal MonthlyPayment { get; set; }
        public DateTime LoanDate { get; set; }
        public DateTime StartDeductionDate { get; set; }
        public DateTime? EndDate { get; set; }
        public LoanStatus Status { get; set; }
        public decimal OutstandingBalance { get; set; }
        public List<LoanPayment> Payments { get; set; } = new List<LoanPayment>();

        /// <summary>
        /// Gets the total amount paid so far
        /// </summary>
        public decimal TotalPaid => Payments.Sum(p => p.Amount);

        /// <summary>
        /// Gets the number of payments made
        /// </summary>
        public int PaymentsMade => Payments.Count;

        /// <summary>
        /// Gets the number of remaining payments
        /// </summary>
        public int RemainingPayments => Status == LoanStatus.Active ? 
            Math.Max(0, TermMonths - PaymentsMade) : 0;

        /// <summary>
        /// Calculates the monthly payment based on loan terms
        /// </summary>
        /// <returns>Monthly payment amount</returns>
        public decimal CalculateMonthlyPayment()
        {
            if (InterestRate == 0)
            {
                return PrincipalAmount / TermMonths;
            }

            // Simple interest calculation
            var totalInterest = PrincipalAmount * (InterestRate / 100) * (TermMonths / 12m);
            var totalWithInterest = PrincipalAmount + totalInterest;
            return totalWithInterest / TermMonths;
        }

        /// <summary>
        /// Adds a payment to the loan
        /// </summary>
        /// <param name="amount">Payment amount</param>
        /// <param name="paymentDate">Payment date</param>
        /// <param name="notes">Payment notes</param>
        /// <returns>True if payment added successfully</returns>
        public bool AddPayment(decimal amount, DateTime paymentDate, string notes = "")
        {
            if (amount <= 0 || amount > OutstandingBalance)
                return false;

            var payment = new LoanPayment
            {
                Id = Payments.Count + 1,
                LoanId = Id,
                Amount = amount,
                PaymentDate = paymentDate,
                Notes = notes
            };

            Payments.Add(payment);
            OutstandingBalance -= amount;

            if (OutstandingBalance <= 0)
            {
                OutstandingBalance = 0;
                Status = LoanStatus.Completed;
                EndDate = paymentDate;
            }

            return true;
        }

        /// <summary>
        /// Gets a formatted string representation of the loan
        /// </summary>
        /// <returns>Formatted loan string</returns>
        public override string ToString()
        {
            return $"{LoanType}: {PrincipalAmount:C} @ {InterestRate}% - {Status} (Balance: {OutstandingBalance:C})";
        }
    }

    /// <summary>
    /// Represents a payment made towards a loan
    /// </summary>
    public class LoanPayment
    {
        public int Id { get; set; }
        public int LoanId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string Notes { get; set; } = string.Empty;
        public PaymentMethod PaymentMethod { get; set; }

        /// <summary>
        /// Gets a formatted string representation of the payment
        /// </summary>
        /// <returns>Formatted payment string</returns>
        public override string ToString()
        {
            return $"{PaymentDate:MMM dd, yyyy}: {Amount:C} - {Notes}";
        }
    }

    /// <summary>
    /// Enumeration of loan statuses
    /// </summary>
    public enum LoanStatus
    {
        Pending,
        Active,
        Suspended,
        Completed,
        Defaulted
    }

    /// <summary>
    /// Enumeration of payment methods
    /// </summary>
    public enum PaymentMethod
    {
        PayrollDeduction,
        Cash,
        BankTransfer,
        Check,
        Other
    }
}
