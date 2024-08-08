using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FalloffGenerator
{
    public static float[,] GenerateFalloffMap(int width, int height, float falloffValue_a, float falloffValue_b)
    {
        float[,] map = new float[width, height];

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                float x = i / (float)width * 2 - 1;
                float y = j / (float)height * 2 - 1;

                float value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                map[i, j] = Evaluate(value, falloffValue_a, falloffValue_b);

            }
        }

        return map;
    }

    public static float[,] GenerateFalloffMapCircle(int width, int height, float falloffValue_a, float falloffValue_b)
    {
        float[,] map = new float[width, height];
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                float distance = Vector2.Distance(new Vector2(i, j), new Vector2(width / 2, height / 2));
                map[i, j] = GetNormalizedValue(distance, 0, width / 2);
            }
        }

        return map;
    }

    public static float GetNormalizedValue(float value, float min, float max)
    {
        return ((value - min) / (max - min));
    }

    static float Evaluate(float value, float falloff_a, float falloff_b)
    {
        // FOR SMOOTH FALLOFF MAP
        float a = falloff_a;
        float b = falloff_b;

        return Mathf.Pow(value, a) / (Mathf.Pow(value, a) + Mathf.Pow(b - b * value, a));
    }
}
