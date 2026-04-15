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
        public ICommand NavigateReportsCommand { get; }
        public ICommand LogoutCommand { get; }

        // ViewModels
        private readonly DashboardViewModel _dashboardViewModel;
        private readonly EmployeeViewModel _employeeViewModel;
        private readonly PayrollViewModel _payrollViewModel;
        private readonly PayslipViewModel _payslipViewModel;
        private readonly ReportsViewModel _reportsViewModel;

        public MainViewModel()
        {
            // Initialize ViewModels
            _dashboardViewModel = new DashboardViewModel();
            _employeeViewModel = new EmployeeViewModel();
            _payrollViewModel = new PayrollViewModel();
            _payslipViewModel = new PayslipViewModel();
            _reportsViewModel = new ReportsViewModel();

            // Set default view
            CurrentView = _dashboardViewModel;

            // Initialize commands
            NavigateDashboardCommand = new RelayCommand(_ => NavigateTo("Dashboard"));
            NavigateEmployeesCommand = new RelayCommand(_ => NavigateTo("Employees"));
            NavigatePayrollCommand = new RelayCommand(_ => NavigateTo("Payroll"));
            NavigatePayslipCommand = new RelayCommand(_ => NavigateTo("Payslip"));
            NavigateReportsCommand = new RelayCommand(_ => NavigateTo("Reports"));
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
            CurrentView = page switch
            {
                "Dashboard" => _dashboardViewModel,
                "Employees" => _employeeViewModel,
                "Payroll" => _payrollViewModel,
                "Payslip" => _payslipViewModel,
                "Reports" => _reportsViewModel,
                _ => _dashboardViewModel
            };

            // Refresh data when navigating
            switch (page)
            {
                case "Dashboard": _dashboardViewModel.LoadData(); break;
                case "Employees": _employeeViewModel.LoadEmployees(); break;
                case "Payroll": _payrollViewModel.LoadData(); break;
                case "Reports": _reportsViewModel.LoadData(); break;
            }
        }

        private void Logout()
        {
            // Signal logout — handled in MainWindow code-behind
            CurrentUserName = "";
            CurrentUserRole = "";
            ActiveNav = "Logout";
            OnPropertyChanged(nameof(ActiveNav));
        }
    }
}
