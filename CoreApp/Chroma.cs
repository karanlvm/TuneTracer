using System;

namespace CoreApp
{
    public class Chroma
    {
        public int NumFrames { get; }
        public int NumBins   { get; } = 12;
        public float[,] Matrix { get; }

        public Chroma(Spectrogram spec, int referenceFreq = 440)
        {
            NumFrames = spec.NumFrames;
            Matrix    = new float[NumFrames, NumBins];

            // binWidth from spec
            double binWidth = spec.SampleRate / (double)spec.WindowSize;

            for (int t = 0; t < NumFrames; t++)
            {
                // accumulate energy
                for (int b = 0; b < spec.NumBins; b++)
                {
                    double freq = b * binWidth;
                    if (freq < 27.5) continue;
                    double midi = 69 + 12 * Math.Log2(freq / referenceFreq);
                    int chroma = ((int)Math.Round(midi) % 12 + 12) % 12;
                    Matrix[t, chroma] += spec.Magnitudes[t, b];
                }
                // normalize
                float sum = 0;
                for (int c = 0; c < NumBins; c++) sum += Matrix[t, c];
                if (sum > 0)
                    for (int c = 0; c < NumBins; c++)
                        Matrix[t, c] /= sum;
            }
        }
    }
}
