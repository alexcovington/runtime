namespace System
{
    public static partial class MathF
    {
        public const float E = 2.71828183f;

        public const float PI = 3.14159265f;

        public const float Tau = 6.283185307f;

        private const int maxRoundingDigits = 6;

        // This table is required for the Round function which can specify the number of digits to round to
        private static readonly float[] roundPower10Single = new float[] {
            1e0f, 1e1f, 1e2f, 1e3f, 1e4f, 1e5f, 1e6f
        };

        private const float singleRoundLimit = 1e8f;

        private const double PiOverTwo = 1.5707963267948966;
        private const double PiOverTwoPartOne = 1.5707963267341256;
        private const double PiOverTwoPartOneTail = 6.077100506506192E-11;
        private const double PiOverTwoPartTwo = 6.077100506303966E-11;
        private const double PiOverTwoPartTwoTail = 2.0222662487959506E-21;
        private const double PiOverFour = 0.7853981633974483;
        private const double TwoOverPi = 0.6366197723675814;
        private const double TwoPowNegSeven = 0.0078125;
        private const double TwoPowNegThirteen = 0.0001220703125;

        private const double C0 = -1.0 / 2.0;       // 1 / 2!
        private const double C1 = +1.0 / 24.0;      // 1 / 4!
        private const double C2 = -1.0 / 720.0;     // 1 / 6!
        private const double C3 = +1.0 / 40320.0;   // 1 / 8!
        private const double C4 = -1.0 / 3628800.0; // 1 / 10!

        private const double S1 = -1.0 / 6.0;       // 1 / 3!
        private const double S2 = +1.0 / 120.0;     // 1 / 5!
        private const double S3 = -1.0 / 5040.0;    // 1 / 7!
        private const double S4 = +1.0 / 362880.0;  // 1 / 9!

        private static readonly long[] PiBits = new long[]
        {
    0,
    5215,
    13000023176,
    11362338026,
    67174558139,
    34819822259,
    10612056195,
    67816420731,
    57840157550,
    19558516809,
    50025467026,
    25186875954,
    18152700886
        };

        private const float Two25 = 3.355443200e7f;
        private const float TwoM25 = 2.9802322388e-8f;
        private const float Huge = 1.0e30f;
        private const float Small = 1.0e-30f;
    }
}
