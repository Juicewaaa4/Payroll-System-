using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using PayrollSystem.Helpers;
using PayrollSystem.DataAccess;
using Microsoft.Data.Sqlite;

namespace PayrollSystem.ViewModels
{
    public class EmployeeViewModel : BaseViewModel
    {
        private string _searchText = "";
        private EmployeeItem? _selectedEmployee;
        private bool _isFormVisible;
        private bool _isEditing;

        // Form fields
        private string _formFirstName = "";
        private string _formLastName = "";
        private string _formPosition = "";
        private string _formDailyRate = "";
        private string _formDepartment = "";
        private string _formHireDate = DateTime.Now.ToString("yyyy-MM-dd");
        private string _formError = "";

        private int _currentPage = 1;
        private int _totalPages = 1;
        private int _pageSize = 20;
        private string _sortColumn = "Full Name";
        private bool _sortAscending = true;

        public string SearchText { get => _searchText; set { SetProperty(ref _searchText, value); FilterEmployees(); } }
        public EmployeeItem? SelectedEmployee { get => _selectedEmployee; set => SetProperty(ref _selectedEmployee, value); }
        public bool IsFormVisible { get => _isFormVisible; set { SetProperty(ref _isFormVisible, value); OnPropertyChanged(nameof(HasUnsavedChanges)); } }
        public bool IsEditing { get => _isEditing; set => SetProperty(ref _isEditing, value); }

        public override bool HasUnsavedChanges => IsFormVisible;

        public string FormFirstName { get => _formFirstName; set => SetProperty(ref _formFirstName, value); }
        public string FormLastName { get => _formLastName; set => SetProperty(ref _formLastName, value); }
        public string FormPosition { get => _formPosition; set => SetProperty(ref _formPosition, value); }
        public string FormDailyRate { get => _formDailyRate; set => SetProperty(ref _formDailyRate, value); }
        public string FormDepartment { get => _formDepartment; set => SetProperty(ref _formDepartment, value); }
        public string FormHireDate { get => _formHireDate; set => SetProperty(ref _formHireDate, value); }
        public string FormError { get => _formError; set => SetProperty(ref _formError, value); }

        public int CurrentPage { get => _currentPage; set { SetProperty(ref _currentPage, value); FilterEmployees(); } }
        public int TotalPages { get => _totalPages; set => SetProperty(ref _totalPages, value); }
        public int PageSize { get => _pageSize; set { SetProperty(ref _pageSize, value); CurrentPage = 1; FilterEmployees(); } }

        public ObservableCollection<EmployeeItem> Employees { get; } = new();
        public ObservableCollection<EmployeeItem> FilteredEmployees { get; } = new();
        public ObservableCollection<string> Departments { get; } = new();

        public ICommand AddEmployeeCommand { get; }
        public ICommand EditEmployeeCommand { get; }
        public ICommand DeleteEmployeeCommand { get; }
        public ICommand SaveEmployeeCommand { get; }
        public ICommand CancelFormCommand { get; }

        public ICommand NextPageCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand SortCommand { get; }

        public void SortData(string column)
        {
            if (_sortColumn == column) _sortAscending = !_sortAscending;
            else { _sortColumn = column; _sortAscending = true; }
            _currentPage = 1;
            OnPropertyChanged(nameof(CurrentPage));
            FilterEmployees();
        }

        public EmployeeViewModel()
        {
            AddEmployeeCommand = new RelayCommand(_ => ShowAddForm());
            EditEmployeeCommand = new RelayCommand(emp => ShowEditForm(emp as EmployeeItem));
            DeleteEmployeeCommand = new RelayCommand(emp => DeleteEmployee(emp as EmployeeItem));
            SaveEmployeeCommand = new RelayCommand(_ => SaveEmployee());
            CancelFormCommand = new RelayCommand(_ => { IsFormVisible = false; FormError = ""; });

            NextPageCommand = new RelayCommand(_ => { if (CurrentPage < TotalPages) CurrentPage++; });
            PreviousPageCommand = new RelayCommand(_ => { if (CurrentPage > 1) CurrentPage--; });
            SortCommand = new RelayCommand(p => SortData(p?.ToString() ?? "Full Name"));
        }

        public void LoadEmployees()
        {
            try
            {
                if (!DatabaseHelper.TestConnection()) return;

                Employees.Clear();
                Departments.Clear();

                using var conn = DatabaseHelper.GetConnection();
                conn.Open();

                // Load departments
                using (var cmd = new SqliteCommand("SELECT name FROM departments WHERE is_active = 1 ORDER BY name", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read()) Departments.Add(reader.GetString(0));
                }

                // Load employees
                using (var cmd = new SqliteCommand(
                    @"SELECT e.*, d.name as dept_name FROM employees e
                      LEFT JOIN departments d ON e.department_id = d.id
                      ORDER BY e.last_name, e.first_name", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var isActive = reader.GetInt32(reader.GetOrdinal("is_active")) == 1;
                        Employees.Add(new EmployeeItem
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("id")),
                            EmpNumber = reader.GetString(reader.GetOrdinal("emp_number")),
                            FirstName = reader.GetString(reader.GetOrdinal("first_name")),
                            LastName = reader.GetString(reader.GetOrdinal("last_name")),
                            FullName = $"{reader.GetString(reader.GetOrdinal("first_name"))} {reader.GetString(reader.GetOrdinal("last_name"))}",
                            Position = reader.GetString(reader.GetOrdinal("position")),
                            Department = reader.IsDBNull(reader.GetOrdinal("dept_name")) ? "" : reader.GetString(reader.GetOrdinal("dept_name")),
                            DailyRate = reader.GetDecimal(reader.GetOrdinal("daily_rate")),
                            DailyRateFormatted = $"₱{reader.GetDecimal(reader.GetOrdinal("daily_rate")):N2}",
                            HireDate = DateTime.Parse(reader.GetString(reader.GetOrdinal("hire_date"))),
                            IsActive = isActive,
                            Status = isActive ? "Active" : "Inactive"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                FormError = $"Failed to load employees: {ex.Message}";
            }

            FilterEmployees();
        }

        private void FilterEmployees()
        {
            if (Employees == null) return;
            
            IEnumerable<EmployeeItem> query = Employees;

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                query = query.Where(e =>
                    e.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    e.Position.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    e.EmpNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    e.Department.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            switch (_sortColumn)
            {
                case "EMP #":
                    query = _sortAscending ? query.OrderBy(x => x.EmpNumber) : query.OrderByDescending(x => x.EmpNumber);
                    break;
                case "Position":
                    query = _sortAscending ? query.OrderBy(x => x.Position) : query.OrderByDescending(x => x.Position);
                    break;
                case "Department":
                    query = _sortAscending ? query.OrderBy(x => x.Department) : query.OrderByDescending(x => x.Department);
                    break;
                case "Daily Rate":
                    query = _sortAscending ? query.OrderBy(x => x.DailyRate) : query.OrderByDescending(x => x.DailyRate);
                    break;
                case "Status":
                    query = _sortAscending ? query.OrderBy(x => x.Status) : query.OrderByDescending(x => x.Status);
                    break;
                case "Full Name":
                default:
                    query = _sortAscending ? query.OrderBy(x => x.LastName).ThenBy(x => x.FirstName) : query.OrderByDescending(x => x.LastName).ThenByDescending(x => x.FirstName);
                    break;
            }

            TotalPages = Math.Max(1, (int)Math.Ceiling(query.Count() / (double)PageSize));
            if (_currentPage > TotalPages) { _currentPage = TotalPages; OnPropertyChanged(nameof(CurrentPage)); }
            if (_currentPage < 1) { _currentPage = 1; OnPropertyChanged(nameof(CurrentPage)); }

            var paged = query.Skip((_currentPage - 1) * PageSize).Take(PageSize).ToList();

            FilteredEmployees.Clear();
            foreach (var emp in paged) FilteredEmployees.Add(emp);
        }

        private void ShowAddForm()
        {
            IsEditing = false;
            FormFirstName = ""; FormLastName = ""; FormPosition = "";
            FormDailyRate = ""; FormDepartment = Departments.FirstOrDefault() ?? "";
            FormHireDate = DateTime.Now.ToString("yyyy-MM-dd");
            FormError = "";
            IsFormVisible = true;
        }

        private void ShowEditForm(EmployeeItem? emp)
        {
            if (emp == null) return;
            IsEditing = true;
            SelectedEmployee = emp;
            FormFirstName = emp.FirstName; FormLastName = emp.LastName;
            FormPosition = emp.Position; FormDailyRate = emp.DailyRate.ToString();
            FormDepartment = emp.Department; FormHireDate = emp.HireDate.ToString("yyyy-MM-dd");
            FormError = "";
            IsFormVisible = true;
        }

        private void SaveEmployee()
        {
            if (string.IsNullOrWhiteSpace(FormFirstName) || string.IsNullOrWhiteSpace(FormLastName) ||
                string.IsNullOrWhiteSpace(FormPosition) || !decimal.TryParse(FormDailyRate, out var rate))
            {
                FormError = "Please fill in all required fields correctly.";
                return;
            }

            try
            {
                if (!DatabaseHelper.TestConnection())
                {
                    FormError = "Database connection error.";
                    return;
                }

                using var conn = DatabaseHelper.GetConnection();
                conn.Open();

                if (IsEditing && SelectedEmployee != null)
                {
                    using var cmd = new SqliteCommand(
                        @"UPDATE employees SET first_name=@fn, last_name=@ln, position=@pos, daily_rate=@rate,
                          department_id=(SELECT id FROM departments WHERE name=@dept LIMIT 1)
                          WHERE id=@id", conn);
                    cmd.Parameters.AddWithValue("@fn", FormFirstName);
                    cmd.Parameters.AddWithValue("@ln", FormLastName);
                    cmd.Parameters.AddWithValue("@pos", FormPosition);
                    cmd.Parameters.AddWithValue("@rate", rate);
                    cmd.Parameters.AddWithValue("@dept", FormDepartment);
                    cmd.Parameters.AddWithValue("@id", SelectedEmployee.Id);
                    cmd.ExecuteNonQuery();
                }
                else
                {
                    using var countCmd = new SqliteCommand("SELECT COUNT(*) FROM employees", conn);
                    var count = Convert.ToInt32(countCmd.ExecuteScalar()) + 1;
                    var empNum = $"EMP-{count:D4}";

                    using var cmd = new SqliteCommand(
                        @"INSERT INTO employees (emp_number, first_name, last_name, position, daily_rate, department_id, hire_date)
                          VALUES (@num, @fn, @ln, @pos, @rate, (SELECT id FROM departments WHERE name=@dept LIMIT 1), @hd)", conn);
                    cmd.Parameters.AddWithValue("@num", empNum);
                    cmd.Parameters.AddWithValue("@fn", FormFirstName);
                    cmd.Parameters.AddWithValue("@ln", FormLastName);
                    cmd.Parameters.AddWithValue("@pos", FormPosition);
                    cmd.Parameters.AddWithValue("@rate", rate);
                    cmd.Parameters.AddWithValue("@dept", FormDepartment);
                    cmd.Parameters.AddWithValue("@hd", DateTime.Parse(FormHireDate));
                    cmd.ExecuteNonQuery();
                }

                IsFormVisible = false;
                FormError = "";
                LoadEmployees();
            }
            catch (Exception ex)
            {
                FormError = $"Error: {ex.Message}";
            }
        }

        private void DeleteEmployee(EmployeeItem? emp)
        {
            if (emp == null) return;

            var result = System.Windows.MessageBox.Show(
                $"Are you sure you want to delete {emp.FullName}?",
                "Confirm Delete",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                if (DatabaseHelper.TestConnection())
                {
                    using var conn = DatabaseHelper.GetConnection();
                    conn.Open();
                    using var cmd = new SqliteCommand("UPDATE employees SET is_active=0 WHERE id=@id", conn);
                    cmd.Parameters.AddWithValue("@id", emp.Id);
                    cmd.ExecuteNonQuery();
                    LoadEmployees();
                }
            }
        }
    }

    public class EmployeeItem
    {
        public int Id { get; set; }
        public string EmpNumber { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Position { get; set; } = "";
        public string Department { get; set; } = "";
        public decimal DailyRate { get; set; }
        public string DailyRateFormatted { get; set; } = "";
        public DateTime HireDate { get; set; }
        public bool IsActive { get; set; }
        public string Status { get; set; } = "";
    }
}
