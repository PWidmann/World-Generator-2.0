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
    public Vector2 terrainPosition;
}

public class MapGenerator : MonoBehaviour
{

    [Header("Map Settings")]
    [SerializeField] private int _mapSize = 500; // Must be a multiple of chunkSize
    [SerializeField] private int _chunkSize = 100;
    [SerializeField] private bool useFalloff = false;

    [SerializeField] private GameObject gameWorld;
    [SerializeField] private Material terrainMaterial;
    [SerializeField] private AnimationCurve animCurve;

    [Header("Perlin Values")]
    public float heightScale = 2.0f;
    public int seed = 1337;
    public float frequency = 6.4f;
    public float amplitude = 3.8f;
    public float lacunarity = 1.8f;
    private float persistance = 0.5f;
    public int octaves = 3;

    [SerializeField] private float fallOffValueA = 3;
    [SerializeField] private float fallOffValueB = 2.2f;

    private int chunksPerRow;
    // Complete map noise
    private PerlinNoise noise;

    private float[,] falloffMap;

    // Terrain chunk temp values
    private float[,] noiseValues;
    private Mesh tempMesh;
    private Vector3[] vertices;
    private int[] triangles;
    private Vector2[] uv;

    private List<GameObject> chunks = new List<GameObject>();
    GameObject tempObj;

    private List<ChunkData> chunkDataList = new List<ChunkData>();

    int lastChunksPerRow = 0;
    float startTime3;

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
        noise = new PerlinNoise(seed, frequency, amplitude, lacunarity, persistance, octaves);
        Debug.Log("instantiating 'noise' took: " + ((Time.realtimeSinceStartup - startTime) * 1000f) + "ms");

        float startTime2 = Time.realtimeSinceStartup;
        GenerateChunkData( _mapSize, _chunkSize);
        Debug.Log("Generating noise data took: " + ((Time.realtimeSinceStartup - startTime2) * 1000f) + "ms");

        startTime3 = Time.realtimeSinceStartup;
        StartCoroutine(GenerateChunkObjects());
        
    }

    private IEnumerator GenerateChunkObjects()
    {
        

        foreach (ChunkData chunkData in chunkDataList)
        {
            var mesh = new Mesh();
            mesh.SetVertices(chunkData.vertices);
            mesh.SetTriangles(chunkData.triangles, 0);
            mesh.SetUVs(0, chunkData.uvs);
            mesh.RecalculateNormals();

            //Create terrain chunks and add them to list
            GameObject tempObj = new GameObject("TerrainChunk");
            MeshRenderer meshRenderer = (MeshRenderer)tempObj.AddComponent(typeof(MeshRenderer));
            meshRenderer.sharedMaterial = terrainMaterial;

            MeshFilter meshFilter = (MeshFilter)tempObj.AddComponent(typeof(MeshFilter));
            meshFilter.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            meshFilter.mesh = mesh;
            
            MeshCollider meshCollider = (MeshCollider)tempObj.AddComponent(typeof(MeshCollider));
            tempObj.transform.SetParent(gameWorld.transform);
            tempObj.transform.position = new Vector3(chunkData.terrainPosition.x, 0, chunkData.terrainPosition.y);
            yield return null;
        }

        Debug.Log("Generating chunk objects took: " + ((Time.realtimeSinceStartup - startTime3) * 1000f) + "ms");
    }

    private void GenerateChunkData(int mapSize, int chunkSize)
    {
        int numberOfJunks = (mapSize / chunkSize) * (mapSize / chunkSize);

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
                chunkData.terrainPosition = new Vector2(x * chunkSize, z * chunkSize);
                chunkData.vertices = GetChunkVertices(chunkData.terrainPosition, chunkSize);

                chunkDataList.Add(chunkData);
                i++;
            }
        }
    }

    private Vector3[] GetChunkVertices(Vector2 terrainPosition, int chunkSize)
    {
        Vector3[] vertices = new Vector3[(chunkSize + 1) * (chunkSize + 1)];
        float[,] noiseValues = noise.GetNoiseValues((int)terrainPosition.x, (int)terrainPosition.y, chunkSize);

        for (int i = 0, z = 0; z <= chunkSize; z++)
        {
            for (int x = 0; x <= chunkSize; x++)
            {
                float y = animCurve.Evaluate(noiseValues[x, z]) * heightScale;
                vertices[i] = new Vector3(x, y, z);
                i++;
            }
        }

        return vertices;
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
