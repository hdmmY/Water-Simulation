using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MathUtitly
{
    public static float Rectangle(float x)
    {
        float absX = Mathf.Abs(x);

        if (absX < 0.5f) return 1;

        if (Mathf.Approximately(x, 0.5f)) return 0.5f;

        return 0;
    }


}
