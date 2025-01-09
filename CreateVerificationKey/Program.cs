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
            var port = 3306;
            var password = "password";
            var schemaName = "schemaName";
            var accessCode = "str";
            Console.Write("Please provide port for the database you intend to connect to: ");
            port = Convert.ToInt32(Console.ReadLine());
            Console.Write("Please provide the password for the database: ");
            password = Console.ReadLine();
            Console.Write("Please provide the database schema name e.g.g expodose_gw_sihp: ");
            schemaName = Console.ReadLine();
            MySql.Data.MySqlClient.MySqlConnection myConnection;
            //set the correct values for your server, user, password and database name
            var myConnectionString = $"server=127.0.0.1;port={port};uid=admin;pwd={password};database={schemaName}";
            Console.Write("Please provide the access code you wish to use for the s4h app e.g. S4H2024: ");
            accessCode = Console.ReadLine();
            try
            {
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(accessCode));
                    string hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

                    // Get the first 32 characters of the hash
                    string shortHash = hashString.Substring(0, 32);
                    string keyId = shortHash.Substring(0, 8);
                    string keyInitialValue = shortHash.Substring(8, 24);

                    Console.WriteLine($"Full SHA256 Hash: {hashString}");
                    Console.WriteLine($"First 32 Characters: {shortHash}");

                    string hash = GenerateHash(keyInitialValue);

                    Console.WriteLine($"Hash: {hash}");

                    string dbUid = GenerateRandomString(8);

                    myConnection = new MySql.Data.MySqlClient.MySqlConnection(myConnectionString);
                    //open a connection
                    myConnection.Open();
                    // create a MySQL command and set the SQL statement with parameters
                    MySqlCommand myCommand = new MySqlCommand();
                    myCommand.Connection = myConnection;
                    myCommand.CommandText = @"INSERT INTO `access_keys` (uid, key_id, key_value, key_type, active) VALUES (@uid, @keyId, @keyValue, 'Verification', 1);";
                    myCommand.Parameters.AddWithValue("@uid", dbUid);
                    myCommand.Parameters.AddWithValue("@keyId", keyId);
                    myCommand.Parameters.AddWithValue("@keyValue", hash);
                    // execute the command and read the results
                    myCommand.ExecuteReader();
                    myConnection.Close();

                    Console.WriteLine("Please make note of the following verification token uid. Use this when creating your practice with the admin endpoint.");
                    Console.WriteLine(dbUid);
                }
            }
            catch (MySql.Data.MySqlClient.MySqlException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static string GenerateRandomString(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz";
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