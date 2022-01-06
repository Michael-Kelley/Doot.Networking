using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Data.Sqlite;



namespace Doot
{
    class DatabaseManager
    {
        const int CONNECTION_POOL_SIZE = 10;

        readonly ConcurrentBag<SqliteConnection> connectionsStore;
        readonly BlockingCollection<SqliteConnection> connections;

        public DatabaseManager()
        {
            if (!File.Exists("doot.db"))
            {
                Logger.Log(LogCategory.Info, "Database not found. Creating...");
                CreateDatabase();
            }

            connectionsStore = new ConcurrentBag<SqliteConnection>();
            connections = new BlockingCollection<SqliteConnection>(connectionsStore, CONNECTION_POOL_SIZE);

            for (int i = 0; i < CONNECTION_POOL_SIZE; i++)
                connections.Add(new SqliteConnection("Data Source=doot.db"));
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
            var connection = BorrowConnection();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT id, password
                FROM account
                WHERE email = $email;";
            cmd.Parameters.AddWithValue("email", email);

            using var reader = cmd.ExecuteReader();

            if (!reader.HasRows)
            {
                ReturnConnection(connection);
                return 0;
            }

            reader.Read();

            var storedPassword = (string)reader["password"];

            if (password != storedPassword)
            {
                ReturnConnection(connection);
                return -1;
            }

            var id = (long)reader["id"];

            ReturnConnection(connection);

            return id;
        }

        public long CreateAccount(string email, string password)
        {
            var connection = BorrowConnection();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT id
                FROM account
                WHERE email = $email;";
            cmd.Parameters.AddWithValue("email", email);

            var reader = cmd.ExecuteReader();

            if (reader.HasRows)
            {
                ReturnConnection(connection);
                return 0;
            }

            reader.Dispose();

            cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO account (email, password)
                VALUES( $email, $password );";
            cmd.Parameters.AddWithValue("email", email);
            cmd.Parameters.AddWithValue("password", password);

            cmd.ExecuteNonQuery();

            cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT last_insert_rowid();";

            reader = cmd.ExecuteReader();
            reader.Read();
            var id = (long)reader[0];
            reader.Dispose();

            ReturnConnection(connection);

            return id;
        }

        SqliteConnection BorrowConnection()
        {
            var connection = connections.Take();
            connection.Open();

            return connection;
        }

        void ReturnConnection(SqliteConnection connection)
        {
            connection.Close();
            connections.Add(connection);
        }
    }
}
