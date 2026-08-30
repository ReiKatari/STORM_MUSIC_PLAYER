using System;

namespace StormMusicPlayer.Common
{
    /// <summary>
    /// Math utilities for performing fast Fourier transforms.
    /// Employs an optimized, allocation-free Cooley-Tukey Radix-2 DIT FFT.
    /// </summary>
    public static class FftHelper
    {
        /// <summary>
        /// Computes the Fast Fourier Transform (FFT) in-place on complex arrays of matching size.
        /// </summary>
        /// <param name="real">The real components of the inputs (and outputs after computation).</param>
        /// <param name="imag">The imaginary components of the inputs (and outputs after computation).</param>
        public static void FFT(float[] real, float[] imag)
        {
            int n = real.Length;
            if ((n & (n - 1)) != 0)
                throw new ArgumentException("FFT size must be a power of 2.");

            // Bit-reversal Permutation
            int limit = 1;
            int bitLength = 0;
            while (limit < n)
            {
                limit <<= 1;
                bitLength++;
            }

            for (int i = 0; i < n; i++)
            {
                int rev = ReverseBits(i, bitLength);
                if (rev > i)
                {
                    float tempReal = real[i];
                    real[i] = real[rev];
                    real[rev] = tempReal;

                    float tempImag = imag[i];
                    imag[i] = imag[rev];
                    imag[rev] = tempImag;
                }
            }

            // Cooley-Tukey Decimation-in-time
            for (int len = 2; len <= n; len <<= 1)
            {
                double angle = -2.0 * Math.PI / len;
                float wlenReal = (float)Math.Cos(angle);
                float wlenImag = (float)Math.Sin(angle);

                for (int i = 0; i < n; i += len)
                {
                    float wReal = 1.0f;
                    float wImag = 0.0f;
                    int halfLen = len / 2;

                    for (int j = 0; j < halfLen; j++)
                    {
                        int uIdx = i + j;
                        int vIdx = i + j + halfLen;

                        float tReal = real[vIdx] * wReal - imag[vIdx] * wImag;
                        float tImag = real[vIdx] * wImag + imag[vIdx] * wReal;

                        real[vIdx] = real[uIdx] - tReal;
                        imag[vIdx] = imag[uIdx] - tImag;

                        real[uIdx] += tReal;
                        imag[uIdx] += tImag;

                        // Rotate w
                        float nextWReal = wReal * wlenReal - wImag * wlenImag;
                        wImag = wReal * wlenImag + wImag * wlenReal;
                        wReal = nextWReal;
                    }
                }
            }
        }

        private static int ReverseBits(int val, int width)
        {
            int result = 0;
            for (int i = 0; i < width; i++)
            {
                result = (result << 1) | (val & 1);
                val >>= 1;
            }
            return result;
        }
    }
}
