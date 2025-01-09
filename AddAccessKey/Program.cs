using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using MySql;
using MySql.Data.MySqlClient;
using BCrypt.Net;

namespace Compiler
{
    class Program
    {

        static void Main()
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
            var myConnectionString = $"server=127.0.0.1;port={port};uid=admin;pwd={password};database={schemaName}";
            Console.Write("Please provide the access key type you wish to create (App/Admin/Service/Verification/Monitor): ");
            string accessKeyType = Console.ReadLine();
            try
            {
                using (SHA256 sha256 = SHA256.Create())
                {
                    string dbUid = GenerateRandomString(8, true);
                    string accessKeyFull = GenerateRandomString(32, false);

                    string keyId = accessKeyFull.Substring(0, 8);
                    string keyInitialValue = accessKeyFull.Substring(8, 24);

                    string hash = GenerateHash(keyInitialValue);

                    Console.WriteLine($"Hash: {hash}");

                    myConnection = new MySql.Data.MySqlClient.MySqlConnection(myConnectionString);
                    //open a connection
                    myConnection.Open();
                    // create a MySQL command and set the SQL statement with parameters
                    MySqlCommand myCommand = new MySqlCommand();
                    myCommand.Connection = myConnection;
                    myCommand.CommandText = @"INSERT INTO `access_keys` (uid, key_id, key_value, key_type, active) VALUES (@uid, @keyId, @keyValue, @keyType, 1);";
                    myCommand.Parameters.AddWithValue("@uid", dbUid);
                    myCommand.Parameters.AddWithValue("@keyId", keyId);
                    myCommand.Parameters.AddWithValue("@keyType", accessKeyType);
                    myCommand.Parameters.AddWithValue("@keyValue", hash);
                    // execute the command and read the results
                    myCommand.ExecuteReader();
                    myConnection.Close();

                    Console.WriteLine("Please make note of the following access key. there will be no reminders.");
                    Console.WriteLine(accessKeyFull);
                }
            }
            catch (MySql.Data.MySqlClient.MySqlException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static string GenerateRandomString(int length, bool simple)
        {
            string chars = "randomletters";
            if (simple == true)
            {
                chars = "abcdefghijklmnopqrstuvwxyz";
            }
            else
            {
                chars = "abcdefghijklmnopqrstuvwxyz1234567890";
            }
            Random random = new Random();
            char[] result = new char[length];

            for (int i = 0; i < length; i++)
            {
                result[i] = chars[random.Next(chars.Length)];
            }

            return new string(result);
        }

        static string GenerateHash(string secret)
        {
            // Manually generate a salt with $2b$ and 12 rounds
            string salt = BCrypt.Net.BCrypt.GenerateSalt(12);
            salt = salt.Replace("$2a$", "$2b$"); // Ensure the prefix is $2b$
            return BCrypt.Net.BCrypt.HashPassword(secret, salt);
        }
    }
}