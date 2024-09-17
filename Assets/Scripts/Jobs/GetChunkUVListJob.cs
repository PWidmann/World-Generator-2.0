using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
struct GetChunkUVListJob : IJob
{
    public NativeArray<float2> uv;
    public int chunkSize;

    public void Execute()
    {
        for (int i = 0, z = 0; z <= chunkSize; z++)
        {
            for (int x = 0; x <= chunkSize; x++)
            {
                uv[i] = new float2(x / (float)chunkSize, z / (float)chunkSize);
                i++;
            }
        }
    }
}
