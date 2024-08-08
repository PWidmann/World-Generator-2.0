using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


public struct ChunkData
{
    public int chunkID;
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uvs;
    public Vector2Int terrainWorldPosition;
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
    [SerializeField] private NoiseVisualizer noiseVisualizer;



    private int chunksPerRow;
    // Complete map noise
    private PerlinNoise[] noises;

    private float[,] falloffMap;

    float minMapHeight = 1000;
    float maxMapHeight = 0;

    private List<GameObject> chunks = new List<GameObject>();
    GameObject tempObj;

    private List<ChunkData> chunkDataList = new List<ChunkData>();

    int lastChunksPerRow = 0;
    float startTime3;

    private List<PerlinNoise> layerNoises = new List<PerlinNoise>();

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

        float startTime = Time.realtimeSinceStartup;

        layerNoises.Clear();


        foreach (NoiseLayer layer in noiseLayers)
        {
            PerlinNoise noise = new PerlinNoise(layer.seed, layer.frequency, layer.amplitude, layer.lacunarity, layer.persistance, layer.octaves);
            layerNoises.Add(noise);
            
        }


        

        falloffMap = FalloffGenerator.GenerateFalloffMapCircle(_mapSize + chunksPerRow, _mapSize + chunksPerRow, fallOffValueA, fallOffValueB);


        //noiseVisualizer.SetImageNoise(falloffMap, 0);

        Debug.Log("instantiating 'noise' took: " + ((Time.realtimeSinceStartup - startTime) * 1000f) + "ms");

        float startTime2 = Time.realtimeSinceStartup;
        GenerateChunkData( _mapSize, _chunkSize);
        Debug.Log("Generating noise data took: " + ((Time.realtimeSinceStartup - startTime2) * 1000f) + "ms");
        
        startTime3 = Time.realtimeSinceStartup;

        StartCoroutine(GenerateChunkObjects());
    }

    private IEnumerator GenerateChunkObjects()
    {
        SetMapToGround(minMapHeight);


        foreach (ChunkData chunkData in chunkDataList)
        {
            float startTime = Time.realtimeSinceStartup;
            
            GameObject tempObj = Instantiate(terrainChunkPrefab);
            
            var mesh = new Mesh();
            mesh.SetVertices(chunkData.vertices);
            mesh.SetTriangles(chunkData.triangles, 0);
            mesh.SetUVs(0, chunkData.uvs);
            mesh.RecalculateNormals();

            tempObj.GetComponent<MeshRenderer>().sharedMaterial = terrainMaterial;
            tempObj.GetComponent<MeshFilter>().mesh = mesh;
            tempObj.GetComponent<MeshCollider>().sharedMesh = mesh;
            //Debug.Log("instantiate mesh and data and assign to GO: " + ((Time.realtimeSinceStartup - startTime) * 1000f) + "ms");


            tempObj.transform.SetParent(gameWorld.transform);
            tempObj.transform.position = new Vector3(chunkData.terrainWorldPosition.x, 0, chunkData.terrainWorldPosition.y);
            yield return null;
        }

        Debug.Log("Generating chunk objects took: " + ((Time.realtimeSinceStartup - startTime3) * 1000f) + "ms");
    }

    private void GenerateChunkData(int mapSize, int chunkSize)
    {
        int[] triangleList = GetTriangleList(chunkSize);
        Vector2[] uvList = GetUVList(chunkSize);

        for (int i = 0, z = 0; z < (mapSize / chunkSize); z++)
        {
            for (int x = 0; x < (mapSize / chunkSize); x++)
            {
                ChunkData chunkData = new ChunkData();
                chunkData.chunkID = i;
                chunkData.triangles = triangleList;
                chunkData.uvs = uvList;
                chunkData.terrainWorldPosition = new Vector2Int(x * chunkSize, z * chunkSize);
                chunkData.vertices = GetChunkVertices(chunkData.terrainWorldPosition, chunkSize);

                chunkDataList.Add(chunkData);
                i++;
            }
        }
    }

    private Vector3[] GetChunkVertices(Vector2Int terrainWorldPosition, int chunkSize)
    {
        Vector3[] vertices = new Vector3[(chunkSize + 1) * (chunkSize + 1)];

        float[,] noiseValues = layerNoises[0].GetNoiseValues(terrainWorldPosition.x, terrainWorldPosition.y, chunkSize, noiseLayers[0].scale);
        float[,] noiseValues2 = layerNoises[1].GetNoiseValues(terrainWorldPosition.x, terrainWorldPosition.y, chunkSize, noiseLayers[1].scale);
        float[,] noiseValues3 = layerNoises[2].GetNoiseValues(terrainWorldPosition.x, terrainWorldPosition.y, chunkSize, noiseLayers[2].scale);

        //float[,] noiseTexture1 = layerNoises[0].GetNoiseValues(0, 0, _mapSize, noiseLayers[0].scale);
        //float[,] noiseTexture2 = layerNoises[1].GetNoiseValues(0, 0, _mapSize, noiseLayers[1].scale);
        //float[,] noiseTexture3 = layerNoises[2].GetNoiseValues(0, 0, _mapSize, noiseLayers[2].scale);
        //
        //noiseVisualizer.SetImageNoise(noiseTexture1, 1);
        //noiseVisualizer.SetImageNoise(noiseTexture2, 2);
        //noiseVisualizer.SetImageNoise(noiseTexture3, 3);

        float totalScale = (noiseLayers[0].heightScale) * (noiseLayers[1].heightScale) * (noiseLayers[2].heightScale);

        for (int i = 0, z = 0; z <= chunkSize; z++)
        {
            for (int x = 0; x <= chunkSize; x++)
            {
                float y = noiseValues[x, z];
                y = SubtractingFalloff(y, terrainWorldPosition.x, terrainWorldPosition.y, x, z);
                

                
                y *= noiseValues2[x, z];
                y = animCurve.Evaluate(y);
                //y *= noiseValues3[x, z];

                //y = SubtractingFalloff(y, terrainWorldPosition.x, terrainWorldPosition.y, x, z);

                //y = FalloffGenerator.GetNormalizedValue(y, 0, y);

                //y *= noiseLayers[0].heightScale;
                //float height = FalloffGenerator.GetNormalizedValue(y, 0, maxMapHeight);
                //y = animCurve.Evaluate(height) * noiseLayers[0].heightScale;

                //float yNormalized = FalloffGenerator.GetNormalizedValue(y, 0, 4);


                y = y * (noiseLayers[0].heightScale);

                if (y < minMapHeight) minMapHeight = y;
                if (y > maxMapHeight) maxMapHeight = y;

                vertices[i] = new Vector3(x, y, z);
                i++;
            }
        }




        return vertices;
    }

    float SubtractingFalloff(float oldHeight, int terrainXpos, int terrainZpos, int x, int z)
    {
        return Mathf.Clamp01(oldHeight - falloffMap[terrainXpos + x, terrainZpos + z]);
    }

    private void SetMapToGround(float minHeight)
    {
        foreach (ChunkData data in chunkDataList)
        {
            for (int i = 0; i < data.vertices.Length; i++)
            {
                data.vertices[i] = new Vector3(data.vertices[i].x, data.vertices[i].y - minHeight, data.vertices[i].z);
            }
        }
    }

    private int[] GetTriangleList(int chunkSize)
    {
        int[] triangleList = new int[chunkSize * chunkSize * 6];
        int vert = 0;
        int tris = 0;

        // Triangles
        for (int z = 0; z < chunkSize; z++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                triangleList[tris + 0] = vert + 0;
                triangleList[tris + 1] = vert + chunkSize + 1;
                triangleList[tris + 2] = vert + 1;

                triangleList[tris + 3] = vert + 1;
                triangleList[tris + 4] = vert + chunkSize + 1;
                triangleList[tris + 5] = vert + chunkSize + 2;

                vert++;
                tris += 6;
            }

            vert++;
        }

        return triangleList;
    }

    private Vector2[] GetUVList(int chunkSize)
    {
        Vector2[] uv = new Vector2[(chunkSize + 1) * (chunkSize + 1)];

        // UVs
        for (int i = 0, z = 0; z <= chunkSize; z++)
        {
            for (int x = 0; x <= chunkSize; x++)
            {
                uv[i] = new Vector2(x / (float)chunkSize, z / (float)chunkSize);
                i++;
            }
        }

        return uv;
    }
}
