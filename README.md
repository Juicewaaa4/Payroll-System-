# 🧾 Payroll System

A desktop payroll management application built with **C# WPF (.NET 8)** that integrates real-time biometric attendance data to automate employee pay computation. The system combines static employee profiles, second-by-second attendance logs, and configurable pay rules to ensure accurate and efficient payroll processing.

---

## ✨ Features

- 👤 **Employee Management** — Store and manage employee profiles and records
- 🕐 **Biometric Attendance Integration** — Process real-time attendance logs from biometric devices
- 💰 **Automated Pay Computation** — Apply configurable pay rules to calculate salaries automatically
- 📊 **Reporting** — Generate payroll reports and summaries
- ✅ **Validation** — Input validation to ensure data integrity
- 🎨 **Themed UI** — Clean and modern WPF interface with custom theming

---

## 🛠️ Tech Stack

| Technology | Purpose |
|---|---|
| C# / .NET 8 | Core application language & runtime |
| WPF (Windows Presentation Foundation) | Desktop UI framework |
| MySQL (`MySql.Data` v9.1.0) | Database backend |
| ExcelDataReader v3.8.0 | Import/export employee and payroll data via Excel |
| MVVM Pattern | UI architecture (Models, ViewModels, Views) |

---

## 📁 Project Structure

```
PayrollSystem/
├── Configuration/       # App-wide configuration settings
├── DataAccess/          # Database queries and data access layer
├── Database/            # Database schema and connection management
├── Exceptions/          # Custom exception handling
├── Helpers/             # Utility/helper methods
├── Models/              # Data models (Employee, Attendance, Payroll, etc.)
├── Reporting/           # Report generation logic
├── Services/            # Business logic and service layer
├── Themes/              # WPF UI themes and styles
├── UI/                  # UI components and controls
├── Utilities/           # General utility classes
├── Validation/          # Input validation logic
├── ViewModels/          # MVVM ViewModels
├── Views/               # WPF XAML Views
├── Logo/                # Application logo assets
├── App.xaml             # Application entry point
└── MainWindow.xaml      # Main application window
```

---

## ⚙️ Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- Windows OS (WPF is Windows-only)
- MySQL Server (local or remote)
- Visual Studio 2022 or later (recommended)

---

## 🚀 Getting Started

1. **Clone the repository**
   ```bash
   git clone https://github.com/Juicewaaa4/Payroll-System-.git
   cd Payroll-System-
   ```

2. **Set up the database**
   - Create a MySQL database for the system
   - Run the SQL scripts found in the `Database/` folder to set up the schema

3. **Configure the connection string**
   - Update your database credentials in the `Configuration/` folder

4. **Restore NuGet packages & build**
   ```bash
   dotnet restore
   dotnet build
   ```

5. **Run the application**
   ```bash
   dotnet run
   ```
   Or open `PayrollSystem.csproj` in Visual Studio and press **F5**.

---

## 📦 Dependencies

```xml
<PackageReference Include="MySql.Data" Version="9.1.0" />
<PackageReference Include="ExcelDataReader" Version="3.8.0" />
<PackageReference Include="ExcelDataReader.DataSet" Version="3.8.0" />
```

---

## 📄 License

This project is open source and available for personal and educational use.

---

## 👤 Author

**Joshua** ([@Juicewaaa4](https://github.com/Juicewaaa4))
