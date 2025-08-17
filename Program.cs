using System;
using Microsoft.Data.Sqlite;
using System.IO;
using System.Collections.Generic;
using System.Linq;

class Program
{
    private const string DatabaseFile = "shows.db";
    private static readonly string ConnectionString = new SqliteConnectionStringBuilder
    {
        DataSource = DatabaseFile,
        Mode = SqliteOpenMode.ReadWriteCreate
    }.ToString();

    private static readonly List<string> ValidStatuses = new List<string> 
    { 
        "Watching", "Completed", "On Hold", "Dropped", "Plan to Watch" 
    };

    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        InitializeDatabase();

        while (true)
        {
            Console.Clear();
            ShowQuickSummary();
            Console.WriteLine("\n📺 Show Tracker");
            Console.WriteLine("1. Add Show");
            Console.WriteLine("2. View Shows");
            Console.WriteLine("3. Update Episodes");
            Console.WriteLine("4. Update Status");
            Console.WriteLine("5. Rate a Show");
            Console.WriteLine("6. Delete a Show");
            Console.WriteLine("7. Export to File");
            Console.WriteLine("0. Exit");
            Console.Write("Select an option: ");
            
            string? choice = Console.ReadLine();

            switch (choice)
            {
                case "1": AddShow(); break;
                case "2": ViewShowsMenu(); break;
                case "3": UpdateEpisodes(); break;
                case "4": UpdateStatus(); break;
                case "5": RateShow(); break;
                case "6": DeleteShow(); break;
                case "7": ExportToFile(); break;
                case "0": return;
                default: Console.WriteLine("❌ Invalid option, try again."); 
                         Console.ReadKey(); break;
            }
        }
    }

    private static void ShowQuickSummary()
    {
        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            string sql = "SELECT Status, COUNT(*) as Count FROM Shows GROUP BY Status;";
            using (var command = new SqliteCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                Console.WriteLine("📊 Your Show Summary:");
                if (!reader.HasRows)
                {
                    Console.WriteLine("  No shows tracked yet");
                    return;
                }

                while (reader.Read())
                {
                    Console.WriteLine($"  {reader["Status"]}: {reader["Count"]}");
                }
            }
        }
    }

    private static void InitializeDatabase()
    {
        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();

            string createTableSql = @"
            CREATE TABLE IF NOT EXISTS Shows (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT NOT NULL,
                Genre TEXT,
                EpisodesWatched INTEGER DEFAULT 0,
                Status TEXT,
                Rating REAL,
                LastUpdated DATETIME DEFAULT CURRENT_TIMESTAMP
            );";

            using (var command = new SqliteCommand(createTableSql, connection))
            {
                command.ExecuteNonQuery();
            }

            // Add LastUpdated column if missing
            try
            {
                string checkColumnSql = "SELECT LastUpdated FROM Shows LIMIT 1;";
                using (var testCommand = new SqliteCommand(checkColumnSql, connection))
                {
                    testCommand.ExecuteScalar();
                }
            }
            catch
            {
                string addColumnSql = "ALTER TABLE Shows ADD COLUMN LastUpdated DATETIME DEFAULT CURRENT_TIMESTAMP;";
                using (var addCommand = new SqliteCommand(addColumnSql, connection))
                {
                    addCommand.ExecuteNonQuery();
                }
                
                string updateSql = "UPDATE Shows SET LastUpdated = CURRENT_TIMESTAMP;";
                using (var updateCommand = new SqliteCommand(updateSql, connection))
                {
                    updateCommand.ExecuteNonQuery();
                }
            }
        }
    }

    private static void AddShow()
    {
        Console.Clear();
        Console.Write("Enter show title: ");
        string? title = Console.ReadLine();

        Console.Write("Enter genre: ");
        string? genre = Console.ReadLine();

        Console.WriteLine("\nAvailable statuses:");
        for (int i = 0; i < ValidStatuses.Count; i++)
        {
            Console.WriteLine($"{i+1}. {ValidStatuses[i]}");
        }

        int statusChoice;
        do
        {
            Console.Write("Select status (1-5): ");
        } while (!int.TryParse(Console.ReadLine(), out statusChoice) || statusChoice < 1 || statusChoice > 5);
        
        string status = ValidStatuses[statusChoice - 1];

        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            string sql = @"
            INSERT INTO Shows (Title, Genre, Status)
            VALUES (@Title, @Genre, @Status);";

            using (var command = new SqliteCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@Title", title);
                command.Parameters.AddWithValue("@Genre", genre);
                command.Parameters.AddWithValue("@Status", status);
                command.ExecuteNonQuery();
            }
        }

        Console.WriteLine("\n✅ Show added!");
        Console.ReadKey();
    }

    private static void ViewShowsMenu()
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("🔍 View Shows");
            Console.WriteLine("1. View All");
            Console.WriteLine("2. View by Status");
            Console.WriteLine("3. View by Rating (High to Low)");
            Console.WriteLine("4. View Recently Updated");
            Console.WriteLine("5. View by Genre");
            Console.WriteLine("0. Back to Main Menu");
            Console.Write("Select an option: ");
            
            string? choice = Console.ReadLine();

            switch (choice)
            {
                case "1": ViewShows(""); break;
                case "2": ViewByStatus(); break;
                case "3": ViewShows("ORDER BY Rating DESC"); break;
                case "4": ViewShows("ORDER BY LastUpdated DESC LIMIT 10"); break;
                case "5": ViewByGenre(); break;
                case "0": return;
                default: Console.WriteLine("❌ Invalid option, try again."); 
                         Console.ReadKey(); break;
            }
        }
    }

    private static void ViewShows(string options)
    {
        Console.Clear();
        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            string sql = $"SELECT * FROM Shows {options};";
            using (var command = new SqliteCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                Console.WriteLine("📋 Your Shows:");
                if (!reader.HasRows)
                {
                    Console.WriteLine("  No shows found");
                    Console.ReadKey();
                    return;
                }

                while (reader.Read())
                {
                    Console.WriteLine(
                        $"[{reader["Id"]}] {reader["Title"]} ({reader["Genre"]}) " +
                        $"- Episodes: {reader["EpisodesWatched"]} " +
                        $"- Status: {reader["Status"]} " +
                        $"- Rating: {GetRatingDisplay(reader["Rating"])} " +
                        $"- Last Updated: {GetDateTimeDisplay(reader["LastUpdated"])}"
                    );
                }
            }
        }
        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey();
    }

    private static string GetRatingDisplay(object ratingValue)
    {
        if (ratingValue is DBNull || ratingValue == null) return "N/A";
        double rating = Convert.ToDouble(ratingValue);
        return rating == 0 ? "N/A" : rating.ToString("0.0");
    }

    private static string GetDateTimeDisplay(object dateValue)
    {
        if (dateValue is DBNull || dateValue == null) return "N/A";
        return Convert.ToDateTime(dateValue).ToString("yyyy-MM-dd");
    }

    private static void ViewByStatus()
    {
        Console.Clear();
        Console.WriteLine("Available statuses:");
        for (int i = 0; i < ValidStatuses.Count; i++)
        {
            Console.WriteLine($"{i+1}. {ValidStatuses[i]}");
        }

        int choice;
        do
        {
            Console.Write("Select status to view (1-5): ");
        } while (!int.TryParse(Console.ReadLine(), out choice) || choice < 1 || choice > 5);

        string status = ValidStatuses[choice - 1];
        ViewShows($"WHERE Status = '{status}'");
    }

    private static void ViewByGenre()
    {
        Console.Clear();
        Console.Write("Enter genre to filter: ");
        string? genre = Console.ReadLine();

        if (!string.IsNullOrWhiteSpace(genre))
        {
            ViewShows($"WHERE Genre LIKE '%{genre}%'");
        }
    }

    private static void UpdateEpisodes()
    {
        Console.Clear();
        int id;
        do
        {
            Console.Write("Enter show ID: ");
        } while (!int.TryParse(Console.ReadLine(), out id));

        int episodes;
        do
        {
            Console.Write("Enter episodes watched: ");
        } while (!int.TryParse(Console.ReadLine(), out episodes));

        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            string sql = "UPDATE Shows SET EpisodesWatched = @Episodes, LastUpdated = CURRENT_TIMESTAMP WHERE Id = @Id;";
            using (var command = new SqliteCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@Episodes", episodes);
                command.Parameters.AddWithValue("@Id", id);
                int affected = command.ExecuteNonQuery();
                if (affected == 0)
                    Console.WriteLine("❌ Show not found!");
                else
                    Console.WriteLine("✅ Episodes updated!");
            }
        }
        Console.ReadKey();
    }

    private static void UpdateStatus()
    {
        Console.Clear();
        int id;
        do
        {
            Console.Write("Enter show ID: ");
        } while (!int.TryParse(Console.ReadLine(), out id));

        Console.WriteLine("\nAvailable statuses:");
        for (int i = 0; i < ValidStatuses.Count; i++)
        {
            Console.WriteLine($"{i+1}. {ValidStatuses[i]}");
        }

        int statusChoice;
        do
        {
            Console.Write("Select new status (1-5): ");
        } while (!int.TryParse(Console.ReadLine(), out statusChoice) || statusChoice < 1 || statusChoice > 5);
        
        string status = ValidStatuses[statusChoice - 1];

        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            string sql = "UPDATE Shows SET Status = @Status, LastUpdated = CURRENT_TIMESTAMP WHERE Id = @Id;";
            using (var command = new SqliteCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@Status", status);
                command.Parameters.AddWithValue("@Id", id);
                int affected = command.ExecuteNonQuery();
                if (affected == 0)
                    Console.WriteLine("❌ Show not found!");
                else
                    Console.WriteLine("✅ Status updated!");
            }
        }
        Console.ReadKey();
    }

    private static void RateShow()
    {
        Console.Clear();
        int id;
        do
        {
            Console.Write("Enter show ID: ");
        } while (!int.TryParse(Console.ReadLine(), out id));

        double rating = -1;
        while (rating < 0 || rating > 10)
        {
            Console.Write("Enter rating (0-10): ");
            double.TryParse(Console.ReadLine(), out rating);
        }

        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            string sql = "UPDATE Shows SET Rating = @Rating, LastUpdated = CURRENT_TIMESTAMP WHERE Id = @Id;";
            using (var command = new SqliteCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@Rating", rating);
                command.Parameters.AddWithValue("@Id", id);
                int affected = command.ExecuteNonQuery();
                if (affected == 0)
                    Console.WriteLine("❌ Show not found!");
                else
                    Console.WriteLine("✅ Rating updated!");
            }
        }
        Console.ReadKey();
    }

    private static void DeleteShow()
    {
        Console.Clear();
        int id;
        do
        {
            Console.Write("Enter show ID: ");
        } while (!int.TryParse(Console.ReadLine(), out id));

        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            string sql = "DELETE FROM Shows WHERE Id = @Id;";
            using (var command = new SqliteCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@Id", id);
                int affected = command.ExecuteNonQuery();
                if (affected == 0)
                    Console.WriteLine("❌ Show not found!");
                else
                    Console.WriteLine("🗑️ Show deleted.");
            }
        }
        Console.ReadKey();
    }

    private static void ExportToFile()
    {
        Console.Clear();
        Console.WriteLine("Select export format:");
        Console.WriteLine("1. Text File");
        Console.WriteLine("2. CSV File");
        Console.Write("Your choice: ");
        
        string? choice = Console.ReadLine();
        string fileName = $"shows_export_{DateTime.Now:yyyyMMddHHmmss}";

        if (choice == "1")
        {
            fileName += ".txt";
            ExportToTextFile(fileName);
        }
        else if (choice == "2")
        {
            fileName += ".csv";
            ExportToCSVFile(fileName);
        }
        else
        {
            Console.WriteLine("❌ Invalid option");
            Console.ReadKey();
            return;
        }

        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), fileName);
        Console.WriteLine($"✅ Shows exported to: {fullPath}");
        Console.ReadKey();
    }

    private static void ExportToTextFile(string fileName)
    {
        using (var writer = new StreamWriter(fileName))
        {
            writer.WriteLine("📋 Show Tracker Export");
            writer.WriteLine($"Generated on: {DateTime.Now}\n");
            
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();
                string sql = "SELECT * FROM Shows ORDER BY Title;";
                using (var command = new SqliteCommand(sql, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        writer.WriteLine(
                            $"Title: {reader["Title"]}\n" +
                            $"Genre: {reader["Genre"]}\n" +
                            $"Episodes Watched: {reader["EpisodesWatched"]}\n" +
                            $"Status: {reader["Status"]}\n" +
                            $"Rating: {GetRatingDisplay(reader["Rating"])}\n" +
                            $"Last Updated: {GetDateTimeDisplay(reader["LastUpdated"])}\n" +
                            new string('-', 40)
                        );
                    }
                }
            }
        }
    }

    private static void ExportToCSVFile(string fileName)
    {
        using (var writer = new StreamWriter(fileName))
        {
            writer.WriteLine("Title,Genre,EpisodesWatched,Status,Rating,LastUpdated");
            
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();
                string sql = "SELECT * FROM Shows;";
                using (var command = new SqliteCommand(sql, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        writer.WriteLine(
                            $"\"{reader["Title"]}\"," +
                            $"\"{reader["Genre"]}\"," +
                            $"{reader["EpisodesWatched"]}," +
                            $"\"{reader["Status"]}\"," +
                            $"{GetRatingDisplay(reader["Rating"])}," +
                            $"\"{GetDateTimeDisplay(reader["LastUpdated"])}\""
                        );
                    }
                }
            }
        }
    }
}
