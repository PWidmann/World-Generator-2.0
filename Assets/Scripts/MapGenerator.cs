using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


public struct ChunkData
{
    public int chunkID;
    public int[] TriangleIndexes;
    public float2[] UVs;
    public float3[] Vertices;
    public int2 ChunkWorldPosition;
}

[Serializable]
public struct NoiseLayer
{
    public bool randomSeed;
    public int seed;
    public int octaves;
    public string name;
    public float heightScale;
    public float frequency;
    public float amplitude;
    public float lacunarity;
    public float persistance;
    public float scale;
}

public class MapGenerator : MonoBehaviour
{
    [Header("Map Settings")]
    [SerializeField] bool noColliders = true;
    [SerializeField] private bool randomSeed = true;
    [SerializeField] private int seed = 1337;
    [SerializeField] private int _mapSize = 2000;
    [SerializeField] private int _chunkSize = 200;
    [SerializeField] private AnimationCurve terrainAnimCurve;
    [SerializeField] private NoiseLayer[] noiseLayers;
    
    [Header("References")]
    [SerializeField] private GameObject gameWorld;
    [SerializeField] private Material terrainMaterial;
    [SerializeField] private GameObject terrainChunkPrefab;
    [SerializeField] private GameObject terrainChunkPrefabNoCollider;
    [SerializeField] private Transform startPosition;
    [SerializeField] private GameObject waterObject;
    [SerializeField] private InputField mapSizeUIInput;
    [SerializeField] private Text genTimeText;
    [SerializeField] private Button genButton;
    [SerializeField] private Toggle colliderToggle;

    private int chunksPerRow;
    private float minMapHeight = 1000;
    private float maxMapHeight = 0;
    private int worldBorderDistance = 1000;
    private List<GameObject> chunks = new List<GameObject>();
    private List<PerlinNoise> layerNoises = new List<PerlinNoise>();
    private float3 worldMiddlePoint;
    private Mesh tempMesh;
    private int[] chunkVertexIndices;
    private float2[] chunkUVMap;
    private float3[] tempVertices;
    private float[,] falloffMap;
    private float[,] tempNoiseValues;
    private float[,] tempNoiseValues2;
    private float[,] tempNoiseValues3;
    private float currentVertHeight;
    private Queue<ChunkData> chunkDataList = new Queue<ChunkData>();
    private Queue<ChunkData> finalChunkDataList = new Queue<ChunkData>();
    private float startTime;
    private float totalGentime;
    private int chunksGenerated = 0;
    private int[,] chunkGenerationCheck;
    private bool readyToCheck = false;

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.U))
        {
            CreateNewMapData();
        }

        OnDemandChunkQueue();

        StartCoroutine(FinalizeChunkData());
        StartCoroutine(CreateTerrainChunks());
    }

    private void OnDemandChunkQueue()
    {
        if (readyToCheck)
        {
            int2 currentPosition = new int2((int)(startPosition.position.x / _mapSize * chunksPerRow), (int)(startPosition.position.z / _mapSize * chunksPerRow));

            if (chunkGenerationCheck[currentPosition.x, currentPosition.y] == 0)
            {
                chunkGenerationCheck[currentPosition.x, currentPosition.y] = 1;
                GenerateChunkData(currentPosition, _mapSize, _chunkSize, chunkVertexIndices, chunkUVMap);
            }

            if (chunkGenerationCheck[currentPosition.x + 1, currentPosition.y] == 0)
            {
                chunkGenerationCheck[currentPosition.x + 1, currentPosition.y] = 1;
                GenerateChunkData(currentPosition + new int2(1, 0), _mapSize, _chunkSize, chunkVertexIndices, chunkUVMap);
            }

            if (chunkGenerationCheck[currentPosition.x - 1, currentPosition.y] == 0)
            {
                chunkGenerationCheck[currentPosition.x - 1, currentPosition.y] = 1;
                GenerateChunkData(currentPosition + new int2(-1, 0), _mapSize, _chunkSize, chunkVertexIndices, chunkUVMap);
            }

            if (chunkGenerationCheck[currentPosition.x, currentPosition.y + 1] == 0)
            {
                chunkGenerationCheck[currentPosition.x, currentPosition.y + 1] = 1;
                GenerateChunkData(currentPosition + new int2(0, 1), _mapSize, _chunkSize, chunkVertexIndices, chunkUVMap);
            }

            if (chunkGenerationCheck[currentPosition.x, currentPosition.y - 1] == 0)
            {
                chunkGenerationCheck[currentPosition.x, currentPosition.y - 1] = 1;
                GenerateChunkData(currentPosition + new int2(0, -1), _mapSize, _chunkSize, chunkVertexIndices, chunkUVMap);
            }
        }
    }

    [BurstCompile]
    public void CreateNewMapData()
    {
        if (Convert.ToInt16(mapSizeUIInput.text) % _chunkSize != 0) return;
        startTime = Time.realtimeSinceStartup;
        chunksGenerated = 0;
        PrepareNewMap();

        falloffMap = Maptools.GenerateFalloffMapCircle(_mapSize + chunksPerRow); // complete map falloff map
        chunkUVMap = Maptools.GetChunkUVList(_chunkSize); // UV lists are the same for every chunk
        chunkVertexIndices = Maptools.GetChunkTriangleIndexList(_chunkSize); // Triangle indices are the same for every chunk

        CreateNoiseLayers();

        readyToCheck = true;
    }

    

    [BurstCompile]
    private IEnumerator FinalizeChunkData()
    {
        if (chunkDataList.Count > 0)
        {
            ChunkData data = chunkDataList.Dequeue();
            data.Vertices = GetChunkVertices(data.ChunkWorldPosition, _chunkSize);
            finalChunkDataList.Enqueue(data);
            chunksGenerated++;
            yield return null;
        }
    }

    [BurstCompile]
    private IEnumerator CreateTerrainChunks()
    {
        if (finalChunkDataList.Count > 0)
        {
            ChunkData data = finalChunkDataList.Dequeue();
            GenerateChunkGO(data);

            if (chunksGenerated == (chunksPerRow * chunksPerRow))
            {
                totalGentime = Time.realtimeSinceStartup - startTime;
                genTimeText.text = "Generation time: " + totalGentime.ToString("0.00") + " s";
                genButton.enabled = true;
            }
        }

        yield return null;
    }

    [BurstCompile]
    // The whole map
    private void GenerateChunkData(int2 mapPos, int mapSize, int chunkSize, int[] tempTriangleList, float2[] tempUvList)
    {
        ChunkData chunkData = new ChunkData();
        chunkData.TriangleIndexes = tempTriangleList;
        chunkData.UVs = tempUvList;
        chunkData.ChunkWorldPosition = new int2(mapPos.x * chunkSize, mapPos.y * chunkSize);
        chunkDataList.Enqueue(chunkData);
    }

    [BurstCompile]
    private float3[] GetChunkVertices(int2 terrainWorldPosition, int chunkSize)
    {
        tempVertices = new float3[(chunkSize + 1) * (chunkSize + 1)];
        int2 chunkPos = new int2(terrainWorldPosition.x, terrainWorldPosition.y);

        tempNoiseValues = layerNoises[0].GetNoiseValues(chunkPos.x, chunkPos.y, chunkSize, noiseLayers[0].scale, seed);
        tempNoiseValues2 = layerNoises[1].GetNoiseValues(chunkPos.x, chunkPos.y, chunkSize, noiseLayers[1].scale, seed);

        for (int i = 0, z = 0; z <= chunkSize; z++)
        {
            for (int x = 0; x <= chunkSize; x++)
            {
                ChunkVerticesCreation(terrainWorldPosition, chunkPos, i, z, x);
                i++;
            }
        }

        return tempVertices;
    }

    private void ChunkVerticesCreation(float2 terrainWorldPosition, float2 worldChunkPos,  int i, int z, int x)
    {
        currentVertHeight = tempNoiseValues[x, z];
        currentVertHeight = Mathf.Clamp01(currentVertHeight - falloffMap[(int)terrainWorldPosition.x + x, (int)terrainWorldPosition.y + z]);
        currentVertHeight *= tempNoiseValues2[x, z];
        //currentVertHeight -= tempNoiseValues2[x, z];
        currentVertHeight = terrainAnimCurve.Evaluate(currentVertHeight) * (noiseLayers[0].heightScale);
        currentVertHeight *= tempNoiseValues[x, z];
        if (currentVertHeight < minMapHeight) minMapHeight = currentVertHeight;
        if (currentVertHeight > maxMapHeight) maxMapHeight = currentVertHeight;
        tempVertices[i] = Maptools.BorderGeneration(worldBorderDistance, worldMiddlePoint, tempVertices[i], worldChunkPos, currentVertHeight, z, x);
    }
   

    private void GenerateChunkGO(ChunkData chunkData)
    {
        GameObject terrainChunk = noColliders? Instantiate(terrainChunkPrefabNoCollider) : Instantiate(terrainChunkPrefab);
        tempMesh = new Mesh();

        Vector3[] vertices = new Vector3[chunkData.Vertices.Length];
        for (int i = 0; i < chunkData.Vertices.Length; i++)
        {
            vertices[i] = new Vector3(chunkData.Vertices[i].x, chunkData.Vertices[i].y, chunkData.Vertices[i].z);
        }

        List<Vector2> uvs = new List<Vector2>();
        for (int i = 0; i < chunkData.UVs.Length; i++)
        {
            uvs.Add(new Vector2(chunkData.UVs[i].x, chunkData.UVs[i].y));
        }

        tempMesh.SetVertices(vertices);
        tempMesh.SetTriangles(chunkData.TriangleIndexes, 0);
        tempMesh.SetUVs(0, Maptools.Float2ToVector2Array(chunkData.UVs));
        tempMesh.RecalculateNormals();

        terrainChunk.GetComponent<MeshRenderer>().sharedMaterial = terrainMaterial;
        terrainChunk.GetComponent<MeshFilter>().mesh = tempMesh;
        if (!noColliders) terrainChunk.GetComponent<MeshCollider>().sharedMesh = tempMesh;
        terrainChunk.transform.SetParent(gameWorld.transform);
        terrainChunk.transform.position = new Vector3(chunkData.ChunkWorldPosition.x, 0, chunkData.ChunkWorldPosition.y);
    }

    [BurstCompile]
    private void CreateNoiseLayers()
    {
        foreach (NoiseLayer layer in noiseLayers)
        {
            layerNoises.Add(new PerlinNoise(layer.frequency, layer.amplitude, layer.lacunarity, layer.persistance, layer.octaves));
        }
    }

    private void PrepareNewMap()
    {
        if (randomSeed) seed = UnityEngine.Random.Range(0, 100000);
        _mapSize = Convert.ToInt16(mapSizeUIInput.text);
        layerNoises.Clear();
        chunkDataList.Clear();
        finalChunkDataList.Clear();
        worldMiddlePoint = new Vector3(_mapSize / 2, 0, _mapSize / 2);
        chunkGenerationCheck = new int[_mapSize /_chunkSize, _mapSize / _chunkSize];

        for (int x = 0; x < _mapSize / _chunkSize; x++)
        {
            for (int y = 0; y < _mapSize / _chunkSize; y++)
            {
                chunkGenerationCheck[x, y] = 0;
            }
        }
        worldBorderDistance = (_mapSize / 2) - 4;
        genTimeText.text = "";
        startTime = Time.realtimeSinceStartup;
        genButton.enabled = false;
        chunksPerRow = _mapSize / _chunkSize;
        noColliders = colliderToggle.isOn ? false : true;

        // TODO: Water must be created/scaled properly
        float scale2k = 19.93f;
        scale2k = (_mapSize / 2000f) * scale2k;
        waterObject.transform.position = new Vector3(worldMiddlePoint.x, 0.8f, worldMiddlePoint.z);
        waterObject.SetActive(true);
        waterObject.transform.localScale = new Vector3(scale2k, 1, scale2k);
        foreach (Transform t in gameWorld.transform)
        {
            Destroy(t.gameObject);
        }
    }
}

