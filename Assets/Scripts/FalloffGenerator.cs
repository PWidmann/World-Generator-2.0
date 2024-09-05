using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class FalloffGenerator
{
    public float[,] GenerateFalloffMapCircle(int width, int height, float falloffValue_a, float falloffValue_b)
    {
        // Flattened 2D array as a NativeArray<float>
        NativeArray<float> falloffMap = new NativeArray<float>(width * height, Allocator.TempJob);

        // Create and schedule the job
        var job = new FalloffMapJob
        {
            width = width,
            height = height,
            falloffValue_a = falloffValue_a,
            falloffValue_b = falloffValue_b,
            falloffMap = falloffMap
        };

        JobHandle jobHandle = job.Schedule(width * height, 64);
        jobHandle.Complete();

        // Convert the flattened NativeArray back to a 2D float array
        float[,] resultMap = new float[width, height];
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                resultMap[i, j] = falloffMap[i * height + j];
            }
        }

        // Dispose of the NativeArray to avoid memory leaks
        falloffMap.Dispose();

        return resultMap;
    }

    [BurstCompile]
    struct FalloffMapJob : IJobParallelFor
    {
        public int width;
        public int height;
        public float falloffValue_a;
        public float falloffValue_b;

        [WriteOnly]
        public NativeArray<float> falloffMap;

        public void Execute(int index)
        {
            // Calculate the 2D coordinates from the flat index
            int x = index / height;
            int y = index % height;

            // Calculate the distance to the center
            float distance = Vector2.Distance(new Vector2(x, y), new Vector2(width / 2f, height / 2f));

            // Normalize the distance using the GetNormalizedValue logic
            float normalizedValue = GetNormalizedValue(distance, 0, width / 2f);

            // Assign the normalized value to the falloff map
            falloffMap[index] = normalizedValue;
        }

        private float GetNormalizedValue(float value, float min, float max)
        {
            return (value - min) / (max - min);
        }
    }
}
