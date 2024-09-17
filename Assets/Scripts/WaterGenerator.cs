using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class WaterGenerator : MonoBehaviour
{
    public List<Mesh> waterMeshes;

    private float _mapSize = 0;
    private int _chunkSize = 0;
    private float _borderDistanceFromMiddle = 0;

    private int[] _triangleList;
    private Vector2[] _uvList;

}
