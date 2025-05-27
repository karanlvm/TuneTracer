using System;

namespace CoreApp
{
    public class Spectrogram
    {
        public int SampleRate  { get; }
        public int WindowSize  { get; }
        public int HopSize     { get; }
        public int NumBins     { get; }
        public int NumFrames   { get; }

        public float[,] Magnitudes { get; }
        public float[,] Harmonic   { get; }
        public float[,] Percussive { get; }

        public Spectrogram(WavFile wav, int windowSize = 2048, int hopSize = 1024, int hpKernel = 31)
        {
            SampleRate = wav.SampleRate;
            WindowSize = windowSize;
            HopSize    = hopSize;
            NumBins    = windowSize / 2;
            NumFrames  = 1 + (wav.Samples.Length - windowSize) / hopSize;

            Magnitudes = new float[NumFrames, NumBins];
            Harmonic   = new float[NumFrames, NumBins];
            Percussive = new float[NumFrames, NumBins];

            // Precompute Hann window
            var window = new double[WindowSize];
            for (int n = 0; n < WindowSize; n++)
                window[n] = 0.5 * (1 - Math.Cos(2 * Math.PI * n / (WindowSize - 1)));

            // FFT buffer
            var buffer = new Complex[WindowSize];
            for (int t = 0; t < NumFrames; t++)
            {
                int offset = t * HopSize;
                for (int n = 0; n < WindowSize; n++)
                {
                    buffer[n] = new Complex(wav.Samples[offset + n] * window[n], 0);
                }
                Fft.Transform(buffer, 1);
                for (int b = 0; b < NumBins; b++)
                {
                    var c = buffer[b];
                    Magnitudes[t, b] = (float)Math.Sqrt(c.Real * c.Real + c.Imag * c.Imag);
                }
            }

            // HP separation
            MedianFilterTime(Magnitudes, Harmonic, kernel: hpKernel);
            MedianFilterFreq(Magnitudes, Percussive, kernel: hpKernel);
        }

        private static void MedianFilterTime(float[,] src, float[,] dst, int kernel)
        {
            int F = src.GetLength(0), B = src.GetLength(1), half = kernel / 2;
            var window = new float[kernel];
            for (int f = 0; f < B; f++)
            {
                for (int t = 0; t < F; t++)
                {
                    int count = 0;
                    for (int k = -half; k <= half; k++)
                    {
                        int tt = t + k;
                        if (tt < 0 || tt >= F) continue;
                        window[count++] = src[tt, f];
                    }
                    Array.Sort(window, 0, count);
                    dst[t, f] = window[count / 2];
                }
            }
        }

        private static void MedianFilterFreq(float[,] src, float[,] dst, int kernel)
        {
            int F = src.GetLength(0), B = src.GetLength(1), half = kernel / 2;
            var window = new float[kernel];
            for (int t = 0; t < F; t++)
            {
                for (int f = 0; f < B; f++)
                {
                    int count = 0;
                    for (int k = -half; k <= half; k++)
                    {
                        int ff = f + k;
                        if (ff < 0 || ff >= B) continue;
                        window[count++] = src[t, ff];
                    }
                    Array.Sort(window, 0, count);
                    dst[t, f] = window[count / 2];
                }
            }
        }
    }
}
