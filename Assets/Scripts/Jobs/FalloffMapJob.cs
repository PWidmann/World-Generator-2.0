using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
struct FalloffMapJob : IJobParallelFor
{
    public int width;
    public int height;

    [WriteOnly]
    public NativeArray<float> falloffMap;

    public void Execute(int index)
    {
        // Calculate the 2D coordinates from the flat index
        int x = index / height;
        int y = index % height;

        // Calculate the distance to the center
        float distance = math.distance(new float2(x, y), new float2(width / 2f, height / 2f));

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
