using System;
using System.Windows.Input;
using PayrollSystem.Helpers;
using PayrollSystem.DataAccess;

namespace PayrollSystem.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private string _username = "";
        private string _errorMessage = "";
        private bool _isLoading;

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

        public ICommand LoginCommand { get; }

        // Event to signal successful login
        public event Action<string, string>? LoginSuccessful;

        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(ExecuteLogin);
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
            if (string.IsNullOrWhiteSpace(password))
            {
                ErrorMessage = "Please enter your password.";
                return;
            }

            IsLoading = true;

            try
            {
                // Try database authentication
                if (DatabaseHelper.TestConnection())
                {
                    using var conn = DatabaseHelper.GetConnection();
                    conn.Open();
                    using var cmd = new MySql.Data.MySqlClient.MySqlCommand(
                        "SELECT full_name, role FROM users WHERE username = @user AND password_hash = @pass AND is_active = 1",
                        conn);
                    cmd.Parameters.AddWithValue("@user", Username);
                    cmd.Parameters.AddWithValue("@pass", password);

                    using var reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        var fullName = reader.GetString("full_name");
                        var role = reader.GetString("role");
                        LoginSuccessful?.Invoke(fullName, role);
                        return;
                    }
                }

                // Fallback when database is not available
                DemoDatabase.Initialize();
                var dbUser = DemoDatabase.Users.FirstOrDefault(u => u.Username == Username && u.PasswordHash == password && u.IsActive);
                if (dbUser != null)
                {
                    LoginSuccessful?.Invoke(dbUser.FullName, dbUser.Role);
                    return;
                }

                ErrorMessage = "Invalid username or password.";
            }
            catch (Exception)
            {
                // Fallback when database is not available
                DemoDatabase.Initialize();
                var demoUser = DemoDatabase.Users.FirstOrDefault(u => u.Username == Username && u.PasswordHash == password && u.IsActive);
                if (demoUser != null)
                {
                    LoginSuccessful?.Invoke(demoUser.FullName, demoUser.Role);
                    return;
                }

                ErrorMessage = "Invalid username or password.";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
