using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using PayrollSystem.Helpers;
using PayrollSystem.DataAccess;
using MySql.Data.MySqlClient;

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

        public string SearchText { get => _searchText; set { SetProperty(ref _searchText, value); FilterEmployees(); } }
        public EmployeeItem? SelectedEmployee { get => _selectedEmployee; set => SetProperty(ref _selectedEmployee, value); }
        public bool IsFormVisible { get => _isFormVisible; set => SetProperty(ref _isFormVisible, value); }
        public bool IsEditing { get => _isEditing; set => SetProperty(ref _isEditing, value); }

        public string FormFirstName { get => _formFirstName; set => SetProperty(ref _formFirstName, value); }
        public string FormLastName { get => _formLastName; set => SetProperty(ref _formLastName, value); }
        public string FormPosition { get => _formPosition; set => SetProperty(ref _formPosition, value); }
        public string FormDailyRate { get => _formDailyRate; set => SetProperty(ref _formDailyRate, value); }
        public string FormDepartment { get => _formDepartment; set => SetProperty(ref _formDepartment, value); }
        public string FormHireDate { get => _formHireDate; set => SetProperty(ref _formHireDate, value); }
        public string FormError { get => _formError; set => SetProperty(ref _formError, value); }

        public ObservableCollection<EmployeeItem> Employees { get; } = new();
        public ObservableCollection<EmployeeItem> FilteredEmployees { get; } = new();
        public ObservableCollection<string> Departments { get; } = new();

        public ICommand AddEmployeeCommand { get; }
        public ICommand EditEmployeeCommand { get; }
        public ICommand DeleteEmployeeCommand { get; }
        public ICommand SaveEmployeeCommand { get; }
        public ICommand CancelFormCommand { get; }

        public EmployeeViewModel()
        {
            AddEmployeeCommand = new RelayCommand(_ => ShowAddForm());
            EditEmployeeCommand = new RelayCommand(emp => ShowEditForm(emp as EmployeeItem));
            DeleteEmployeeCommand = new RelayCommand(emp => DeleteEmployee(emp as EmployeeItem));
            SaveEmployeeCommand = new RelayCommand(_ => SaveEmployee());
            CancelFormCommand = new RelayCommand(_ => { IsFormVisible = false; FormError = ""; });
        }

        public void LoadEmployees()
        {
            try
            {
                if (!DatabaseHelper.TestConnection())
                {
                    if (DemoDatabase.Employees == null) DemoDatabase.Initialize();
                    
                    Employees.Clear();
                    foreach (var emp in DemoDatabase.Employees) Employees.Add(emp);
                    
                    if (Departments.Count == 0)
                    {
                        Departments.Add("ADMIN");
                        Departments.Add("Zoey's Eatery");
                        Departments.Add("Billiard Tenant");
                    }
                    FilterEmployees();
                    return;
                }

                Employees.Clear();
                Departments.Clear();

                using var conn = DatabaseHelper.GetConnection();
                conn.Open();

                // Load departments
                using (var cmd = new MySqlCommand("SELECT name FROM departments WHERE is_active = 1 ORDER BY name", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read()) Departments.Add(reader.GetString("name"));
                }

                // Load employees
                using (var cmd = new MySqlCommand(
                    @"SELECT e.*, d.name as dept_name FROM employees e
                      LEFT JOIN departments d ON e.department_id = d.id
                      ORDER BY e.last_name, e.first_name", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Employees.Add(new EmployeeItem
                        {
                            Id = reader.GetInt32("id"),
                            EmpNumber = reader.GetString("emp_number"),
                            FirstName = reader.GetString("first_name"),
                            LastName = reader.GetString("last_name"),
                            FullName = $"{reader.GetString("first_name")} {reader.GetString("last_name")}",
                            Position = reader.GetString("position"),
                            Department = reader.IsDBNull(reader.GetOrdinal("dept_name")) ? "" : reader.GetString("dept_name"),
                            DailyRate = reader.GetDecimal("daily_rate"),
                            DailyRateFormatted = $"₱{reader.GetDecimal("daily_rate"):N2}",
                            HireDate = reader.GetDateTime("hire_date"),
                            IsActive = reader.GetBoolean("is_active"),
                            Status = reader.GetBoolean("is_active") ? "Active" : "Inactive"
                        });
                    }
                }
            }
            catch
            {
                LoadDemoData();
            }

            FilterEmployees();
        }

        private void LoadDemoData()
        {
            Departments.Add("ADMIN");
            Departments.Add("Zoey's Eatery");
            Departments.Add("Billiard Tenant");

            var demoEmployees = new[]
            {
                ("EMP-0001", "Kenneth Ariel", "Francisco", "Administrator", "ADMIN", 1200m),
                ("EMP-0002", "Judy", "Peralta", "HR Manager", "ADMIN", 1500m),
                ("EMP-0003", "Trecia", "De Jesus", "Office Administrator", "ADMIN", 1100m),
                ("EMP-0004", "Alyssa Marie", "Zamudio", "Restaurant Manager", "Zoey's Eatery", 1000m),
                ("EMP-0005", "Alliyah", "Lobendino", "Head Chef", "Zoey's Eatery", 950m),
                ("EMP-0006", "Cristel Khaye", "Sevilla", "Service Staff", "Zoey's Eatery", 650m),
                ("EMP-0007", "Michael", "Villasenor", "Kitchen Staff", "Zoey's Eatery", 600m),
                ("EMP-0008", "Beverly", "Gabriel", "Cashier", "Zoey's Eatery", 550m),
                ("EMP-0009", "Charmine", "Resus", "Cashier", "Zoey's Eatery", 550m),
                ("EMP-0010", "Kiven", "Paez", "Service Staff", "Zoey's Eatery", 600m),
                ("EMP-0011", "Lucky", "Flores", "Billiard Manager", "Billiard Tenant", 800m),
                ("EMP-0012", "Romez", "Bautista", "Game Attendant", "Billiard Tenant", 500m),
                ("EMP-0013", "Jerryco", "Viador", "Game Attendant", "Billiard Tenant", 500m),
            };

            int id = 1;
            foreach (var (num, fn, ln, pos, dept, rate) in demoEmployees)
            {
                Employees.Add(new EmployeeItem
                {
                    Id = id++, EmpNumber = num, FirstName = fn, LastName = ln,
                    FullName = $"{fn} {ln}", Position = pos, Department = dept,
                    DailyRate = rate, DailyRateFormatted = $"₱{rate:N2}",
                    HireDate = DateTime.Now.AddMonths(-id), IsActive = true, Status = "Active"
                });
            }
        }

        private void FilterEmployees()
        {
            FilteredEmployees.Clear();
            var filtered = string.IsNullOrWhiteSpace(SearchText)
                ? Employees
                : new ObservableCollection<EmployeeItem>(Employees.Where(e =>
                    e.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    e.Position.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    e.EmpNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    e.Department.Contains(SearchText, StringComparison.OrdinalIgnoreCase)));

            foreach (var emp in filtered) FilteredEmployees.Add(emp);
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
                if (DatabaseHelper.TestConnection())
                {
                    using var conn = DatabaseHelper.GetConnection();
                    conn.Open();

                    if (IsEditing && SelectedEmployee != null)
                    {
                        using var cmd = new MySqlCommand(
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
                        using var countCmd = new MySqlCommand("SELECT COUNT(*) FROM employees", conn);
                        var count = Convert.ToInt32(countCmd.ExecuteScalar()) + 1;
                        var empNum = $"EMP-{count:D4}";

                        using var cmd = new MySqlCommand(
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
                else
                {
                    // Demo mode: update in-memory
                    if (IsEditing && SelectedEmployee != null)
                    {
                        // Update directly in the Employees collection
                        var emp = Employees.FirstOrDefault(e => e.Id == SelectedEmployee.Id);
                        if (emp != null)
                        {
                            emp.FirstName = FormFirstName;
                            emp.LastName = FormLastName;
                            emp.FullName = $"{FormFirstName} {FormLastName}";
                            emp.Position = FormPosition;
                            emp.DailyRate = rate;
                            emp.DailyRateFormatted = $"₱{rate:N2}";
                            emp.Department = FormDepartment;
                        }
                    }
                    else
                    {
                        var newId = Employees.Count > 0 ? Employees.Max(e => e.Id) + 1 : 1;
                        var newEmp = new EmployeeItem
                        {
                            Id = newId,
                            EmpNumber = $"EMP-{newId:D4}",
                            FirstName = FormFirstName,
                            LastName = FormLastName,
                            FullName = $"{FormFirstName} {FormLastName}",
                            Position = FormPosition,
                            DailyRate = rate,
                            DailyRateFormatted = $"₱{rate:N2}",
                            Department = FormDepartment,
                            HireDate = DateTime.TryParse(FormHireDate, out var hd) ? hd : DateTime.Now,
                            IsActive = true,
                            Status = "Active"
                        };
                        Employees.Add(newEmp);
                        if (DemoDatabase.Employees != null) DemoDatabase.Employees.Add(newEmp);
                    }

                    IsFormVisible = false;
                    FormError = "";
                    FilterEmployees();
                }
            }
            catch (Exception ex)
            {
                FormError = $"Error: {ex.Message}";
            }
        }

        private void DeleteEmployee(EmployeeItem? emp)
        {
            if (emp == null) return;
            try
            {
                if (DatabaseHelper.TestConnection())
                {
                    using var conn = DatabaseHelper.GetConnection();
                    conn.Open();
                    using var cmd = new MySqlCommand("DELETE FROM employees WHERE id = @id", conn);
                    cmd.Parameters.AddWithValue("@id", emp.Id);
                    cmd.ExecuteNonQuery();
                }
                Employees.Remove(emp);
                FilterEmployees();
            }
            catch (Exception ex)
            {
                FormError = $"Cannot delete: {ex.Message}";
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
