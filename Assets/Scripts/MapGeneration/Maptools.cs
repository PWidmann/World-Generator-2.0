using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public static class Maptools
{
    /// <summary>
    /// UV mapping is the same for every chunk. This list can be cashed for all chunks.
    /// </summary>
    /// <param name="chunkSize"></param>
    /// <returns></returns>
    public static float2[] GetChunkUVList(int chunkSize)
    {
        // Is the same for every chunk
        NativeArray<float2> tempUVList = new NativeArray<float2>((chunkSize + 1) * (chunkSize + 1), Allocator.TempJob);
        var job = new GetChunkUVListJob
        {
            uv = tempUVList,
            chunkSize = chunkSize
        };
        JobHandle jobHandle = job.Schedule();
        jobHandle.Complete();
        float2[] uv = new float2[tempUVList.Length];

        for (int i = 0; i < tempUVList.Length; i++)
        {
            uv[i] = new float2(tempUVList[i].x, tempUVList[i].y);
        }

        tempUVList.Dispose();
        return uv;
    }

    /// <summary>
    /// Trianlge index mapping is the same for every chunk. This list can be cashed for all chunks.
    /// </summary>
    /// <param name="chunkSize"></param>
    /// <returns></returns>
    public static int[] GetChunkTriangleIndexList(int chunkSize)
    {
        // Is the same for every chunk
        NativeArray<int> tempTriangleList = new NativeArray<int>(chunkSize * chunkSize * 6, Allocator.TempJob);
        var job = new GetTriangleListJob
        {
            chunkSize = chunkSize,
            ChunkIndices = tempTriangleList
        };
        JobHandle jobHandle = job.Schedule();
        jobHandle.Complete();
        int[] triangleList = tempTriangleList.ToArray();
        tempTriangleList.Dispose();
        return triangleList;
    }

    [BurstCompile]
    public static float[,] GenerateChunkFalloffMap(int mapSize, int chunkSize, int2 terrainPos)
    {
        // Generate a FalloffMap based on chunk position and world middle point

        // Flattened array
        NativeArray<float> fallOffMap = new NativeArray<float>(chunkSize * chunkSize, Allocator.TempJob);

        ChunkFalloffMapJob job = new ChunkFalloffMapJob
        {
            mapSize = mapSize,
            chunkSize = chunkSize,
            chunkWorldPos = terrainPos,
            falloffMap = fallOffMap
        };

        JobHandle jobHandle = job.Schedule();
        jobHandle.Complete();

        // Convert the flattened NativeArray back to a 2D float array
        float[,] resultMap = new float[chunkSize, chunkSize];
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                resultMap[x, y] = fallOffMap[x * chunkSize + y];
            }
        }

        // Dispose of the NativeArray to avoid memory leaks
        fallOffMap.Dispose();

        return resultMap;
    }

    [BurstCompile]
    public static float3 BorderGeneration(int worldBorderDistance, float3 worldMiddlePoint, float3 tempVertex, float2 worldChunkPos, float currentHeight, int z, int x)
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

    public static bool ChunkGenerated(int[,] generatedChunks, int2 targetPos)
    {
        bool generated = false;

        for (int x = 0; x < generatedChunks.GetLength(1); x++)
        {
            for (int y = 0; y < generatedChunks.GetLength(0); y++)
            {
                if (generatedChunks[x, y] == 1) generated = true;
            }
        }

        return generated;
    }

    public static float[] FlattenFloat(float[,] array)
    {
        if (array == null)
            throw new ArgumentNullException(nameof(array), "Input array cannot be null.");

        int rows = array.GetLength(0);
        int cols = array.GetLength(1);
        float[] flatArray = new float[rows * cols];

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                flatArray[i * cols + j] = array[i, j];
            }
        }

        return flatArray;
    }

    public static NativeArray<int> FlattenInt(int[,] array)
    {
        if (array == null)
            throw new ArgumentNullException(nameof(array), "Input array cannot be null.");

        int rows = array.GetLength(0);
        int cols = array.GetLength(1);
        NativeArray<int> flatArray = new NativeArray<int>(rows * cols, Allocator.Temp);

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                flatArray[i * cols + j] = array[i, j];
            }
        }

        return flatArray;
    }

    public static float[,] UnflattenFloat(float[] flatArray, int rows, int cols)
    {
        if (flatArray == null)
            throw new ArgumentNullException(nameof(flatArray), "Input flat array cannot be null.");
        if (rows <= 0)
            throw new ArgumentOutOfRangeException(nameof(rows), "Number of rows must be positive.");
        if (cols <= 0)
            throw new ArgumentOutOfRangeException(nameof(cols), "Number of columns must be positive.");
        if (flatArray.Length != rows * cols)
            throw new ArgumentException("The length of the flat array does not match the specified dimensions.", nameof(flatArray));

        float[,] array = new float[rows, cols];
        
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                array[i, j] = flatArray[i * cols + j];
            }
        }

        return array;
    }

    
    public static Vector2[] Float2ToVector2Array(float2[] array)
    {
        Vector2[] vector2Array = new Vector2[array.Length];

        // Convert each float2 to Vector2
        for (int i = 0; i < array.Length; i++)
        {
            vector2Array[i] = new Vector2(array[i].x, array[i].y);
        }

        return vector2Array;
    }
}
