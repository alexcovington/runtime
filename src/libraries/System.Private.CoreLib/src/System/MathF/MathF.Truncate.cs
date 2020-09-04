namespace System
{
    public static partial class MathF
    {
        public static unsafe float Truncate(float x)
        {
            ModF(x, &x);
            return x;
        }
    }
}
