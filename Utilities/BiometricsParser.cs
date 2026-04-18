using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using ExcelDataReader;

namespace PayrollSystem.Utilities
{
    public class AttendanceSummary
    {
        public string EmpNumber { get; set; } = "";
        public string Name { get; set; } = "";
        public int PresentDays { get; set; }
        public int LateMinutes { get; set; }
        public int UndertimeMinutes { get; set; }
        public int OvertimeHours { get; set; }
    }

    public class BiometricsParser
    {
        static BiometricsParser()
        {
            // Required for ExcelDataReader on .NET Core / .NET 5+ to support Windows-1252 encodings
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        }

        public static List<AttendanceSummary> ParseExcelFile(string filePath)
        {
            var summaries = new List<AttendanceSummary>();

            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    var result = reader.AsDataSet();

                    // Specifically look for the "Attend. Report" table, or fallback to first table
                    DataTable? table = null;
                    foreach (DataTable dt in result.Tables)
                    {
                        if (dt.TableName.Contains("Attend. Report") || dt.TableName.Contains("Attend"))
                        {
                            table = dt;
                            break;
                        }
                    }
                    if (table == null && result.Tables.Count > 0)
                        table = result.Tables[0]; // fallback
                    
                    if (table == null) return summaries;

                    // Scan for Employee Blocks
                    for (int r = 0; r < table.Rows.Count; r++)
                    {
                        for (int c = 0; c < table.Columns.Count; c++)
                        {
                            string cellValue = table.Rows[r][c]?.ToString()?.Trim() ?? "";

                            // Identify the start of a data block
                            if (cellValue.Equals("Date/Week", StringComparison.OrdinalIgnoreCase))
                            {
                                int anchorRow = r;
                                int anchorCol = c;

                                // 1. Extract Name and ID (search above this cell)
                                string name = "Unknown";
                                string empId = "0";

                                // Search the rows directly above (r-1 to r-6) and to the right (c to c+15)
                                for (int searchR = Math.Max(0, anchorRow - 6); searchR < anchorRow; searchR++)
                                {
                                    for (int searchC = anchorCol; searchC < Math.Min(table.Columns.Count, anchorCol + 20); searchC++)
                                    {
                                        string val = table.Rows[searchR][searchC]?.ToString()?.Trim() ?? "";
                                        
                                        if (val.Equals("Name", StringComparison.OrdinalIgnoreCase))
                                        {
                                            // Name is usually the next non-empty cell to the right
                                            for (int nextC = searchC + 1; nextC < table.Columns.Count; nextC++)
                                            {
                                                string nextVal = table.Rows[searchR][nextC]?.ToString()?.Trim() ?? "";
                                                if (!string.IsNullOrEmpty(nextVal))
                                                {
                                                    name = nextVal;
                                                    break;
                                                }
                                            }
                                        }

                                        if (val.Equals("ID", StringComparison.OrdinalIgnoreCase))
                                        {
                                            // ID is usually the next non-empty cell to the right
                                            for (int nextC = searchC + 1; nextC < table.Columns.Count; nextC++)
                                            {
                                                string nextVal = table.Rows[searchR][nextC]?.ToString()?.Trim() ?? "";
                                                if (!string.IsNullOrEmpty(nextVal))
                                                {
                                                    empId = nextVal;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }

                                // 2. Parse the Data Rows below
                                int presentDays = 0;
                                int lateMins = 0;
                                int undertimeMins = 0;
                                int overtimeHours = 0;

                                // Data starts 2 rows below "Date/Week"
                                int dataStartRow = anchorRow + 2;
                                
                                TimeSpan shiftStart = new TimeSpan(8, 30, 0); // 8:30 AM
                                TimeSpan shiftEnd = new TimeSpan(17, 30, 0);  // 5:30 PM
                                TimeSpan graceLimit = new TimeSpan(8, 45, 0); // 8:45 AM

                                for (int dataR = dataStartRow; dataR < table.Rows.Count; dataR++)
                                {
                                    string dateStr = table.Rows[dataR][anchorCol]?.ToString()?.Trim() ?? "";
                                    
                                    // If we hit an empty row or a new header, the block is done
                                    if (string.IsNullOrEmpty(dateStr) && 
                                       (dataR + 1 < table.Rows.Count && string.IsNullOrEmpty(table.Rows[dataR+1][anchorCol]?.ToString()?.Trim())))
                                    {
                                        break; 
                                    }
                                    
                                    // Make sure we have columns
                                    if (anchorCol + 4 >= table.Columns.Count) continue;

                                    string inStr = table.Rows[dataR][anchorCol + 2]?.ToString()?.Trim() ?? "";
                                    string outStr = table.Rows[dataR][anchorCol + 4]?.ToString()?.Trim() ?? "";

                                    // Fallbacks if Time1 is empty but maybe OT IN is present
                                    if (string.IsNullOrEmpty(inStr) && string.IsNullOrEmpty(outStr) && anchorCol + 12 < table.Columns.Count)
                                    {
                                         inStr = table.Rows[dataR][anchorCol + 10]?.ToString()?.Trim() ?? "";
                                         outStr = table.Rows[dataR][anchorCol + 12]?.ToString()?.Trim() ?? "";
                                    }

                                    if (!string.IsNullOrEmpty(inStr) || !string.IsNullOrEmpty(outStr))
                                    {
                                        presentDays++;

                                        if (TimeSpan.TryParse(inStr, out TimeSpan timeIn))
                                        {
                                            if (timeIn > graceLimit)
                                                lateMins += (int)(timeIn - shiftStart).TotalMinutes;
                                        }

                                        if (TimeSpan.TryParse(outStr, out TimeSpan timeOut))
                                        {
                                            if (timeOut < shiftEnd)
                                            {
                                                undertimeMins += (int)(shiftEnd - timeOut).TotalMinutes;
                                            }
                                            else if (timeOut > shiftEnd)
                                            {
                                                double otDuration = (timeOut - shiftEnd).TotalHours;
                                                int fullHours = (int)Math.Floor(otDuration);
                                                
                                                if (fullHours >= 1)
                                                    overtimeHours += Math.Min(fullHours, 3);
                                            }
                                        }
                                    }
                                }

                                summaries.Add(new AttendanceSummary
                                {
                                    EmpNumber = empId,
                                    Name = name,
                                    PresentDays = presentDays,
                                    LateMinutes = lateMins,
                                    UndertimeMinutes = undertimeMins,
                                    OvertimeHours = overtimeHours
                                });
                            }
                        }
                    }
                }
            }

            return summaries;
        }
    }
}
