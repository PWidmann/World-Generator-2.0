using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using Unity.Mathematics;
using System;

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



    public PerlinNoise(float frequency, float amplitude, float lacunarity, float persistance, int octaves)
    {
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
   
    public float[,] GetNoiseValues(int xPos, int yPos, int chunkSize, float scale, int seed)
    {
        float[,] noiseValues = new float[chunkSize + 1, chunkSize + 1];

        int xMinPos = xPos;
        int yMinPos = yPos;

        for (int x = 0; x < noiseValues.GetLength(0); x++)
        {
            for (int y = 0; y < noiseValues.GetLength(1); y++)
            {
                noiseValues[x, y] = 0;

                float tempAmplitude = amplitude;
                float tempFrequency = frequency;

                for (int k = 0; k < octaves; k++)
                {
                    noiseValues[x, y] += Mathf.PerlinNoise(((xMinPos + x) / scale * frequency) * amplitude + seed, ((yMinPos + y) / scale * frequency) * amplitude + seed);
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

                noiseValues[x, y] = Mathf.InverseLerp(8, 0, noiseValues[x, y]);
            }
        }

        return noiseValues;
    }
}
