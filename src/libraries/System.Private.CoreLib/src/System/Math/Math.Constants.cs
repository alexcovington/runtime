namespace System
{
    public static partial class Math
    {
        public const double E = 2.7182818284590452354;

        public const double PI = 3.14159265358979323846;

        public const double Tau = 6.283185307179586476925;

        private const int maxRoundingDigits = 15;

        private const double doubleRoundLimit = 1e16d;

        // This table is required for the Round function which can specify the number of digits to round to
        private static readonly double[] roundPower10Double = new double[] {
          1E0, 1E1, 1E2, 1E3, 1E4, 1E5, 1E6, 1E7, 1E8,
          1E9, 1E10, 1E11, 1E12, 1E13, 1E14, 1E15
        };
    }
}
