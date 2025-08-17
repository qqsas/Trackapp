using System;
using Microsoft.Data.Sqlite;
using System.IO;

class Program
{
    private const string DatabaseFile = "shows.db";
    private static readonly string ConnectionString = new SqliteConnectionStringBuilder
    {
        DataSource = DatabaseFile,
        Mode = SqliteOpenMode.ReadWriteCreate // Automatically creates DB if missing
    }.ToString();

    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // Initialize database
        InitializeDatabase();

        // Main loop
        while (true)
        {
            Console.WriteLine("\n📺 Show Tracker");
            Console.WriteLine("1. Add Show");
            Console.WriteLine("2. View All Shows");
            Console.WriteLine("3. Update Episodes Watched");
            Console.WriteLine("4. Rate a Show");
            Console.WriteLine("5. Delete a Show");
            Console.WriteLine("0. Exit");
            Console.Write("Select an option: ");
            
            string? choice = Console.ReadLine();

            switch (choice)
            {
                case "1": AddShow(); break;
                case "2": ViewShows(); break;
                case "3": UpdateEpisodes(); break;
                case "4": RateShow(); break;
                case "5": DeleteShow(); break;
                case "0": return;
                default: Console.WriteLine("❌ Invalid option, try again."); break;
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
                TotalEpisodes INTEGER DEFAULT 0,
                Status TEXT,
                Rating REAL
            );";

            using (var command = new SqliteCommand(createTableSql, connection))
            {
                command.ExecuteNonQuery();
            }
        }
    }

    private static void AddShow()
    {
        Console.Write("Enter show title: ");
        string? title = Console.ReadLine();

        Console.Write("Enter genre: ");
        string? genre = Console.ReadLine();

        Console.Write("Enter total episodes: ");
        int totalEpisodes = int.TryParse(Console.ReadLine(), out var t) ? t : 0;

        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            string Sql = @"
            INSERT INTO Shows (Title, Genre, TotalEpisodes, Status)
            VALUES (@Title, @Genre, @TotalEpisodes, 'Watching');";

            using (var command = new SqliteCommand(Sql, connection))
            {
                command.Parameters.AddWithValue("@Title", title);
                command.Parameters.AddWithValue("@Genre", genre);
                command.Parameters.AddWithValue("@TotalEpisodes", totalEpisodes);
                command.ExecuteNonQuery();
            }
        }

        Console.WriteLine("✅ Show added!");
    }

    private static void ViewShows()
    {
        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            string Sql = "SELECT * FROM Shows;";
            using (var command = new SqliteCommand(Sql, connection))
            using (var reader = command.ExecuteReader())
            {
                Console.WriteLine("\n📋 Your Shows:");
                while (reader.Read())
                {
                    Console.WriteLine(
                        $"[{reader["Id"]}] {reader["Title"]} ({reader["Genre"]}) " +
                        $"- {reader["EpisodesWatched"]}/{reader["TotalEpisodes"]} eps " +
                        $"- Status: {reader["Status"]} " +
                        $"- Rating: {reader["Rating"]}"
                    );
                }
            }
        }
    }

    private static void UpdateEpisodes()
    {
        Console.Write("Enter show ID: ");
        int id = int.Parse(Console.ReadLine() ?? "0");

        Console.Write("Enter new episodes watched: ");
        int episodes = int.Parse(Console.ReadLine() ?? "0");

        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            string Sql = "UPDATE Shows SET EpisodesWatched = @Episodes WHERE Id = @Id;";
            using (var command = new SqliteCommand(Sql, connection))
            {
                command.Parameters.AddWithValue("@Episodes", episodes);
                command.Parameters.AddWithValue("@Id", id);
                command.ExecuteNonQuery();
            }
        }

        Console.WriteLine("✅ Episodes updated!");
    }

    private static void RateShow()
    {
        Console.Write("Enter show ID: ");
        int id = int.Parse(Console.ReadLine() ?? "0");

        Console.Write("Enter rating (0–10): ");
        double rating = double.Parse(Console.ReadLine() ?? "0");

        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            string Sql = "UPDATE Shows SET Rating = @Rating WHERE Id = @Id;";
            using (var command = new SqliteCommand(Sql, connection))
            {
                command.Parameters.AddWithValue("@Rating", rating);
                command.Parameters.AddWithValue("@Id", id);
                command.ExecuteNonQuery();
            }
        }

        Console.WriteLine("✅ Rating updated!");
    }

    private static void DeleteShow()
    {
        Console.Write("Enter show ID: ");
        int id = int.Parse(Console.ReadLine() ?? "0");

        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            string Sql = "DELETE FROM Shows WHERE Id = @Id;";
            using (var command = new SqliteCommand(Sql, connection))
            {
                command.Parameters.AddWithValue("@Id", id);
                command.ExecuteNonQuery();
            }
        }

        Console.WriteLine("🗑️ Show deleted.");
    }
}

