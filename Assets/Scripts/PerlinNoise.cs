using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class PerlinNoise
{
    private int seed;

    private float frequency;
    private float amplitude;
    private float lacunarity; // gaps between patterns / lakes
    private float persistance;
    private int octaves;
    private float minVal;
    private float maxVal;

    public PerlinNoise(int seed, float frequency, float amplitude, float lacunarity, float persistance, int octaves)
    {
        this.seed = seed;
        this.frequency = frequency;
        this.amplitude = amplitude;
        this.lacunarity = lacunarity;
        this.persistance = persistance;
        this.octaves = octaves;

        maxVal = 0f;
        minVal = float.MaxValue;
    }

    /// <summary>
    /// Gets called per chunk
    /// </summary>
    public float[,] GetNoiseValues(int xPos, int yPos, int chunkSize)
    {
        float[,] noiseValues = new float[chunkSize + 1, chunkSize + 1];

        int xMinPos = xPos * chunkSize;
        int yMinPos = yPos * chunkSize;

        for (int x = 0; x < noiseValues.GetLength(0); x++)
        {
            for (int y = 0; y < noiseValues.GetLength(1); y++)
            {
                noiseValues[x, y] = 0;

                float tempAmplitude = amplitude;
                float tempFrequency = frequency;

                for (int k = 0; k < octaves; k++)
                {
                    noiseValues[x, y] += Mathf.PerlinNoise((xMinPos + x + seed) / 200f * frequency, (yMinPos + y + seed) / 200f * frequency) * amplitude;
                    frequency *= lacunarity;
                    amplitude *= persistance;
                }

                amplitude = tempAmplitude;
                frequency = tempFrequency;

                if (noiseValues[x, y] > maxVal)
                {
                    maxVal = noiseValues[x, y];
                }

                if (noiseValues[x, y] < minVal)
                {
                    minVal = noiseValues[x, y];
                }
            }
        }

        for (int i = 0; i < noiseValues.GetLength(0); i++)
        {
            for (int j = 0; j < noiseValues.GetLength(1); j++)
            {
                noiseValues[i, j] = Mathf.InverseLerp(8, 0, noiseValues[i, j]);
            }
        }

        return noiseValues;
    }
}
