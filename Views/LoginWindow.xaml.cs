using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PayrollSystem.ViewModels;

namespace PayrollSystem.Views
{
    public partial class LoginWindow : Window
    {
        private readonly LoginViewModel _viewModel;
        private bool _isPasswordVisible = false;
        private bool _isSyncing = false;

        public LoginWindow()
        {
            InitializeComponent();
            _viewModel = new LoginViewModel();
            _viewModel.LoginSuccessful += OnLoginSuccessful;
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            DataContext = _viewModel;

            if (_viewModel.RememberMe)
            {
                PasswordBox.Password = _viewModel.RememberedPassword;
            }

            UsernameBox.Focus();
        }

        // ─── Window Drag ─────────────────────────────────────────
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        // ─── Login ───────────────────────────────────────────────
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            DoLogin();
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) DoLogin();
        }

        private void PasswordTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) DoLogin();
        }

        private void DoLogin()
        {
            var password = _isPasswordVisible ? PasswordTextBox.Text : PasswordBox.Password;
            _viewModel.ExecuteLogin(password);
        }

        private void OnLoginSuccessful(string name, string role)
        {
            var mainWindow = new MainWindow();
            mainWindow.SetUser(name, role);
            mainWindow.Show();
            this.Close();
        }

        // ─── Error Display ───────────────────────────────────────
        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LoginViewModel.ErrorMessage))
            {
                if (!string.IsNullOrEmpty(_viewModel.ErrorMessage))
                {
                    ErrorBorder.Visibility = Visibility.Visible;
                    ErrorText.Text = _viewModel.ErrorMessage;
                }
                else
                {
                    ErrorBorder.Visibility = Visibility.Collapsed;
                }
            }
        }

        // ─── Show/Hide Password Eye Toggle ───────────────────────
        private void EyeToggle_Click(object sender, RoutedEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;

            if (_isPasswordVisible)
            {
                // Show password as plain text
                PasswordTextBox.Text = PasswordBox.Password;
                PasswordBox.Visibility = Visibility.Collapsed;
                PasswordTextBox.Visibility = Visibility.Visible;
                EyeIcon.Text = "👁‍🗨";
                PasswordTextBox.Focus();
                PasswordTextBox.CaretIndex = PasswordTextBox.Text.Length;
            }
            else
            {
                // Hide password
                PasswordBox.Password = PasswordTextBox.Text;
                PasswordTextBox.Visibility = Visibility.Collapsed;
                PasswordBox.Visibility = Visibility.Visible;
                EyeIcon.Text = "👁";
                PasswordBox.Focus();
            }

            UpdatePasswordPlaceholder();
        }

        // ─── Password Sync ──────────────────────────────────────
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isSyncing) return;
            _isSyncing = true;
            PasswordTextBox.Text = PasswordBox.Password;
            _isSyncing = false;
            UpdatePasswordPlaceholder();
        }

        private void PasswordTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSyncing) return;
            _isSyncing = true;
            PasswordBox.Password = PasswordTextBox.Text;
            _isSyncing = false;
            UpdatePasswordPlaceholder();
        }

        private void UpdatePasswordPlaceholder()
        {
            var hasText = !string.IsNullOrEmpty(PasswordBox.Password) || !string.IsNullOrEmpty(PasswordTextBox.Text);
            PasswordPlaceholder.Visibility = hasText ? Visibility.Collapsed : Visibility.Visible;
        }

        // ─── Username Placeholder ────────────────────────────────
        private void UsernameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UsernamePlaceholder.Visibility = string.IsNullOrEmpty(UsernameBox.Text)
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UsernameBox_GotFocus(object sender, RoutedEventArgs e)
        {
            UsernameBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
            UsernameBorder.BorderThickness = new Thickness(2);
            if (string.IsNullOrEmpty(UsernameBox.Text))
                UsernamePlaceholder.Visibility = Visibility.Collapsed;
        }

        private void UsernameBox_LostFocus(object sender, RoutedEventArgs e)
        {
            UsernameBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
            UsernameBorder.BorderThickness = new Thickness(1);
            if (string.IsNullOrEmpty(UsernameBox.Text))
                UsernamePlaceholder.Visibility = Visibility.Visible;
        }

        // ─── Password Focus ─────────────────────────────────────
        private void PasswordBox_GotFocus(object sender, RoutedEventArgs e)
        {
            PasswordBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
            PasswordBorder.BorderThickness = new Thickness(2);
            PasswordPlaceholder.Visibility = Visibility.Collapsed;
        }

        private void PasswordBox_LostFocusE(object sender, RoutedEventArgs e)
        {
            PasswordBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
            PasswordBorder.BorderThickness = new Thickness(1);
            UpdatePasswordPlaceholder();
        }

        // ─── Window Controls ─────────────────────────────────────
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
