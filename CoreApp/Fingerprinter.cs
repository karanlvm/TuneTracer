// Fingerprinter.cs
using System;
using System.Collections.Generic;
using System.Linq;

namespace CoreApp
{
    public static class Fingerprinter
    {
        private const int MaxDeltaFrames = 50;
        private const int MaxDeltaBins   = 50;
        private const int KNeighbors     = 5;

        private const int TimeBins   = 16;
        private const int FreqBins   = 32;
        private const int WeightBins = 8;

        private const ulong FnvOffset = 0xcbf29ce484222325;
        private const ulong FnvPrime  = 0x00000100000001B3;

        public static List<(ulong code, string songId, int offset)> Extract(
            WavFile wav, string songId)
        {
            var spec   = new Spectrogram(wav, windowSize:2048, hopSize:1024);
            var chroma = new Chroma(spec);

            var harm = ExtractFromChannel(spec.Harmonic, chroma, songId);
            var perc = ExtractFromChannel(spec.Percussive, chroma, songId);

            var all = new List<(ulong,string,int)>(harm.Count + perc.Count);
            all.AddRange(harm);
            all.AddRange(perc);

            Console.WriteLine($"    Harmonic codes: {harm.Count}, Percussive codes: {perc.Count}");
            Console.WriteLine($"    Total codes: {all.Count}");
            return all;
        }

        private static List<(ulong, string, int)> ExtractFromChannel(
            float[,] mags, Chroma chroma, string songId)
        {
            // detect peaks in this channel
            var peaks = PeakDetector.Detect(mags, nbhdSize: 3, thresholdFactor: 1.5f);
            if (peaks.Count == 0) return new();

            // normalize weight
            float maxMag = peaks.Max(p => p.Mag);

            var codes = new List<(ulong, string, int)>();

            foreach (var pi in peaks)
            {
                // collect neighbors
                var nbrs = peaks
                    .Where(pj =>
                        pj.Frame > pi.Frame &&
                        pj.Frame - pi.Frame <= MaxDeltaFrames &&
                        Math.Abs(pj.Bin - pi.Bin) <= MaxDeltaBins)
                    .Select(pj => (dt: pj.Frame - pi.Frame, df: pj.Bin - pi.Bin, mag: pj.Mag))
                    .OrderByDescending(n => pi.Mag * n.mag)
                    .Take(KNeighbors)
                    .ToList();
                if (nbrs.Count == 0) continue;

                // descriptor: for each neighbor 3 bytes (t,f,w) + 2 bytes (chroma1, chroma2)
                byte[] desc = new byte[KNeighbors * 5];

                // graph bytes
                for (int n = 0; n < KNeighbors; n++)
                {
                    int idx = n * 5;
                    if (n < nbrs.Count)
                    {
                        var (dt, df, mag) = nbrs[n];
                        desc[idx + 0] = (byte)Math.Min(TimeBins - 1, (dt * (TimeBins - 1)) / MaxDeltaFrames);
                        int dfShift = df + MaxDeltaBins;
                        desc[idx + 1] = (byte)Math.Min(FreqBins - 1, (dfShift * (FreqBins - 1)) / (2 * MaxDeltaBins));
                        desc[idx + 2] = (byte)Math.Min(WeightBins - 1, (int)((mag / maxMag) * (WeightBins - 1)));
                    }
                    else
                    {
                        desc[idx + 0] = 0;
                        desc[idx + 1] = 0;
                        desc[idx + 2] = 0;
                    }
                }

                // chroma bytes: extract 12-length vector for this frame
                float[] cvec = new float[chroma.NumBins];
                for (int c = 0; c < chroma.NumBins; c++)
                    cvec[c] = chroma.Matrix[pi.Frame, c];

                // find top two chroma bins
                int c1 = Array.IndexOf(cvec, cvec.Max());
                cvec[c1] = 0;
                int c2 = Array.IndexOf(cvec, cvec.Max());

                // fill chroma bytes into each neighbor slot
                for (int n = 0; n < KNeighbors; n++)
                {
                    int idx = n * 5;
                    desc[idx + 3] = (byte)c1;
                    desc[idx + 4] = (byte)c2;
                }

                // FNV-1a hash
                ulong hash = FnvOffset;
                foreach (var b in desc)
                {
                    hash ^= b;
                    hash *= FnvPrime;
                }

                codes.Add((hash, songId, pi.Frame));
            }

            return codes;
        }
    }
}
