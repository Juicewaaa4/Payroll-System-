using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using PayrollSystem.DataAccess;

namespace PayrollSystem
{
    public partial class App : Application
    {
        private static readonly string _settingsPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "theme_settings.txt");

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Load saved theme preference
            bool isDark = LoadThemePreference();
            ApplyTheme(isDark);

            // Pre-test and cache DB connection on background thread
            // so login and dashboard are instant
            Task.Run(() =>
            {
                DatabaseHelper.TestConnection();
                try { DatabaseHelper.InitializeDatabase(); } catch { }
            });
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Create a backup of the database before the application closes completely
            DatabaseHelper.BackupDatabase();
            base.OnExit(e);
        }

        /// <summary>
        /// Switches the application theme at runtime.
        /// </summary>
        public static void ChangeTheme(bool isDark)
        {
            ApplyTheme(isDark);
            SaveThemePreference(isDark);
        }

        private static void ApplyTheme(bool isDark)
        {
            var mergedDicts = Current.Resources.MergedDictionaries;

            // Remove existing theme dictionaries
            for (int i = mergedDicts.Count - 1; i >= 0; i--)
            {
                var source = mergedDicts[i].Source;
                if (source != null && (source.OriginalString.Contains("GreenTheme") || source.OriginalString.Contains("DarkTheme")))
                {
                    mergedDicts.RemoveAt(i);
                }
            }

            // Add the selected theme
            var themeUri = isDark
                ? new Uri("Themes/DarkTheme.xaml", UriKind.Relative)
                : new Uri("Themes/GreenTheme.xaml", UriKind.Relative);

            mergedDicts.Insert(0, new ResourceDictionary { Source = themeUri });
        }

        private static bool LoadThemePreference()
        {
            try
            {
                if (File.Exists(_settingsPath))
                    return File.ReadAllText(_settingsPath).Trim() == "dark";
            }
            catch { }
            return false; // default to light
        }

        private static void SaveThemePreference(bool isDark)
        {
            try
            {
                File.WriteAllText(_settingsPath, isDark ? "dark" : "light");
            }
            catch { }
        }

        /// <summary>
        /// Returns true if the current theme is dark.
        /// </summary>
        public static bool IsDarkMode()
        {
            return LoadThemePreference();
        }
    }
}
