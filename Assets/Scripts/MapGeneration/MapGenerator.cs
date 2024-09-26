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
using UnityEngine.SceneManagement;
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
    [SerializeField] bool generateWholeMap = false;
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
    private List<Noise> layerNoises = new List<Noise>();
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
    int2 playerPosition;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            CreateNewMapData();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene("MainScene");
        }

        if (generateWholeMap)
        {
            if (readyToCheck)
            {
                for (int x = 0; x < _mapSize / _chunkSize; x++)
                {
                    for (int y = 0; y < _mapSize / _chunkSize; y++)
                    {
                        GenerateChunkData(new int2(x, y), _mapSize, _chunkSize, chunkVertexIndices, chunkUVMap);
                    }
                }
                
            }
        }
        else
        { 
            StartCoroutine(OnDemandChunkQueue());
        }
        StartCoroutine(FinalizeChunkData());
        StartCoroutine(CreateTerrainChunks());
    }

    public void CreateNewMapData()
    {
        if (Convert.ToInt32(mapSizeUIInput.text) % _chunkSize != 0) return;
        startTime = Time.realtimeSinceStartup;
        chunksGenerated = 0;
        PrepareNewMap();
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


    private float3[] GetChunkVertices(int2 terrainWorldPosition, int chunkSize)
    {
        tempVertices = new float3[(chunkSize + 1) * (chunkSize + 1)];
        int2 chunkPos = new int2(terrainWorldPosition.x, terrainWorldPosition.y);
        float[,] fallOffHeights = Maptools.GenerateChunkFalloffMap(_mapSize + 1, _chunkSize + 1, terrainWorldPosition);


        tempNoiseValues = layerNoises[0].GetNoiseValues(chunkPos.x, chunkPos.y, chunkSize, noiseLayers[0].scale, seed);
        tempNoiseValues2 = layerNoises[1].GetNoiseValues(chunkPos.x, chunkPos.y, chunkSize, noiseLayers[1].scale, seed);
        for (int i = 0, z = 0; z <= chunkSize; z++)
        {
            for (int x = 0; x <= chunkSize; x++)
            {
                tempVertices[i] = GetCurrentChunkVertex(terrainWorldPosition, chunkPos, i, z, x, fallOffHeights[x, z]);
                i++;
            }
        }

        return tempVertices;
    }

    private float3 GetCurrentChunkVertex(float2 terrainWorldPosition, float2 worldChunkPos,  int i, int z, int x, float fallOffValue)
    {
        currentVertHeight = tempNoiseValues[x, z];
        currentVertHeight = Mathf.Clamp01(currentVertHeight - fallOffValue);
        currentVertHeight *= tempNoiseValues2[x, z];
        currentVertHeight = terrainAnimCurve.Evaluate(currentVertHeight) * (noiseLayers[0].heightScale);
        //currentVertHeight -= tempNoiseValues2[x, z];
        currentVertHeight *= tempNoiseValues[x, z];
        if (currentVertHeight < minMapHeight) minMapHeight = currentVertHeight;
        if (currentVertHeight > maxMapHeight) maxMapHeight = currentVertHeight;
        return Maptools.BorderGeneration(worldBorderDistance, worldMiddlePoint, tempVertices[i], worldChunkPos, currentVertHeight, z, x);
    }
   

    private void GenerateChunkGO(ChunkData chunkData)
    {
        GameObject terrainChunk = noColliders? Instantiate(terrainChunkPrefabNoCollider) : Instantiate(terrainChunkPrefab);
        terrainChunk.name = "TerrainChunk";
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
            layerNoises.Add(new Noise(layer.frequency, layer.amplitude, layer.lacunarity, layer.persistance, layer.octaves));
        }
    }

    private void PrepareNewMap()
    {
        if (randomSeed) seed = UnityEngine.Random.Range(0, 100000);
        _mapSize = Convert.ToInt32(mapSizeUIInput.text);
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
        //genButton.enabled = false;
        chunksPerRow = _mapSize / _chunkSize;
        noColliders = colliderToggle.isOn ? false : true;

        //startPosition.position = new Vector3(_mapSize / 2, 10, _mapSize / 4);

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

    private IEnumerator OnDemandChunkQueue()
    {
        if (readyToCheck)
        {
            playerPosition = new int2((int)(startPosition.position.x / _mapSize * chunksPerRow), (int)(startPosition.position.z / _mapSize * chunksPerRow));

            try
            {
                // current pos
                if (chunkGenerationCheck[playerPosition.x, playerPosition.y] == 0)
                {
                    chunkGenerationCheck[playerPosition.x, playerPosition.y] = 1;
                    GenerateChunkData(playerPosition, _mapSize, _chunkSize, chunkVertexIndices, chunkUVMap);
                }

                // Right
                if (chunkGenerationCheck[playerPosition.x + 1, playerPosition.y] == 0)
                {
                    chunkGenerationCheck[playerPosition.x + 1, playerPosition.y] = 1;
                    GenerateChunkData(playerPosition + new int2(1, 0), _mapSize, _chunkSize, chunkVertexIndices, chunkUVMap);
                }

                // Left
                if (chunkGenerationCheck[playerPosition.x - 1, playerPosition.y] == 0)
                {
                    chunkGenerationCheck[playerPosition.x - 1, playerPosition.y] = 1;
                    GenerateChunkData(playerPosition + new int2(-1, 0), _mapSize, _chunkSize, chunkVertexIndices, chunkUVMap);
                }

                // Up
                if (chunkGenerationCheck[playerPosition.x, playerPosition.y + 1] == 0)
                {
                    chunkGenerationCheck[playerPosition.x, playerPosition.y + 1] = 1;
                    GenerateChunkData(playerPosition + new int2(0, 1), _mapSize, _chunkSize, chunkVertexIndices, chunkUVMap);
                }

                // down
                if (chunkGenerationCheck[playerPosition.x, playerPosition.y - 1] == 0)
                {
                    chunkGenerationCheck[playerPosition.x, playerPosition.y - 1] = 1;
                    GenerateChunkData(playerPosition + new int2(0, -1), _mapSize, _chunkSize, chunkVertexIndices, chunkUVMap); // down
                }

                

                // right down
                if (chunkGenerationCheck[playerPosition.x + 1, playerPosition.y - 1] == 0)
                {
                    chunkGenerationCheck[playerPosition.x + 1, playerPosition.y - 1] = 1;
                    GenerateChunkData(playerPosition + new int2(1, -1), _mapSize, _chunkSize, chunkVertexIndices, chunkUVMap);
                }

                // right up
                if (chunkGenerationCheck[playerPosition.x + 1, playerPosition.y + 1] == 0)
                {
                    chunkGenerationCheck[playerPosition.x + 1, playerPosition.y + 1] = 1;
                    GenerateChunkData(playerPosition + new int2(1, 1), _mapSize, _chunkSize, chunkVertexIndices, chunkUVMap);
                }

                // left down
                if (chunkGenerationCheck[playerPosition.x - 1, playerPosition.y - 1] == 0)
                {
                    chunkGenerationCheck[playerPosition.x - 1, playerPosition.y - 1] = 1;
                    GenerateChunkData(playerPosition + new int2(-1, -1), _mapSize, _chunkSize, chunkVertexIndices, chunkUVMap);
                }

                // left up
                if (chunkGenerationCheck[playerPosition.x - 1, playerPosition.y + 1] == 0)
                {
                    chunkGenerationCheck[playerPosition.x - 1, playerPosition.y + 1] = 1;
                    GenerateChunkData(playerPosition + new int2(-1, 1), _mapSize, _chunkSize, chunkVertexIndices, chunkUVMap);
                }
            }

            catch
            {
                
            }

            yield return null;
        }
    }
}

