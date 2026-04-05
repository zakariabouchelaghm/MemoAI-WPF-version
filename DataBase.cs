using ElBruno.LocalEmbeddings;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;
using System.Windows;

namespace MemoAI
{
    internal class DataBase
    {
        public async Task InitializeDatabase()
        {
            string connectionString = "Data Source=MemoEmbed.sqlite";
            using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();
                connection.EnableExtensions(true);
                connection.LoadExtension("sqlite-vec", "sqlite3_vec_init");
                using (var command = connection.CreateCommand())
                {
                    // 4. Create your tables
                    command.CommandText = @"
                CREATE TABLE IF NOT EXISTS memos (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    title TEXT,
                    note TEXT,
                    date TEXT
                );

                CREATE VIRTUAL TABLE IF NOT EXISTS embed USING vec0(
                    id INTEGER PRIMARY KEY,
                    vector FLOAT[384],
                    note_id INTEGER
                );
            ";

                    await command.ExecuteNonQueryAsync();
                }
            }
            Console.WriteLine("Tables created successfully via C#!");
        }



            public async Task Deletefromdatabase()
        {
            string connectionString = "Data Source=MemoEmbed.sqlite";
            using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();
                connection.EnableExtensions(true);
                connection.LoadExtension("sqlite-vec", "sqlite3_vec_init");
                using (var command = connection.CreateCommand())
                {
                    // 4. Create your tables
                    command.CommandText = @"
                 DELETE FROM memos;

                 DELETE FROM embed;
            ";

                    await command.ExecuteNonQueryAsync();
                }
            }
            Console.WriteLine("Tables");
        }

        public async Task CheckEmbeddingsCount()
        {
            string connectionString = "Data Source=smartmemo.db";
            using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();
                connection.EnableExtensions(true);
                connection.LoadExtension("sqlite-vec", "sqlite3_vec_init");

                using (var command = connection.CreateCommand())
                {
                    // We check the count and the byte-length of the vector BLOB
                    command.CommandText = "SELECT count(*), length(vector) FROM embed;";

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            long count = reader.GetInt64(0);
                            // If count is 0, the second column might be null, so we check carefully
                            object rawLength = reader.GetValue(1);

                            string message = $"Database Check:\n- Total Embeddings: {count}";
                            if (count > 0)
                            {
                                message += $"\n- Vector Size: {rawLength} bytes (Expected: 1536)";
                            }

                            // Using MessageBox so you can see it easily in WPF
                            MessageBox.Show(message, "Database Sync Check");
                        }
                    }
                }
            }
        }

        
    }

    }

