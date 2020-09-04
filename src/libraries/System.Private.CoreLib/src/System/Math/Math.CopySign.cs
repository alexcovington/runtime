using System.Runtime.CompilerServices;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics.Arm;

namespace System
{
    public static partial class Math
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double CopySign(double x, double y)
        {
            if (Sse2.IsSupported || AdvSimd.IsSupported)
            {
                return VectorMath.ConditionalSelectBitwise(Vector128.CreateScalarUnsafe(-0.0), Vector128.CreateScalarUnsafe(y), Vector128.CreateScalarUnsafe(x)).ToScalar();
            }
            else
            {
                return SoftwareFallback(x, y);
            }

            static double SoftwareFallback(double x, double y)
            {
                const long signMask = 1L << 63;

                // This method is required to work for all inputs,
                // including NaN, so we operate on the raw bits.
                long xbits = BitConverter.DoubleToInt64Bits(x);
                long ybits = BitConverter.DoubleToInt64Bits(y);

                // Remove the sign from x, and remove everything but the sign from y
                xbits &= ~signMask;
                ybits &= signMask;

                // Simply OR them to get the correct sign
                return BitConverter.Int64BitsToDouble(xbits | ybits);
            }
        }
    }
}
