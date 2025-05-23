using System;

namespace CoreApp
{
    public class Spectrogram
    {
        public readonly int WindowSize;
        public readonly int HopSize;
        public readonly int NumBins;
        public readonly int NumFrames;
        public readonly float[,] Magnitudes; // [frame, bin]

        public Spectrogram(WavFile wav, int windowSize = 2048, int hopSize = 1024)
        {
            WindowSize = windowSize;
            HopSize    = hopSize;
            NumBins    = windowSize / 2;
            NumFrames  = 1 + (wav.Samples.Length - windowSize) / hopSize;
            Magnitudes = new float[NumFrames, NumBins];

            // Precompute Hann window
            var window = new double[WindowSize];
            for (int n = 0; n < WindowSize; n++)
                window[n] = 0.5 * (1 - Math.Cos(2 * Math.PI * n / (WindowSize - 1)));

            // Buffer for FFT
            var buffer = new Complex[WindowSize];

            for (int frame = 0; frame < NumFrames; frame++)
            {
                int offset = frame * HopSize;
                // Load windowed samples into buffer
                for (int n = 0; n < WindowSize; n++)
                {
                    double sample = wav.Samples[offset + n];
                    buffer[n] = new Complex(sample * window[n], 0);
                }

                // Perform FFT
                Fft.Transform(buffer, 1);

                // Compute magnitudes for first half
                for (int bin = 0; bin < NumBins; bin++)
                {
                    var c = buffer[bin];
                    Magnitudes[frame, bin] = (float)Math.Sqrt(c.Real * c.Real + c.Imag * c.Imag);
                }
            }
        }
    }
}
