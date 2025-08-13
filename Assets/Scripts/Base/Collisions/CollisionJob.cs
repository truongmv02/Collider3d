using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct CollisionJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<float3> positions;

    [ReadOnly]
    public NativeArray<float3> speeds;

    [ReadOnly]
    public NativeArray<float3> sizes;

    [ReadOnly]
    public NativeHashMap<int2, NativeList<int>> chunkDict;

    
    public void Execute(int index)
    {
        float3 position = positions[index];
        int2 key;
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                key.x = x + (int)math.floor(position.x);
                key.y = y + (int)math.floor(position.z);

                if (chunkDict.TryGetValue(key, out NativeList<int> list))
                {
                    
                }
            }
        }
    }
    
    
    
    
}