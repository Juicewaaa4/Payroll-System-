using System;
using System.Windows.Input;
using PayrollSystem.Helpers;
using PayrollSystem.DataAccess;
using PayrollSystem.Utilities;

namespace PayrollSystem.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private string _username = "";
        private string _errorMessage = "";
        private bool _isLoading;
        private bool _rememberMe;
        private string _lastAttemptedPassword = "";
        private readonly string _settingsPath;

        public string RememberedPassword { get; private set; } = "";

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool RememberMe
        {
            get => _rememberMe;
            set => SetProperty(ref _rememberMe, value);
        }

        public ICommand LoginCommand { get; }

        // Event to signal successful login
        public event Action<string, string>? LoginSuccessful;

        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(ExecuteLogin);
            
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _settingsPath = System.IO.Path.Combine(appData, "PayrollSystem", "remember.txt");
            LoadRememberedUser();
        }

        private void LoadRememberedUser()
        {
            try
            {
                if (System.IO.File.Exists(_settingsPath))
                {
                    var lines = System.IO.File.ReadAllLines(_settingsPath);
                    if (lines.Length >= 2 && lines[0] == "1")
                    {
                        RememberMe = true;
                        Username = lines[1];
                        if (lines.Length >= 3)
                        {
                            try { RememberedPassword = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(lines[2])); } catch {}
                        }
                    }
                }
            }
            catch { /* Ignore if it fails */ }
        }

        private void SaveRememberedUser()
        {
            try
            {
                var dir = System.IO.Path.GetDirectoryName(_settingsPath);
                if (!System.IO.Directory.Exists(dir))
                    System.IO.Directory.CreateDirectory(dir!);

                if (RememberMe)
                {
                    var pass64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(_lastAttemptedPassword));
                    System.IO.File.WriteAllLines(_settingsPath, new[] { "1", Username, pass64 });
                }
                else if (System.IO.File.Exists(_settingsPath))
                    System.IO.File.Delete(_settingsPath);
            }
            catch { /* Ignore if it fails */ }
        }

        public void ExecuteLogin(object? parameter)
        {
            ErrorMessage = "";

            if (string.IsNullOrWhiteSpace(Username))
            {
                ErrorMessage = "Please enter your username.";
                return;
            }

            var password = parameter as string ?? "";
            _lastAttemptedPassword = password;
            if (string.IsNullOrWhiteSpace(password))
            {
                ErrorMessage = "Please enter your password.";
                return;
            }

            IsLoading = true;

            try
            {
                if (!DatabaseHelper.TestConnection())
                {
                    ErrorMessage = "Cannot connect to the database.";
                    return;
                }

                var hashedPassword = PasswordHelper.HashPassword(password);

                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                using var cmd = new Microsoft.Data.Sqlite.SqliteCommand(
                    "SELECT full_name, role FROM users WHERE username = @user AND password_hash = @pass AND is_active = 1",
                    conn);
                cmd.Parameters.AddWithValue("@user", Username);
                cmd.Parameters.AddWithValue("@pass", hashedPassword);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    var fullName = reader.GetString(reader.GetOrdinal("full_name"));
                    var role = reader.GetString(reader.GetOrdinal("role"));
                    SaveRememberedUser();
                    LoginSuccessful?.Invoke(fullName, role);
                    return;
                }

                ErrorMessage = "Invalid username or password.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Database error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
