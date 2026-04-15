using System.ComponentModel;
using System.Windows;
using PayrollSystem.DataAccess;
using PayrollSystem.ViewModels;
using PayrollSystem.Views;

namespace PayrollSystem
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            // Try initializing database
            try { DatabaseHelper.InitializeDatabase(); } catch { }

            _viewModel = new MainViewModel();
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            DataContext = _viewModel;
        }

        public void SetUser(string name, string role)
        {
            _viewModel.SetUser(name, role);
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.ActiveNav) && _viewModel.ActiveNav == "Logout")
            {
                // Show login window again
                var loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
        }
    }
}
