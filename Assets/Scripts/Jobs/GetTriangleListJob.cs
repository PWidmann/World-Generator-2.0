using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[BurstCompile]
struct GetTriangleListJob : IJob
{
    public NativeArray<int> ChunkIndices;
    public int chunkSize;

    public void Execute()
    {
        int vert = 0;
        int tris = 0;

        for (int z = 0; z < chunkSize; z++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                ChunkIndices[tris + 0] = vert + 0;
                ChunkIndices[tris + 1] = vert + chunkSize + 1;
                ChunkIndices[tris + 2] = vert + 1;

                ChunkIndices[tris + 3] = vert + 1;
                ChunkIndices[tris + 4] = vert + chunkSize + 1;
                ChunkIndices[tris + 5] = vert + chunkSize + 2;

                vert++;
                tris += 6;
            }

            vert++;
        }
    }
}
