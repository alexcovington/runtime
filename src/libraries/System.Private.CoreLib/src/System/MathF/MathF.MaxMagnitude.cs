// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ===================================================================================================
// Portions of the code implemented below are based on the 'Berkeley SoftFloat Release 3e' algorithms.
// ===================================================================================================

/*============================================================
**
** Purpose: Some single-precision floating-point math operations
**
===========================================================*/

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics.Arm;

namespace System
{
    public static partial class MathF
    {
        public static float MaxMagnitude(float x, float y)
        {
            // This matches the IEEE 754:2019 `maximumMagnitude` function
            //
            // It propagates NaN inputs back to the caller and
            // otherwise returns the input with a larger magnitude.
            // It treats +0 as larger than -0 as per the specification.

            float ax = Abs(x);
            float ay = Abs(y);

            if ((ax > ay) || float.IsNaN(ax))
            {
                return x;
            }

            if (ax == ay)
            {
                return float.IsNegative(x) ? y : x;
            }

            return y;
        }
    }
}
