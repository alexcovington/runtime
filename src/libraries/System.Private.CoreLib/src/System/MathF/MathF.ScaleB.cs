namespace System
{
    public static partial class MathF
    {
        public static float ScaleB(float x, int n)
        {
            int ix = BitConverter.SingleToInt32Bits(x);
            int k = (ix & 0x7f800000) >> 23; // Extract exponent
            int sign = float.IsNegative(x) ? -1 : 1;

            if (k == 0)
            {
                // 0 or subnormal
                if ((ix & 0x7fffffff) == 0)
                    return x; // +-0

                x *= Two25;
                ix = BitConverter.SingleToInt32Bits(x);
                k = ((ix & 0x7f800000) >> 23) - 25;
            }
            if (k == 0xFF)
            {
                // NaN or Infinity
                return x + x;
            }
            if (n < -50000)
                return Small * Small * sign; // Underflow
            if (n > 50000 || k + n > 0xFE)
                return Huge * Huge * sign;

            k = k + n;
            if (k > 0)
            {
                return BitConverter.Int32BitsToSingle((int)((int)(ix & 0x807fffff) | (int)(k << 23)));
            }
            if (k <= -25)
            {
                return Small * Small * sign; // Underflow
            }

            k += 25;
            x = BitConverter.Int32BitsToSingle((int)((int)(ix & 0x807fffff) | (int)(k << 23)));
            return x * TwoM25;
        }
    }
}
