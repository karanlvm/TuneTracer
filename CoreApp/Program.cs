using System;
using System.IO;

namespace CoreApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var baseDir    = Directory.GetCurrentDirectory();
            var songsFolder= Path.Combine(baseDir, "..", "songsDatabase");
            var dbPath     = Path.Combine(baseDir, "fingerprints.db");

            if (args.Length == 1 && args[0] == "index")
            {
                FingerprintIndexer.BuildIndex(songsFolder);
                return;
            }

            if (args.Length == 2 && args[0] == "query")
            {
                var queryFile = args[1];
                if (!File.Exists(queryFile))
                {
                    Console.WriteLine($"❌ Query file not found: {queryFile}");
                    return;
                }

                // Load DB
                using var db = new Database(dbPath);

                // Load query WAV
                var queryWav = WavFile.Load(queryFile);
                Console.WriteLine($"Loaded query: {Path.GetFileName(queryFile)}");

                // Match
                var (songId, delta, votes) = Matcher.Match(queryWav, db);
                Console.WriteLine($"\n▶ Best match: {songId}");
                Console.WriteLine($"   Time offset (frames): {delta}");
                Console.WriteLine($"   Votes: {votes}");
                return;
            }

            Console.WriteLine("Usage:");
            Console.WriteLine("  dotnet run index                  → build fingerprint DB");
            Console.WriteLine("  dotnet run query <snippet.wav>    → identify snippet");
        }
    }
}
