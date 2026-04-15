using System;
using System.Collections.Generic;
using System.Linq;
using PayrollSystem.Models;
using PayrollSystem.Exceptions;

namespace PayrollSystem.Configuration
{
    /// <summary>
    /// Service for managing system configuration settings
    /// </summary>
    public class ConfigurationService
    {
        private static Dictionary<string, ConfigurationItem> _configurations = new Dictionary<string, ConfigurationItem>();

        /// <summary>
        /// Initializes default configuration values
        /// </summary>
        public static void InitializeDefaultConfigurations()
        {
            // Tax Configuration
            SetConfiguration("TAX_BRACKET_1_MIN", 0m, "Minimum salary for tax bracket 1");
            SetConfiguration("TAX_BRACKET_1_MAX", 20833m, "Maximum salary for tax bracket 1");
            SetConfiguration("TAX_BRACKET_1_RATE", 0m, "Tax rate for bracket 1");

            SetConfiguration("TAX_BRACKET_2_MIN", 20834m, "Minimum salary for tax bracket 2");
            SetConfiguration("TAX_BRACKET_2_MAX", 33333m, "Maximum salary for tax bracket 2");
            SetConfiguration("TAX_BRACKET_2_RATE", 15m, "Tax rate for bracket 2");

            SetConfiguration("TAX_BRACKET_3_MIN", 33334m, "Minimum salary for tax bracket 3");
            SetConfiguration("TAX_BRACKET_3_MAX", 66667m, "Maximum salary for tax bracket 3");
            SetConfiguration("TAX_BRACKET_3_RATE", 20m, "Tax rate for bracket 3");

            SetConfiguration("TAX_BRACKET_4_MIN", 66668m, "Minimum salary for tax bracket 4");
            SetConfiguration("TAX_BRACKET_4_MAX", 166667m, "Maximum salary for tax bracket 4");
            SetConfiguration("TAX_BRACKET_4_RATE", 25m, "Tax rate for bracket 4");

            SetConfiguration("TAX_BRACKET_5_MIN", 166668m, "Minimum salary for tax bracket 5");
            SetConfiguration("TAX_BRACKET_5_MAX", 666667m, "Maximum salary for tax bracket 5");
            SetConfiguration("TAX_BRACKET_5_RATE", 30m, "Tax rate for bracket 5");

            SetConfiguration("TAX_BRACKET_6_MIN", 666668m, "Minimum salary for tax bracket 6");
            SetConfiguration("TAX_BRACKET_6_RATE", 32m, "Tax rate for bracket 6");

            // SSS Configuration
            SetConfiguration("SSS_RATE", 4.5m, "SSS contribution rate (%)");
            SetConfiguration("SSS_MAX", 1125m, "Maximum SSS contribution");

            // PAG-IBIG Configuration
            SetConfiguration("PAGIBIG_RATE", 2m, "PAG-IBIG contribution rate (%)");
            SetConfiguration("PAGIBIG_MAX", 100m, "Maximum PAG-IBIG contribution");

            // PhilHealth Configuration
            SetConfiguration("PHILHEALTH_RATE", 2.75m, "PhilHealth contribution rate (%)");
            SetConfiguration("PHILHEALTH_MAX", 1650m, "Maximum PhilHealth contribution");

            // Overtime Configuration
            SetConfiguration("OVERTIME_RATE", 1.25m, "Overtime rate multiplier");
            SetConfiguration("HOLIDAY_RATE", 2m, "Holiday pay rate multiplier");
            SetConfiguration("REST_DAY_RATE", 1.3m, "Rest day rate multiplier");

            // Working Hours Configuration
            SetConfiguration("WORK_HOURS_PER_DAY", 8m, "Standard working hours per day");
            SetConfiguration("WORK_DAYS_PER_MONTH", 22m, "Standard working days per month");
            SetConfiguration("LUNCH_BREAK_DURATION", 1m, "Lunch break duration in hours");

            // Loan Configuration
            SetConfiguration("MAX_ACTIVE_LOANS", 3m, "Maximum active loans per employee");
            SetConfiguration("MAX_LOAN_AMOUNT", 1000000m, "Maximum loan amount");
            SetConfiguration("MAX_LOAN_TERM", 120m, "Maximum loan term in months");

            // Attendance Configuration
            SetConfiguration("WORK_START_TIME", "09:00", "Standard work start time");
            SetConfiguration("WORK_END_TIME", "18:00", "Standard work end time");
            SetConfiguration("LATE_TOLERANCE_MINUTES", 15m, "Late tolerance in minutes");

            // Deduction Configuration
            SetConfiguration("MINIMUM_WAGE", 500m, "Minimum daily wage");
            SetConfiguration("MAXIMUM_DEDUCTION_PERCENTAGE", 40m, "Maximum deduction percentage of gross salary");

            // Company Information
            SetConfiguration("COMPANY_NAME", "Sample Company Inc.", "Company name");
            SetConfiguration("COMPANY_ADDRESS", "123 Business St., City, Country", "Company address");
            SetConfiguration("COMPANY_PHONE", "+63-2-1234-5678", "Company phone");
            SetConfiguration("COMPANY_EMAIL", "hr@samplecompany.com", "Company email");

            // Payroll Configuration
            SetConfiguration("PAYROLL_CUTOFF_DAY", 15m, "Payroll cutoff day of the month");
            SetConfiguration("PAYROLL_RELEASE_DAY", 25m, "Payroll release day of the month");
            SetConfiguration("PAYROLL_PERIOD_TYPE", "SemiMonthly", "Default payroll period type");
        }

        /// <summary>
        /// Gets a configuration value
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="defaultValue">Default value if not found</param>
        /// <returns>Configuration value</returns>
        public static T GetConfiguration<T>(string key, T defaultValue = default(T))
        {
            if (_configurations.ContainsKey(key))
            {
                var config = _configurations[key];
                try
                {
                    return (T)Convert.ChangeType(config.Value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Sets a configuration value
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="value">Configuration value</param>
        /// <param name="description">Configuration description</param>
        public static void SetConfiguration(string key, object value, string description = "")
        {
            var config = new ConfigurationItem
            {
                Key = key,
                Value = value,
                Description = description,
                LastModified = DateTime.Now,
                ModifiedBy = "System"
            };

            _configurations[key] = config;
        }

        /// <summary>
        /// Gets all configurations
        /// </summary>
        /// <returns>Dictionary of all configurations</returns>
        public static Dictionary<string, ConfigurationItem> GetAllConfigurations()
        {
            return _configurations.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        /// <summary>
        /// Gets configurations by category
        /// </summary>
        /// <param name="category">Configuration category</param>
        /// <returns>List of configurations in the category</returns>
        public static List<ConfigurationItem> GetConfigurationsByCategory(string category)
        {
            return _configurations.Values
                                 .Where(c => c.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                                 .OrderBy(c => c.Key)
                                 .ToList();
        }

        /// <summary>
        /// Deletes a configuration
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <returns>True if deleted, false if not found</returns>
        public static bool DeleteConfiguration(string key)
        {
            return _configurations.Remove(key);
        }

        /// <summary>
        /// Gets tax bracket information
        /// </summary>
        /// <returns>List of tax brackets</returns>
        public static List<TaxBracket> GetTaxBrackets()
        {
            var brackets = new List<TaxBracket>();

            for (int i = 1; i <= 6; i++)
            {
                var minKey = $"TAX_BRACKET_{i}_MIN";
                var maxKey = $"TAX_BRACKET_{i}_MAX";
                var rateKey = $"TAX_BRACKET_{i}_RATE";

                var min = GetConfiguration<decimal>(minKey);
                var max = GetConfiguration<decimal>(maxKey, decimal.MaxValue);
                var rate = GetConfiguration<decimal>(rateKey);

                brackets.Add(new TaxBracket
                {
                    Bracket = i,
                    Minimum = min,
                    Maximum = max,
                    Rate = rate / 100m
                });
            }

            return brackets;
        }

        /// <summary>
        /// Gets government contribution rates
        /// </summary>
        /// <returns>Government contribution rates</returns>
        public static GovernmentContributions GetGovernmentContributions()
        {
            return new GovernmentContributions
            {
                SSS = new ContributionRate
                {
                    Rate = GetConfiguration<decimal>("SSS_RATE") / 100m,
                    Maximum = GetConfiguration<decimal>("SSS_MAX")
                },
                PAGIBIG = new ContributionRate
                {
                    Rate = GetConfiguration<decimal>("PAGIBIG_RATE") / 100m,
                    Maximum = GetConfiguration<decimal>("PAGIBIG_MAX")
                },
                PhilHealth = new ContributionRate
                {
                    Rate = GetConfiguration<decimal>("PHILHEALTH_RATE") / 100m,
                    Maximum = GetConfiguration<decimal>("PHILHEALTH_MAX")
                }
            };
        }

        /// <summary>
        /// Gets overtime rates
        /// </summary>
        /// <returns>Overtime rates</returns>
        public static OvertimeRates GetOvertimeRates()
        {
            return new OvertimeRates
            {
                Regular = GetConfiguration<decimal>("OVERTIME_RATE"),
                Holiday = GetConfiguration<decimal>("HOLIDAY_RATE"),
                RestDay = GetConfiguration<decimal>("REST_DAY_RATE")
            };
        }

        /// <summary>
        /// Gets working hours configuration
        /// </summary>
        /// <returns>Working hours configuration</returns>
        public static WorkingHours GetWorkingHours()
        {
            return new WorkingHours
            {
                HoursPerDay = GetConfiguration<decimal>("WORK_HOURS_PER_DAY"),
                DaysPerMonth = GetConfiguration<decimal>("WORK_DAYS_PER_MONTH"),
                LunchBreak = GetConfiguration<decimal>("LUNCH_BREAK_DURATION"),
                StartTime = GetConfiguration<string>("WORK_START_TIME"),
                EndTime = GetConfiguration<string>("WORK_END_TIME"),
                LateTolerance = GetConfiguration<int>("LATE_TOLERANCE_MINUTES")
            };
        }

        /// <summary>
        /// Gets company information
        /// </summary>
        /// <returns>Company information</returns>
        public static CompanyInfo GetCompanyInfo()
        {
            return new CompanyInfo
            {
                Name = GetConfiguration<string>("COMPANY_NAME"),
                Address = GetConfiguration<string>("COMPANY_ADDRESS"),
                Phone = GetConfiguration<string>("COMPANY_PHONE"),
                Email = GetConfiguration<string>("COMPANY_EMAIL")
            };
        }

        /// <summary>
        /// Validates configuration values
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="value">Value to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool ValidateConfiguration(string key, object value)
        {
            try
            {
                switch (key)
                {
                    case string s when s.StartsWith("TAX_"):
                        var taxValue = Convert.ToDecimal(value);
                        return taxValue >= 0 && taxValue <= 100;

                    case string s when s.StartsWith("SSS_") || s.StartsWith("PAGIBIG_") || s.StartsWith("PHILHEALTH_"):
                        var contribValue = Convert.ToDecimal(value);
                        return contribValue >= 0;

                    case string s when s.Contains("_RATE"):
                        var rateValue = Convert.ToDecimal(value);
                        return rateValue >= 0 && rateValue <= 10;

                    case string s when s.Contains("_MAX"):
                        var maxValue = Convert.ToDecimal(value);
                        return maxValue >= 0;

                    case string s when s.Contains("_TIME"):
                        var timeValue = Convert.ToString(value);
                        return TimeSpan.TryParse(timeValue, out _);

                    default:
                        return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }

    #region Configuration Models

    /// <summary>
    /// Represents a configuration item
    /// </summary>
    public class ConfigurationItem
    {
        public string Key { get; set; } = string.Empty;
        public object Value { get; set; } = null!;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = "General";
        public DateTime LastModified { get; set; }
        public string ModifiedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Tax bracket model
    /// </summary>
    public class TaxBracket
    {
        public int Bracket { get; set; }
        public decimal Minimum { get; set; }
        public decimal Maximum { get; set; }
        public decimal Rate { get; set; }
    }

    /// <summary>
    /// Contribution rate model
    /// </summary>
    public class ContributionRate
    {
        public decimal Rate { get; set; }
        public decimal Maximum { get; set; }
    }

    /// <summary>
    /// Government contributions model
    /// </summary>
    public class GovernmentContributions
    {
        public ContributionRate SSS { get; set; } = null!;
        public ContributionRate PAGIBIG { get; set; } = null!;
        public ContributionRate PhilHealth { get; set; } = null!;
    }

    /// <summary>
    /// Overtime rates model
    /// </summary>
    public class OvertimeRates
    {
        public decimal Regular { get; set; }
        public decimal Holiday { get; set; }
        public decimal RestDay { get; set; }
    }

    /// <summary>
    /// Working hours model
    /// </summary>
    public class WorkingHours
    {
        public decimal HoursPerDay { get; set; }
        public decimal DaysPerMonth { get; set; }
        public decimal LunchBreak { get; set; }
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public int LateTolerance { get; set; }
    }

    /// <summary>
    /// Company information model
    /// </summary>
    public class CompanyInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    #endregion
}
