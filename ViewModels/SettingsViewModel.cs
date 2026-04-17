using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using PayrollSystem.Helpers;
using PayrollSystem.DataAccess;
using MySql.Data.MySqlClient;

namespace PayrollSystem.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private ObservableCollection<UserItem> _users = new();
        private ObservableCollection<DepartmentItem> _departments = new();

        public ObservableCollection<UserItem> Users { get => _users; set => SetProperty(ref _users, value); }
        public ObservableCollection<DepartmentItem> Departments { get => _departments; set => SetProperty(ref _departments, value); }

        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        // --- View states ---
        private bool _isUserFormVisible;
        public bool IsUserFormVisible { get => _isUserFormVisible; set => SetProperty(ref _isUserFormVisible, value); }

        private bool _isDeptFormVisible;
        public bool IsDeptFormVisible { get => _isDeptFormVisible; set => SetProperty(ref _isDeptFormVisible, value); }

        // --- Current Tab (0=Users, 1=Departments)
        private int _selectedTabIndex = 0;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set { SetProperty(ref _selectedTabIndex, value); LoadData(); }
        }

        // --- User Form Data ---
        private UserItem? _selectedUser;
        public string FormUsername { get; set; } = "";
        public string FormFullName { get; set; } = "";
        public string FormPassword { get; set; } = "";
        public string FormRole { get; set; } = "Staff";

        // --- Dept Form Data ---
        private DepartmentItem? _selectedDept;
        public string FormDeptName { get; set; } = "";
        public string FormDeptDesc { get; set; } = "";

        // Commands
        public ICommand AddUserCommand { get; }
        public ICommand EditUserCommand { get; }
        public ICommand SaveUserCommand { get; }
        public ICommand CancelUserCommand { get; }
        public ICommand DeleteUserCommand { get; }

        public ICommand AddDeptCommand { get; }
        public ICommand EditDeptCommand { get; }
        public ICommand SaveDeptCommand { get; }
        public ICommand CancelDeptCommand { get; }
        public ICommand DeleteDeptCommand { get; }

        public SettingsViewModel()
        {
            AddUserCommand = new RelayCommand(_ => { 
                _selectedUser = null; FormUsername = ""; FormFullName = ""; FormPassword = ""; FormRole = "Staff"; StatusMessage = ""; IsUserFormVisible = true; 
                OnPropertyChanged(nameof(FormUsername)); OnPropertyChanged(nameof(FormFullName)); OnPropertyChanged(nameof(FormPassword)); OnPropertyChanged(nameof(FormRole)); 
            });
            EditUserCommand = new RelayCommand(p => {
                if (p is UserItem u) {
                    _selectedUser = u; FormUsername = u.Username; FormFullName = u.FullName; FormPassword = ""; FormRole = u.Role; StatusMessage = ""; IsUserFormVisible = true;
                    OnPropertyChanged(nameof(FormUsername)); OnPropertyChanged(nameof(FormFullName)); OnPropertyChanged(nameof(FormPassword)); OnPropertyChanged(nameof(FormRole));
                }
            });
            CancelUserCommand = new RelayCommand(_ => { IsUserFormVisible = false; StatusMessage = ""; });
            SaveUserCommand = new RelayCommand(_ => SaveUser());
            DeleteUserCommand = new RelayCommand(p => DeleteUser(p as UserItem));

            AddDeptCommand = new RelayCommand(_ => { 
                _selectedDept = null; FormDeptName = ""; FormDeptDesc = ""; StatusMessage = ""; IsDeptFormVisible = true; 
                OnPropertyChanged(nameof(FormDeptName)); OnPropertyChanged(nameof(FormDeptDesc)); 
            });
            EditDeptCommand = new RelayCommand(p => {
                if (p is DepartmentItem d) {
                    _selectedDept = d; FormDeptName = d.Name; FormDeptDesc = d.Description; StatusMessage = ""; IsDeptFormVisible = true;
                    OnPropertyChanged(nameof(FormDeptName)); OnPropertyChanged(nameof(FormDeptDesc));
                }
            });
            CancelDeptCommand = new RelayCommand(_ => { IsDeptFormVisible = false; StatusMessage = ""; });
            SaveDeptCommand = new RelayCommand(_ => SaveDepartment());
            DeleteDeptCommand = new RelayCommand(p => DeleteDepartment(p as DepartmentItem));
        }

        public void LoadData()
        {
            if (SelectedTabIndex == 0) LoadUsers();
            else LoadDepartments();
        }

        // ═════════════════ USERS CRUD ═════════════════ //

        private void LoadUsers()
        {
            Users.Clear();
            try
            {
                if (!DatabaseHelper.TestConnection()) { LoadDemoUsers(); return; }

                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                using var cmd = new MySqlCommand("SELECT id, username, full_name, role FROM users WHERE is_active=1", conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Users.Add(new UserItem
                    {
                        Id = reader.GetInt32("id"),
                        Username = reader.GetString("username"),
                        FullName = reader.GetString("full_name"),
                        Role = reader.GetString("role"),
                        IsActive = true
                    });
                }
            }
            catch { LoadDemoUsers(); }
        }

        private void LoadDemoUsers()
        {
            DemoDatabase.Initialize();
            foreach (var u in DemoDatabase.Users) if (u.IsActive) Users.Add(u);
        }

        private void SaveUser()
        {
            if (string.IsNullOrWhiteSpace(FormUsername) || string.IsNullOrWhiteSpace(FormFullName))
            {
                StatusMessage = "Username and Full Name are required.";
                return;
            }

            try
            {
                if (DatabaseHelper.TestConnection())
                {
                    using var conn = DatabaseHelper.GetConnection();
                    conn.Open();

                    if (_selectedUser == null) 
                    {
                        if (string.IsNullOrWhiteSpace(FormPassword)) FormPassword = "password123"; // default fallback
                        using var cmd = new MySqlCommand("INSERT INTO users (username, password_hash, full_name, role) VALUES (@u, @p, @f, @r)", conn);
                        cmd.Parameters.AddWithValue("@u", FormUsername);
                        cmd.Parameters.AddWithValue("@p", FormPassword);
                        cmd.Parameters.AddWithValue("@f", FormFullName);
                        cmd.Parameters.AddWithValue("@r", FormRole);
                        cmd.ExecuteNonQuery();
                    }
                    else
                    {
                        var updateQuery = "UPDATE users SET username=@u, full_name=@f, role=@r" +
                                        (!string.IsNullOrWhiteSpace(FormPassword) ? ", password_hash=@p" : "") +
                                        " WHERE id=@id";
                        using var cmd = new MySqlCommand(updateQuery, conn);
                        cmd.Parameters.AddWithValue("@u", FormUsername);
                        if (!string.IsNullOrWhiteSpace(FormPassword)) cmd.Parameters.AddWithValue("@p", FormPassword);
                        cmd.Parameters.AddWithValue("@f", FormFullName);
                        cmd.Parameters.AddWithValue("@r", FormRole);
                        cmd.Parameters.AddWithValue("@id", _selectedUser.Id);
                        cmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    if (_selectedUser == null)
                    {
                        var newId = DemoDatabase.Users.Count > 0 ? DemoDatabase.Users.Max(u => u.Id) + 1 : 1;
                        if (string.IsNullOrWhiteSpace(FormPassword)) FormPassword = "password123";
                        DemoDatabase.Users.Add(new UserItem
                        {
                            Id = newId, Username = FormUsername, PasswordHash = FormPassword,
                            FullName = FormFullName, Role = FormRole, IsActive = true
                        });
                    }
                    else
                    {
                        var existing = DemoDatabase.Users.FirstOrDefault(u => u.Id == _selectedUser.Id);
                        if (existing != null)
                        {
                            existing.Username = FormUsername;
                            existing.FullName = FormFullName;
                            existing.Role = FormRole;
                            if (!string.IsNullOrWhiteSpace(FormPassword)) existing.PasswordHash = FormPassword;
                        }
                    }
                    DemoDatabase.SaveChanges();
                }

                IsUserFormVisible = false;
                StatusMessage = "User saved successfully.";
                LoadUsers();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        private void DeleteUser(UserItem? user)
        {
            if (user == null) return;
            if (user.Username.ToLower() == "admin") { StatusMessage = "Cannot delete the default admin account."; return; }

            var result = System.Windows.MessageBox.Show(
                $"Are you sure you want to delete the user account '{user.Username}'?",
                "Confirm Delete",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result != System.Windows.MessageBoxResult.Yes) return;

            try
            {
                if (DatabaseHelper.TestConnection())
                {
                    using var conn = DatabaseHelper.GetConnection();
                    conn.Open();
                    using var cmd = new MySqlCommand("UPDATE users SET is_active=0 WHERE id=@id", conn);
                    cmd.Parameters.AddWithValue("@id", user.Id);
                    cmd.ExecuteNonQuery();
                }
                else
                {
                    var demoUser = DemoDatabase.Users.FirstOrDefault(u => u.Id == user.Id);
                    if (demoUser != null) { DemoDatabase.Users.Remove(demoUser); DemoDatabase.SaveChanges(); }
                }
                StatusMessage = "User deleted.";
                LoadUsers();
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
        }

        // ═════════════════ DEPARTMENTS CRUD ═════════════════ //

        private void LoadDepartments()
        {
            Departments.Clear();
            try
            {
                if (!DatabaseHelper.TestConnection()) { LoadDemoDepartments(); return; }

                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                using var cmd = new MySqlCommand("SELECT id, name, description FROM departments WHERE is_active=1", conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Departments.Add(new DepartmentItem
                    {
                        Id = reader.GetInt32("id"),
                        Name = reader.GetString("name"),
                        Description = reader.IsDBNull(2) ? "" : reader.GetString("description")
                    });
                }
            }
            catch { LoadDemoDepartments(); }
        }

        private void LoadDemoDepartments()
        {
            DemoDatabase.Initialize();
            foreach (var d in DemoDatabase.Departments) Departments.Add(d);
        }

        private void SaveDepartment()
        {
            if (string.IsNullOrWhiteSpace(FormDeptName))
            {
                StatusMessage = "Department Name is required.";
                return;
            }

            try
            {
                if (DatabaseHelper.TestConnection())
                {
                    using var conn = DatabaseHelper.GetConnection();
                    conn.Open();

                    if (_selectedDept == null)
                    {
                        using var cmd = new MySqlCommand("INSERT INTO departments (name, description) VALUES (@n, @d)", conn);
                        cmd.Parameters.AddWithValue("@n", FormDeptName);
                        cmd.Parameters.AddWithValue("@d", FormDeptDesc);
                        cmd.ExecuteNonQuery();
                    }
                    else
                    {
                        using var cmd = new MySqlCommand("UPDATE departments SET name=@n, description=@d WHERE id=@id", conn);
                        cmd.Parameters.AddWithValue("@n", FormDeptName);
                        cmd.Parameters.AddWithValue("@d", FormDeptDesc);
                        cmd.Parameters.AddWithValue("@id", _selectedDept.Id);
                        cmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    if (_selectedDept == null)
                    {
                        var newId = DemoDatabase.Departments.Count > 0 ? DemoDatabase.Departments.Max(d => d.Id) + 1 : 1;
                        DemoDatabase.Departments.Add(new DepartmentItem
                        {
                            Id = newId, Name = FormDeptName, Description = FormDeptDesc
                        });
                    }
                    else
                    {
                        var existing = DemoDatabase.Departments.FirstOrDefault(d => d.Id == _selectedDept.Id);
                        if (existing != null)
                        {
                            existing.Name = FormDeptName;
                            existing.Description = FormDeptDesc;
                        }
                    }
                    DemoDatabase.SaveChanges();
                }

                IsDeptFormVisible = false;
                StatusMessage = "Department saved successfully.";
                LoadDepartments();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        private void DeleteDepartment(DepartmentItem? dept)
        {
            if (dept == null) return;

            var result = System.Windows.MessageBox.Show(
                $"Are you sure you want to delete the department '{dept.Name}'?",
                "Confirm Delete",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result != System.Windows.MessageBoxResult.Yes) return;

            try
            {
                if (DatabaseHelper.TestConnection())
                {
                    using var conn = DatabaseHelper.GetConnection();
                    conn.Open();
                    using var cmd = new MySqlCommand("UPDATE departments SET is_active=0 WHERE id=@id", conn);
                    cmd.Parameters.AddWithValue("@id", dept.Id);
                    cmd.ExecuteNonQuery();
                }
                else
                {
                    var demoDept = DemoDatabase.Departments.FirstOrDefault(d => d.Id == dept.Id);
                    if (demoDept != null) { DemoDatabase.Departments.Remove(demoDept); DemoDatabase.SaveChanges(); }
                }
                StatusMessage = "Department deleted.";
                LoadDepartments();
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
        }
    }
}
