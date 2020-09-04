using System.Diagnostics.CodeAnalysis;

namespace System
{
    public static partial class Math
    {
        [DoesNotReturn]
        private static void ThrowMinMaxException<T>(T min, T max)
        {
            throw new ArgumentException(SR.Format(SR.Argument_MinMaxValue, min, max));
        }
    }
}
