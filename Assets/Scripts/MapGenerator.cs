using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


public struct ChunkData
{
    public int chunkID;
    public Vector3[] verticesOld;
    public int[] trianglesOld;
    public Vector2[] uvs;
    public Vector2Int terrainWorldPosition;
    public NativeArray<Vector3> chunkVertices;
}

[Serializable]
public struct NoiseLayer
{
    public string name;
    public float heightScale;
    public int seed;
    public float frequency;
    public float amplitude;
    public float lacunarity;
    public float persistance;
    public int octaves;
    public float scale;
}

public class MapGenerator : MonoBehaviour
{
    [Header("Map Settings")]
    [SerializeField] private int _mapSize = 500;
    [SerializeField] private int _chunkSize = 100;
    [SerializeField] private bool useFalloff = false;
    [SerializeField] private GameObject gameWorld;
    [SerializeField] private Material terrainMaterial;
    [SerializeField] private AnimationCurve animCurve;
    [SerializeField] private GameObject terrainChunkPrefab;
    [SerializeField] private NoiseLayer[] noiseLayers;
    [SerializeField] private float fallOffValueA = 3;
    [SerializeField] private float fallOffValueB = 2.2f;
    //[SerializeField] private NoiseVisualizer noiseVisualizer;
    [SerializeField] private Transform startPosition;


    private int chunksPerRow;
    private PerlinNoise[] noises;
    private float[,] falloffMap;
    float minMapHeight = 1000;
    float maxMapHeight = 0;

    private List<GameObject> chunks = new List<GameObject>();
    GameObject tempObj;

    private List<ChunkData> chunkDataList = new List<ChunkData>();
    float startTime3;

    private List<PerlinNoise> layerNoises = new List<PerlinNoise>();

    // temps
    private Mesh tempMesh;
    int[] tempTriangleList;
    Vector2[] tempUvList;
    Vector3[] tempVertices;
    float[,] tempNoiseValues;
    float[,] tempNoiseValues2;
    float[,] tempNoiseValues3;
    float currentVertHeight;

    int[,] chunksGeneratedCheck;

    List<NativeArray<Vector3>> terrainVerts = new List<NativeArray<Vector3>>();

    FalloffGenerator falloffGenerator = new FalloffGenerator();

    private void Start()
    {
        GenerateNewMap();
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.U))
        {
            GenerateNewMap();
        }
    }

    public void GenerateNewMap()
    {
        chunkDataList.Clear();

        foreach (Transform t in gameWorld.transform)
        {
            Destroy(t.gameObject);
        }

        chunksPerRow = _mapSize / _chunkSize;
        chunksGeneratedCheck = new int[chunksPerRow, chunksPerRow];
        for (int x = 0; x < chunksGeneratedCheck.GetLength(0); x++)
        {
            for (int y = 0; y < chunksGeneratedCheck.GetLength(1); y++)
            {
                chunksGeneratedCheck[x, y] = 0;
            }
        }

        layerNoises.Clear();


        foreach (NoiseLayer layer in noiseLayers)
        {
            PerlinNoise noise = new PerlinNoise(layer.seed, layer.frequency, layer.amplitude, layer.lacunarity, layer.persistance, layer.octaves);
            layerNoises.Add(noise);

        }

        float startTime = Time.realtimeSinceStartup;

        // Jobs
        falloffMap = falloffGenerator.GenerateFalloffMapCircle(_mapSize + chunksPerRow, _mapSize + chunksPerRow, fallOffValueA, fallOffValueB); // complete map falloff map
        Debug.Log("instantiating 'complete map falloffMap' took: " + ((Time.realtimeSinceStartup - startTime) * 1000f) + "ms");
        float startTime2 = Time.realtimeSinceStartup;
        tempTriangleList = GetTriangleList(_chunkSize); // Triangle indeces are the same for every chunk
        tempUvList = GetUVList(_chunkSize); // UV lists are the same for every chunk

        


        // No jobs yet
        GenerateChunkDataNaked(_mapSize, _chunkSize, tempTriangleList, tempUvList);
        Debug.Log("Generating chunk data without vertices: " + ((Time.realtimeSinceStartup - startTime2) * 1000f) + "ms");



        for (int i = 0, z = 0; z < (_mapSize / _chunkSize); z++)
        {
            for (int x = 0; x < (_mapSize / _chunkSize); x++)
            {
                ChunkData data = chunkDataList.ElementAt(i);
                //chunkDataList[i].trianglesOld = tempTriangleList;
                data.verticesOld = AddChunkVerticesToChunkData(data.terrainWorldPosition, _mapSize, _chunkSize);
                chunkDataList.RemoveAt(i);
                chunkDataList.Insert(i, data);
                i++;
            }
        }
        Debug.Log("Generating complete chunk data: " + ((Time.realtimeSinceStartup - startTime2) * 1000f) + "ms");
        
        startTime3 = Time.realtimeSinceStartup;
        StartCoroutine(GenerateChunkObjects());
    }

    // The whole map
    private void GenerateChunkDataNaked(int mapSize, int chunkSize, int[] tempTriangleList, Vector2[] tempUvList)
    {
        for (int i = 0, z = 0; z < (mapSize / chunkSize); z++)
        {
            for (int x = 0; x < (mapSize / chunkSize); x++)
            {
                ChunkData chunkData = new ChunkData();
                chunkData.chunkID = i;
                chunkData.trianglesOld = tempTriangleList;
                chunkData.uvs = tempUvList;
                chunkData.terrainWorldPosition = new Vector2Int(x * chunkSize, z * chunkSize);
                chunkDataList.Add(chunkData);
                i++;
            }
        }
    }

    private ChunkData AddJunkVertices(ChunkData currentJunkData, int mapSize, int chunkSize)
    {
        currentJunkData.verticesOld = AddChunkVerticesToChunkData(currentJunkData.terrainWorldPosition, mapSize, chunkSize);
        return currentJunkData;
    }
                

    private Vector3[] AddChunkVerticesToChunkData(Vector2Int chunkDataTerrainWorldPosition, int mapSize, int chunkSize)
    {
        Vector3[] verticesOld = GetChunkVertices(chunkDataTerrainWorldPosition, chunkSize);
        return verticesOld;
    }

    


    [BurstCompile]
    private Vector3[] GetChunkVertices(Vector2Int terrainWorldPosition, int chunkSize)
    {
        tempVertices = new Vector3[(chunkSize + 1) * (chunkSize + 1)];

        tempNoiseValues = layerNoises[0].GetNoiseValues(terrainWorldPosition.x, terrainWorldPosition.y, chunkSize, noiseLayers[0].scale);
        tempNoiseValues2 = layerNoises[1].GetNoiseValues(terrainWorldPosition.x, terrainWorldPosition.y, chunkSize, noiseLayers[1].scale);
        tempNoiseValues3 = layerNoises[2].GetNoiseValues(terrainWorldPosition.x, terrainWorldPosition.y, chunkSize, noiseLayers[2].scale);


        for (int i = 0, z = 0; z <= chunkSize; z++)
        {
            for (int x = 0; x <= chunkSize; x++)
            {
                currentVertHeight = tempNoiseValues[x, z];
                currentVertHeight = SubtractingFalloff(currentVertHeight, terrainWorldPosition.x, terrainWorldPosition.y, x, z);
                currentVertHeight *= tempNoiseValues2[x, z];
                currentVertHeight = animCurve.Evaluate(currentVertHeight);
                currentVertHeight = currentVertHeight * (noiseLayers[0].heightScale);

                if (currentVertHeight < minMapHeight) minMapHeight = currentVertHeight;
                if (currentVertHeight > maxMapHeight) maxMapHeight = currentVertHeight;

                tempVertices[i] = new Vector3(x, currentVertHeight, z);
                i++;
            }
        }

        return tempVertices;
    }

    [BurstCompile]
    float SubtractingFalloff(float oldHeight, int terrainXpos, int terrainZpos, int x, int z)
    {
        return Mathf.Clamp01(oldHeight - falloffMap[terrainXpos + x, terrainZpos + z]);
    }

    private void SetMapToGround(float minHeight)
    {
        foreach (ChunkData data in chunkDataList)
        {
            for (int i = 0; i < data.verticesOld.Length; i++)
            {
                data.verticesOld[i] = new Vector3(data.verticesOld[i].x, data.verticesOld[i].y - minHeight, data.verticesOld[i].z);
            }
        }
    }

    private IEnumerator GenerateChunkObjects()
    {
        //SetMapToGround(minMapHeight);


        foreach (ChunkData chunkData in chunkDataList)
        {
            float startTime = Time.realtimeSinceStartup;
            GameObject terrainChunk = Instantiate(terrainChunkPrefab);

            tempMesh = new Mesh();
            tempMesh.SetVertices(chunkData.verticesOld);
            tempMesh.SetTriangles(chunkData.trianglesOld, 0);
            tempMesh.SetUVs(0, chunkData.uvs);
            tempMesh.RecalculateNormals();

            terrainChunk.GetComponent<MeshRenderer>().sharedMaterial = terrainMaterial;
            terrainChunk.GetComponent<MeshFilter>().mesh = tempMesh;
            terrainChunk.GetComponent<MeshCollider>().sharedMesh = tempMesh;



            terrainChunk.transform.SetParent(gameWorld.transform);
            terrainChunk.transform.position = new Vector3(chunkData.terrainWorldPosition.x, 0, chunkData.terrainWorldPosition.y);
            yield return null;
        }

        Debug.Log("Generating chunk objects took: " + ((Time.realtimeSinceStartup - startTime3) * 1000f) + "ms");
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

