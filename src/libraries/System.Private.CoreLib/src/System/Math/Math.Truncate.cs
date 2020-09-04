using System.Runtime.CompilerServices;

namespace System
{
    public static partial class Math
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal Truncate(decimal d)
        {
            return decimal.Truncate(d);
        }

        public static unsafe double Truncate(double d)
        {
            ModF(d, &d);
            return d;
        }
    }
}
