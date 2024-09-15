using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct GenerateChunkJob : IJob
{
    public int chunkSize;
    public int2 chunkPos;
    public float seed;
    public NativeArray<Vector3> vertices;
    public NativeArray<float> falloffMap;
    [ReadOnly] public NativeArray<Keyframe> animationCurveKeys;
    public float minMapHeight;
    public float maxMapHeight;
    public float2 worldChunkPos;
    public float3 worldMiddlePoint;
    public float worldBorderDistance;
    public float noiseLayer0Scale;
    public float noiseLayer0HeightScale;
    public float noiseLayer1Scale;

    public void Execute()
    {
        int totalVertices = (chunkSize + 1) * (chunkSize + 1);

        for (int i = 0, z = 0; z <= chunkSize; z++)
        {
            for (int x = 0; x <= chunkSize; x++, i++)
            {
                int index = z * (chunkSize + 1) + x;

                // Compute noise values using job-compatible noise functions
                float2 noisePosition = (float2)(chunkPos + new int2(x, z)) * noiseLayer0Scale + seed;
                float noiseValue = noise.snoise(noisePosition);
                noiseValue = (noiseValue + 1f) * 0.5f; // Normalize to 0-1

                float2 noisePosition2 = (float2)(chunkPos + new int2(x, z)) * noiseLayer1Scale + seed;
                float noiseValue2 = noise.snoise(noisePosition2);
                noiseValue2 = (noiseValue2 + 1f) * 0.5f; // Normalize to 0-1

                float currentVertHeight = noiseValue;
                currentVertHeight = math.clamp(currentVertHeight - falloffMap[index], 0f, 1f);
                currentVertHeight *= noiseValue2;

                // Apply the baked animation curve
                currentVertHeight = EvaluateCurve(animationCurveKeys, currentVertHeight) * noiseLayer0HeightScale;
                currentVertHeight *= noiseValue;

                // Border generation
                float3 vertexWorldPos = new float3(worldChunkPos.x + x, 0, worldChunkPos.y + z);
                float distance = math.distance(vertexWorldPos, worldMiddlePoint);

                if (distance <= worldBorderDistance)
                {
                    minMapHeight = math.min(minMapHeight, currentVertHeight);
                    maxMapHeight = math.max(maxMapHeight, currentVertHeight);
                    vertices[index] = new float3(x, currentVertHeight, z);
                }
                else
                {
                    float3 towardsMiddle = worldMiddlePoint - vertexWorldPos;
                    float distanceVertexToBorder = distance - worldBorderDistance;

                    if (distance < worldBorderDistance + 2)
                    {
                        vertices[index] = new float3(x, 1f, z);
                    }
                    else if (distance >= worldBorderDistance + 2 && distance < worldBorderDistance + 3.5f)
                    {
                        vertices[index] = new float3(x, -distanceVertexToBorder, z);
                    }
                    else if (distance >= worldBorderDistance + 3.5f)
                    {
                        vertices[index] = new float3(x, -3.5f, z) + math.normalize(towardsMiddle) * distanceVertexToBorder;
                    }
                }
            }
        }
    }

    private float EvaluateCurve(NativeArray<Keyframe> keys, float time)
    {
        // Implement linear interpolation for the animation curve
        if (keys.Length == 0)
            return 0f;

        if (time <= keys[0].time)
            return keys[0].value;

        if (time >= keys[keys.Length - 1].time)
            return keys[keys.Length - 1].value;

        for (int i = 0; i < keys.Length - 1; i++)
        {
            if (time >= keys[i].time && time <= keys[i + 1].time)
            {
                float t = (time - keys[i].time) / (keys[i + 1].time - keys[i].time);
                return math.lerp(keys[i].value, keys[i + 1].value, t);
            }
        }

        return 0f;
    }
}
