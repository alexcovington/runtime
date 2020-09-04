namespace System
{
    public static partial class MathF
    {
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
    }
}
