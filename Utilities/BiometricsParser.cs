using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using ExcelDataReader;

namespace PayrollSystem.Utilities
{
    public class DailyAttendance
    {
        public int DayNumber { get; set; }
        public int LateMinutes { get; set; }
        public int UndertimeMinutes { get; set; }
        public int OvertimeHours { get; set; }
        public bool IsPresent { get; set; }
    }

    public class AttendanceSummary
    {
        public string EmpNumber { get; set; } = "";
        public string Name { get; set; } = "";
        public int PresentDays { get; set; }
        public int LateMinutes { get; set; }
        public int UndertimeMinutes { get; set; }
        public int OvertimeHours { get; set; }
        public List<DailyAttendance> DailyRecords { get; set; } = new();
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

                    // Scan ALL tables for Employee Blocks because Excel default names might differ
                    foreach (DataTable table in result.Tables)
                    {
                        for (int r = 0; r < table.Rows.Count; r++)
                        {
                            for (int c = 0; c < table.Columns.Count; c++)
                            {
                                string cellValue = table.Rows[r][c]?.ToString()?.Trim() ?? "";
                                string normalizedCell = cellValue.Replace("\n", "").Replace("\r", "").Replace(" ", "").ToLower();

                                // Identify the start of a data block
                                if (normalizedCell == "date/week" || normalizedCell == "date")
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
                                List<DailyAttendance> dailyRecs = new List<DailyAttendance>();

                                // Data starts 2 rows below "Date/Week"
                                int dataStartRow = anchorRow + 2;
                                
                                TimeSpan shiftStart = new TimeSpan(8, 30, 0); // 8:30 AM
                                TimeSpan shiftEnd = new TimeSpan(17, 30, 0);  // 5:30 PM
                                TimeSpan graceLimit = new TimeSpan(8, 45, 0); // 8:45 AM

                                for (int dataR = dataStartRow; dataR < table.Rows.Count; dataR++)
                                {
                                    string dateStr = table.Rows[dataR][anchorCol]?.ToString()?.Trim() ?? "";
                                    
                                    // Extract the day number (e.g. "1/Wed" -> 1)
                                    int dayNum = -1;
                                    if (dateStr.Contains("/")) {
                                        int.TryParse(dateStr.Split('/')[0], out dayNum);
                                    } else {
                                        int.TryParse(dateStr.Replace(" ", ""), out dayNum);
                                    }

                                    // If we hit an empty row or a new header, the block is done
                                    if (string.IsNullOrEmpty(dateStr) && 
                                       (dataR + 1 < table.Rows.Count && string.IsNullOrEmpty(table.Rows[dataR+1][anchorCol]?.ToString()?.Trim())))
                                    {
                                        break; 
                                    }
                                    
                                    // Gather any punches across Time1 (c+2, c+4), Time2 (c+6, c+8), OT (c+10, c+12)
                                    List<TimeSpan> rowPunches = new List<TimeSpan>();
                                    int[] timeCols = { anchorCol+2, anchorCol+4, anchorCol+6, anchorCol+8, anchorCol+10, anchorCol+12 };
                                    
                                    foreach (int tc in timeCols)
                                    {
                                        if (tc < table.Columns.Count)
                                        {
                                            string tStr = table.Rows[dataR][tc]?.ToString()?.Trim() ?? "";
                                            if (TimeSpan.TryParse(tStr, out TimeSpan ts))
                                            {
                                                rowPunches.Add(ts);
                                            }
                                        }
                                    }

                                    if (rowPunches.Count > 0)
                                    {
                                        presentDays++;

                                        TimeSpan firstPunch = rowPunches.Min();
                                        TimeSpan lastPunch = rowPunches.Max();

                                        int rLate = 0;
                                        int rUnder = 0;
                                        int rOT = 0;

                                        // Evaluate Late (only if it's reasonably a morning/arrival punch, say before 3PM)
                                        if (firstPunch > graceLimit && firstPunch < new TimeSpan(15, 0, 0))
                                        {
                                            rLate = (int)(firstPunch - shiftStart).TotalMinutes;
                                        }
                                        
                                        // Evaluate Undertime & OT (only if they have 2 punches or an Afternoon punch)
                                        if (rowPunches.Count > 1 || lastPunch >= new TimeSpan(13, 0, 0)) // Post-lunch punch
                                        {
                                            if (lastPunch < shiftEnd)
                                            {
                                                rUnder = (int)(shiftEnd - lastPunch).TotalMinutes;
                                            }
                                            else if (lastPunch > shiftEnd)
                                            {
                                                double otDuration = (lastPunch - shiftEnd).TotalHours;
                                                int fullHours = (int)Math.Floor(otDuration);
                                                
                                                if (fullHours >= 1)
                                                    rOT = Math.Min(fullHours, 3);
                                            }
                                        }

                                        lateMins += rLate;
                                        undertimeMins += rUnder;
                                        overtimeHours += rOT;

                                        dailyRecs.Add(new DailyAttendance {
                                            DayNumber = dayNum,
                                            IsPresent = true,
                                            LateMinutes = rLate,
                                            UndertimeMinutes = rUnder,
                                            OvertimeHours = rOT
                                        });
                                    }
                                }

                                summaries.Add(new AttendanceSummary
                                {
                                    EmpNumber = empId,
                                    Name = name,
                                    PresentDays = presentDays,
                                    LateMinutes = lateMins,
                                    UndertimeMinutes = undertimeMins,
                                    OvertimeHours = overtimeHours,
                                    DailyRecords = dailyRecs
                                });
                            }
                        }
                    }
                } // End foreach table
                }
            }

            return summaries;
        }
    }
}
