using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Data.Sqlite;



namespace Doot
{
    class DatabaseManager
    {
        readonly SqliteConnection connection;

        public DatabaseManager()
        {
            if (!File.Exists("doot.db"))
            {
                Logger.Log(LogCategory.Information, "Database not found. Creating...");
                CreateDatabase();
            }

            connection = new SqliteConnection("Data Source=doot.db");
            connection.Open();
        }

        static void CreateDatabase()
        {
            var tmpConnection = new SqliteConnection("Data Source=doot.db");
            tmpConnection.Open();

            var cmd = tmpConnection.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE account (
                    id INTEGER PRIMARY KEY,
                    email TEXT NOT NULL,
                    password TEXT NOT NULL
                );";
            cmd.ExecuteNonQuery();

            tmpConnection.Close();
        }

        public long LogIn(string email, string password)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT id, password
                FROM account
                WHERE email = $email;";
            cmd.Parameters.AddWithValue("email", email);

            using var reader = cmd.ExecuteReader();

            if (!reader.HasRows)
                return 0;

            reader.Read();

            var storedPassword = (string)reader["password"];

            if (password != storedPassword)
                return -1;

            var id = (long)reader["id"];

            return id;
        }

        public long CreateAccount(string email, string password)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT id
                FROM account
                WHERE email = $email;";
            var reader = cmd.ExecuteReader();

            if (reader.HasRows)
                return 0;

            reader.Dispose();

            cmd.CommandText = @"
                INSERT INTO account (email, password)
                VALUES( $email, $password );";
            cmd.Parameters.AddWithValue("email", email);
            cmd.Parameters.AddWithValue("password", password);

            cmd.ExecuteNonQuery();

            cmd = connection.CreateCommand();
            // TODO: Database queries should be queued and run on a single thread so as not to execute mulitple inserts simultaneously.
            //   The calling code should await the result.
            cmd.CommandText = @"
                SELECT last_insert_rowid();";

            reader = cmd.ExecuteReader();
            reader.Read();
            var id = (long)reader[0];
            reader.Dispose();

            return id;
        }
    }
}
