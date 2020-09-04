// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ===================================================================================================
// Portions of the code implemented below are based on the 'Berkeley SoftFloat Release 3e' algorithms.
// ===================================================================================================

/*============================================================
**
** Purpose: Some single-precision floating-point math operations
**
===========================================================*/

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics.Arm;

namespace System
{
    public static partial class MathF
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CopySign(float x, float y)
        {
            if (Sse.IsSupported || AdvSimd.IsSupported)
            {
                return VectorMath.ConditionalSelectBitwise(Vector128.CreateScalarUnsafe(-0.0f), Vector128.CreateScalarUnsafe(y), Vector128.CreateScalarUnsafe(x)).ToScalar();
            }
            else
            {
                return SoftwareFallback(x, y);
            }

            static float SoftwareFallback(float x, float y)
            {
                const int signMask = 1 << 31;

                // This method is required to work for all inputs,
                // including NaN, so we operate on the raw bits.
                int xbits = BitConverter.SingleToInt32Bits(x);
                int ybits = BitConverter.SingleToInt32Bits(y);

                // Remove the sign from x, and remove everything but the sign from y
                xbits &= ~signMask;
                ybits &= signMask;

                // Simply OR them to get the correct sign
                return BitConverter.Int32BitsToSingle(xbits | ybits);
            }
        }
    }
}
