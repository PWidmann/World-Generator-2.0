using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
struct ChunkVerticesJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float> tempNoiseValues;
    [ReadOnly] public NativeArray<float> tempNoiseValues2;
    [ReadOnly] public NativeArray<float> falloffMap;
   

    public NativeArray<float3> tempVertices;

    public float2 terrainWorldPosition;
    public float2 worldChunkPos;
    public float worldBorderDistance;
    public float3 worldMiddlePoint;
    public float heightScale;
    public int chunkSize;
    public int falloffMapWidth;
    public int falloffMapHeight;

    public void Execute(int index)
    {
        int x = index % (chunkSize + 1);
        int z = index / (chunkSize + 1);

        int noiseIndex = z * (chunkSize + 1) + x;

        float currentVertHeight = tempNoiseValues[noiseIndex];

        int falloffX = (int)terrainWorldPosition.x + x;
        int falloffZ = (int)terrainWorldPosition.y + z;
        int falloffIndex = falloffZ * falloffMapWidth + falloffX;

        float falloffValue = falloffMap[falloffIndex];

        currentVertHeight = math.clamp(currentVertHeight - falloffValue, 0f, 1f);
        currentVertHeight *= tempNoiseValues2[noiseIndex];

        // Evaluate the animation curve using keyframes
        

        
        currentVertHeight *= tempNoiseValues[noiseIndex];

        // Border generation logic
        float3 vertexWorldPos = new float3(worldChunkPos.x + x, 0, worldChunkPos.y + z);
        float distance = math.distance(vertexWorldPos, worldMiddlePoint);

        float3 outputVertex = new float3(0, 0, 0);

        if (distance <= worldBorderDistance)
        {
            outputVertex = new float3(x, currentVertHeight, z);
        }
        else
        {
            float3 towardsMiddle = worldMiddlePoint - vertexWorldPos;
            float distanceVertexToBorder = distance - worldBorderDistance;

            if (distance < worldBorderDistance + 2)
            {
                outputVertex = new float3(x, 1f, z);
            }
            else
            {
                if (distance >= worldBorderDistance + 2)
                {
                    outputVertex = new float3(x, 0f - distanceVertexToBorder, z);
                }

                if (distance >= worldBorderDistance + 3.5f)
                {
                    float3 wasPos = new float3(x, -3.5f, z);
                    outputVertex = wasPos + math.normalize(towardsMiddle) * distanceVertexToBorder;
                }
            }
        }

        tempVertices[index] = outputVertex;
    }

}
