using System.Linq;
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
        private bool _isHelpModalVisible = false;

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

        public bool IsHelpModalVisible
        {
            get => _isHelpModalVisible;
            set => SetProperty(ref _isHelpModalVisible, value);
        }

        // --- Toast Notification ---
        private string _toastMessage = "";
        private string _toastIcon = "✅";
        private bool _isToastVisible = false;

        public string ToastMessage
        {
            get => _toastMessage;
            set => SetProperty(ref _toastMessage, value);
        }

        public string ToastIcon
        {
            get => _toastIcon;
            set => SetProperty(ref _toastIcon, value);
        }

        public bool IsToastVisible
        {
            get => _isToastVisible;
            set => SetProperty(ref _isToastVisible, value);
        }

        public async void ShowToastNotification(string message, string icon)
        {
            ToastMessage = message;
            ToastIcon = icon;
            IsToastVisible = true;
            await System.Threading.Tasks.Task.Delay(3000);
            IsToastVisible = false;
        }

        // --- Appearance (Dark Mode) ---
        public bool IsDarkMode
        {
            get => App.IsDarkMode();
            set
            {
                if (App.IsDarkMode() != value)
                {
                    App.ChangeTheme(value);
                    OnPropertyChanged(nameof(IsDarkMode));
                }
            }
        }

        // Navigation Commands
        public ICommand NavigateDashboardCommand { get; }
        public ICommand NavigateEmployeesCommand { get; }
        public ICommand NavigatePayrollCommand { get; }
        public ICommand NavigatePayslipCommand { get; }
        public ICommand NavigateBiometricsCommand { get; }
        public ICommand NavigateBatchPrintCommand { get; }
        public ICommand NavigateReportsCommand { get; }
        public ICommand NavigateSettingsCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand ToggleHelpCommand { get; }

        // ViewModels
        private readonly DashboardViewModel _dashboardViewModel;
        private readonly EmployeeViewModel _employeeViewModel;
        private readonly PayrollViewModel _payrollViewModel;
        private readonly PayslipViewModel _payslipViewModel;
        private readonly BiometricsViewModel _biometricsViewModel;
        private readonly BatchPrintViewModel _batchPrintViewModel;
        private readonly ReportsViewModel _reportsViewModel;
        private readonly SettingsViewModel _settingsViewModel;

        public MainViewModel()
        {
            _dashboardViewModel = new DashboardViewModel();
            _employeeViewModel = new EmployeeViewModel();
            _payrollViewModel = new PayrollViewModel();
            _payslipViewModel = new PayslipViewModel();
            _biometricsViewModel = new BiometricsViewModel();
            _batchPrintViewModel = new BatchPrintViewModel();
            _reportsViewModel = new ReportsViewModel();
            _settingsViewModel = new SettingsViewModel();

            // Subscribe to Toast requests
            _dashboardViewModel.ToastRequested += (s, e) => ShowToastNotification(e.Message, e.Icon);
            _employeeViewModel.ToastRequested += (s, e) => ShowToastNotification(e.Message, e.Icon);
            _payrollViewModel.ToastRequested += (s, e) => ShowToastNotification(e.Message, e.Icon);
            _payslipViewModel.ToastRequested += (s, e) => ShowToastNotification(e.Message, e.Icon);
            _biometricsViewModel.ToastRequested += (s, e) => ShowToastNotification(e.Message, e.Icon);
            _batchPrintViewModel.ToastRequested += (s, e) => ShowToastNotification(e.Message, e.Icon);
            _reportsViewModel.ToastRequested += (s, e) => ShowToastNotification(e.Message, e.Icon);
            _settingsViewModel.ToastRequested += (s, e) => ShowToastNotification(e.Message, e.Icon);

            // Auto-navigate to Payslip after processing payroll
            _payrollViewModel.PayrollProcessed += emp => 
            {
                _payslipViewModel.LoadEmployees();
                _payslipViewModel.SelectedEmployee = _payslipViewModel.Employees.FirstOrDefault(e => e.EmpNumber == emp.EmpNumber);
                NavigateTo("Payslip");
            };

            CurrentView = _dashboardViewModel;

            NavigateDashboardCommand = new RelayCommand(_ => NavigateTo("Dashboard"));
            NavigateEmployeesCommand = new RelayCommand(_ => NavigateTo("Employees"));
            NavigatePayrollCommand = new RelayCommand(_ => NavigateTo("Payroll"));
            NavigatePayslipCommand = new RelayCommand(_ => NavigateTo("Payslip"));
            NavigateBiometricsCommand = new RelayCommand(_ => NavigateTo("Biometrics"));
            NavigateBatchPrintCommand = new RelayCommand(_ => NavigateTo("BatchPrint"));
            NavigateReportsCommand = new RelayCommand(_ => NavigateTo("Reports"));
            NavigateSettingsCommand = new RelayCommand(_ => NavigateTo("Settings"));
            LogoutCommand = new RelayCommand(_ => Logout());
            ToggleHelpCommand = new RelayCommand(_ => IsHelpModalVisible = !IsHelpModalVisible);
        }

        public void SetUser(string name, string role)
        {
            CurrentUserName = name;
            CurrentUserRole = role;
            _dashboardViewModel.LoadData();
        }

        private void NavigateTo(string page)
        {
            if (CurrentView != null && CurrentView.HasUnsavedChanges)
            {
                var result = System.Windows.MessageBox.Show(
                    "You have unsaved changes. Are you sure you want to leave?",
                    "Unsaved Changes",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);
                if (result == System.Windows.MessageBoxResult.No)
                {
                    return; // Cancel navigation
                }
                
                // If they proceed, forcefully close the form in the employee view
                if (CurrentView is EmployeeViewModel evm) evm.IsFormVisible = false;
            }

            ActiveNav = page;

            // Switch view immediately for instant feel
            CurrentView = page switch
            {
                "Dashboard" => _dashboardViewModel,
                "Employees" => _employeeViewModel,
                "Payroll" => _payrollViewModel,
                "Payslip" => _payslipViewModel,
                "Biometrics" => _biometricsViewModel,
                "BatchPrint" => _batchPrintViewModel,
                "Reports" => _reportsViewModel,
                "Settings" => _settingsViewModel,
                _ => _dashboardViewModel
            };

            // Load data directly — lightweight offline ops, no need for background thread
            switch (page)
            {
                case "Dashboard": _dashboardViewModel.LoadData(); break;
                case "Employees": _employeeViewModel.LoadEmployees(); break;
                case "Payroll": _payrollViewModel.LoadData(); break;
                case "Payslip": _payslipViewModel.LoadEmployees(); break;
                case "Biometrics": _biometricsViewModel.LoadData(); break;
                case "BatchPrint": _batchPrintViewModel.LoadData(); break;
                case "Reports": _reportsViewModel.LoadData(); break;
                case "Settings": _settingsViewModel.LoadData(); break;
            }
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
