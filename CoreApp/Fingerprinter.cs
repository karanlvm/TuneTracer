// Fingerprinter.cs
using System;
using System.Collections.Generic;
using System.Linq;

namespace CoreApp
{
    public static class Fingerprinter
    {
        // Graph & quantization parameters
        private const int MaxDeltaFrames = 50;    // e.g. ~1s if hop=1024@44.1kHz
        private const int MaxDeltaBins   = 50;    // ~5kHz if bin=100Hz
        private const int KNeighbors     = 5;     // edges per node

        // Quantization bins
        private const int TimeBins   = 16;
        private const int FreqBins   = 32;
        private const int WeightBins = 8;

        // FNV-1a constants
        private const ulong FnvOffset = 0xcbf29ce484222325;
        private const ulong FnvPrime  = 0x00000100000001B3;

        public static List<(ulong code, string songId, int offset)> Extract(
            WavFile wav, string songId)
        {
            var spec = new Spectrogram(wav, windowSize:2048, hopSize:1024);

            // 1. Detect peaks
            var peaks = PeakDetector.Detect(spec, nbhdSize:3, thresholdFactor:1.5f);
            if (peaks.Count == 0) return new();

            // 2. Precompute max magnitude for weight normalization
            float maxMag = 0f;
            foreach (var v in spec.Magnitudes) 
                if (v > maxMag) maxMag = v;

            var codes = new List<(ulong, string, int)>(peaks.Count);

            // 3. Build graph & extract descriptor per peak
            for (int i = 0; i < peaks.Count; i++)
            {
                var pi = peaks[i];
                // find neighbor peaks within Δt, Δf
                var neighbors = new List<(int dt, int df, float mag)>();
                for (int j = 0; j < peaks.Count; j++)
                {
                    if (i == j) continue;
                    var pj = peaks[j];
                    int dt = pj.Frame - pi.Frame;
                    if (dt <= 0 || dt > MaxDeltaFrames) continue;
                    int df = pj.Bin - pi.Bin;
                    if (Math.Abs(df) > MaxDeltaBins) continue;
                    neighbors.Add((dt, df, pj.Mag));
                }

                // pick top-K by magnitude product weight pi.Mag * pj.Mag
                var topK = neighbors
                    .OrderByDescending(n => pi.Mag * n.mag)
                    .Take(KNeighbors)
                    .ToList();

                if (topK.Count == 0) continue;

                // 4. Quantize & build byte descriptor
                var desc = new byte[KNeighbors * 3];
                for (int n = 0; n < KNeighbors; n++)
                {
                    int baseIdx = n * 3;
                    if (n < topK.Count)
                    {
                        var (dt, df, mag) = topK[n];
                        // quantize
                        byte tQ = (byte)(Math.Min(TimeBins - 1,
                            (dt * (TimeBins - 1)) / MaxDeltaFrames));
                        // shift df to [0..2*MaxDeltaBins]
                        int dfShift = df + MaxDeltaBins;
                        byte fQ = (byte)(Math.Min(FreqBins - 1,
                            (dfShift * (FreqBins - 1)) / (2 * MaxDeltaBins)));
                        byte wQ = (byte)(Math.Min(WeightBins - 1,
                            (int)((mag / maxMag) * (WeightBins - 1))));
                        desc[baseIdx + 0] = tQ;
                        desc[baseIdx + 1] = fQ;
                        desc[baseIdx + 2] = wQ;
                    }
                    else
                    {
                        // pad with zeros if fewer than KNeighbors
                        desc[baseIdx + 0] = 0;
                        desc[baseIdx + 1] = 0;
                        desc[baseIdx + 2] = 0;
                    }
                }

                // 5. Hash descriptor with FNV-1a → 64-bit code
                ulong hash = FnvOffset;
                for (int b = 0; b < desc.Length; b++)
                {
                    hash ^= desc[b];
                    hash *= FnvPrime;
                }

                // 6. Record (code, songId, timeOffset = frame index)
                codes.Add((hash, songId, pi.Frame));
            }

            Console.WriteLine($"    Generated {codes.Count} codes");
            return codes;
        }
    }
}
