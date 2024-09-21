using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
struct GetChunkGenerationJob : IJob
{
    public NativeArray<int> chunkGenerationCheck;
    public int chunkSize;
    public int2 playerChunkPos;
    public NativeArray<int2> checkRadius;

    


    public void Execute()
    {
        checkRadius = new NativeArray<int2>(9, Allocator.Temp);

        checkRadius[0] = new int2(0, 0); // Middle
        checkRadius[1] = new int2(1, 0); // Right
        checkRadius[2] = new int2(0, -1); // Down
        checkRadius[3] = new int2(-1, 0); // Left
        checkRadius[4] = new int2(0, 1); // Up
        checkRadius[5] = new int2(1, 1); // Up Right
        checkRadius[6] = new int2(1, -1); // Right Down
        checkRadius[7] = new int2(-1, -1); // Left Down
        checkRadius[8] = new int2(-1, 1); // Left Up

        // * chunkSize on X because flattened array;
        foreach (int2 checkPos in checkRadius)
        {
            if (chunkGenerationCheck[checkPos.x * chunkSize + checkPos.y] == 0)
            {
                chunkGenerationCheck[checkPos.x * chunkSize + checkPos.y] = 1;


            };
        }
    }
}
