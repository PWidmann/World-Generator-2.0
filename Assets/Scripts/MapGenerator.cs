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
    public float3[] verticesOld;
    public int[] trianglesOld;
    public float2[] uvs;
    public int2 terrainWorldPosition;
    public NativeArray<Vector3> chunkVertices;
}

[Serializable]
public struct NoiseLayer
{
    public string name;
    public float heightScale;
    public int seed;
    public bool randomSeed;
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
    private PerlinNoise[] noises;
    private float[,] falloffMap;
    private float minMapHeight = 1000;
    private float maxMapHeight = 0;
    private int worldBorderDistance = 1000;
    private List<GameObject> chunks = new List<GameObject>();
    private List<PerlinNoise> layerNoises = new List<PerlinNoise>();
    private float3 worldMiddlePoint;
    private Mesh tempMesh;
    private int[] tempTriangleList;
    private float2[] tempUvList;
    private float3[] tempVertices;
    private float[,] tempNoiseValues;
    private float[,] tempNoiseValues2;
    private float[,] tempNoiseValues3;
    private float startTime3;
    private float currentVertHeight;
    private int[,] chunksGeneratedCheck;
    private List<NativeArray<Vector3>> terrainVerts = new List<NativeArray<Vector3>>();
    private List<Mesh> waterMeshes = new List<Mesh>();
    private List<ChunkData> chunkDataList = new List<ChunkData>();
    private Queue<ChunkData> finalChunkDataList = new Queue<ChunkData>();
    private float startTime;
    private float totalGentime;

    private void Start()
    {
        //GenerateNewMap();
        startTime = Time.realtimeSinceStartup;
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.U))
        {
            GenerateNewMap();
        }

        StartCoroutine(CreatePreparedChunks());
    }

    [BurstCompile]
    private IEnumerator CreatePreparedChunks()
    {
        if (finalChunkDataList.Count > 0)
        {
            ChunkData data = finalChunkDataList.Dequeue();

            GenerateChunkGO(data);

            if (data.chunkID == (chunksPerRow * chunksPerRow) - 1)
            {
                totalGentime = Time.realtimeSinceStartup - startTime;
                genTimeText.text = "Generation time: " + totalGentime.ToString("0.00") + " s";
                genButton.enabled = true;
            }
        }

        yield return null;
    }

    [BurstCompile]
    public void GenerateNewMap()
    {
        if (Convert.ToInt16(mapSizeUIInput.text) % _chunkSize != 0) return;
        if (randomSeed) seed = UnityEngine.Random.Range(0, 10000);

        float startTime = Time.realtimeSinceStartup;

        PrepareNewMap();

        // Jobs
        falloffMap = Maptools.GenerateFalloffMapCircle(_mapSize + chunksPerRow); // complete map falloff map
        tempUvList = Maptools.GetChunkUVList(_chunkSize); // UV lists are the same for every chunk
        tempTriangleList = Maptools.GetChunkTriangleIndexList(_chunkSize); // Triangle indices are the same for every chunk

        // No jobs yet
        CreateNoiseLayers();
        GenerateChunkDataNaked(_mapSize, _chunkSize, tempTriangleList, tempUvList);
        StartCoroutine(FillFinalChunkData());
    }

    [BurstCompile]
    private void CreateNoiseLayers()
    {
        foreach (NoiseLayer layer in noiseLayers)
        {
            PerlinNoise noise = new PerlinNoise(layer.frequency, layer.amplitude, layer.lacunarity, layer.persistance, layer.octaves);
            layerNoises.Add(noise);

        }
    }

    [BurstCompile]
    private IEnumerator FillFinalChunkData()
    {
        for (int i = 0, z = 0; z < chunksPerRow; z++)
        {
            for (int x = 0; x < chunksPerRow; x++)
            {
                ChunkData data = chunkDataList.ElementAt(i);
                data.verticesOld = GetChunkVertices(data.terrainWorldPosition, _chunkSize);
                finalChunkDataList.Enqueue(data);
                i++;

                yield return null;
            }
        }
    }

    [BurstCompile]
    private void PrepareNewMap()
    {
        _mapSize = Convert.ToInt16(mapSizeUIInput.text);
        waterMeshes.Clear();
        chunkDataList.Clear();
        finalChunkDataList.Clear();
        worldMiddlePoint = new Vector3(_mapSize / 2, 0, _mapSize / 2);
        worldBorderDistance = (_mapSize / 2) - 4;
        genTimeText.text = "";
        startTime = Time.realtimeSinceStartup;
        genButton.enabled = false;
        chunksPerRow = _mapSize / _chunkSize;
        layerNoises.Clear();
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

    [BurstCompile]
    // The whole map
    private void GenerateChunkDataNaked(int mapSize, int chunkSize, int[] tempTriangleList, float2[] tempUvList)
    {
        for (int i = 0, z = 0; z < (mapSize / chunkSize); z++)
        {
            for (int x = 0; x < (mapSize / chunkSize); x++)
            {
                ChunkData chunkData = new ChunkData();
                chunkData.chunkID = i;
                chunkData.trianglesOld = tempTriangleList;
                chunkData.uvs = tempUvList;
                chunkData.terrainWorldPosition = new int2(x * chunkSize, z * chunkSize);
                chunkDataList.Add(chunkData);
                i++;
            }
        }
    }

    [BurstCompile]
    private float3[] GetChunkVertices(int2 terrainWorldPosition, int chunkSize)
    {
        tempVertices = new float3[(chunkSize + 1) * (chunkSize + 1)];
        int2 chunkPos = new int2(terrainWorldPosition.x, terrainWorldPosition.y);

        tempNoiseValues = layerNoises[0].GetNoiseValues(chunkPos.x, chunkPos.y, chunkSize, noiseLayers[0].scale, seed);
        tempNoiseValues2 = layerNoises[1].GetNoiseValues(chunkPos.x, chunkPos.y, chunkSize, noiseLayers[1].scale, seed);

        float2 worldChunkPos = new float2(terrainWorldPosition.x, terrainWorldPosition.y);

        for (int i = 0, z = 0; z <= chunkSize; z++)
        {
            for (int x = 0; x <= chunkSize; x++)
            {
                ChunkVerticesCreation(terrainWorldPosition, worldChunkPos, i, z, x);
                i++;
            }
        }

        return tempVertices;
    }

    [BurstCompile]
    private void ChunkVerticesCreation(float2 terrainWorldPosition, float2 worldChunkPos,  int i, int z, int x)
    {
        currentVertHeight = tempNoiseValues[x, z];
        currentVertHeight = Mathf.Clamp01(currentVertHeight - falloffMap[(int)terrainWorldPosition.x + x, (int)terrainWorldPosition.y + z]);
        currentVertHeight *= tempNoiseValues2[x, z];

        //currentVertHeight -= tempNoiseValues2[x, z];
        currentVertHeight = terrainAnimCurve.Evaluate(currentVertHeight) * (noiseLayers[0].heightScale);
        currentVertHeight *= tempNoiseValues[x, z];
        

        tempVertices[i] = BorderGeneration(tempVertices[i], worldChunkPos, currentVertHeight, i, z, x);
    }

    [BurstCompile]
    private float3 BorderGeneration(float3 tempVertices, float2 worldChunkPos, float currentHeight, int i, int z, int x)
    {
        float3 output = tempVertices;

        // Border generation
        float3 vertextWorldPos = new float3(worldChunkPos.x + x, 0, worldChunkPos.y + z);
        float distance = Vector3.Distance(vertextWorldPos, worldMiddlePoint);

        if (distance <= worldBorderDistance)
        {

            if (currentVertHeight < minMapHeight) minMapHeight = currentVertHeight;
            if (currentVertHeight > maxMapHeight) maxMapHeight = currentVertHeight;
            output = new float3(x, currentVertHeight, z);

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

    [BurstCompile]
    private void GenerateChunkGO(ChunkData chunkData)
    {
        GameObject terrainChunk = noColliders? Instantiate(terrainChunkPrefabNoCollider) : Instantiate(terrainChunkPrefab);
        tempMesh = new Mesh();

        Vector3[] vertices = new Vector3[chunkData.verticesOld.Length];
        for (int i = 0; i < chunkData.verticesOld.Length; i++)
        {
            vertices[i] = new Vector3(chunkData.verticesOld[i].x, chunkData.verticesOld[i].y, chunkData.verticesOld[i].z);
        }

        List<Vector2> uvs = new List<Vector2>();
        for (int i = 0; i < chunkData.uvs.Length; i++)
        {
            uvs.Add(new Vector2(chunkData.uvs[i].x, chunkData.uvs[i].y));
        }

        tempMesh.SetVertices(vertices);
        tempMesh.SetTriangles(chunkData.trianglesOld, 0);
        tempMesh.SetUVs(0, uvs);
        tempMesh.RecalculateNormals();

        terrainChunk.GetComponent<MeshRenderer>().sharedMaterial = terrainMaterial;
        terrainChunk.GetComponent<MeshFilter>().mesh = tempMesh;
        if (!noColliders) terrainChunk.GetComponent<MeshCollider>().sharedMesh = tempMesh;
        terrainChunk.transform.SetParent(gameWorld.transform);
        terrainChunk.transform.position = new Vector3(chunkData.terrainWorldPosition.x, 0, chunkData.terrainWorldPosition.y);
    }

    
}

