using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

class Program
{
    static void Main(string[] args)
    {

        Console.Write("Please ensure that you are connected to the siHealth VPN and that you have established a connection to the intended database\n\n");
        Console.Write("Please provide port for the database you intend to connect to: ");
        int port = Convert.ToInt32(Console.ReadLine());
        Console.Write("Please provide the password for the database: ");
        string password = Console.ReadLine();
        Console.Write("Please provide the database schema name e.g.g expodose_gw_sihp: ");
        string schemaName = Console.ReadLine();
        MySql.Data.MySqlClient.MySqlConnection myConnection;
        //set the correct values for your server, user, password and database name
        string connectionString = $"server=127.0.0.1;port={port};uid=admin;pwd={password};database={schemaName}";
        Dictionary<int, (string uid, string name)> practices = new Dictionary<int, (string, string)>();

        try
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                // Fetch practices
                string query = "SELECT uid, name FROM practices";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    int index = 1;
                    while (reader.Read())
                    {
                        string uid = reader["uid"].ToString();
                        string name = reader["name"].ToString();
                        practices.Add(index, (uid, name));
                        Console.WriteLine($"{index}. {name}");
                        index++;
                    }
                }

                if (practices.Count == 0)
                {
                    Console.WriteLine("No practices found.");
                    return;
                }

                // User selection
                Console.Write("\nEnter the number corresponding to your choice: ");
                if (int.TryParse(Console.ReadLine(), out int choice) && practices.ContainsKey(choice))
                {
                    string selectedUid = practices[choice].uid;
                    string selectedName = practices[choice].name;

                    Console.WriteLine($"\nYou selected: {selectedName}");
                    Console.WriteLine($"UID: {selectedUid}");

                    // Ask user whether to activate or deactivate
                    Console.Write("\nWould you like to deactivate (0) or activate (1) the practice? Enter 0 or 1: ");
                    if (int.TryParse(Console.ReadLine(), out int activeStatus) && (activeStatus == 0 || activeStatus == 1))
                    {
                        string action = activeStatus == 0 ? "deactivate" : "activate";
                        Console.WriteLine($"\nYou chose to {action} the practice.");

                        // Run Update SQL Statements
                        ExecuteUpdateStatements(conn, selectedUid, activeStatus);
                    }
                    else
                    {
                        Console.WriteLine("Invalid input. Please enter 0 or 1.");
                    }
                }
                else
                {
                    Console.WriteLine("Invalid selection. Please try again.");
                }
            }
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($"Database error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
        }
    }

    static void ExecuteUpdateStatements(MySqlConnection conn, string practiceUid, int activeStatus)
    {
        try
        {
            // Statement 1: Update Users from user_practice
            string query1 = @"
                UPDATE users
                SET active = @activeStatus
                WHERE users.uid IN (
                    SELECT user_practice.user_uid
                    FROM user_practice
                    WHERE user_practice.practice_uid = @practiceUid
                )";

            using (MySqlCommand cmd1 = new MySqlCommand(query1, conn))
            {
                cmd1.Parameters.AddWithValue("@practiceUid", practiceUid);
                cmd1.Parameters.AddWithValue("@activeStatus", activeStatus);
                int rowsAffected1 = cmd1.ExecuteNonQuery();
                Console.WriteLine($"1. Updated {rowsAffected1} users linked via user_practice.");
            }

            // Statement 2: Update Users from staff_practice
            string query2 = @"
                UPDATE users
                SET active = @activeStatus
                WHERE users.uid IN (
                    SELECT staff_practice.user_uid
                    FROM staff_practice
                    WHERE staff_practice.practice_uid = @practiceUid
                )";

            using (MySqlCommand cmd2 = new MySqlCommand(query2, conn))
            {
                cmd2.Parameters.AddWithValue("@practiceUid", practiceUid);
                cmd2.Parameters.AddWithValue("@activeStatus", activeStatus);
                int rowsAffected2 = cmd2.ExecuteNonQuery();
                Console.WriteLine($"2. Updated {rowsAffected2} users linked via staff_practice.");
            }

            // Statement 3: Update Practice
            string query3 = @"
                UPDATE practices
                SET active = @activeStatus
                WHERE practices.uid = @practiceUid";

            using (MySqlCommand cmd3 = new MySqlCommand(query3, conn))
            {
                cmd3.Parameters.AddWithValue("@practiceUid", practiceUid);
                cmd3.Parameters.AddWithValue("@activeStatus", activeStatus);
                int rowsAffected3 = cmd3.ExecuteNonQuery();
                Console.WriteLine($"3. Updated the practice's active status.");
            }
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($"SQL Execution Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected Error during SQL execution: {ex.Message}");
        }
    }
}
