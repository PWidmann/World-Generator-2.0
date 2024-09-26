using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Biome", menuName = "MapGenerator/Biome")]
public class BiomeScriptableObject : ScriptableObject
{
    public string BiomeName;

    public int seed;
    public bool randomSeed;

    private float frequency;
    private float amplitude;
    private float lacunarity; // gaps between patterns / lakes
    private float persistance;

    public Noise noise;

    public AnimationCurve TerrainHeightCurve;

    
}
