using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;

[BurstCompile]
struct GetChunkVerticesJob : IJob
{
    public NativeArray<Vector3> tempVertices;
    public Vector2Int terrainWorldPosition;
    public int chunkSize;
    public NativeArray<float> fallOffMap;


    public void Execute()
    {
        
    }
}


