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
    public static float[,] GenerateFalloffMapCircle(int mapSize)
    {
        // Flattened 2D array as a NativeArray<float>
        NativeArray<float> falloffMap = new NativeArray<float>(mapSize * mapSize, Allocator.TempJob);

        // Create and schedule the job
        var job = new FalloffMapJob
        {
            width = mapSize,
            height = mapSize,
            falloffMap = falloffMap
        };

        JobHandle jobHandle = job.Schedule(mapSize * mapSize, 128);
        jobHandle.Complete();

        // Convert the flattened NativeArray back to a 2D float array
        float[,] resultMap = new float[mapSize, mapSize];
        for (int i = 0; i < mapSize; i++)
        {
            for (int j = 0; j < mapSize; j++)
            {
                resultMap[i, j] = falloffMap[i * mapSize + j];
            }
        }

        // Dispose of the NativeArray to avoid memory leaks
        falloffMap.Dispose();

        return resultMap;
    }

    public static float[] Flatten(float[,] array)
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

    public static float[,] Unflatten(float[] flatArray, int rows, int cols)
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
}
