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
    public NativeList<int> priorities;

    [ReadOnly]
    public NativeList<float> angles;

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

        float3 sizeA = sizes[index];
        float3 sizeB = sizes[otherIndex];

        bool isBoxA = sizeA.y != 0f;
        bool isBoxB = sizeB.y != 0f;

        if (!isBoxA && !isBoxB) // circle x circle
        {
            float radiusA = sizeA.x;
            float radiusB = sizeB.x;

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
        else if (isBoxA && isBoxB)
        {
            float angleA = angles[index];
            float angleB = angles[otherIndex];
            return OBBvsOBB(posA.xz, sizeA.xz * 0.5f, angleA,
                posB.xz, sizeB.xz * 0.5f, angleB,
                out normal, out depth);
        }
        // Circle - Box
        else
        {
            float angleA = angles[index];
            float angleB = angles[otherIndex];
            bool aIsBox = sizeA.y != 0;
            float2 boxPos = aIsBox ? posA.xz : posB.xz;
            float2 boxHalf = (aIsBox ? sizeA.xz : sizeB.xz) * 0.5f;
            float boxAngle = aIsBox ? angleA : angleB;

            float2 circlePos = aIsBox ? posB.xz : posA.xz;
            float circleRadius = aIsBox ? sizeB.x : sizeA.x;

            if (OBBvsCircle(boxPos, boxHalf, boxAngle, circlePos, circleRadius,
                    out normal, out depth))
            {
                if (!aIsBox) normal = -normal; // đảo lại nếu circle là A
                return true;
            }

            return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool OBBvsOBB(float2 posA, float2 halfA, float angleA,
        float2 posB, float2 halfB, float angleB,
        out float3 normal, out float depth)
    {
        normal = float3.zero;
        depth = float.MaxValue;

        float2x2 rotA = float2x2.Rotate(angleA);
        float2x2 rotB = float2x2.Rotate(angleB);

        NativeArray<float2> axes = new NativeArray<float2>(4, Allocator.Temp);
        axes[0] = rotA.c0;
        axes[1] = rotA.c1;
        axes[2] = rotB.c0;
        axes[3] = rotB.c1;

        float2 d = posB - posA;
        float minOverlap = float.MaxValue;
        float2 smallestAxis = float2.zero;

        for (int i = 0; i < 4; i++)
        {
            float2 axis = math.normalize(axes[i]);

            // Project A
            float projA = math.abs(math.dot(rotA.c0, axis)) * halfA.x +
                          math.abs(math.dot(rotA.c1, axis)) * halfA.y;
            // Project B
            float projB = math.abs(math.dot(rotB.c0, axis)) * halfB.x +
                          math.abs(math.dot(rotB.c1, axis)) * halfB.y;

            float dist = math.abs(math.dot(d, axis));
            float overlap = projA + projB - dist;
            if (overlap <= 0)
            {
                axes.Dispose();
                return false;
            }

            if (overlap < minOverlap)
            {
                minOverlap = overlap;
                smallestAxis = axis;
            }
        }

        axes.Dispose();

        normal = new float3(smallestAxis.x, 0, smallestAxis.y);
        if (math.dot(d, smallestAxis) < 0) normal = -normal;
        depth = minOverlap;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool OBBvsCircle(float2 boxPos, float2 halfExtents, float angle,
        float2 circlePos, float radius,
        out float3 normal, out float depth)
    {
        normal = float3.zero;
        depth = 0f;

        // rotate world -> box local (rotate by -angle)
        float2x2 rot = float2x2.Rotate(-angle);

        // circle position in box local space
        float2 localCircle = math.mul(rot, circlePos - boxPos);

        // closest point on the AABB (in local space)
        float2 closest = math.clamp(localCircle, -halfExtents, halfExtents);

        float2 diff = localCircle - closest;
        float distSq = math.lengthsq(diff);

        // no overlap
        if (distSq > radius * radius) return false;

        // normal and depth in the easy case (circle outside or touching)
        if (distSq > 1e-6f)
        {
            float dist = math.sqrt(distSq);
            float2 localNormal = diff / dist; // points from box surface -> circle center (local space)

            // transform local normal back to world: local->world = rot^T (because rot = R(-angle))
            float2 worldNormal2 = math.mul(math.transpose(rot), localNormal);

            normal = new float3(worldNormal2.x, 0f, worldNormal2.y);
            depth = radius - dist;
            return true;
        }
        else
        {
            // circle center is essentially on the box surface or inside the box
            // pick normal as direction from box center -> circle center in world space (or an axis if overlapping exactly)
            float2 worldDir = circlePos - boxPos;
            float worldLenSq = math.lengthsq(worldDir);

            if (worldLenSq > 1e-6f)
            {
                float invLen = math.rsqrt(worldLenSq);
                float2 worldNormal2 = worldDir * invLen;
                normal = new float3(worldNormal2.x, 0f, worldNormal2.y);

                // depth: radius + distance from circle center to nearest inner face (in local space)
                float2 distToEdge = halfExtents - math.abs(localCircle);
                float minDistToEdge = math.min(distToEdge.x, distToEdge.y);
                depth = radius + minDistToEdge;
            }
            else
            {
                // exactly at box center: use box local +X as fallback normal (rotated to world)
                float2 localAxis = new float2(1f, 0f);
                float2 worldNormal2 = math.mul(math.transpose(rot), localAxis);
                normal = new float3(worldNormal2.x, 0f, worldNormal2.y);

                float minDistToEdge = math.min(halfExtents.x, halfExtents.y);
                depth = radius + minDistToEdge;
            }

            return true;
        }
    }


    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // private bool OBBvsCircle(float2 boxPos, float2 halfExtents, float angle,
    //     float2 circlePos, float radius,
    //     out float3 normal, out float depth)
    // {
    //     normal = float3.zero;
    //     depth = 0;
    //
    //     // rotate circle into box local space
    //     float cosA = math.cos(-angle);
    //     float sinA = math.sin(-angle);
    //     float2x2 rot = new float2x2(cosA, -sinA, sinA, cosA);
    //
    //     float2 localCircle = math.mul(rot, circlePos - boxPos);
    //
    //     float2 closest = math.clamp(localCircle, -halfExtents, halfExtents);
    //
    //     float2 diff = localCircle - closest;
    //     float distSq = math.lengthsq(diff);
    //
    //     if (distSq > radius * radius) return false;
    //
    //     float dist = math.sqrt(distSq);
    //     if (dist < 1e-6f)
    //     {
    //         normal = new float3(0, 0, 1);
    //         depth = radius;
    //     }
    //     else
    //     {
    //         normal = new float3(diff.x, 0, diff.y) / dist;
    //         depth = radius - dist;
    //     }
    //
    //     return true;
    // }


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

        float3 correction = normal * (depth * 0.3f);

        var priorityA = priorities[index];
        var priorityB = priorities[otherIndex];

        if (priorityA == priorityB)
        {
            if (dot > 0)
                deltaPositions[index] -= correction;
            else
                deltaPositions[otherIndex] += correction;
            return;
        }

        if (priorityA > priorityB)
        {
            deltaPositions[otherIndex] += correction * 2f;
        }
        else
        {
            deltaPositions[index] -= correction * 2f;
        }
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

        ApplyCollisionResponse(index, otherIndex, normal, depth);
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