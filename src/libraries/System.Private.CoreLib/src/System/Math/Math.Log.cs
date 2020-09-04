namespace System
{
    public static partial class Math
    {
        public static double Log(double a, double newBase)
        {
            if (double.IsNaN(a))
            {
                return a; // IEEE 754-2008: NaN payload must be preserved
            }

            if (double.IsNaN(newBase))
            {
                return newBase; // IEEE 754-2008: NaN payload must be preserved
            }

            if (newBase == 1)
            {
                return double.NaN;
            }

            if ((a != 1) && ((newBase == 0) || double.IsPositiveInfinity(newBase)))
            {
                return double.NaN;
            }

            return Log(a) / Log(newBase);
        }
    }
}
