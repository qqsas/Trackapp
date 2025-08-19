using System;
using Microsoft.Data.Sqlite;
using System.IO;
using System.Collections.Generic;
using System.Linq;

class Program
{
    private static string DatabaseFile = "";
    private static string ConnectionString = "";

    private static readonly List<string> ValidStatuses = new List<string>
    {
        "Watching", "Completed", "On Hold", "Dropped", "Plan to Watch"
    };

    // ConfigFile now stored in user's AppData folder
    private static readonly string AppDataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ShowTracker"
    );
    private static readonly string ConfigFile = Path.Combine(AppDataDir, "dbconfig.txt");

    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        SetupDatabasePath();
        InitializeDatabase();

        while (true)
        {
            Console.Clear();
            ShowQuickSummary();
            Console.WriteLine("\n📺 Show Tracker");
            Console.WriteLine("1. Add Show");
            Console.WriteLine("2. View Shows");
            Console.WriteLine("3. Update Progress (Season/Episode)");
            Console.WriteLine("4. Update Status");
            Console.WriteLine("5. Rate a Show");
            Console.WriteLine("6. Delete a Show");
            Console.WriteLine("7. Export to File");
            Console.WriteLine("8. Change Database Directory");
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
                case "8": ChangeDatabaseDirectory(); break;
                case "0": return;
                default:
                    Console.WriteLine("❌ Invalid option, try again.");
                    Console.ReadKey(); break;
            }
        }
    }

    private static void SetupDatabasePath()
    {
        if (!Directory.Exists(AppDataDir))
            Directory.CreateDirectory(AppDataDir);

        if (File.Exists(ConfigFile))
        {
            DatabaseFile = File.ReadAllText(ConfigFile).Trim();
        }
        else
        {
            Console.WriteLine("📂 No database path set.");
            Console.WriteLine($"By default, the database will be stored here:\n{AppDataDir}");
            Console.Write("Press Enter to accept or type a custom directory: ");
            string? inputDir = Console.ReadLine();

            string finalDir = string.IsNullOrWhiteSpace(inputDir) || !Directory.Exists(inputDir)
                ? AppDataDir
                : inputDir;

            DatabaseFile = Path.Combine(finalDir, "shows.db");

            File.WriteAllText(ConfigFile, DatabaseFile);

            Console.WriteLine($"✅ Database will be stored at: {DatabaseFile}");
            Console.ReadKey();
        }

        BuildConnectionString();
    }

    private static void BuildConnectionString()
    {
        ConnectionString = new SqliteConnectionStringBuilder
        {
            DataSource = DatabaseFile,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();
    }

    private static void ChangeDatabaseDirectory()
    {
        Console.Clear();
        Console.WriteLine("📂 Change Database Directory");
        Console.Write("Enter new directory path: ");
        string? dir = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
        {
            Console.WriteLine("⚠ Invalid directory. No changes made.");
            Console.ReadKey();
            return;
        }

        string newDbFile = Path.Combine(dir, "shows.db");

        try
        {
            if (File.Exists(DatabaseFile))
                File.Copy(DatabaseFile, newDbFile, true);

            DatabaseFile = newDbFile;
            File.WriteAllText(ConfigFile, DatabaseFile);

            BuildConnectionString();
            InitializeDatabase();

            Console.WriteLine($"✅ Database directory changed successfully!\nNew Path: {DatabaseFile}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error changing directory: {ex.Message}");
        }

        Console.ReadKey();
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
                SeasonNumber INTEGER DEFAULT 0,
                EpisodeNumber INTEGER DEFAULT 0,
                Status TEXT,
                Rating REAL,
                LastUpdated DATETIME
            );";

            using (var command = new SqliteCommand(createTableSql, connection))
                command.ExecuteNonQuery();

            EnsureColumnExists(connection, "SeasonNumber", "INTEGER DEFAULT 0");
            EnsureColumnExists(connection, "EpisodeNumber", "INTEGER DEFAULT 0");
            EnsureColumnExists(connection, "LastUpdated", "DATETIME");
        }
    }

    private static void EnsureColumnExists(SqliteConnection connection, string columnName, string columnType)
    {
        try
        {
            string checkCol = $"SELECT {columnName} FROM Shows LIMIT 1;";
            using (var testCommand = new SqliteCommand(checkCol, connection))
                testCommand.ExecuteScalar();
        }
        catch
        {
            string addCol = $"ALTER TABLE Shows ADD COLUMN {columnName} {columnType};";
            using (var addCommand = new SqliteCommand(addCol, connection))
                addCommand.ExecuteNonQuery();

            if (columnName == "LastUpdated")
            {
                string updateSql = "UPDATE Shows SET LastUpdated = datetime('now');";
                using (var updateCommand = new SqliteCommand(updateSql, connection))
                    updateCommand.ExecuteNonQuery();
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
            Console.WriteLine($"{i + 1}. {ValidStatuses[i]}");
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
            INSERT INTO Shows (Title, Genre, Status, LastUpdated)
            VALUES (@Title, @Genre, @Status, datetime('now'));";

            using (var command = new SqliteCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@Title", title);
                command.Parameters.AddWithValue("@Genre", genre);
                command.Parameters.AddWithValue("@Status", status);
                command.ExecuteNonQuery();
            }
        }

        Console.WriteLine("\n✅ Show added (Press Enter to Continue)!");
        Console.ReadKey();
    }

    // ⚠ Remaining functions unchanged (ViewShowsMenu, ViewShows, UpdateEpisodes, UpdateStatus, RateShow, DeleteShow, ExportToFile, ExportToTextFile, ExportToCSVFile) 
    // They already use ConnectionString dynamically.
    
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
                case "1":
                    ViewShows("");
                    Console.WriteLine("\nPress any key to continue...");
                    Console.ReadKey();
                    break;
                case "2": ViewByStatus(); break;
                case "3": ViewShows("ORDER BY Rating DESC"); break;
                case "4": ViewShows("ORDER BY LastUpdated DESC LIMIT 10"); break;
                case "5": ViewByGenre(); break;
                case "0": return;
                default:
                    Console.WriteLine("❌ Invalid option, try again.");
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
                        $"- Progress: S{reader["SeasonNumber"]}E{reader["EpisodeNumber"]} " +
                        $"- Status: {reader["Status"]} " +
                        $"- Rating: {GetRatingDisplay(reader["Rating"])} " +
                        $"- Last Updated: {GetDateTimeDisplay(reader["LastUpdated"])}"
                    );
                }
            }
        }
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
            Console.WriteLine($"{i + 1}. {ValidStatuses[i]}");
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
        ViewShows("");
        int id;
        do
        {
            Console.Write("Enter show ID: ");
        } while (!int.TryParse(Console.ReadLine(), out id));

        int season;
        do
        {
            Console.Write("Enter season number: ");
        } while (!int.TryParse(Console.ReadLine(), out season));

        int episode;
        do
        {
            Console.Write("Enter episode number: ");
        } while (!int.TryParse(Console.ReadLine(), out episode));

        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            string sql = @"UPDATE Shows 
                           SET SeasonNumber = @Season, EpisodeNumber = @Episode, LastUpdated = datetime('now') 
                           WHERE Id = @Id;";
            using (var command = new SqliteCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@Season", season);
                command.Parameters.AddWithValue("@Episode", episode);
                command.Parameters.AddWithValue("@Id", id);
                int affected = command.ExecuteNonQuery();
                if (affected == 0)
                    Console.WriteLine("❌ Show not found!");
                else
                    Console.WriteLine("✅ Progress updated!");
            }
        }
        Console.ReadKey();
    }

    private static void UpdateStatus()
    {
        Console.Clear();
        ViewShows("");
        int id;
        do
        {
            Console.Write("Enter show ID: ");
        } while (!int.TryParse(Console.ReadLine(), out id));

        Console.WriteLine("\nAvailable statuses:");
        for (int i = 0; i < ValidStatuses.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {ValidStatuses[i]}");
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
            string sql = "UPDATE Shows SET Status = @Status, LastUpdated = datetime('now') WHERE Id = @Id;";
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
        ViewShows("");
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
            string sql = "UPDATE Shows SET Rating = @Rating, LastUpdated = datetime('now') WHERE Id = @Id;";
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
        ViewShows("");
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
                            $"Progress: S{reader["SeasonNumber"]}E{reader["EpisodeNumber"]}\n" +
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
            writer.WriteLine("Title,Genre,SeasonNumber,EpisodeNumber,Status,Rating,LastUpdated");

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
                            $"{reader["SeasonNumber"]}," +
                            $"{reader["EpisodeNumber"]}," +
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

