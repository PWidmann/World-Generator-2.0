using UnityEngine;
using System;
using Unity.Burst;

public class SimplexNoise
{
    private const int PERM_SIZE = 512;
    private const float SQRT3 = 1.7320508075688772f;
    private const float F2 = 0.5f * (SQRT3 - 1.0f);
    private const float G2 = (3.0f - SQRT3) / 6.0f;

    private int[] perm = new int[PERM_SIZE];

    private static Vector2[] grad2D = new Vector2[]
    {
        new Vector2(1, 1), new Vector2(-1, 1), new Vector2(1, -1), new Vector2(-1, -1),
        new Vector2(1, 0), new Vector2(-1, 0), new Vector2(1, 0), new Vector2(-1, 0),
        new Vector2(0, 1), new Vector2(0, -1), new Vector2(0, 1), new Vector2(0, -1)
    };

    public SimplexNoise()
    {
        for (int i = 0; i < PERM_SIZE / 2; i++)
        {
            perm[i] = i;
        }

        for (int i = 0; i < PERM_SIZE / 2 - 1; i++)
        {
            int j = UnityEngine.Random.Range(i, PERM_SIZE / 2);
            int temp = perm[i];
            perm[i] = perm[j];
            perm[j] = temp;
        }

        for (int i = 0; i < PERM_SIZE / 2; i++)
        {
            perm[PERM_SIZE / 2 + i] = perm[i];
        }
    }

    [BurstCompile]
    public float Noise(float x, float y)
    {
        float n0, n1, n2;

        float s = (x + y) * F2;
        int i = Mathf.FloorToInt(x + s);
        int j = Mathf.FloorToInt(y + s);

        float t = (i + j) * G2;
        float X0 = i - t;
        float Y0 = j - t;
        float x0 = x - X0;
        float y0 = y - Y0;

        int i1, j1;
        if (x0 > y0)
        {
            i1 = 1;
            j1 = 0;
        }
        else
        {
            i1 = 0;
            j1 = 1;
        }

        float x1 = x0 - i1 + G2;
        float y1 = y0 - j1 + G2;
        float x2 = x0 - 1.0f + 2.0f * G2;
        float y2 = y0 - 1.0f + 2.0f * G2;

        int ii = i & 255;
        int jj = j & 255;

        int gi0 = perm[ii + perm[jj]] % 12;
        int gi1 = perm[ii + i1 + perm[jj + j1]] % 12;
        int gi2 = perm[ii + 1 + perm[jj + 1]] % 12;

        float t0 = 0.5f - x0 * x0 - y0 * y0;
        if (t0 < 0)
        {
            n0 = 0.0f;
        }
        else
        {
            t0 *= t0;
            n0 = t0 * t0 * Vector2.Dot(grad2D[gi0], new Vector2(x0, y0));
        }

        float t1 = 0.5f - x1 * x1 - y1 * y1;
        if (t1 < 0)
        {
            n1 = 0.0f;
        }
        else
        {
            t1 *= t1;
            n1 = t1 * t1 * Vector2.Dot(grad2D[gi1], new Vector2(x1, y1));
        }

        float t2 = 0.5f - x2 * x2 - y2 * y2;
        if (t2 < 0)
        {
            n2 = 0.0f;
        }
        else
        {
            t2 *= t2;
            n2 = t2 * t2 * Vector2.Dot(grad2D[gi2], new Vector2(x2, y2));
        }

        return 70.0f * (n0 + n1 + n2) * 0.5f + 0.5f;
    }
}