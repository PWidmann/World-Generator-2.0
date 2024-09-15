using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class WaterGenerator : MonoBehaviour
{
    public List<Mesh> waterMeshes;

    private float _mapSize = 0;
    private int _chunkSize = 0;
    private float _borderDistanceFromMiddle = 0;

    private int[] _triangleList;
    private Vector2[] _uvList;


    private Vector3 worldMiddlePoint;
    public WaterGenerator(int mapSize, int chunkSize, float borderDistance)
    {
        _mapSize = mapSize;
        _chunkSize = chunkSize;
        _borderDistanceFromMiddle = borderDistance;

        worldMiddlePoint = new Vector3(_mapSize / 2, _mapSize / 2);
    }

    public List<Mesh> GetWaterChunkMeshes()
    {
        _triangleList = GetTriangleList(_chunkSize);
        _uvList = GetUVList(_chunkSize);

        float chunksPerRow = _mapSize / _chunkSize;

        for (int x = 0; x <= chunksPerRow; x++)
        {
            for (int y = 0; y <= chunksPerRow; y++)
            {
                Mesh mesh = new Mesh();
                mesh.triangles = _triangleList;
                mesh.uv = _uvList;
                mesh.vertices = GetChunkVertices(new Vector2(x, y), _chunkSize);
                waterMeshes.Add(mesh);
            }
        }

        return waterMeshes;
    }

    private Vector3[] GetChunkVertices(Vector2 chunkPos, int chunkSize)
    {
        Vector3[] tempVertices = new Vector3[(chunkSize + 1) * (chunkSize + 1)];
        int i = 0;
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                //Vector3 vertextWorldPos = new Vector3((chunkPos.x * chunkSize)+ x, 0, (chunkPos.y * chunkSize) + y);
                //float distance = Vector3.Distance(vertextWorldPos, worldMiddlePoint);
                //Vector3 toWardsMiddle = worldMiddlePoint - vertextWorldPos;
                //float distanceVertexToBorder = distance - _borderDistanceFromMiddle;
                //
                //if (distance > _borderDistanceFromMiddle)
                //{
                //
                //    Vector3 wasPos = tempVertices[i] = new Vector3(x, 0, y);
                //    tempVertices[i] = wasPos + toWardsMiddle.normalized * distanceVertexToBorder;
                //}

                tempVertices[i] = new Vector3(chunkPos.x + x, 0, chunkPos.y + y);

                i++;
            }
        }

        return tempVertices;
    }

    [BurstCompile]
    private int[] GetTriangleList(int chunkSize)
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
    private Vector2[] GetUVList(int chunkSize)
    {
        // Is the same for every chunk
        NativeArray<Vector2> tempUVList = new NativeArray<Vector2>((chunkSize + 1) * (chunkSize + 1), Allocator.TempJob);
        var job = new GetUVListJob
        {
            uv = tempUVList,
            chunkSize = chunkSize
        };
        JobHandle jobHandle = job.Schedule();
        jobHandle.Complete();
        Vector2[] uv = tempUVList.ToArray<Vector2>();
        tempUVList.Dispose();
        return uv;
    }
}
