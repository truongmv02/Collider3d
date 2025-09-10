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
    public NativeList<bool> isTriggers, isKinematics;

    [ReadOnly]
    public NativeList<CollisionLayer> layers, collisionMasks, interactionMasks;

    [ReadOnly]
    public NativeList<float3> positions;

    [ReadOnly]
    public NativeList<float3> sizes;

    [ReadOnly]
    public NativeParallelMultiHashMap<int2, int> chunks;

    [ReadOnly]
    public NativeList<float3> speeds;

    [ReadOnly]
    public float chunkSize;

    [NativeDisableParallelForRestriction]
    public NativeArray<float3> deltaPositions;

    public NativeParallelMultiHashMap<int, int>.ParallelWriter collisions;

    public void Execute(int index)
    {
        float3 position = positions[index];
        int2 key;
        int cellX = (int)math.floor(position.x / chunkSize);
        int cellY = (int)math.floor(position.z / chunkSize);
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                key.x = cellX + x;
                key.y = cellY + y;

                NativeParallelMultiHashMapIterator<int2> it;
                int otherIndex;
                if (!chunks.TryGetFirstValue(key, out otherIndex, out it)) continue;
                do
                {
                    if (otherIndex <= index) continue;
                    CheckCollision(index, otherIndex);
                } while (chunks.TryGetNextValue(out otherIndex, ref it));
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CanCollide(CollisionLayer layerA, CollisionLayer maskA, CollisionLayer layerB, CollisionLayer maskB)
    {
        return (layerA & maskB) != 0 && (layerB & maskA) != 0;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsValidMask(int index, int otherIndex, out bool canCollide, out bool canInteract)
    {
        CollisionLayer layerA = layers[index];
        CollisionLayer layerB = layers[otherIndex];

        CollisionLayer collisionMaskA = collisionMasks[index];
        CollisionLayer collisionMaskB = collisionMasks[otherIndex];

        CollisionLayer interactionA = interactionMasks[index];
        CollisionLayer interactionB = interactionMasks[otherIndex];

        canCollide = CanCollide(layerA, collisionMaskA, layerB, collisionMaskB);
        canInteract = CanCollide(layerA, interactionA, layerB, interactionB);

        return canCollide || canInteract;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryComputeOverlap(int index, int otherIndex, out float3 normal, out float depth)
    {
        normal = float3.zero;
        depth = 0f;
        float3 posA = positions[index] + deltaPositions[index];
        float3 posB = positions[otherIndex] + deltaPositions[otherIndex];

        float radiusA = sizes[index].x;
        float radiusB = sizes[otherIndex].x;

        float dx = posB.x - posA.x;
        float dz = posB.z - posA.z;
        float distSq = dx * dx + dz * dz;
        float minDist = radiusA + radiusB;
        if (distSq >= minDist * minDist) return false;

        float dist = math.sqrt(distSq);
        if (dist < 1e-6f) return false; // tránh chia 0

        float invDist = 1f / dist;
        normal = new float3(dx * invDist, 0, dz * invDist);
        depth = minDist - dist;

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool HandleTriggerAndKinematic(int index, int otherIndex, float3 normal, float depth)
    {
        if (isTriggers[index] || isTriggers[otherIndex])
            return false;
        bool kinematicA = isKinematics[index];
        bool kinematicB = isKinematics[otherIndex];

        if (kinematicA && kinematicB) return false;

        if (kinematicA)
        {
            deltaPositions[otherIndex] += normal * depth;
            return false;
        }

        if (kinematicB)
        {
            deltaPositions[index] -= normal * depth;
            return false;
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ApplyCollisionResponse(int index, int otherIndex, float3 normal, float depth)
    {
        float3 velocity = speeds[index];
        float lenSq = math.lengthsq(velocity);
        bool hasVelocity = lenSq > 1e-6f;

        float dot = hasVelocity ? math.dot(velocity * math.rsqrt(lenSq), normal) : 0f;

        float3 correction = normal * (depth * 0.5f * 0.6f);

        if (dot > 0)
            deltaPositions[index] -= correction;
        else
            deltaPositions[otherIndex] += correction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckCollision(int index, int otherIndex)
    {
        if (!IsValidMask(index, otherIndex, out bool canCollide, out bool canInteract)) return;

        if (!TryComputeOverlap(index, otherIndex, out float3 normal, out float depth))
        {
            return;
        }


        if (canInteract)
        {
            collisions.Add(index, otherIndex);
        }

        if (!canCollide) return;

        if (!HandleTriggerAndKinematic(index, otherIndex, normal, depth))
        {
            return;
        }

        ApplyCollisionResponse(index,  otherIndex, normal, depth);

        // float3 position = positions[index] + deltaPositions[index];
        // float3 size = sizes[index];
        // float3 otherPosition = positions[otherIndex] + deltaPositions[otherIndex];
        // float3 otherSize = sizes[otherIndex];
        //
        // float distance =
        //     math.distance(new float2(position.x, position.z), new float2(otherPosition.x, otherPosition.z));
        // if (size.x + otherSize.x <= distance) return;
        //
        // float3 direction = otherPosition - position;
        // var normal = math.normalize(direction);
        // var depth = size.x + otherSize.x - distance ;
        //
        // float3 velocity = speeds[index];
        // float dot = math.dot(math.normalize(velocity), normal);
        //
        // float3 correction = normal * (depth * 0.5f * 0.6f);
        // if (dot > 0)
        // {
        //     deltaPositions[index] -= correction; 
        // }
        // else
        // {
        //     deltaPositions[otherIndex] += correction;
        // }
    }
}

[BurstCompile(FloatMode = FloatMode.Fast)]
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

[BurstCompile(FloatMode = FloatMode.Fast)]
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