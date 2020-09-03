using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System
{
    public static partial class MathF
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double CosTaylorSeriesOneIteration(double x1)
        {
            // 1 - (x^2 / 2!)
            double x2 = x1 * x1;
            return 1.0 + (x2 * C1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double CosTaylorSeriesFourIterations(double x1)
        {
            // 1 - (x^2 / 2!) + (x^4 / 4!) - (x^6 / 6!) + (x^8 / 8!) - (x^10 / 10!)

            double x2 = x1 * x1;
            double x4 = x2 * x2;

            return 1.0 + (x2 * C0) + (x4 * ((C1 + (x2 * C2)) + (x4 * (C3 + (x2 * C4)))));
        }

        private static unsafe double ReduceForLargeInput(double x, out int region)
        {
            Debug.Assert(!double.IsNegative(x));

            // This method simulates multi-precision floating-point
            // arithmetic and is accurate for all 1 <= x < infinity

            const int BitsPerIteration = 36;
            long ux = BitConverter.DoubleToInt64Bits(x);

            int xExp = (int)(((ux & 0x7FF0000000000000) >> 52) - 1023);
            ux = ((ux & 0x000FFFFFFFFFFFFF) | 0x0010000000000000) >> 29;

            // Now ux is the mantissa bit pattern of x as a long integer
            long mask = 1;
            mask = (mask << BitsPerIteration) - 1;

            // Set first and last to the positions of the first and last chunks of (2 / PI) that we need
            int first = xExp / BitsPerIteration;
            int resultExp = xExp - (first * BitsPerIteration);

            // 120 is the theoretical maximum number of bits (actually
            // 115 for IEEE single precision) that we need to extract
            // from the middle of (2 / PI) to compute the reduced argument
            // accurately enough for our purposes

            int last = first + (120 / BitsPerIteration);

            // Unroll the loop. This is only correct because we know that bitsper is fixed as 36.

            long* result = stackalloc long[10];
            long u, carry;

            result[4] = 0;
            u = PiBits[last] * ux;

            result[3] = u & mask;
            carry = u >> BitsPerIteration;
            u = PiBits[last - 1] * ux + carry;

            result[2] = u & mask;
            carry = u >> BitsPerIteration;
            u = PiBits[last - 2] * ux + carry;

            result[1] = u & mask;
            carry = u >> BitsPerIteration;
            u = PiBits[first] * ux + carry;

            result[0] = u & mask;

            // Reconstruct the result
            int ltb = (int)((((result[0] << BitsPerIteration) | result[1]) >> (BitsPerIteration - 1 - resultExp)) & 7);

            long mantissa;
            long nextBits;

            // determ says whether the fractional part is >= 0.5
            bool determ = (ltb & 1) != 0;

            int i = 1;

            if (determ)
            {
                // The mantissa is >= 0.5. We want to subtract it from 1.0 by negating all the bits
                region = ((ltb >> 1) + 1) & 3;

                mantissa = 1;
                mantissa = ~(result[1]) & ((mantissa << (BitsPerIteration - resultExp)) - 1);

                while (mantissa < 0x0000000000010000)
                {
                    i++;
                    mantissa = (mantissa << BitsPerIteration) | (~(result[i]) & mask);
                }

                nextBits = (~(result[i + 1]) & mask);
            }
            else
            {
                region = (ltb >> 1);

                mantissa = 1;
                mantissa = result[1] & ((mantissa << (BitsPerIteration - resultExp)) - 1);

                while (mantissa < 0x0000000000010000)
                {
                    i++;
                    mantissa = (mantissa << BitsPerIteration) | result[i];
                }

                nextBits = result[i + 1];
            }

            // Normalize the mantissa.
            // The shift value 6 here, determined by trial and error, seems to give optimal speed.

            int bc = 0;

            while (mantissa < 0x0000400000000000)
            {
                bc += 6;
                mantissa <<= 6;
            }

            while (mantissa < 0x0010000000000000)
            {
                bc++;
                mantissa <<= 1;
            }

            mantissa |= nextBits >> (BitsPerIteration - bc);

            int rExp = 52 + resultExp - bc - i * BitsPerIteration;

            // Put the result exponent rexp onto the mantissa pattern
            u = (rExp + 1023L) << 52;
            ux = (mantissa & 0x000FFFFFFFFFFFFF) | u;

            if (determ)
            {
                // If we negated the mantissa we negate x too
                ux |= unchecked((long)(0x8000000000000000));
            }

            x = BitConverter.Int64BitsToDouble(ux);

            // x is a double precision version of the fractional part of
            // (x * (2 / PI)). Multiply x by (PI / 2) in double precision
            // to get the reduced result.

            return x * PiOverTwo;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double SinTaylorSeriesOneIteration(double x1)
        {
            // x - (x^3 / 3!)

            double x2 = x1 * x1;
            double x3 = x2 * x1;

            return x1 + (x3 * S1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double SinTaylorSeriesFourIterations(double x1)
        {
            // x - (x^3 / 3!) + (x^5 / 5!) - (x^7 / 7!) + (x^9 / 9!)

            double x2 = x1 * x1;
            double x3 = x2 * x1;
            double x4 = x2 * x2;

            return x1 + ((S1 + (x2 * S2) + (x4 * (S3 + (x2 * S4)))) * x3);
        }
    }
}
