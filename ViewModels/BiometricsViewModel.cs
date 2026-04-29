using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Input;
using PayrollSystem.DataAccess;
using PayrollSystem.Helpers;
using Microsoft.Data.Sqlite;
using PayrollSystem.Models;

namespace PayrollSystem.ViewModels
{
    public class BiometricsViewModel : BaseViewModel
    {
        private string _statusMessage = "";
        private string _searchText = "";
        private bool _isDeleteModalVisible = false;
        private BiometricsImportRecord? _pendingDeleteRecord = null;
        private string _deleteTargetName = "";

        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
        public string SearchText 
        { 
            get => _searchText; 
            set 
            { 
                SetProperty(ref _searchText, value); 
                FilterRecords(); 
            } 
        }
        public bool IsDeleteModalVisible { get => _isDeleteModalVisible; set => SetProperty(ref _isDeleteModalVisible, value); }
        public string DeleteTargetName { get => _deleteTargetName; set => SetProperty(ref _deleteTargetName, value); }

        public ObservableCollection<BiometricsImportRecord> AllRecords { get; } = new();
        public ObservableCollection<BiometricsImportRecord> FilteredRecords { get; } = new();

        public ICommand ImportCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ConfirmDeleteCommand { get; }
        public ICommand CancelDeleteCommand { get; }

        public BiometricsViewModel()
        {
            ImportCommand = new RelayCommand(_ => ImportBiometrics());
            DeleteCommand = new RelayCommand(p => ShowDeleteModal(p as BiometricsImportRecord));
            ConfirmDeleteCommand = new RelayCommand(_ => ConfirmDelete());
            CancelDeleteCommand = new RelayCommand(_ => { IsDeleteModalVisible = false; _pendingDeleteRecord = null; });
            EnsureTableExists();
        }

        private void EnsureTableExists()
        {
            try
            {
                if (!DatabaseHelper.TestConnection()) return;
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                using var cmd = new SqliteCommand(@"
                    CREATE TABLE IF NOT EXISTS biometrics_imports (
                        id INT AUTO_INCREMENT PRIMARY KEY,
                        file_name VARCHAR(255) NOT NULL,
                        file_path VARCHAR(255) NOT NULL,
                        file_hash VARCHAR(64) NOT NULL UNIQUE,
                        employee_count INT NOT NULL,
                        period_start DATE NOT NULL,
                        period_end DATE NOT NULL,
                        imported_at DATETIME DEFAULT CURRENT_TIMESTAMP
                    );", conn);
                cmd.ExecuteNonQuery();
            }
            catch { }
        }

        public void LoadData()
        {
            AllRecords.Clear();
            try
            {
                if (!DatabaseHelper.TestConnection()) return;

                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                using var cmd = new SqliteCommand("SELECT * FROM biometrics_imports ORDER BY imported_at DESC", conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    AllRecords.Add(new BiometricsImportRecord
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("id")),
                        FileName = reader.GetString(reader.GetOrdinal("file_name")),
                        FilePath = reader.GetString(reader.GetOrdinal("file_path")),
                        FileHash = reader.GetString(reader.GetOrdinal("file_hash")),
                        EmployeeCount = reader.GetInt32(reader.GetOrdinal("employee_count")),
                        PeriodStart = DateTime.Parse(reader.GetString(reader.GetOrdinal("period_start"))),
                        PeriodEnd = DateTime.Parse(reader.GetString(reader.GetOrdinal("period_end"))),
                        ImportedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("imported_at")))
                    });
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading data: {ex.Message}";
            }

            FilterRecords();
        }

        private void FilterRecords()
        {
            FilteredRecords.Clear();
            foreach (var r in AllRecords)
            {
                if (string.IsNullOrWhiteSpace(SearchText) ||
                    r.FileName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    r.PeriodRange.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                {
                    FilteredRecords.Add(r);
                }
            }
        }

        private void ImportBiometrics()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Excel Files|*.xls;*.xlsx|All Files|*.*",
                Title = "Select Biometrics Excel File"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    if (!DatabaseHelper.TestConnection())
                    {
                        StatusMessage = "Database connection required to import.";
                        return;
                    }

                    // 1. Compute file hash for duplicate detection
                    string fileHash = ComputeFileHash(dialog.FileName);

                    using var conn = DatabaseHelper.GetConnection();
                    conn.Open();

                    // 2. Check if already imported
                    using var checkCmd = new SqliteCommand("SELECT COUNT(*) FROM biometrics_imports WHERE file_hash=@hash", conn);
                    checkCmd.Parameters.AddWithValue("@hash", fileHash);
                    if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0)
                    {
                        MessageBox.Show(
                            $"This exact Excel file has already been imported!",
                            "Duplicate Import Detected",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    // 3. Parse the file
                    StatusMessage = "Parsing biometrics data...";
                    var result = Utilities.BiometricsParser.ParseExcelFile(dialog.FileName);

                    if (result.Records.Count == 0)
                    {
                        StatusMessage = "⚠️ No valid attendance records found in the file.";
                        return;
                    }

                    // 4. Save the import record to MySQL
                    using var insCmd = new SqliteCommand(@"
                        INSERT INTO biometrics_imports (file_name, file_path, file_hash, employee_count, period_start, period_end, imported_at)
                        VALUES (@fname, @fpath, @fhash, @ecount, @pstart, @pend, @iat)", conn);
                    insCmd.Parameters.AddWithValue("@fname", Path.GetFileName(dialog.FileName));
                    insCmd.Parameters.AddWithValue("@fpath", dialog.FileName);
                    insCmd.Parameters.AddWithValue("@fhash", fileHash);
                    insCmd.Parameters.AddWithValue("@ecount", result.Records.Count);
                    insCmd.Parameters.AddWithValue("@pstart", result.StartDate);
                    insCmd.Parameters.AddWithValue("@pend", result.EndDate);
                    insCmd.Parameters.AddWithValue("@iat", DateTime.Now);
                    insCmd.ExecuteNonQuery();

                    LoadData();

                    StatusMessage = $"✓ Successfully imported \"{Path.GetFileName(dialog.FileName)}\" — {result.Records.Count} employees";
                }
                catch (Exception ex)
                {
                    StatusMessage = "Error parsing file: " + ex.Message;
                }
            }
        }

        private void ShowDeleteModal(BiometricsImportRecord? record)
        {
            if (record == null) return;
            _pendingDeleteRecord = record;
            DeleteTargetName = $"{record.FileName} ({record.PeriodRange})";
            IsDeleteModalVisible = true;
        }

        private void ConfirmDelete()
        {
            if (_pendingDeleteRecord != null)
            {
                try
                {
                    if (DatabaseHelper.TestConnection())
                    {
                        using var conn = DatabaseHelper.GetConnection();
                        conn.Open();
                        using var cmd = new SqliteCommand("DELETE FROM biometrics_imports WHERE id=@id", conn);
                        cmd.Parameters.AddWithValue("@id", _pendingDeleteRecord.Id);
                        cmd.ExecuteNonQuery();
                    }
                    
                    AllRecords.Remove(_pendingDeleteRecord);
                    FilterRecords();
                    StatusMessage = $"✓ Deleted biometrics import: {_pendingDeleteRecord.FileName}";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error deleting: {ex.Message}";
                }

                _pendingDeleteRecord = null;
            }
            IsDeleteModalVisible = false;
        }

        private static string ComputeFileHash(string filePath)
        {
            using (var sha256 = SHA256.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    byte[] hash = sha256.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
    }
}
