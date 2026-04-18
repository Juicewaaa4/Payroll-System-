using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Input;
using PayrollSystem.DataAccess;
using PayrollSystem.Helpers;

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
        }

        public void LoadData()
        {
            DemoDatabase.Initialize();

            AllRecords.Clear();
            foreach (var record in DemoDatabase.BiometricsImports.OrderByDescending(r => r.ImportedAt))
            {
                AllRecords.Add(record);
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
                    // 1. Compute file hash for duplicate detection
                    string fileHash = ComputeFileHash(dialog.FileName);

                    // 2. Check if already imported
                    var existingImport = DemoDatabase.BiometricsImports.FirstOrDefault(r => r.FileHash == fileHash);
                    if (existingImport != null)
                    {
                        MessageBox.Show(
                            $"This exact Excel file has already been imported!\n\n" +
                            $"File: {existingImport.FileName}\n" +
                            $"Period: {existingImport.PeriodRange}\n" +
                            $"Imported On: {existingImport.ImportedAtFormatted}\n\n" +
                            $"If you need to re-import, please delete the existing record first.",
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

                    // 4. Save the import record
                    int newId = DemoDatabase.BiometricsImports.Count > 0
                        ? DemoDatabase.BiometricsImports.Max(r => r.Id) + 1
                        : 1;

                    var importRecord = new BiometricsImportRecord
                    {
                        Id = newId,
                        FileName = Path.GetFileName(dialog.FileName),
                        FilePath = dialog.FileName,
                        ImportedAt = DateTime.Now,
                        PeriodStart = result.StartDate,
                        PeriodEnd = result.EndDate,
                        EmployeeCount = result.Records.Count,
                        FileHash = fileHash
                    };

                    DemoDatabase.BiometricsImports.Insert(0, importRecord);
                    DemoDatabase.SaveChanges();

                    // 5. Refresh local list
                    AllRecords.Insert(0, importRecord);
                    FilterRecords();

                    StatusMessage = $"✓ Successfully imported \"{importRecord.FileName}\" — {result.Records.Count} employees, Period: {importRecord.PeriodRange}";
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
                DemoDatabase.BiometricsImports.Remove(_pendingDeleteRecord);
                DemoDatabase.SaveChanges();

                AllRecords.Remove(_pendingDeleteRecord);
                FilterRecords();

                StatusMessage = $"✓ Deleted biometrics import: {_pendingDeleteRecord.FileName}";
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
