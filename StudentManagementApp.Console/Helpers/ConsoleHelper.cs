
using System.Globalization;

namespace StudentManagementSystem.Helpers
{
   public class ConsoleHelper
    {
        public static string ReadRequired(string prompt)
        {
            while (true)
            {
                Console.Write($"{prompt}: ");
                string? input = Console.ReadLine()?.Trim();

                if (!string.IsNullOrWhiteSpace(input))
                    return input;

                Error("This field is mandatory. Please provide a valid value.");
            }
        }
        
        public static string ReadOptional(string prompt, string currentValue)
        {
            Console.Write($"{prompt} [{currentValue}]: ");
            string? input = Console.ReadLine()?.Trim();
            return string.IsNullOrWhiteSpace(input) ? currentValue : input;
        }

        public static int ReadInt(string prompt, int min = int.MinValue, int max = int.MaxValue)
        {
            while (true)
            {
                Console.Write($"{prompt}: ");
                if (int.TryParse(Console.ReadLine(), out int result) && result >= min && result <= max)
                    return result;

                Error($"Invalid numeric choice. Please enter an integer between {min} and {max}.");
            }
        }

        public static decimal ReadDecimal(string prompt, decimal min = 0.00m)
        {
            while (true)
            {
                Console.Write($"{prompt}: ");
                if (decimal.TryParse(Console.ReadLine(), out decimal result) && result >= min)
                    return result;

                Error($"Invalid financial value. Please enter a decimal amount greater than or equal to {min:F2}.");
            }
        }

        public static DateTime ReadDate(string prompt)
        {
            while (true)
            {
                Console.Write($"{prompt} (yyyy-MM-dd): ");
                if (DateTime.TryParseExact(Console.ReadLine(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime result))
                    return result.ToUniversalTime();

                Error("Invalid format layout. You must explicitly follow the 'yyyy-MM-dd' pattern.");
            }
        }

        public static bool Confirm(string prompt)
        {
            while (true)
            {
                Console.Write($"{prompt} (y/n): ");
                char choice = char.ToLower(Console.ReadKey(intercept: false).KeyChar);
                Console.WriteLine(); // Advance cursor line safely

                if (choice == 'y') return true;
                if (choice == 'n') return false;

                Error("Invalid key registry. Press 'y' for Yes or 'n' for No.");
            }
        }

        public static int ShowMenu(string title, string[] options)
        {
            Console.Clear();
            Info($"=== {title.ToUpper()} ===");

            for (int i = 0; i < options.Length; i++)
            {
                Console.WriteLine($"{i + 1}. {options[i]}");
            }
            Console.WriteLine("0. Back / Exit");
            Console.WriteLine(new string('-', title.Length + 8));

            return ReadInt("Select an option", 0, options.Length);
        }

        public static void PrintList<T>(string header, IEnumerable<T> items)
        {
            Console.WriteLine();
            Info($"--- {header} ---");

            var list = items.ToList();
            if (!list.Any())
            {
                Console.WriteLine("No records discovered or available.");
                return;
            }

            foreach (var item in list)
            {
                Console.WriteLine(item?.ToString());
            }
            Console.WriteLine(new string('-', header.Length + 8));
        }


        public static void Success(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[SUCCESS] {message}");
            Console.ResetColor();
        }

        public static void Error(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] {message}");
            Console.ResetColor();
        }

        public static void Info(string message)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        public static void Pause()
        {
            Console.WriteLine("\nPress any key to proceed back to the execution stream...");
            Console.ReadKey(intercept: true);
        }
    }
}
