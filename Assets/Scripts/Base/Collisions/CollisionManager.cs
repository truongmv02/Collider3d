using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMV.Base;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class CollisionManager : SingletonMonoBehaviour<CollisionManager>
{
    private const int OBJECT_CAPACITY = 1000;
    private const float CHUNK_SIZE = 1f;

    private List<ColliderBase> colliderList = new List<ColliderBase>();

    private NativeList<float3> colliderSpeeds;
    private NativeList<float3> colliderPositions;
    private NativeList<quaternion> colliderRotations;
    private NativeList<float3> colliderOffsets;
    private NativeList<float3> colliderSizes;
    private NativeList<float3> deltaPositions;
    private NativeParallelMultiHashMap<int2, int> chunks;
    public NativeParallelMultiHashMap<int, int> collisions;

    #region UNITY EVENT METHODS

    protected override void Awake()
    {
        base.Awake();
        colliderSpeeds = new NativeList<float3>(OBJECT_CAPACITY, Allocator.Persistent);
        colliderPositions = new NativeList<float3>(OBJECT_CAPACITY, Allocator.Persistent);
        colliderOffsets = new NativeList<float3>(OBJECT_CAPACITY, Allocator.Persistent);
        colliderSizes = new NativeList<float3>(OBJECT_CAPACITY, Allocator.Persistent);
        colliderRotations = new NativeList<quaternion>(OBJECT_CAPACITY, Allocator.Persistent);
        deltaPositions = new NativeList<float3>(OBJECT_CAPACITY, Allocator.Persistent);
        chunks = new NativeParallelMultiHashMap<int2, int>(OBJECT_CAPACITY, Allocator.Persistent);
        collisions = new NativeParallelMultiHashMap<int, int>(OBJECT_CAPACITY, Allocator.Persistent);
    }

    private void OnDestroy()
    {
        Clear();
    }

    public void Clear()
    {
        colliderSpeeds.Dispose();
        colliderPositions.Dispose();
        colliderOffsets.Dispose();
        colliderSizes.Dispose();
        colliderRotations.Dispose();
        chunks.Dispose();
        deltaPositions.Dispose();
        collisions.Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetSpeed(ColliderBase collider, Vector3 speed)
    {
        colliderSpeeds[collider.Index] = speed;
    }

    private void FixedUpdate()
    {
        var count = colliderList.Count;
        MoveJob moveJob = new MoveJob()
        {
            positions = colliderPositions,
            speeds = colliderSpeeds,
        };
        moveJob.Schedule(count, 128).Complete();

        for (int i = 0; i < count; i++)
        {
            colliderList[i].transform.position = new Vector3(colliderPositions[i].x, 0, colliderPositions[i].z);
            // colliderPositions[i] = colliderList[i].transform.position;
        }


        chunks.Clear();

        AssignToChunkJob assignJob = new AssignToChunkJob()
        {
            positions = colliderPositions,
            chunks = chunks.AsParallelWriter(),
            chunkSize = CHUNK_SIZE
        };
        assignJob.Schedule(count, 128).Complete();

        CollisionJob job = new CollisionJob()
        {
            positions = colliderPositions,
            sizes = colliderSizes,
            deltaPositions = deltaPositions,
            speeds = colliderSpeeds,
            chunks = chunks
        };
        JobHandle handle = job.Schedule(count, 64);
        handle.Complete();

        ApplyDeltaJob applyDeltaJob = new ApplyDeltaJob()
        {
            positions = colliderPositions,
            deltaPositions = deltaPositions,
        };
        applyDeltaJob.Schedule(count, 128).Complete();
        UpdateColliderPositions(deltaPositions);

        // chunks.Dispose();
        // deltaPositions.Dispose();
    }

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateColliderPositions(NativeList<float3> positions)
    {
        for (int i = 0, count = colliderPositions.Length; i < count; i++)
        {
            colliderList[i].transform.position += new Vector3(positions[i].x, 0, positions[i].z);
            positions[i] = float3.zero;
        }
    }

    public void AddCollider(ColliderBase collider)
    {
        if (collider == null) return;
        collider.Index = colliderList.Count;
        switch (collider)
        {
            case CircleCollider3d circle:
                colliderSizes.Add(new float3(circle.Radius, 0, 0));
                break;

            case BoxCollider3d box:
                colliderSizes.Add(box.Size);
                break;
        }

        deltaPositions.Add(float3.zero);
        colliderList.Add(collider);
        colliderSpeeds.Add(collider.Speed);
        colliderPositions.Add(collider.transform.position);
    }

    public virtual void RemoveCollider(ColliderBase collider)
    {
        if (collider == null) return;
        Debug.Log("delete index: " + collider.Index + " ," + colliderList.Count);
        int index = collider.Index;
        if (index == -1) return;
        if (index >= colliderList.Count)
        {
            Debug.Log($"Collider index: {index} out of range");
            return;
        }

        int lastIndex = colliderList.Count - 1;
        colliderPositions[index] = colliderPositions[lastIndex];
        colliderSpeeds[index] = colliderSpeeds[lastIndex];
        colliderSizes[index] = colliderSizes[lastIndex];
        deltaPositions[index] = deltaPositions[lastIndex];

        var lastCollider = colliderList[lastIndex];
        colliderList[index] = lastCollider;
        colliderList[lastIndex] = collider;
        lastCollider.Index = index;

        colliderList.RemoveAt(lastIndex);
        colliderSpeeds.RemoveAt(lastIndex);
        colliderPositions.RemoveAt(lastIndex);
        colliderSpeeds.RemoveAt(lastIndex);
        deltaPositions.RemoveAt(lastIndex);

        collider.Index = -1;
    }

    private void ResizeCollisionMap()
    {
        int capacity = collisions.Capacity * 2;
        var newCollisions = new NativeParallelMultiHashMap<int, int>(capacity, Allocator.Persistent);
        var keys = collisions.GetKeyArray(Allocator.Temp);
        foreach (var key in keys)
        {
            if (collisions.TryGetFirstValue(key, out var value, out var it))
            {
                do
                {
                    newCollisions.Add(key, value);
                } while (collisions.TryGetNextValue(out value, ref it));
            }
        }
        keys.Dispose();
        collisions.Dispose();
        collisions = newCollisions;
    }
}