using System;
using System.Collections.Generic;

namespace CoreApp
{
    public struct SpectralPeak
    {
        public int Frame;     // time index
        public int Bin;       // frequency index
        public float Mag;     // magnitude

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
        /// Detects local peaks in the spectrogram.
        /// A peak at (f,t) is greater than all neighbours within nbhd x nbhd.
        /// </summary>
        public static List<SpectralPeak> Detect(
            Spectrogram spec,
            int nbhdSize = 3,
            float thresholdFactor = 1.5f
        )
        {
            int F = spec.NumFrames;
            int B = spec.NumBins;
            var mags = spec.Magnitudes;
            var peaks = new List<SpectralPeak>();

            // Compute global mean & std
            double sum = 0, sumSq = 0;
            int N = F * B;
            for (int i = 0; i < F; i++)
                for (int j = 0; j < B; j++)
                {
                    sum   += mags[i, j];
                    sumSq += mags[i, j] * mags[i, j];
                }
            double mean = sum / N;
            double std  = Math.Sqrt(sumSq / N - mean * mean);
            double thresh = mean + thresholdFactor * std;

            // Slide and find local maxima above threshold
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
    }
}
