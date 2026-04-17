using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using PayrollSystem.Helpers;

namespace PayrollSystem.ViewModels
{
    /// <summary>
    /// Main ViewModel controlling navigation and app state
    /// </summary>
    public class MainViewModel : BaseViewModel
    {
        private BaseViewModel _currentView = null!;
        private string _currentUserName = "";
        private string _currentUserRole = "";
        private string _activeNav = "Dashboard";

        public BaseViewModel CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        public string CurrentUserName
        {
            get => _currentUserName;
            set => SetProperty(ref _currentUserName, value);
        }

        public string CurrentUserRole
        {
            get => _currentUserRole;
            set => SetProperty(ref _currentUserRole, value);
        }

        public string ActiveNav
        {
            get => _activeNav;
            set => SetProperty(ref _activeNav, value);
        }

        // Navigation Commands
        public ICommand NavigateDashboardCommand { get; }
        public ICommand NavigateEmployeesCommand { get; }
        public ICommand NavigatePayrollCommand { get; }
        public ICommand NavigatePayslipCommand { get; }
        public ICommand NavigateBatchPrintCommand { get; }
        public ICommand NavigateReportsCommand { get; }
        public ICommand NavigateSettingsCommand { get; }
        public ICommand LogoutCommand { get; }

        // ViewModels
        private readonly DashboardViewModel _dashboardViewModel;
        private readonly EmployeeViewModel _employeeViewModel;
        private readonly PayrollViewModel _payrollViewModel;
        private readonly PayslipViewModel _payslipViewModel;
        private readonly BatchPrintViewModel _batchPrintViewModel;
        private readonly ReportsViewModel _reportsViewModel;
        private readonly SettingsViewModel _settingsViewModel;

        public MainViewModel()
        {
            _dashboardViewModel = new DashboardViewModel();
            _employeeViewModel = new EmployeeViewModel();
            _payrollViewModel = new PayrollViewModel();
            _payslipViewModel = new PayslipViewModel();
            _batchPrintViewModel = new BatchPrintViewModel();
            _reportsViewModel = new ReportsViewModel();
            _settingsViewModel = new SettingsViewModel();

            CurrentView = _dashboardViewModel;

            NavigateDashboardCommand = new RelayCommand(_ => NavigateTo("Dashboard"));
            NavigateEmployeesCommand = new RelayCommand(_ => NavigateTo("Employees"));
            NavigatePayrollCommand = new RelayCommand(_ => NavigateTo("Payroll"));
            NavigatePayslipCommand = new RelayCommand(_ => NavigateTo("Payslip"));
            NavigateBatchPrintCommand = new RelayCommand(_ => NavigateTo("BatchPrint"));
            NavigateReportsCommand = new RelayCommand(_ => NavigateTo("Reports"));
            NavigateSettingsCommand = new RelayCommand(_ => NavigateTo("Settings"));
            LogoutCommand = new RelayCommand(_ => Logout());
        }

        public void SetUser(string name, string role)
        {
            CurrentUserName = name;
            CurrentUserRole = role;
            _dashboardViewModel.LoadData();
        }

        private void NavigateTo(string page)
        {
            ActiveNav = page;

            // Switch view immediately for instant feel
            CurrentView = page switch
            {
                "Dashboard" => _dashboardViewModel,
                "Employees" => _employeeViewModel,
                "Payroll" => _payrollViewModel,
                "Payslip" => _payslipViewModel,
                "BatchPrint" => _batchPrintViewModel,
                "Reports" => _reportsViewModel,
                "Settings" => _settingsViewModel,
                _ => _dashboardViewModel
            };

            // Load data on background thread to avoid UI freeze
            Task.Run(() =>
            {
                switch (page)
                {
                    case "Dashboard": _dashboardViewModel.LoadData(); break;
                    case "Employees":
                        Application.Current.Dispatcher.Invoke(() => _employeeViewModel.LoadEmployees());
                        break;
                    case "Payroll":
                        Application.Current.Dispatcher.Invoke(() => _payrollViewModel.LoadData());
                        break;
                    case "Payslip":
                        Application.Current.Dispatcher.Invoke(() => _payslipViewModel.LoadEmployees());
                        break;
                    case "BatchPrint":
                        Application.Current.Dispatcher.Invoke(() => _batchPrintViewModel.LoadData());
                        break;
                    case "Reports":
                        Application.Current.Dispatcher.Invoke(() => _reportsViewModel.LoadData());
                        break;
                    case "Settings":
                        Application.Current.Dispatcher.Invoke(() => _settingsViewModel.LoadData());
                        break;
                }
            });
        }

        private void Logout()
        {
            var result = System.Windows.MessageBox.Show(
                "Are you sure you want to logout?",
                "Confirm Logout",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                CurrentUserName = "";
                CurrentUserRole = "";
                ActiveNav = "Logout";
            }
        }
    }
}
