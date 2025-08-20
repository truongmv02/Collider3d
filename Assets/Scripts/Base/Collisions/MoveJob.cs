using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile(FloatMode = FloatMode.Fast)]
public struct MoveJob: IJobParallelFor
{
    public NativeArray<float3> positions;
    
    [ReadOnly]
    public NativeList<float3> speeds;
    public void Execute(int index)
    {
        positions[index] += speeds[index];
    }
}
