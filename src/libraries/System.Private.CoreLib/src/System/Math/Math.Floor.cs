using System.Runtime.CompilerServices;

namespace System
{
    public static partial class Math
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal Floor(decimal d)
        {
            return decimal.Floor(d);
        }
    }
}
