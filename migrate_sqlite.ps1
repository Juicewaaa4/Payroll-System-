$files = @(
    'c:\Payroll System\ViewModels\SettingsViewModel.cs',
    'c:\Payroll System\ViewModels\BiometricsViewModel.cs',
    'c:\Payroll System\ViewModels\BatchPrintViewModel.cs',
    'c:\Payroll System\ViewModels\ReportsViewModel.cs',
    'c:\Payroll System\ViewModels\PayslipViewModel.cs',
    'c:\Payroll System\ViewModels\PayrollViewModel.cs'
)

foreach ($f in $files) {
    $content = Get-Content $f -Raw
    $content = $content -replace 'using MySql\.Data\.MySqlClient;', 'using Microsoft.Data.Sqlite;'
    $content = $content -replace 'MySqlCommand', 'SqliteCommand'
    $content = $content -replace 'MySqlConnection', 'SqliteConnection'
    $content = $content -replace 'reader\.GetString\("(\w+)"\)', 'reader.GetString(reader.GetOrdinal("$1"))'
    $content = $content -replace 'reader\.GetInt32\("(\w+)"\)', 'reader.GetInt32(reader.GetOrdinal("$1"))'
    $content = $content -replace 'reader\.GetDecimal\("(\w+)"\)', 'reader.GetDecimal(reader.GetOrdinal("$1"))'
    $content = $content -replace 'reader\.GetDateTime\("(\w+)"\)', 'DateTime.Parse(reader.GetString(reader.GetOrdinal("$1")))'
    Set-Content $f $content -NoNewline
    Write-Host "Updated: $f"
}
