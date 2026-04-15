using System;
using System.Collections.Generic;
using System.Linq;

namespace PayrollSystem.Utilities
{
    /// <summary>
    /// Utility class for generating random Filipino surnames
    /// </summary>
    public static class SurnameGenerator
    {
        private static readonly string[] _commonSurnames = new string[]
        {
            "Garcia", "Santos", "Mendoza", "Reyes", "Cruz", "Bautista", "Ramos", "Aquino", "Flores", "Torres",
            "Lopez", "Gonzales", "Villanueva", "Castillo", "Morales", "Rivera", "Santiago", "Dela Cruz", "David", "Navarro",
            "Fernandez", "Martinez", "Silva", "Roxas", "Paredes", "Lim", "Tan", "Ong", "Lee", "Chua",
            "Dizon", "Salazar", "Vargas", "Molina", "Perez", "Hernandez", "Alvarez", "Sanchez", "Williams", "Brown",
            "Jones", "Miller", "Davis", "Rodriguez", "Wilson", "Anderson", "Thomas", "Taylor", "Moore", "Jackson",
            "Martin", "Lee", "Perez", "Thompson", "White", "Harris", "Sanchez", "Clark", "Lewis", "Robinson",
            "Walker", "Young", "Allen", "King", "Wright", "Scott", "Green", "Baker", "Adams", "Nelson",
            "Carter", "Mitchell", "Perez", "Roberts", "Turner", "Phillips", "Campbell", "Parker", "Evans", "Edwards",
            "Collins", "Stewart", "Sanchez", "Morris", "Rogers", "Reed", "Cook", "Morgan", "Bell", "Murphy",
            "Bailey", "Rivera", "Cooper", "Richardson", "Cox", "Howard", "Ward", "Torres", "Peterson", "Gray"
        };

        private static readonly Random _random = new Random();

        /// <summary>
        /// Generates a specified number of random surnames
        /// </summary>
        /// <param name="count">Number of surnames to generate</param>
        /// <returns>List of random surnames</returns>
        public static List<string> GenerateRandomSurnames(int count)
        {
            var surnames = new List<string>();
            var availableSurnames = _commonSurnames.ToList();

            // Ensure we don't request more surnames than available
            count = Math.Min(count, availableSurnames.Count);

            for (int i = 0; i < count; i++)
            {
                int index = _random.Next(availableSurnames.Count);
                string surname = availableSurnames[index];
                surnames.Add(surname);
                availableSurnames.RemoveAt(index); // Remove to avoid duplicates
            }

            return surnames.OrderBy(s => s).ToList(); // Sort alphabetically
        }

        /// <summary>
        /// Gets a single random surname
        /// </summary>
        /// <returns>A random surname</returns>
        public static string GetRandomSurname()
        {
            return _commonSurnames[_random.Next(_commonSurnames.Length)];
        }

        /// <summary>
        /// Displays surnames in a numbered list format
        /// </summary>
        /// <param name="surnames">List of surnames to display</param>
        public static void DisplaySurnames(List<string> surnames)
        {
            Console.WriteLine("Generated Random Surnames:");
            Console.WriteLine("==========================");
            
            for (int i = 0; i < surnames.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {surnames[i]}");
            }
            
            Console.WriteLine("==========================");
        }
    }
}
