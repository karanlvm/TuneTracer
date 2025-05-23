using System;
using System.IO;

namespace CoreApp
{
    public class WavFile
    {
        public int SampleRate { get; private set; }
        public short BitsPerSample { get; private set; }
        public short Channels { get; private set; }
        public float[] Samples { get; private set; }

        public static WavFile Load(string path)
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(fs);

            // RIFF header
            if (new string(reader.ReadChars(4)) != "RIFF")
                throw new InvalidDataException("Not a RIFF file");
            reader.ReadInt32(); // chunk size
            if (new string(reader.ReadChars(4)) != "WAVE")
                throw new InvalidDataException("Not a WAVE file");

            // fmt subchunk
            if (new string(reader.ReadChars(4)) != "fmt ")
                throw new InvalidDataException("Invalid fmt chunk");
            int fmtSize = reader.ReadInt32();
            short audioFormat = reader.ReadInt16();
            if (audioFormat != 1)
                throw new InvalidDataException("Only PCM WAV supported");
            short channels = reader.ReadInt16();
            int sampleRate = reader.ReadInt32();
            reader.ReadInt32(); // byte rate
            reader.ReadInt16(); // block align
            short bitsPerSample = reader.ReadInt16();
            if (fmtSize > 16)
                reader.ReadBytes(fmtSize - 16);

            // data subchunk
            string id;
            int dataSize;
            while (true)
            {
                id = new string(reader.ReadChars(4));
                dataSize = reader.ReadInt32();
                if (id == "data") break;
                reader.ReadBytes(dataSize);
            }

            int bytesPerSample = bitsPerSample / 8;
            int totalSamples = dataSize / bytesPerSample;
            var samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                samples[i] = bitsPerSample switch
                {
                    8  => (reader.ReadByte() - 128) / 128f,
                    16 => reader.ReadInt16() / 32768f,
                    _  => throw new NotSupportedException($"Unsupported bit depth: {bitsPerSample}")
                };
            }

            return new WavFile
            {
                SampleRate   = sampleRate,
                BitsPerSample= bitsPerSample,
                Channels     = channels,
                Samples      = samples
            };
        }
    }
}
