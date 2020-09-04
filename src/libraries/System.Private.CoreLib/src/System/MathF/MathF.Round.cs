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
    }
}
