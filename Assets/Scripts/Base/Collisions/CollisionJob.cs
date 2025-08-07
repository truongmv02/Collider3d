using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct CollisionJob : IJobParallelFor
{
    public NativeArray<float3> colliderPositions;
    public NativeArray<float3> colliderSpeeds;
    public void Execute(int index)
    {
        throw new System.NotImplementedException();
    }
}
