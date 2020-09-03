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
        public static float Abs(float x)
        {
            return Math.Abs(x);
        }

        public static float BitDecrement(float x)
        {
            int bits = BitConverter.SingleToInt32Bits(x);

            if ((bits & 0x7F800000) >= 0x7F800000)
            {
                // NaN returns NaN
                // -Infinity returns -Infinity
                // +Infinity returns float.MaxValue
                return (bits == 0x7F800000) ? float.MaxValue : x;
            }

            if (bits == 0x00000000)
            {
                // +0.0 returns -float.Epsilon
                return -float.Epsilon;
            }

            // Negative values need to be incremented
            // Positive values need to be decremented

            bits += ((bits < 0) ? +1 : -1);
            return BitConverter.Int32BitsToSingle(bits);
        }

        public static float BitIncrement(float x)
        {
            int bits = BitConverter.SingleToInt32Bits(x);

            if ((bits & 0x7F800000) >= 0x7F800000)
            {
                // NaN returns NaN
                // -Infinity returns float.MinValue
                // +Infinity returns +Infinity
                return (bits == unchecked((int)(0xFF800000))) ? float.MinValue : x;
            }

            if (bits == unchecked((int)(0x80000000)))
            {
                // -0.0 returns float.Epsilon
                return float.Epsilon;
            }

            // Negative values need to be decremented
            // Positive values need to be incremented

            bits += ((bits < 0) ? -1 : +1);
            return BitConverter.Int32BitsToSingle(bits);
        }

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

        public static float IEEERemainder(float x, float y)
        {
            if (float.IsNaN(x))
            {
                return x; // IEEE 754-2008: NaN payload must be preserved
            }

            if (float.IsNaN(y))
            {
                return y; // IEEE 754-2008: NaN payload must be preserved
            }

            float regularMod = x % y;

            if (float.IsNaN(regularMod))
            {
                return float.NaN;
            }

            if ((regularMod == 0) && float.IsNegative(x))
            {
                return float.NegativeZero;
            }

            float alternativeResult = (regularMod - (Abs(y) * Sign(x)));

            if (Abs(alternativeResult) == Abs(regularMod))
            {
                float divisionResult = x / y;
                float roundedResult = Round(divisionResult);

                if (Abs(roundedResult) > Abs(divisionResult))
                {
                    return alternativeResult;
                }
                else
                {
                    return regularMod;
                }
            }

            if (Abs(alternativeResult) < Abs(regularMod))
            {
                return alternativeResult;
            }
            else
            {
                return regularMod;
            }
        }

        public static float Log(float x, float y)
        {
            if (float.IsNaN(x))
            {
                return x; // IEEE 754-2008: NaN payload must be preserved
            }

            if (float.IsNaN(y))
            {
                return y; // IEEE 754-2008: NaN payload must be preserved
            }

            if (y == 1)
            {
                return float.NaN;
            }

            if ((x != 1) && ((y == 0) || float.IsPositiveInfinity(y)))
            {
                return float.NaN;
            }

            return Log(x) / Log(y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Max(float x, float y)
        {
            return Math.Max(x, y);
        }

        public static float MaxMagnitude(float x, float y)
        {
            // This matches the IEEE 754:2019 `maximumMagnitude` function
            //
            // It propagates NaN inputs back to the caller and
            // otherwise returns the input with a larger magnitude.
            // It treats +0 as larger than -0 as per the specification.

            float ax = Abs(x);
            float ay = Abs(y);

            if ((ax > ay) || float.IsNaN(ax))
            {
                return x;
            }

            if (ax == ay)
            {
                return float.IsNegative(x) ? y : x;
            }

            return y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Min(float x, float y)
        {
            return Math.Min(x, y);
        }

        public static float MinMagnitude(float x, float y)
        {
            // This matches the IEEE 754:2019 `minimumMagnitude` function
            //
            // It propagates NaN inputs back to the caller and
            // otherwise returns the input with a larger magnitude.
            // It treats +0 as larger than -0 as per the specification.

            float ax = Abs(x);
            float ay = Abs(y);

            if ((ax < ay) || float.IsNaN(ax))
            {
                return x;
            }

            if (ax == ay)
            {
                return float.IsNegative(x) ? x : y;
            }

            return y;
        }

        [Intrinsic]
        public static float Round(float x)
        {
            // ************************************************************************************
            // IMPORTANT: Do not change this implementation without also updating MathF.Round(float),
            //            FloatingPointUtils::round(double), and FloatingPointUtils::round(float)
            // ************************************************************************************

            // This is based on the 'Berkeley SoftFloat Release 3e' algorithm

            uint bits = (uint)BitConverter.SingleToInt32Bits(x);
            int exponent = float.ExtractExponentFromBits(bits);

            if (exponent <= 0x7E)
            {
                if ((bits << 1) == 0)
                {
                    // Exactly +/- zero should return the original value
                    return x;
                }

                // Any value less than or equal to 0.5 will always round to exactly zero
                // and any value greater than 0.5 will always round to exactly one. However,
                // we need to preserve the original sign for IEEE compliance.

                float result = ((exponent == 0x7E) && (float.ExtractSignificandFromBits(bits) != 0)) ? 1.0f : 0.0f;
                return CopySign(result, x);
            }

            if (exponent >= 0x96)
            {
                // Any value greater than or equal to 2^23 cannot have a fractional part,
                // So it will always round to exactly itself.

                return x;
            }

            // The absolute value should be greater than or equal to 1.0 and less than 2^23
            Debug.Assert((0x7F <= exponent) && (exponent <= 0x95));

            // Determine the last bit that represents the integral portion of the value
            // and the bits representing the fractional portion

            uint lastBitMask = 1U << (0x96 - exponent);
            uint roundBitsMask = lastBitMask - 1;

            // Increment the first fractional bit, which represents the midpoint between
            // two integral values in the current window.

            bits += lastBitMask >> 1;

            if ((bits & roundBitsMask) == 0)
            {
                // If that overflowed and the rest of the fractional bits are zero
                // then we were exactly x.5 and we want to round to the even result

                bits &= ~lastBitMask;
            }
            else
            {
                // Otherwise, we just want to strip the fractional bits off, truncating
                // to the current integer value.

                bits &= ~roundBitsMask;
            }

            return BitConverter.Int32BitsToSingle((int)bits);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Round(float x, int digits)
        {
            return Round(x, digits, MidpointRounding.ToEven);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Round(float x, MidpointRounding mode)
        {
            return Round(x, 0, mode);
        }

        public static unsafe float Round(float x, int digits, MidpointRounding mode)
        {
            if ((digits < 0) || (digits > maxRoundingDigits))
            {
                throw new ArgumentOutOfRangeException(nameof(digits), SR.ArgumentOutOfRange_RoundingDigits);
            }

            if (mode < MidpointRounding.ToEven || mode > MidpointRounding.ToPositiveInfinity)
            {
                throw new ArgumentException(SR.Format(SR.Argument_InvalidEnumValue, mode, nameof(MidpointRounding)), nameof(mode));
            }

            if (Abs(x) < singleRoundLimit)
            {
                float power10 = roundPower10Single[digits];

                x *= power10;

                switch (mode)
                {
                    // Rounds to the nearest value; if the number falls midway,
                    // it is rounded to the nearest value with an even least significant digit
                    case MidpointRounding.ToEven:
                    {
                        x = Round(x);
                        break;
                    }
                    // Rounds to the nearest value; if the number falls midway,
                    // it is rounded to the nearest value above (for positive numbers) or below (for negative numbers)
                    case MidpointRounding.AwayFromZero:
                    {
                        float fraction = ModF(x, &x);

                        if (Abs(fraction) >= 0.5)
                        {
                            x += Sign(fraction);
                        }

                        break;
                    }
                    // Directed rounding: Round to the nearest value, toward to zero
                    case MidpointRounding.ToZero:
                    {
                        x = Truncate(x);
                        break;
                    }
                    // Directed Rounding: Round down to the next value, toward negative infinity
                    case MidpointRounding.ToNegativeInfinity:
                    {
                        x = Floor(x);
                        break;
                    }
                    // Directed rounding: Round up to the next value, toward positive infinity
                    case MidpointRounding.ToPositiveInfinity:
                    {
                        x = Ceiling(x);
                        break;
                    }
                    default:
                    {
                        throw new ArgumentException(SR.Format(SR.Argument_InvalidEnumValue, mode, nameof(MidpointRounding)), nameof(mode));
                    }
                }

                x /= power10;
            }

            return x;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Sign(float x)
        {
            return Math.Sign(x);
        }

        public static unsafe float Truncate(float x)
        {
            ModF(x, &x);
            return x;
        }

        public static float Sin(float x)
        {
            double result = x;

            if (float.IsFinite(x))
            {
                double ax = Math.Abs(x);

                if (ax <= PiOverFour)
                {
                    if (ax >= TwoPowNegSeven)
                    {
                        result = SinTaylorSeriesFourIterations(x);
                    }
                    else if (ax >= TwoPowNegThirteen)
                    {
                        result = SinTaylorSeriesOneIteration(x);
                    }
                    else
                    {
                        result = x;
                    }
                }
                else
                {
                    int wasNegative = 0;

                    if (float.IsNegative(x))
                    {
                        x = -x;
                        wasNegative = 1;
                    }

                    int region;

                    if (x < 16000000.0)
                    {
                        // Reduce x to be in the range of -(PI / 4) to (PI / 4), inclusive

                        // This is done by subtracting multiples of (PI / 2). Double-precision
                        // isn't quite accurate enough and introduces some error, but we account
                        // for that using a tail value that helps account for this.

                        long axExp = BitConverter.DoubleToInt64Bits(ax) >> 52;

                        region = (int)(x * TwoOverPi + 0.5);
                        double piOverTwoCount = region;

                        double rHead = x - (piOverTwoCount * PiOverTwoPartOne);
                        double rTail = (piOverTwoCount * PiOverTwoPartOneTail);

                        double r = rHead - rTail;
                        long rExp = (BitConverter.DoubleToInt64Bits(r) << 1) >> 53;

                        if ((axExp - rExp) > 15)
                        {
                            // The remainder is pretty small compared with x, which implies that x is
                            // near a multiple of (PI / 2). That is, x matches the multiple to at least
                            // 15 bits and so we perform an additional fixup to account for any error

                            r = rHead;

                            rTail = (piOverTwoCount * PiOverTwoPartTwo);
                            rHead = r - rTail;
                            rTail = (piOverTwoCount * PiOverTwoPartTwoTail) - ((r - rHead) - rTail);

                            r = rHead - rTail;
                        }

                        if (rExp >= 0x3F2)      // r >= 2^-13
                        {
                            if ((region & 1) == 0)  // region 0 or 2
                            {
                                result = SinTaylorSeriesFourIterations(r);
                            }
                            else                    // region 1 or 3
                            {
                                result = CosTaylorSeriesFourIterations(r);
                            }
                        }
                        else if (rExp > 0x3DE)  // r > 1.1641532182693481E-10
                        {
                            if ((region & 1) == 0)  // region 0 or 2
                            {
                                result = SinTaylorSeriesOneIteration(r);
                            }
                            else                    // region 1 or 3
                            {
                                result = CosTaylorSeriesOneIteration(r);

                            }
                        }
                        else
                        {
                            if ((region & 1) == 0)  // region 0 or 2
                            {
                                result = r;
                            }
                            else                    // region 1 or 3
                            {
                                result = 1;
                            }
                        }
                    }
                    else
                    {
                        double r = ReduceForLargeInput(x, out region);

                        if ((region & 1) == 0)  // region 0 or 2
                        {
                            result = SinTaylorSeriesFourIterations(r);
                        }
                        else                    // region 1 or 3
                        {
                            result = CosTaylorSeriesFourIterations(r);
                        }
                    }

                    region >>= 1;

                    int tmp1 = region & wasNegative;

                    region = ~region;
                    wasNegative = ~wasNegative;

                    int tmp2 = region & wasNegative;

                    if (((tmp1 | tmp2) & 1) == 0)
                    {
                        // If the original region was 0/1 and arg is negative, then we negate the result.
                        // -or-
                        // If the original region was 2/3 and arg is positive, then we negate the result.

                        result = -result;
                    }
                }
            }

            return (float)result;
        }

        public static float Cos(float x)
        {
            double result = x;

            if (float.IsFinite(x))
            {
                double ax = Math.Abs(x);

                if (ax <= PiOverFour)
                {
                    if (ax >= TwoPowNegSeven)
                    {
                        result = CosTaylorSeriesFourIterations(x);
                    }
                    else if (ax >= TwoPowNegThirteen)
                    {
                        result = CosTaylorSeriesOneIteration(x);
                    }
                    else
                    {
                        result = x;
                    }
                }
                else
                {
                    if (float.IsNegative(x))
                    {
                        x = -x;
                    }

                    int region;

                    if (x < 16000000.0)
                    {
                        // Reduce x to be in the range of -(PI / 4) to (PI / 4), inclusive

                        // This is done by subtracting multiples of (PI / 2). Double-precision
                        // isn't quite accurate enough and introduces some error, but we account
                        // for that using a tail value that helps account for this.

                        long axExp = BitConverter.DoubleToInt64Bits(ax) >> 52;

                        region = (int)(x * TwoOverPi + 0.5);
                        double piOverTwoCount = region;

                        double rHead = x - (piOverTwoCount * PiOverTwoPartOne);
                        double rTail = (piOverTwoCount * PiOverTwoPartOneTail);

                        double r = rHead - rTail;
                        long rExp = (BitConverter.DoubleToInt64Bits(r) << 1) >> 53;

                        if ((axExp - rExp) > 15)
                        {
                            // The remainder is pretty small compared with x, which implies that x is
                            // near a multiple of (PI / 2). That is, x matches the multiple to at least
                            // 15 bits and so we perform an additional fixup to account for any error

                            r = rHead;

                            rTail = (piOverTwoCount * PiOverTwoPartTwo);
                            rHead = r - rTail;
                            rTail = (piOverTwoCount * PiOverTwoPartTwoTail) - ((r - rHead) - rTail);

                            r = rHead - rTail;
                        }

                        if (rExp >= 0x3F2)      // r >= 2^-13
                        {
                            if ((region & 1) == 0)  // region 0 or 2
                            {
                                result = CosTaylorSeriesFourIterations(r);
                            }
                            else                    // region 1 or 3
                            {
                                result = SinTaylorSeriesFourIterations(r);
                            }
                        }
                        else if (rExp > 0x3DE)  // r > 1.1641532182693481E-10
                        {
                            if ((region & 1) == 0)  // region 0 or 2
                            {
                                result = CosTaylorSeriesOneIteration(r);
                            }
                            else                    // region 1 or 3
                            {
                                result = SinTaylorSeriesOneIteration(r);

                            }
                        }
                        else
                        {
                            if ((region & 1) == 0)  // region 0 or 2
                            {
                                result = r;
                            }
                            else                    // region 1 or 3
                            {
                                result = 1;
                            }
                        }
                    }
                    else
                    {
                        double r = ReduceForLargeInput(x, out region);

                        if ((region & 1) == 0)  // region 0 or 2
                        {
                            result = CosTaylorSeriesFourIterations(r);
                        }
                        else                    // region 1 or 3
                        {
                            result = SinTaylorSeriesFourIterations(r);
                        }
                    }

                    if (region == 1 || region == 2)
                        result = -result;

                }
            }

            return (float)result;
        }

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
