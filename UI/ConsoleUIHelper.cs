using System;
using System.Collections.Generic;
using System.Linq;

namespace PayrollSystem.UI
{
    /// <summary>
    /// Centralized console UI helper for professional rendering
    /// Uses a "Slate & Teal" minimalist color scheme
    /// </summary>
    public static class ConsoleUIHelper
    {
        // ─── Box Drawing Characters ─────────────────────────────────
        public const char TopLeft = '╔';
        public const char TopRight = '╗';
        public const char BottomLeft = '╚';
        public const char BottomRight = '╝';
        public const char Horizontal = '═';
        public const char Vertical = '║';
        public const char TeeLeft = '╠';
        public const char TeeRight = '╣';
        public const char TeeTop = '╦';
        public const char TeeBottom = '╩';
        public const char Cross = '╬';

        // Thin line variants
        public const char ThinHorizontal = '─';
        public const char ThinVertical = '│';
        public const char ThinTopLeft = '┌';
        public const char ThinTopRight = '┐';
        public const char ThinBottomLeft = '└';
        public const char ThinBottomRight = '┘';
        public const char ThinTeeLeft = '├';
        public const char ThinTeeRight = '┤';

        // ─── Layout Constants ───────────────────────────────────────
        public const int DefaultWidth = 72;
        public const int WideWidth = 90;

        // ─── Color Scheme ───────────────────────────────────────────
        public static readonly ConsoleColor PrimaryColor = ConsoleColor.Cyan;
        public static readonly ConsoleColor SecondaryColor = ConsoleColor.DarkCyan;
        public static readonly ConsoleColor AccentColor = ConsoleColor.DarkYellow;
        public static readonly ConsoleColor TextColor = ConsoleColor.White;
        public static readonly ConsoleColor MutedColor = ConsoleColor.DarkGray;
        public static readonly ConsoleColor SubtleColor = ConsoleColor.Gray;
        public static readonly ConsoleColor SuccessColor = ConsoleColor.Green;
        public static readonly ConsoleColor WarningColor = ConsoleColor.Yellow;
        public static readonly ConsoleColor ErrorColor = ConsoleColor.Red;
        public static readonly ConsoleColor HighlightColor = ConsoleColor.DarkYellow;

        // ─── Initialization ─────────────────────────────────────────

        /// <summary>
        /// Initializes the console for the professional theme
        /// </summary>
        public static void InitializeConsole()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = TextColor;
            Console.Title = "Payroll Management System v1.0";
        }

        // ─── Box Drawing ────────────────────────────────────────────

        /// <summary>
        /// Draws a top border line
        /// </summary>
        public static void DrawTopBorder(int width = DefaultWidth)
        {
            Write($"{TopLeft}", MutedColor);
            Write(new string(Horizontal, width - 2), MutedColor);
            WriteLine($"{TopRight}", MutedColor);
        }

        /// <summary>
        /// Draws a bottom border line
        /// </summary>
        public static void DrawBottomBorder(int width = DefaultWidth)
        {
            Write($"{BottomLeft}", MutedColor);
            Write(new string(Horizontal, width - 2), MutedColor);
            WriteLine($"{BottomRight}", MutedColor);
        }

        /// <summary>
        /// Draws a separator line within a box
        /// </summary>
        public static void DrawSeparator(int width = DefaultWidth)
        {
            Write($"{TeeLeft}", MutedColor);
            Write(new string(Horizontal, width - 2), MutedColor);
            WriteLine($"{TeeRight}", MutedColor);
        }

        /// <summary>
        /// Draws a thin separator line within a box
        /// </summary>
        public static void DrawThinSeparator(int width = DefaultWidth)
        {
            Write($"{Vertical}", MutedColor);
            Write($" {new string(ThinHorizontal, width - 4)} ", MutedColor);
            WriteLine($"{Vertical}", MutedColor);
        }

        /// <summary>
        /// Draws an empty padded line inside a box
        /// </summary>
        public static void DrawEmptyLine(int width = DefaultWidth)
        {
            Write($"{Vertical}", MutedColor);
            Write(new string(' ', width - 2));
            WriteLine($"{Vertical}", MutedColor);
        }

        /// <summary>
        /// Draws a line of text inside a box, left-aligned with padding
        /// </summary>
        public static void DrawBoxLine(string text, ConsoleColor color = ConsoleColor.White, int width = DefaultWidth)
        {
            Write($"{Vertical}", MutedColor);
            Write($"  ", MutedColor);
            var availableWidth = width - 6;
            var displayText = text.Length > availableWidth ? text.Substring(0, availableWidth) : text;
            Write(displayText, color);
            var padding = width - 4 - displayText.Length;
            Write(new string(' ', padding > 0 ? padding : 0));
            WriteLine($"{Vertical}", MutedColor);
        }

        /// <summary>
        /// Draws a line of text centered inside a box
        /// </summary>
        public static void DrawCenteredLine(string text, ConsoleColor color = ConsoleColor.White, int width = DefaultWidth)
        {
            Write($"{Vertical}", MutedColor);
            var totalPadding = width - 2 - text.Length;
            var leftPad = totalPadding / 2;
            var rightPad = totalPadding - leftPad;
            Write(new string(' ', leftPad > 0 ? leftPad : 0));
            Write(text, color);
            Write(new string(' ', rightPad > 0 ? rightPad : 0));
            WriteLine($"{Vertical}", MutedColor);
        }

        /// <summary>
        /// Draws a key-value pair inside a box
        /// </summary>
        public static void DrawKeyValue(string key, string value, int keyWidth = 22, int width = DefaultWidth)
        {
            Write($"{Vertical}", MutedColor);
            Write("  ");
            Write(key.PadRight(keyWidth), SubtleColor);
            var availableWidth = width - keyWidth - 6;
            var displayValue = value.Length > availableWidth ? value.Substring(0, availableWidth) : value;
            Write(displayValue, TextColor);
            var padding = width - 4 - keyWidth - displayValue.Length;
            Write(new string(' ', padding > 0 ? padding : 0));
            WriteLine($"{Vertical}", MutedColor);
        }

        /// <summary>
        /// Draws a key-value pair with colored value
        /// </summary>
        public static void DrawKeyValueColored(string key, string value, ConsoleColor valueColor, int keyWidth = 22, int width = DefaultWidth)
        {
            Write($"{Vertical}", MutedColor);
            Write("  ");
            Write(key.PadRight(keyWidth), SubtleColor);
            var availableWidth = width - keyWidth - 6;
            var displayValue = value.Length > availableWidth ? value.Substring(0, availableWidth) : value;
            Write(displayValue, valueColor);
            var padding = width - 4 - keyWidth - displayValue.Length;
            Write(new string(' ', padding > 0 ? padding : 0));
            WriteLine($"{Vertical}", MutedColor);
        }

        // ─── Headers ────────────────────────────────────────────────

        /// <summary>
        /// Draws a major header with double-line borders
        /// </summary>
        public static void DrawHeader(string title, string? subtitle = null, int width = DefaultWidth)
        {
            Console.WriteLine();
            DrawTopBorder(width);
            DrawEmptyLine(width);
            DrawCenteredLine(title.ToUpper(), PrimaryColor, width);
            if (!string.IsNullOrEmpty(subtitle))
            {
                DrawCenteredLine(subtitle, MutedColor, width);
            }
            DrawEmptyLine(width);
            DrawBottomBorder(width);
        }

        /// <summary>
        /// Draws a section header inside a content area
        /// </summary>
        public static void DrawSectionHeader(string title, int width = DefaultWidth)
        {
            Console.WriteLine();
            Write("  ", MutedColor);
            Write($"{ThinTopLeft}{new string(ThinHorizontal, 2)} ", MutedColor);
            Write(title.ToUpper(), SecondaryColor);
            Write($" {new string(ThinHorizontal, width - title.Length - 8)}{ThinTopRight}", MutedColor);
            Console.WriteLine();
        }

        /// <summary>
        /// Draws a mini header for sub-sections
        /// </summary>
        public static void DrawMiniHeader(string title, int width = DefaultWidth)
        {
            Console.WriteLine();
            var line = new string(ThinHorizontal, 3);
            Write($"  {line} ", MutedColor);
            Write(title, PrimaryColor);
            Write($" {new string(ThinHorizontal, width - title.Length - 8)}", MutedColor);
            Console.WriteLine();
            Console.WriteLine();
        }

        // ─── Menus ──────────────────────────────────────────────────

        /// <summary>
        /// Draws a menu option with number highlight
        /// </summary>
        public static void DrawMenuOption(string number, string text, int width = DefaultWidth)
        {
            Write($"{Vertical}", MutedColor);
            Write("    ");
            Write($"[", MutedColor);
            Write(number, PrimaryColor);
            Write($"]", MutedColor);
            Write($"  {text}", TextColor);
            var padding = width - text.Length - number.Length - 10;
            Write(new string(' ', padding > 0 ? padding : 0));
            WriteLine($"{Vertical}", MutedColor);
        }

        /// <summary>
        /// Draws input prompt
        /// </summary>
        public static void DrawPrompt(string text)
        {
            Console.WriteLine();
            Write("  ", MutedColor);
            Write("▸ ", PrimaryColor);
            Write(text, SubtleColor);
        }

        /// <summary>
        /// Draws a styled input prompt with arrow
        /// </summary>
        public static void DrawInputPrompt(string text)
        {
            Write("  ", MutedColor);
            Write("› ", PrimaryColor);
            Write(text, SubtleColor);
        }

        // ─── Tables ─────────────────────────────────────────────────

        /// <summary>
        /// Draws a table with headers and rows
        /// </summary>
        public static void DrawTable(string[] headers, List<string[]> rows, int[]? columnWidths = null, int width = DefaultWidth)
        {
            if (columnWidths == null)
            {
                // Auto-calculate column widths
                var totalCols = headers.Length;
                var availableWidth = width - 4 - (totalCols + 1); // borders + separators
                var colWidth = availableWidth / totalCols;
                columnWidths = Enumerable.Repeat(colWidth, totalCols).ToArray();
                // Give remaining space to last column
                columnWidths[totalCols - 1] += availableWidth - (colWidth * totalCols);
            }

            // Table top border
            Write("  ", MutedColor);
            Write($"{ThinTopLeft}", MutedColor);
            for (int i = 0; i < headers.Length; i++)
            {
                Write(new string(ThinHorizontal, columnWidths[i] + 2), MutedColor);
                Write(i < headers.Length - 1 ? $"{TeeTop}" : $"{ThinTopRight}", MutedColor);
            }
            Console.WriteLine();

            // Headers
            Write("  ", MutedColor);
            Write($"{ThinVertical}", MutedColor);
            for (int i = 0; i < headers.Length; i++)
            {
                Write(" ", MutedColor);
                var header = headers[i].Length > columnWidths[i] ? headers[i].Substring(0, columnWidths[i]) : headers[i];
                Write(header.PadRight(columnWidths[i]), PrimaryColor);
                Write($" {ThinVertical}", MutedColor);
            }
            Console.WriteLine();

            // Header separator
            Write("  ", MutedColor);
            Write($"{ThinTeeLeft}", MutedColor);
            for (int i = 0; i < headers.Length; i++)
            {
                Write(new string(ThinHorizontal, columnWidths[i] + 2), MutedColor);
                Write(i < headers.Length - 1 ? $"{Cross}" : $"{ThinTeeRight}", MutedColor);
            }
            Console.WriteLine();

            // Data rows
            foreach (var row in rows)
            {
                Write("  ", MutedColor);
                Write($"{ThinVertical}", MutedColor);
                for (int i = 0; i < headers.Length; i++)
                {
                    Write(" ", MutedColor);
                    var cellValue = i < row.Length ? row[i] : "";
                    var displayValue = cellValue.Length > columnWidths[i] ? cellValue.Substring(0, columnWidths[i]) : cellValue;

                    // Color coding for specific content
                    var cellColor = TextColor;
                    if (displayValue.Trim() == "Active") cellColor = SuccessColor;
                    else if (displayValue.Trim() == "Inactive") cellColor = ErrorColor;
                    else if (displayValue.StartsWith("₱") || displayValue.StartsWith("$") || displayValue.StartsWith("PHP")) cellColor = AccentColor;

                    Write(displayValue.PadRight(columnWidths[i]), cellColor);
                    Write($" {ThinVertical}", MutedColor);
                }
                Console.WriteLine();
            }

            // Table bottom border
            Write("  ", MutedColor);
            Write($"{ThinBottomLeft}", MutedColor);
            for (int i = 0; i < headers.Length; i++)
            {
                Write(new string(ThinHorizontal, columnWidths[i] + 2), MutedColor);
                Write(i < headers.Length - 1 ? $"{TeeBottom}" : $"{ThinBottomRight}", MutedColor);
            }
            Console.WriteLine();
        }

        // ─── Status & Feedback ──────────────────────────────────────

        /// <summary>
        /// Draws a success message
        /// </summary>
        public static void DrawSuccess(string message)
        {
            Console.WriteLine();
            Write("  ", MutedColor);
            Write(" ✓ ", ConsoleColor.Black, SuccessColor);
            Write($" {message}", SuccessColor);
            Console.WriteLine();
        }

        /// <summary>
        /// Draws an error message
        /// </summary>
        public static void DrawError(string message)
        {
            Console.WriteLine();
            Write("  ", MutedColor);
            Write(" ✗ ", ConsoleColor.Black, ErrorColor);
            Write($" {message}", ErrorColor);
            Console.WriteLine();
        }

        /// <summary>
        /// Draws a warning message
        /// </summary>
        public static void DrawWarning(string message)
        {
            Console.WriteLine();
            Write("  ", MutedColor);
            Write(" ! ", ConsoleColor.Black, WarningColor);
            Write($" {message}", WarningColor);
            Console.WriteLine();
        }

        /// <summary>
        /// Draws an info message
        /// </summary>
        public static void DrawInfo(string message)
        {
            Console.Write("  ");
            Write(" i ", ConsoleColor.Black, PrimaryColor);
            Write($" {message}", SubtleColor);
            Console.WriteLine();
        }

        /// <summary>
        /// Draws a status badge
        /// </summary>
        public static string GetStatusBadge(bool isActive)
        {
            return isActive ? "Active" : "Inactive";
        }

        // ─── Special Screens ────────────────────────────────────────

        /// <summary>
        /// Draws a professional welcome/splash screen
        /// </summary>
        public static void DrawWelcomeScreen()
        {
            Console.Clear();
            Console.WriteLine();
            Console.WriteLine();

            var width = DefaultWidth;
            DrawTopBorder(width);
            DrawEmptyLine(width);
            DrawEmptyLine(width);

            // ASCII-style logo
            DrawCenteredLine("╭─────────────────────────────────╮", MutedColor, width);
            DrawCenteredLine("│   PAYROLL MANAGEMENT SYSTEM     │", PrimaryColor, width);
            DrawCenteredLine("╰─────────────────────────────────╯", MutedColor, width);

            DrawEmptyLine(width);
            DrawCenteredLine("Enterprise Edition  •  v1.0", MutedColor, width);
            DrawCenteredLine($"{DateTime.Now:dddd, MMMM dd, yyyy}", SubtleColor, width);
            DrawEmptyLine(width);
            DrawEmptyLine(width);
            DrawSeparator(width);
            DrawEmptyLine(width);
            DrawCenteredLine("Powered by .NET 8  •  Clean Architecture", MutedColor, width);
            DrawEmptyLine(width);
            DrawBottomBorder(width);

            Console.WriteLine();
        }

        /// <summary>
        /// Draws a "press any key" prompt
        /// </summary>
        public static void DrawPressAnyKey(string message = "Press any key to continue...")
        {
            Console.WriteLine();
            Write("  ", MutedColor);
            Write(message, MutedColor);
            Console.ReadKey(true);
        }

        /// <summary>
        /// Draws a confirmation prompt and returns true/false
        /// </summary>
        public static bool DrawConfirmation(string message)
        {
            Console.WriteLine();
            Write("  ", MutedColor);
            Write(" ? ", ConsoleColor.Black, WarningColor);
            Write($" {message} ", WarningColor);
            Write("(", MutedColor);
            Write("Y", PrimaryColor);
            Write("/", MutedColor);
            Write("N", ErrorColor);
            Write("): ", MutedColor);

            var key = Console.ReadKey(false);
            Console.WriteLine();
            return key.KeyChar == 'y' || key.KeyChar == 'Y';
        }

        /// <summary>
        /// Clears the screen and draws a consistent page header
        /// </summary>
        public static void DrawPageHeader(string pageTitle, string? breadcrumb = null)
        {
            Console.Clear();
            Console.WriteLine();

            // Top bar
            Write("  ", MutedColor);
            Write(" PAYROLL SYSTEM ", ConsoleColor.Black, PrimaryColor);
            if (!string.IsNullOrEmpty(breadcrumb))
            {
                Write($"  {ThinHorizontal}{ThinHorizontal}  ", MutedColor);
                Write(breadcrumb, SubtleColor);
            }
            Console.WriteLine();

            // Thin line separator
            Write("  ", MutedColor);
            Write(new string(ThinHorizontal, DefaultWidth - 4), MutedColor);
            Console.WriteLine();
        }

        /// <summary>
        /// Draws a summary footer with totals
        /// </summary>
        public static void DrawFooter(string label, string value, int width = DefaultWidth)
        {
            Console.WriteLine();
            Write("  ", MutedColor);
            Write(new string(ThinHorizontal, width - 4), MutedColor);
            Console.WriteLine();
            Write("  ", MutedColor);
            Write($"  {label}: ", SubtleColor);
            Write(value, PrimaryColor);
            Console.WriteLine();
        }

        // ─── Core Write Helpers ─────────────────────────────────────

        /// <summary>
        /// Writes text with foreground color
        /// </summary>
        public static void Write(string text, ConsoleColor foreground)
        {
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = foreground;
            Console.Write(text);
            Console.ForegroundColor = prev;
        }

        /// <summary>
        /// Writes text with foreground and background color
        /// </summary>
        public static void Write(string text, ConsoleColor foreground, ConsoleColor background)
        {
            var prevFg = Console.ForegroundColor;
            var prevBg = Console.BackgroundColor;
            Console.ForegroundColor = foreground;
            Console.BackgroundColor = background;
            Console.Write(text);
            Console.ForegroundColor = prevFg;
            Console.BackgroundColor = prevBg;
        }

        /// <summary>
        /// Writes text with default color
        /// </summary>
        public static void Write(string text)
        {
            Console.Write(text);
        }

        /// <summary>
        /// Writes a line with foreground color
        /// </summary>
        public static void WriteLine(string text, ConsoleColor foreground)
        {
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = foreground;
            Console.WriteLine(text);
            Console.ForegroundColor = prev;
        }

        /// <summary>
        /// Writes a line with default color
        /// </summary>
        public static void WriteLine(string text)
        {
            Console.WriteLine(text);
        }
    }
}
