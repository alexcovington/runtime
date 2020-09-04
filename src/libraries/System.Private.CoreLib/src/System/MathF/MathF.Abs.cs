using System.Runtime.CompilerServices;

namespace System
{
    public static partial class MathF
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Abs(float x)
        {
            return Math.Abs(x);
        }
    }
}
