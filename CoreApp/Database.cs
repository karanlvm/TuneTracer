using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace CoreApp
{
    public class Database : IDisposable
    {
        private readonly SqliteConnection _conn;

        public Database(string dbPath = "fingerprints.db")
        {
            _conn = new SqliteConnection($"Data Source={dbPath}");
            _conn.Open();
            InitSchema();
        }

        private void InitSchema()
        {
            var cmd = _conn.CreateCommand();
            cmd.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS Fingerprints (
                Code   INTEGER NOT NULL,
                SongId TEXT    NOT NULL,
                Offset INTEGER NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_code ON Fingerprints(Code);
            ";
            cmd.ExecuteNonQuery();
        }

        public void BulkInsert(IEnumerable<(ulong code, string songId, int offset)> items)
        {
            using var tx = _conn.BeginTransaction();
            var cmd = _conn.CreateCommand();
            cmd.CommandText = "INSERT INTO Fingerprints(Code, SongId, Offset) VALUES($c, $s, $o)";
            var pC = cmd.Parameters.Add("$c", SqliteType.Integer);
            var pS = cmd.Parameters.Add("$s", SqliteType.Text);
            var pO = cmd.Parameters.Add("$o", SqliteType.Integer);

            foreach (var (code, songId, offset) in items)
            {
                pC.Value = (long)code;
                pS.Value = songId;
                pO.Value = offset;
                cmd.ExecuteNonQuery();
            }

            tx.Commit();
        }

        public List<(string songId, int offset)> Query(ulong code)
        {
            var results = new List<(string, int)>();
            var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT SongId, Offset FROM Fingerprints WHERE Code = $c";
            cmd.Parameters.AddWithValue("$c", (long)code);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                results.Add((reader.GetString(0), reader.GetInt32(1)));

            return results;
        }

        public void Dispose() => _conn?.Dispose();
    }
}
