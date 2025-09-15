using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct RemoveCollisionJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<int2> pairs;
    public int removeIndex;
    public NativeParallelMultiHashMap<int, int>.ParallelWriter newMap;

    public void Execute(int i)
    {
        var kv = pairs[i];
        if (kv.x != removeIndex && kv.y != removeIndex)
        {
            newMap.Add(kv.x, kv.y);
        }
    }
}