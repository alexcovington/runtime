using System.Runtime.CompilerServices;

namespace System
{
    public static partial class Math
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal Ceiling(decimal d)
        {
            return decimal.Ceiling(d);
        }
    }
}
