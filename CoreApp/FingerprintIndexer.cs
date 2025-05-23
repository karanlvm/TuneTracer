using System;
using System.IO;

namespace CoreApp
{
    public static class FingerprintIndexer
    {
        public static void BuildIndex(string songsFolderPath)
        {
            using var db = new Database("fingerprints.db");

            foreach (var file in Directory.GetFiles(songsFolderPath, "*.wav"))
            {
                string songId = Path.GetFileNameWithoutExtension(file);
                Console.WriteLine($"Indexing {songId}...");

                var wav = WavFile.Load(file);
                // Build & report spectrogram
                var spec = new Spectrogram(wav);
                Console.WriteLine($"  Spectrogram: {spec.NumFrames} frames x {spec.NumBins} bins");
                var fingerprints = Fingerprinter.Extract(wav, songId);

                db.BulkInsert(fingerprints);
            }

            Console.WriteLine("Index build complete.");
        }
    }
}
