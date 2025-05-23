using System;

namespace CoreApp
{
    public struct Complex
    {
        public double Real, Imag;
        public Complex(double r, double i = 0) { Real = r; Imag = i; }
        public static Complex operator +(Complex a, Complex b)
            => new Complex(a.Real + b.Real, a.Imag + b.Imag);
        public static Complex operator -(Complex a, Complex b)
            => new Complex(a.Real - b.Real, a.Imag - b.Imag);
        public static Complex operator *(Complex a, Complex b)
            => new Complex(
                a.Real * b.Real - a.Imag * b.Imag,
                a.Real * b.Imag + a.Imag * b.Real
            );
    }

    public static class Fft
    {
        public static void Transform(Complex[] buffer, int dir)
        {
            int n = buffer.Length;
            if ((n & (n - 1)) != 0)
                throw new ArgumentException("Length must be power of two");

            // Bit-reverse
            int j = 0;
            for (int i = 1; i < n; i++)
            {
                int bit = n >> 1;
                for (; j >= bit; bit >>= 1)
                    j -= bit;
                j += bit;
                if (i < j)
                {
                    var tmp = buffer[i];
                    buffer[i] = buffer[j];
                    buffer[j] = tmp;
                }
            }

            // Cooley–Tukey
            for (int len = 2; len <= n; len <<= 1)
            {
                double ang = 2 * Math.PI / len * dir;
                var wlen = new Complex(Math.Cos(ang), Math.Sin(ang));
                for (int i = 0; i < n; i += len)
                {
                    var w = new Complex(1, 0);
                    for (int k = 0; k < (len >> 1); k++)
                    {
                        var u = buffer[i + k];
                        var v = buffer[i + k + (len >> 1)] * w;
                        buffer[i + k]          = u + v;
                        buffer[i + k + (len >> 1)] = u - v;
                        w = w * wlen;
                    }
                }
            }

            // Normalize if inverse
            if (dir == -1)
            {
                for (int i = 0; i < n; i++)
                {
                    buffer[i].Real /= n;
                    buffer[i].Imag /= n;
                }
            }
        }
    }
}
