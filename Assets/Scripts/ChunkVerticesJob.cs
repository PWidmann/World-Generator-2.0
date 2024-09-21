using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
struct ChunkVerticesJob : IJob
{
    public float3[] TempVertices;
    public float[] TempNoiseValues;
    public float[] TempNoiseValues2;

    public int MapSize;
    public int ChunkSize;
    public int2 MapPos;






    public void Execute()
    {
        
    }


    [BurstCompile]
    public float3 BorderGeneration(int worldBorderDistance, float3 worldMiddlePoint, float3 tempVertex, float2 worldChunkPos, float currentHeight, int z, int x)
    {
        float3 output = tempVertex;

        // Border generation
        float3 vertextWorldPos = new float3(worldChunkPos.x + x, 0, worldChunkPos.y + z);
        float distance = math.distance(vertextWorldPos, worldMiddlePoint);

        if (distance <= worldBorderDistance)
        {
            output = new float3(x, currentHeight, z);
        }
        if (distance > worldBorderDistance)
        {

            float3 toWardsMiddle = worldMiddlePoint - vertextWorldPos;
            float distanceVertexToBorder = distance - worldBorderDistance;

            if (distance < worldBorderDistance + 2) output = new float3(x, 1f, z);
            else
            {
                if (distance >= worldBorderDistance + 2)
                {
                    output = new float3(x, 0 - distanceVertexToBorder, z);
                }

                if (distance >= worldBorderDistance + 3.5f)
                {
                    float3 wasPos = output = new float3(x, -3.5f, z);
                    output = wasPos + math.normalize(toWardsMiddle) * distanceVertexToBorder;
                }
            }
        }

        return output;
    }
}


