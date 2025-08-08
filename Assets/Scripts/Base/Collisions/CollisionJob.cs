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
    public NativeArray<float3> centers;

    public void Execute(int index)
    {
        
    }
}