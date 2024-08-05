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
    public Vector2Int terrainPosition;
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

    private int chunksPerRow;
    // Complete map noise
    private PerlinNoise[] noises;

    private float[,] falloffMap;

    float minMapHeight = 1000;

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
        //noise = new PerlinNoise(seed, frequency, amplitude, lacunarity, persistance, octaves);
        //noise2 = new PerlinNoise(seed, frequency/2, amplitude, lacunarity, persistance, octaves);
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
            var mesh = new Mesh();
            mesh.SetVertices(chunkData.vertices);
            mesh.SetTriangles(chunkData.triangles, 0);
            mesh.SetUVs(0, chunkData.uvs);
            mesh.RecalculateNormals();

            GameObject tempObj = Instantiate(terrainChunkPrefab);
            tempObj.GetComponent<MeshRenderer>().sharedMaterial = terrainMaterial;
            tempObj.GetComponent<MeshFilter>().mesh = mesh;
            tempObj.GetComponent<MeshCollider>().sharedMesh = mesh;
            tempObj.transform.SetParent(gameWorld.transform);
            tempObj.transform.position = new Vector3(chunkData.terrainPosition.x, 0, chunkData.terrainPosition.y);
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
                chunkData.terrainPosition = new Vector2Int(x * chunkSize, z * chunkSize);
                chunkData.vertices = GetChunkVertices(chunkData.terrainPosition, chunkSize);

                chunkDataList.Add(chunkData);
                i++;
            }
        }
    }

    private Vector3[] GetChunkVertices(Vector2Int terrainPosition, int chunkSize)
    {
        Vector3[] vertices = new Vector3[(chunkSize + 1) * (chunkSize + 1)];

        // Base noise map
        float[,] noiseValues = layerNoises[0].GetNoiseValues(terrainPosition.x, terrainPosition.y, chunkSize, noiseLayers[0].scale);

        float[,] noiseValues2 = layerNoises[1].GetNoiseValues(terrainPosition.x, terrainPosition.y, chunkSize, noiseLayers[0].scale);

        //// add additional noise maps here

        for (int i = 0, z = 0; z <= chunkSize; z++)
        {
            for (int x = 0; x <= chunkSize; x++)
            {
                float y = (((noiseValues[x, z] * noiseLayers[0].heightScale)) + (noiseValues2[x, z] * noiseLayers[1].heightScale));



                if (y < minMapHeight) minMapHeight = y;
                vertices[i] = new Vector3(x, y, z);
                i++;
            }
        }

        return vertices;
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

    float[,] SubtractingFalloff(float[,] noiseValues, int terrainXpos, int terrainYpos)
    {
        float[,] newNoiseValues = noiseValues;
        //Texture2D tex = new Texture2D(newNoiseValues.GetLength(0), newNoiseValues.GetLength(1));

        //blah to do wo auf der falloffmap???

        //int xMinPos = terrainXpos * chunkSize;
        //int yMinPos = terrainYpos * chunkSize;
        //
        //
        //for (int x = 0; x < newNoiseValues.GetLength(0); x++)
        //{
        //    for (int y = 0; y < newNoiseValues.GetLength(1); y++)
        //    {
        //        // SUBSTRACT FALLOFF MAP VALUES || test: string [] c = a.Except(b).ToArray();
        //        newNoiseValues[x, y] = Mathf.Clamp01(newNoiseValues[x, y] - falloffMap[x + xMinPos, y + yMinPos]);
        //
        //        //float c = Mathf.Clamp01(newNoiseValues[x, y]);
        //        //tex.SetPixel(x, y, new Color(c, c, c));
        //    }
        //}



        //tex.Apply();
        //
        //fallOffImage.texture = tex;
        //fallOffImage.texture.filterMode = FilterMode.Point;
        //fallOffImage.color = Color.white;

        return newNoiseValues;
    }
}
