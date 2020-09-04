using System.Runtime.CompilerServices;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics.Arm;

namespace System
{
    public static partial class Math
    {
        public static long BigMul(int a, int b)
        {
            return ((long)a) * b;
        }

        /// <summary>Produces the full product of two unsigned 64-bit numbers.</summary>
        /// <param name="a">The first number to multiply.</param>
        /// <param name="b">The second number to multiply.</param>
        /// <param name="low">The low 64-bit of the product of the specied numbers.</param>
        /// <returns>The high 64-bit of the product of the specied numbers.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe ulong BigMul(ulong a, ulong b, out ulong low)
        {
            if (Bmi2.X64.IsSupported)
            {
                ulong tmp;
                ulong high = Bmi2.X64.MultiplyNoFlags(a, b, &tmp);
                low = tmp;
                return high;
            }

            return SoftwareFallback(a, b, out low);

            static ulong SoftwareFallback(ulong a, ulong b, out ulong low)
            {
                // Adaptation of algorithm for multiplication
                // of 32-bit unsigned integers described
                // in Hacker's Delight by Henry S. Warren, Jr. (ISBN 0-201-91465-4), Chapter 8
                // Basically, it's an optimized version of FOIL method applied to
                // low and high dwords of each operand

                // Use 32-bit uints to optimize the fallback for 32-bit platforms.
                uint al = (uint)a;
                uint ah = (uint)(a >> 32);
                uint bl = (uint)b;
                uint bh = (uint)(b >> 32);

                ulong mull = ((ulong)al) * bl;
                ulong t = ((ulong)ah) * bl + (mull >> 32);
                ulong tl = ((ulong)al) * bh + (uint)t;

                low = tl << 32 | (uint)mull;

                return ((ulong)ah) * bh + (t >> 32) + (tl >> 32);
            }
        }

        /// <summary>Produces the full product of two 64-bit numbers.</summary>
        /// <param name="a">The first number to multiply.</param>
        /// <param name="b">The second number to multiply.</param>
        /// <param name="low">The low 64-bit of the product of the specied numbers.</param>
        /// <returns>The high 64-bit of the product of the specied numbers.</returns>
        public static long BigMul(long a, long b, out long low)
        {
            ulong high = BigMul((ulong)a, (ulong)b, out ulong ulow);
            low = (long)ulow;
            return (long)high - ((a >> 63) & b) - ((b >> 63) & a);
        }
    }
}
