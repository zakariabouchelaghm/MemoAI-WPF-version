using ElBruno.LocalEmbeddings;
using ElBruno.LocalEmbeddings.Options;
using Google.Protobuf;
using MemoAI.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MemoAI
{
    public class EmbeddingService : IDisposable
    {
        private static readonly LocalEmbeddingGenerator _generator = new();
        private SqliteConnection _persistentConnection;

        // 1. Constructeur STATIQUE : Pour l'IA (exécuté une seule fois pour l'app)
        static EmbeddingService()
        {
            try
            {
                // On récupère le chemin de ton dossier "model" dans le répertoire d'exécution
                string executionPath = AppDomain.CurrentDomain.BaseDirectory;
                string modelsPath = Path.Combine(executionPath, "model");

                // Sécurité : On vérifie que le fichier est bien là avant de lancer quoi que ce soit
                string onnxFile = Path.Combine(modelsPath, "model.onnx");

                if (!File.Exists(onnxFile))
                {
                    throw new FileNotFoundException($"ERREUR CRITIQUE : Le modèle spécifique est absent de {onnxFile}");
                }

                // Configuration des options
                var options = new LocalEmbeddingsOptions
                {
                    // C'est cette ligne qui force l'utilisation de ton dossier local
                    ModelPath = modelsPath,

                    // Si cette propriété existe dans ta version de la lib, elle empêche le download
                    // CaseDownloadIfMissing = false 
                };

                _generator = new LocalEmbeddingGenerator(options);

                // Log de confirmation pour le debug
                Console.WriteLine($"IA initialisée avec le modèle local : {onnxFile}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur d'initialisation IA : {ex.Message}");
                throw;
            }
        }

        // 2. Constructeur d'INSTANCE : Pour la base de données (Fix du NullReferenceException)
        public EmbeddingService()
        {
            // Initialisation des drivers SQLite
            SQLitePCL.Batteries_V2.Init();

            // Définition du chemin de la base de données
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MemoEmbed.sqlite");

            // INSTANCIATION : On crée enfin l'objet
            _persistentConnection = new SqliteConnection($"Data Source={dbPath}");
        }

       
        public async Task InitializeAsync()
        {
            if (_persistentConnection.State != System.Data.ConnectionState.Open)
            {
                await _persistentConnection.OpenAsync();
                _persistentConnection.EnableExtensions(true);
                _persistentConnection.LoadExtension("sqlite-vec","sqlite3_vec_init");

                await InitializeDatabase();
            }
        }


        public async Task InitializeDatabase()
        {


            using var command = _persistentConnection .CreateCommand();
                
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
            Console.WriteLine("Tables created successfully via C#!");
        }
            
            
        



        public async Task Deletefromdatabase()
        {
            
                using (var command = _persistentConnection.CreateCommand())
                {
                    // 4. Create your tables
                    command.CommandText = @"
                 DELETE FROM memos;

                 DELETE FROM embed;
            ";

                    await command.ExecuteNonQueryAsync();
                }
            
            Console.WriteLine("Tables");
        }

        public async Task SaveNoteWithEmbedding(string noteTitle,string noteText,string Date) {
            var result = await _generator.GenerateAsync(noteText);
            float[] Vector = result[0].Vector.ToArray();

           
            

            using var transaction = _persistentConnection.BeginTransaction();

            try
            {
                var cmdMemo = _persistentConnection.CreateCommand();
                cmdMemo.CommandText = "INSERT INTO memos (title,note,date) VALUES (@title,@note,@date); SELECT last_insert_rowid();";
                cmdMemo.Parameters.AddWithValue("@title", noteTitle);
                cmdMemo.Parameters.AddWithValue("@note", noteText);
                cmdMemo.Parameters.AddWithValue("@date", Date);
                long noteId = (long)await cmdMemo.ExecuteScalarAsync();

                var cmdEmbed = _persistentConnection.CreateCommand();
                cmdEmbed.CommandText = "INSERT INTO embed (id, vector, note_id) VALUES (@id, @vector, @noteId)";
                cmdEmbed.Parameters.AddWithValue("@id", noteId);
                cmdEmbed.Parameters.AddWithValue("@noteId", noteId);

                byte[] vectorBlob = new byte[Vector.Length * 4];
                Buffer.BlockCopy(Vector, 0, vectorBlob, 0, vectorBlob.Length);
                cmdEmbed.Parameters.AddWithValue("@vector", vectorBlob);

                await cmdEmbed.ExecuteNonQueryAsync();
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

        }

        public async Task<List<NoteItem>> SearchSimilarNotes(string queryText, int limit = 3)
        {
            
            var results = new List<NoteItem>();

            // 1. Generate the vector for the search query using your existing generator
            var result = await _generator.GenerateAsync(queryText).ConfigureAwait(false); ;
            float[] queryVector = result[0].Vector.ToArray();
            double distanceThreshold = 15;

            using var command = _persistentConnection.CreateCommand();
            command.CommandText = @"
                    SELECT 
                m.id, 
                m.title, 
                m.note, 
                m.date,
                e.distance  -- On récupère la distance pour pouvoir filtrer
            FROM embed e
            JOIN memos m ON e.note_id = m.id
            WHERE e.vector MATCH @queryVec 
              AND k = @limit
              AND e.distance < @threshold  -- Le filtre magique
            ORDER BY e.distance ASC";
            
            // Convert float array to BLOB (Same logic as your Save method)
            byte[] blob = new byte[queryVector.Length * 4];
            Buffer.BlockCopy(queryVector, 0, blob, 0, blob.Length);

            command.Parameters.AddWithValue("@queryVec", blob);
            command.Parameters.AddWithValue("@limit", limit);
            command.Parameters.AddWithValue("@threshold", distanceThreshold);
            using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                results.Add(new NoteItem()
                {
                    id = reader.GetInt32(0),
                    TitleText = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    NoteText = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Date = reader.IsDBNull(3) ? "" : reader.GetString(3)
                });
                
            }
            
            return results;
        }

        public async Task<List<NoteItem>> GetAllNotesAsync()
        {

            var results = new List<NoteItem>();
            using var command = _persistentConnection.CreateCommand();
            command.CommandText = @"
            SELECT m.id,m.title,m.note,m.date 
            FROM  memos m order by m.id desc";
            using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                results.Add(new NoteItem()
                {
                    id = reader.GetInt32(0),
                    TitleText = reader.IsDBNull(1) ? "" : reader.GetString(1), // Sécurité IsDBNull
                    NoteText = reader.IsDBNull(2) ? "" : reader.GetString(2),  // Sécurité IsDBNull
                    Date = reader.IsDBNull(3) ? "" : reader.GetString(3)
                });
            }

            return results;
        }

        public async Task DeleteNote(int id)
        {

            using var transaction = _persistentConnection.BeginTransaction();
            try
            {
                using var command = _persistentConnection.CreateCommand();

                // 1. Supprimer le vecteur dans la table 'embed'
                command.CommandText = "DELETE FROM embed WHERE note_id = @id";
                command.Parameters.AddWithValue("@id", id);
                await command.ExecuteNonQueryAsync();

                // 2. Supprimer la note dans la table 'memos'
                command.Parameters.Clear();
                command.CommandText = "DELETE FROM memos WHERE id = @id";
                command.Parameters.AddWithValue("@id", id);
                await command.ExecuteNonQueryAsync();

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task UpdateNoteContent(NoteItem note)
        {

            var result = await _generator.GenerateAsync(note.NoteText).ConfigureAwait(false); ;
            using var transaction = _persistentConnection.BeginTransaction();
            try
            {


                using var cmdMemo = _persistentConnection.CreateCommand();
                cmdMemo.CommandText = @"
            UPDATE memos SET title=@title, note=@note WHERE id=@id";
                cmdMemo.Parameters.AddWithValue("@title", note.TitleText ?? "");
                cmdMemo.Parameters.AddWithValue("@note", note.NoteText ?? "");
                cmdMemo.Parameters.AddWithValue("@id", note.id);
                await cmdMemo.ExecuteNonQueryAsync();

                using var cmdEmbed = _persistentConnection.CreateCommand();
                float[] Vector = result[0].Vector.ToArray();
                byte[] vectorBlob = new byte[Vector.Length * 4];
                Buffer.BlockCopy(Vector, 0, vectorBlob, 0, vectorBlob.Length);
                cmdEmbed.CommandText = @"
            UPDATE embed SET vector=@vector WHERE note_id=@id";

                cmdEmbed.Parameters.AddWithValue("@vector", vectorBlob);
                cmdEmbed.Parameters.AddWithValue("@id", note.id);
                await cmdEmbed.ExecuteNonQueryAsync();
                transaction.Commit();

            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public void Dispose()
        {
            _persistentConnection?.Dispose();
        }
    }

}
