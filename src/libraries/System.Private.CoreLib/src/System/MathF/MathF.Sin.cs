namespace System
{
    public static partial class MathF
    {
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
    }
}
