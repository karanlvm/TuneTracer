using System;
using System.Collections.Generic;

namespace CoreApp
{
    public struct SpectralPeak
    {
        public int Frame;  // time index
        public int Bin;    // frequency index
        public float Mag;  // magnitude

        public SpectralPeak(int frame, int bin, float mag)
        {
            Frame = frame;
            Bin   = bin;
            Mag   = mag;
        }
    }

    public static class PeakDetector
    {
        /// <summary>
        /// Detects local peaks in any 2D magnitude array.
        /// </summary>
        public static List<SpectralPeak> Detect(
            float[,] mags,
            int nbhdSize = 3,
            float thresholdFactor = 1.5f
        )
        {
            int F = mags.GetLength(0);
            int B = mags.GetLength(1);
            var peaks = new List<SpectralPeak>();

            // Compute global mean & std
            double sum = 0, sumSq = 0;
            int N = F * B;
            for (int t = 0; t < F; t++)
            for (int f = 0; f < B; f++)
            {
                var v = mags[t, f];
                sum   += v;
                sumSq += v * v;
            }

            double mean   = sum / N;
            double std    = Math.Sqrt(sumSq / N - mean * mean);
            double thresh = mean + thresholdFactor * std;

            int r = nbhdSize;
            for (int t = r; t < F - r; t++)
            for (int f = r; f < B - r; f++)
            {
                float v = mags[t, f];
                if (v < thresh) continue;

                bool isPeak = true;
                for (int dt = -r; dt <= r && isPeak; dt++)
                for (int df = -r; df <= r; df++)
                {
                    if (dt == 0 && df == 0) continue;
                    if (mags[t + dt, f + df] > v)
                    {
                        isPeak = false;
                        break;
                    }
                }

                if (isPeak)
                    peaks.Add(new SpectralPeak(t, f, v));
            }

            return peaks;
        }

        /// <summary>
        /// Detects peaks in a Spectrogram by delegating to the float[,] overload.
        /// </summary>
        public static List<SpectralPeak> Detect(
            Spectrogram spec,
            int nbhdSize = 3,
            float thresholdFactor = 1.5f
        )
        {
            return Detect(spec.Magnitudes, nbhdSize, thresholdFactor);
        }
    }
}
