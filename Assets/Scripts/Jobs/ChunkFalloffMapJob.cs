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
    
    public void Execute()
    {
        // Generate a FalloffMap based on chunk position and world middle point

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                float2 currentPos = new float2((chunkWorldPos.x) + x, (chunkWorldPos.y) + y);
                float2 mapMiddle = new float2(mapSize / 2f, mapSize / 2f);
                float distance = math.distance(currentPos, mapMiddle);
                float height01 = distance / (mapSize / 2f);
                falloffMap[x * chunkSize + y] = height01;
            }
        }
    }
}
