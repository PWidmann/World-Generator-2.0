using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
struct ChunkFalloffMapJob : IJob
{
    public int chunkSize;
    public int mapSize;
    public int2 chunkWorldPos;
    public NativeArray<float> falloffMap;

    private float2 currentPos;
    private float2 mapMiddle;
    private float distance;
    private float height;

    public void Execute()
    {
        // Generate a FalloffMap based on chunk position and world middle point

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                currentPos = new float2((chunkWorldPos.x) + x, (chunkWorldPos.y) + y);
                mapMiddle = new float2(mapSize / 2f, mapSize / 2f);
                distance = math.distance(currentPos, mapMiddle);
                height = distance / (mapSize / 2f);
                falloffMap[x * chunkSize + y] = height;
            }
        }
    }
}
