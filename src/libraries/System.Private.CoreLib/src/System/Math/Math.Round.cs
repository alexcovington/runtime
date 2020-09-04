using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace System
{
    public static partial class Math
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal Round(decimal d)
        {
            return decimal.Round(d, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal Round(decimal d, int decimals)
        {
            return decimal.Round(d, decimals);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal Round(decimal d, MidpointRounding mode)
        {
            return decimal.Round(d, 0, mode);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal Round(decimal d, int decimals, MidpointRounding mode)
        {
            return decimal.Round(d, decimals, mode);
        }

        [Intrinsic]
        public static double Round(double a)
        {
            // ************************************************************************************
            // IMPORTANT: Do not change this implementation without also updating MathF.Round(float),
            //            FloatingPointUtils::round(double), and FloatingPointUtils::round(float)
            // ************************************************************************************

            // This is based on the 'Berkeley SoftFloat Release 3e' algorithm

            ulong bits = (ulong)BitConverter.DoubleToInt64Bits(a);
            int exponent = double.ExtractExponentFromBits(bits);

            if (exponent <= 0x03FE)
            {
                if ((bits << 1) == 0)
                {
                    // Exactly +/- zero should return the original value
                    return a;
                }

                // Any value less than or equal to 0.5 will always round to exactly zero
                // and any value greater than 0.5 will always round to exactly one. However,
                // we need to preserve the original sign for IEEE compliance.

                double result = ((exponent == 0x03FE) && (double.ExtractSignificandFromBits(bits) != 0)) ? 1.0 : 0.0;
                return CopySign(result, a);
            }

            if (exponent >= 0x0433)
            {
                // Any value greater than or equal to 2^52 cannot have a fractional part,
                // So it will always round to exactly itself.

                return a;
            }

            // The absolute value should be greater than or equal to 1.0 and less than 2^52
            Debug.Assert((0x03FF <= exponent) && (exponent <= 0x0432));

            // Determine the last bit that represents the integral portion of the value
            // and the bits representing the fractional portion

            ulong lastBitMask = 1UL << (0x0433 - exponent);
            ulong roundBitsMask = lastBitMask - 1;

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

            return BitConverter.Int64BitsToDouble((long)bits);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Round(double value, int digits)
        {
            return Round(value, digits, MidpointRounding.ToEven);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Round(double value, MidpointRounding mode)
        {
            return Round(value, 0, mode);
        }

        public static unsafe double Round(double value, int digits, MidpointRounding mode)
        {
            if ((digits < 0) || (digits > maxRoundingDigits))
            {
                throw new ArgumentOutOfRangeException(nameof(digits), SR.ArgumentOutOfRange_RoundingDigits);
            }

            if (mode < MidpointRounding.ToEven || mode > MidpointRounding.ToPositiveInfinity)
            {
                throw new ArgumentException(SR.Format(SR.Argument_InvalidEnumValue, mode, nameof(MidpointRounding)), nameof(mode));
            }

            if (Abs(value) < doubleRoundLimit)
            {
                double power10 = roundPower10Double[digits];

                value *= power10;

                switch (mode)
                {
                    // Rounds to the nearest value; if the number falls midway,
                    // it is rounded to the nearest value with an even least significant digit
                    case MidpointRounding.ToEven:
                        {
                            value = Round(value);
                            break;
                        }
                    // Rounds to the nearest value; if the number falls midway,
                    // it is rounded to the nearest value above (for positive numbers) or below (for negative numbers)
                    case MidpointRounding.AwayFromZero:
                        {
                            double fraction = ModF(value, &value);

                            if (Abs(fraction) >= 0.5)
                            {
                                value += Sign(fraction);
                            }

                            break;
                        }
                    // Directed rounding: Round to the nearest value, toward to zero
                    case MidpointRounding.ToZero:
                        {
                            value = Truncate(value);
                            break;
                        }
                    // Directed Rounding: Round down to the next value, toward negative infinity
                    case MidpointRounding.ToNegativeInfinity:
                        {
                            value = Floor(value);
                            break;
                        }
                    // Directed rounding: Round up to the next value, toward positive infinity
                    case MidpointRounding.ToPositiveInfinity:
                        {
                            value = Ceiling(value);
                            break;
                        }
                    default:
                        {
                            throw new ArgumentException(SR.Format(SR.Argument_InvalidEnumValue, mode, nameof(MidpointRounding)), nameof(mode));
                        }
                }

                value /= power10;
            }

            return value;
        }

    }
}
