using System;
using System.Collections.ObjectModel;
using PayrollSystem.ViewModels;

namespace PayrollSystem.DataAccess
{
    public static class DemoDatabase
    {
        public static ObservableCollection<EmployeeItem> Employees { get; private set; }

        public static void Initialize()
        {
            Employees = new ObservableCollection<EmployeeItem>();

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
    }
}
