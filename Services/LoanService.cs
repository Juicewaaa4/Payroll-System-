using System;
using System.Collections.Generic;
using System.Linq;
using PayrollSystem.Models;
using PayrollSystem.Exceptions;
using PayrollSystem.Validation;

namespace PayrollSystem.Services
{
    /// <summary>
    /// Service for managing employee loans
    /// </summary>
    public class LoanService
    {
        private static List<Loan> _loans = new List<Loan>();
        private static int _nextLoanId = 1;

        /// <summary>
        /// Creates a new loan for an employee
        /// </summary>
        /// <param name="employeeId">Employee ID</param>
        /// <param name="loanType">Type of loan</param>
        /// <param name="description">Loan description</param>
        /// <param name="principalAmount">Principal amount</param>
        /// <param name="interestRate">Interest rate (percentage)</param>
        /// <param name="termMonths">Loan term in months</param>
        /// <param name="startDeductionDate">Date to start deductions</param>
        /// <returns>The created loan</returns>
        /// <exception cref="PayrollException">Thrown when validation fails</exception>
        public Loan CreateLoan(int employeeId, string loanType, string description, decimal principalAmount,
                              decimal interestRate, int termMonths, DateTime startDeductionDate)
        {
            ValidateLoan(employeeId, loanType, principalAmount, interestRate, termMonths, startDeductionDate);

            var loan = new Loan
            {
                Id = _nextLoanId++,
                EmployeeId = employeeId,
                LoanType = loanType,
                Description = description,
                PrincipalAmount = principalAmount,
                InterestRate = interestRate,
                TermMonths = termMonths,
                LoanDate = DateTime.Now,
                StartDeductionDate = startDeductionDate,
                Status = LoanStatus.Pending
            };

            loan.MonthlyPayment = loan.CalculateMonthlyPayment();
            loan.TotalAmount = loan.MonthlyPayment * termMonths;
            loan.OutstandingBalance = loan.TotalAmount;

            _loans.Add(loan);
            return loan;
        }

        /// <summary>
        /// Gets a loan by ID
        /// </summary>
        /// <param name="id">The loan ID</param>
        /// <returns>The loan if found, null otherwise</returns>
        public Loan? GetLoan(int id)
        {
            return _loans.FirstOrDefault(l => l.Id == id);
        }

        /// <summary>
        /// Gets all loans
        /// </summary>
        /// <returns>List of all loans</returns>
        public List<Loan> GetAllLoans()
        {
            return _loans.OrderByDescending(l => l.LoanDate).ToList();
        }

        /// <summary>
        /// Gets loans for a specific employee
        /// </summary>
        /// <param name="employeeId">The employee ID</param>
        /// <returns>List of employee loans</returns>
        public List<Loan> GetEmployeeLoans(int employeeId)
        {
            return _loans.Where(l => l.EmployeeId == employeeId).OrderByDescending(l => l.LoanDate).ToList();
        }

        /// <summary>
        /// Gets active loans for a specific employee
        /// </summary>
        /// <param name="employeeId">The employee ID</param>
        /// <returns>List of active employee loans</returns>
        public List<Loan> GetActiveEmployeeLoans(int employeeId)
        {
            return _loans.Where(l => l.EmployeeId == employeeId && l.Status == LoanStatus.Active)
                        .OrderByDescending(l => l.LoanDate).ToList();
        }

        /// <summary>
        /// Gets loans by status
        /// </summary>
        /// <param name="status">The loan status</param>
        /// <returns>List of loans with the specified status</returns>
        public List<Loan> GetLoansByStatus(LoanStatus status)
        {
            return _loans.Where(l => l.Status == status).OrderByDescending(l => l.LoanDate).ToList();
        }

        /// <summary>
        /// Activates a pending loan
        /// </summary>
        /// <param name="loanId">The loan ID</param>
        /// <returns>True if activated successfully</returns>
        public bool ActivateLoan(int loanId)
        {
            var loan = _loans.FirstOrDefault(l => l.Id == loanId);
            if (loan != null && loan.Status == LoanStatus.Pending)
            {
                loan.Status = LoanStatus.Active;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Suspends an active loan
        /// </summary>
        /// <param name="loanId">The loan ID</param>
        /// <param name="reason">Reason for suspension</param>
        /// <returns>True if suspended successfully</returns>
        public bool SuspendLoan(int loanId, string reason)
        {
            var loan = _loans.FirstOrDefault(l => l.Id == loanId);
            if (loan != null && loan.Status == LoanStatus.Active)
            {
                loan.Status = LoanStatus.Suspended;
                loan.Description += $" [SUSPENDED: {reason}]";
                return true;
            }
            return false;
        }

        /// <summary>
        /// Resumes a suspended loan
        /// </summary>
        /// <param name="loanId">The loan ID</param>
        /// <returns>True if resumed successfully</returns>
        public bool ResumeLoan(int loanId)
        {
            var loan = _loans.FirstOrDefault(l => l.Id == loanId);
            if (loan != null && loan.Status == LoanStatus.Suspended)
            {
                loan.Status = LoanStatus.Active;
                loan.Description = loan.Description.Replace(" [SUSPENDED: ", " [RESUMED FROM: ");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Makes a payment towards a loan
        /// </summary>
        /// <param name="loanId">The loan ID</param>
        /// <param name="amount">Payment amount</param>
        /// <param name="paymentDate">Payment date</param>
        /// <param name="notes">Payment notes</param>
        /// <returns>True if payment successful</returns>
        public bool MakePayment(int loanId, decimal amount, DateTime paymentDate, string notes = "")
        {
            var loan = _loans.FirstOrDefault(l => l.Id == loanId);
            if (loan != null && loan.Status == LoanStatus.Active)
            {
                return loan.AddPayment(amount, paymentDate, notes);
            }
            return false;
        }

        /// <summary>
        /// Gets the total monthly loan deductions for an employee
        /// </summary>
        /// <param name="employeeId">The employee ID</param>
        /// <returns>Total monthly deduction amount</returns>
        public decimal GetMonthlyLoanDeductions(int employeeId)
        {
            return _loans.Where(l => l.EmployeeId == employeeId && l.Status == LoanStatus.Active)
                        .Sum(l => l.MonthlyPayment);
        }

        /// <summary>
        /// Gets loan statistics
        /// </summary>
        /// <returns>Dictionary with loan statistics</returns>
        public Dictionary<string, object> GetLoanStatistics()
        {
            var stats = new Dictionary<string, object>
            {
                ["TotalLoans"] = _loans.Count,
                ["ActiveLoans"] = _loans.Count(l => l.Status == LoanStatus.Active),
                ["CompletedLoans"] = _loans.Count(l => l.Status == LoanStatus.Completed),
                ["PendingLoans"] = _loans.Count(l => l.Status == LoanStatus.Pending),
                ["TotalLoanAmount"] = _loans.Sum(l => l.TotalAmount),
                ["TotalOutstandingBalance"] = _loans.Sum(l => l.OutstandingBalance),
                ["TotalCollected"] = _loans.Sum(l => l.TotalPaid)
            };

            return stats;
        }

        /// <summary>
        /// Deletes a loan
        /// </summary>
        /// <param name="loanId">The loan ID to delete</param>
        /// <returns>True if deleted, false if not found or has payments</returns>
        public bool DeleteLoan(int loanId)
        {
            var loan = _loans.FirstOrDefault(l => l.Id == loanId);
            if (loan != null)
            {
                if (loan.Payments.Any())
                {
                    throw new PayrollException("Cannot delete loan with existing payments");
                }

                if (loan.Status == LoanStatus.Active)
                {
                    throw new PayrollException("Cannot delete active loan");
                }

                return _loans.Remove(loan);
            }
            return false;
        }

        /// <summary>
        /// Updates loan information
        /// </summary>
        /// <param name="loan">The loan with updated information</param>
        /// <returns>True if updated, false if not found</returns>
        public bool UpdateLoan(Loan loan)
        {
            var existingLoan = _loans.FirstOrDefault(l => l.Id == loan.Id);
            if (existingLoan != null)
            {
                existingLoan.Description = loan.Description;
                existingLoan.InterestRate = loan.InterestRate;
                existingLoan.TermMonths = loan.TermMonths;
                existingLoan.MonthlyPayment = existingLoan.CalculateMonthlyPayment();
                return true;
            }
            return false;
        }

        #region Private Validation Methods

        private void ValidateLoan(int employeeId, string loanType, decimal principalAmount, 
                                 decimal interestRate, int termMonths, DateTime startDeductionDate)
        {
            if (string.IsNullOrWhiteSpace(loanType))
                throw new PayrollException("Loan type is required");

            if (principalAmount <= 0)
                throw new PayrollException("Principal amount must be greater than 0");

            if (interestRate < 0 || interestRate > 100)
                throw new PayrollException("Interest rate must be between 0 and 100");

            if (termMonths <= 0 || termMonths > 120) // Max 10 years
                throw new PayrollException("Term must be between 1 and 120 months");

            if (startDeductionDate < DateTime.Now.Date)
                throw new PayrollException("Start deduction date cannot be in the past");

            if (!ValidationHelper.IsValidMonetaryAmount(principalAmount, 1000000m))
                throw new PayrollException("Principal amount exceeds maximum limit");

            // Check if employee has too many active loans
            var activeLoans = _loans.Count(l => l.EmployeeId == employeeId && l.Status == LoanStatus.Active);
            if (activeLoans >= 3)
                throw new PayrollException("Employee cannot have more than 3 active loans");
        }

        #endregion
    }
}
