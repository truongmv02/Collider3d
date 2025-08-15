using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile(FloatMode = FloatMode.Fast)]
public struct CollisionJob : IJobParallelFor
{
    [ReadOnly]
    public NativeList<float3> positions;

    [ReadOnly]
    public NativeList<float3> sizes;

    [ReadOnly]
    public NativeParallelMultiHashMap<int2, int> chunks;

    [NativeDisableParallelForRestriction]
    public NativeArray<float3> deltaPositions;

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

                NativeParallelMultiHashMapIterator<int2> it;
                int otherIndex;
                if (!chunks.TryGetFirstValue(key, out otherIndex, out it)) continue;
                do
                {
                    if (otherIndex == index) continue;
                    CheckCollision(index, otherIndex);
                } while (chunks.TryGetNextValue(out otherIndex, ref it));

                // if (chunkDict.TryGetValue(key, out NativeList<int> colliders))
                // {
                //     CheckCollision(index, colliders);
                // }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckCollision(int index, int otherIndex)
    {
        float3 position = positions[index];
        float3 size = sizes[index];
        float3 otherPosition = positions[otherIndex];
        float3 otherSize = sizes[otherIndex];
        float3 delta = otherPosition - position;
        float distSquared = math.lengthsq(delta);
        float radiusSum = size.x + otherSize.x;
        if (distSquared >= radiusSum * radiusSum) return;
        float dist = math.sqrt(distSquared);
        float3 normal = dist > 0 ? delta / dist : new float3(1, 0, 0);
        normal.y = 0;
        float overlap = radiusSum - dist;
        deltaPositions[index] -= normal * (overlap * 0.5f);
        deltaPositions[otherIndex] += normal * (overlap * 0.5f);
    }
}

[BurstCompile]
public struct ApplyDeltaJob : IJobParallelFor
{
    public NativeArray<float3> positions;

    [ReadOnly]
    public NativeArray<float3> deltaPositions;

    public void Execute(int index)
    {
        positions[index] += deltaPositions[index];
    }
}

[BurstCompile]
public struct AssignToChunkJob : IJobParallelFor
{
    [ReadOnly]
    public NativeList<float3> positions;

    public float chunkSize;

    public NativeParallelMultiHashMap<int2, int>.ParallelWriter chunks;

    public void Execute(int index)
    {
        float3 pos = positions[index];
        int2 cell = GetCellIndex(pos, chunkSize);
        chunks.Add(cell, index);
    }

    private static int2 GetCellIndex(float3 position, float cellSize)
    {
        return new int2(
            (int)math.floor(position.x / cellSize),
            (int)math.floor(position.z / cellSize)
        );
    }
}