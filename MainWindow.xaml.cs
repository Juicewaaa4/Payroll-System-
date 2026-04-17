using System.ComponentModel;
using System.Windows;
using PayrollSystem.ViewModels;
using PayrollSystem.Views;

namespace PayrollSystem
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private bool _isLoggingOut = false;

        public MainWindow()
        {
            InitializeComponent();

            // No DB init here — already done in App.xaml.cs background thread
            _viewModel = new MainViewModel();
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            DataContext = _viewModel;
            
            this.Closing += MainWindow_Closing;
        }

        public void SetUser(string name, string role)
        {
            _viewModel.SetUser(name, role);
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.ActiveNav) && _viewModel.ActiveNav == "Logout")
            {
                _isLoggingOut = true;
                var loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
        }

        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            if (_isLoggingOut) return;

            var result = MessageBox.Show("Are you sure you want to exit the application?", 
                                         "Payroll System", 
                                         MessageBoxButton.YesNo, 
                                         MessageBoxImage.Question);
            if (result == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
        }
    }
}
