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

    private const int COLLISION_MAX = 16;

    private List<ColliderBase> colliderList = new List<ColliderBase>();

    private NativeList<CollisionLayer> layers;
    private NativeList<CollisionLayer> collisionMasks;
    private NativeList<CollisionLayer> interactionMasks;

    private NativeList<bool> isTriggers;
    private NativeList<bool> isKinematics;

    private NativeList<float3> colliderSpeeds;
    private NativeList<float3> colliderPositions;
    private NativeList<float3> colliderSizes;
    private NativeList<float3> deltaPositions;
    private NativeList<quaternion> colliderRotations;
    private NativeParallelMultiHashMap<int2, int> chunks;
    public NativeParallelMultiHashMap<int, int> collisions;
    public NativeParallelMultiHashMap<int, int> newCollisions;

    #region UNITY EVENT METHODS

    protected override void Awake()
    {
        base.Awake();
        isTriggers = new NativeList<bool>(OBJECT_CAPACITY, Allocator.Persistent);
        isKinematics = new NativeList<bool>(OBJECT_CAPACITY, Allocator.Persistent);
        layers = new NativeList<CollisionLayer>(OBJECT_CAPACITY, Allocator.Persistent);
        collisionMasks = new NativeList<CollisionLayer>(OBJECT_CAPACITY, Allocator.Persistent);
        interactionMasks = new NativeList<CollisionLayer>(OBJECT_CAPACITY, Allocator.Persistent);
        colliderPositions = new NativeList<float3>(OBJECT_CAPACITY, Allocator.Persistent);
        colliderSpeeds = new NativeList<float3>(OBJECT_CAPACITY, Allocator.Persistent);
        colliderSizes = new NativeList<float3>(OBJECT_CAPACITY, Allocator.Persistent);
        colliderRotations = new NativeList<quaternion>(OBJECT_CAPACITY, Allocator.Persistent);
        deltaPositions = new NativeList<float3>(OBJECT_CAPACITY, Allocator.Persistent);
        chunks = new NativeParallelMultiHashMap<int2, int>(OBJECT_CAPACITY, Allocator.Persistent);
        collisions = new NativeParallelMultiHashMap<int, int>(OBJECT_CAPACITY * COLLISION_MAX, Allocator.Persistent);
        newCollisions = new NativeParallelMultiHashMap<int, int>(OBJECT_CAPACITY * COLLISION_MAX, Allocator.Persistent);
    }

    private void OnDestroy()
    {
        Clear();
    }

    public void Clear()
    {
        isTriggers.Dispose();
        isKinematics.Dispose();
        layers.Dispose();
        collisionMasks.Dispose();
        interactionMasks.Dispose();
        colliderSpeeds.Dispose();
        colliderPositions.Dispose();
        colliderSizes.Dispose();
        colliderRotations.Dispose();
        chunks.Dispose();
        deltaPositions.Dispose();
        collisions.Dispose();
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetIsTrigger(ColliderBase collider, bool isTrigger)
    {
        isTriggers[collider.Index] = isTrigger;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetIsKinematic(ColliderBase collider, bool isKinematic)
    {
        isKinematics[collider.Index] = isKinematic;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetSpeed(ColliderBase collider, Vector3 speed)
    {
        colliderSpeeds[collider.Index] = speed;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetPosition(ColliderBase collider, Vector3 position)
    {
        colliderPositions[collider.Index] = position;
    }

    private void Update()
    {
        (collisions, newCollisions) = (newCollisions, collisions);

        var count = colliderList.Count;
        MoveJob moveJob = new MoveJob()
        {
            positions = colliderPositions,
            speeds = colliderSpeeds,
        };
        moveJob.Schedule(count, 128).Complete();

        for (int i = 0; i < count; i++)
        {
            // colliderList[i].Transform.position = new Vector3(colliderPositions[i].x, 0, colliderPositions[i].z);
            colliderPositions[i] = colliderList[i].Transform.position;
        }

        chunks.Clear();

        AssignToChunkJob assignJob = new AssignToChunkJob()
        {
            positions = colliderPositions,
            chunks = chunks.AsParallelWriter(),
            chunkSize = CHUNK_SIZE
        };
        assignJob.Schedule(count, 128).Complete();
        newCollisions.Clear();
        CollisionJob job = new CollisionJob()
        {
            isTriggers = isTriggers,
            isKinematics = isKinematics,
            layers = layers,
            collisionMasks = collisionMasks,
            interactionMasks = interactionMasks,
            positions = colliderPositions,
            sizes = colliderSizes,
            deltaPositions = deltaPositions,
            speeds = colliderSpeeds,
            chunkSize = CHUNK_SIZE,
            chunks = chunks,
            collisions = newCollisions.AsParallelWriter(),
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
        CheckCollision();
        // chunks.Dispose();
        // deltaPositions.Dispose();
    }

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateColliderPositions(NativeList<float3> positions)
    {
        for (int i = 0, count = colliderPositions.Length; i < count; i++)
        {
            colliderList[i].Transform.position += new Vector3(positions[i].x, 0, positions[i].z);
            positions[i] = float3.zero;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckCollision()
    {
        int count = colliderList.Count;
        for (int i = 0; i < count; i++)
        {
            NativeParallelMultiHashMapIterator<int> it;
            int other;
            if (newCollisions.TryGetFirstValue(i, out other, out it))
            {
                do
                {
                    if (i > other) continue;
                    if (collisions.ContainsKeyValue(i, other))
                    {
                        var col1 = colliderList[i];
                        var col2 = colliderList[other];
                        col1.OnColliderStay(col2);
                        col2.OnColliderStay(col1);
                        Debug.Log($"Stay {i}<->{other}");
                    }
                    else
                    {
                        Debug.Log($"Enter {i}<->{other}");
                    }
                } while (newCollisions.TryGetNextValue(out other, ref it));
            }
        }

        // OnTriggerExit
        for (int i = 0; i < count; i++)
        {
            NativeParallelMultiHashMapIterator<int> it;
            int other;
            if (collisions.TryGetFirstValue(i, out other, out it))
            {
                do
                {
                    if (i > other) continue;
                    if (!newCollisions.ContainsKeyValue(i, other))
                    {
                        Debug.Log($"Exit {i}<->{other}");
                    }
                } while (collisions.TryGetNextValue(out other, ref it));
            }
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

        isTriggers.Add(collider.IsTrigger);
        isKinematics.Add(collider.IsKinematic);
        layers.Add(collider.Layer);
        collisionMasks.Add(collider.CollisionMask);
        interactionMasks.Add(collider.InteractionMask);
        deltaPositions.Add(float3.zero);
        colliderList.Add(collider);
        colliderSpeeds.Add(collider.Speed);
        colliderPositions.Add(collider.Transform.position);
        ResizeChunk();
        ResizeCollisionMap();
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
        isTriggers[index] = isTriggers[lastIndex];
        isKinematics[index] = isKinematics[lastIndex];
        layers[index] = layers[lastIndex];
        collisionMasks[index] = collisionMasks[lastIndex];
        interactionMasks[index] = interactionMasks[lastIndex];
        colliderPositions[index] = colliderPositions[lastIndex];
        colliderSpeeds[index] = colliderSpeeds[lastIndex];
        colliderSizes[index] = colliderSizes[lastIndex];
        deltaPositions[index] = deltaPositions[lastIndex];

        var lastCollider = colliderList[lastIndex];
        colliderList[index] = lastCollider;
        colliderList[lastIndex] = collider;
        lastCollider.Index = index;


        colliderList.RemoveAt(lastIndex);
        isTriggers.RemoveAt(lastIndex);
        isKinematics.RemoveAt(lastIndex);
        layers.RemoveAt(lastIndex);
        collisionMasks.RemoveAt(lastIndex);
        interactionMasks.RemoveAt(lastIndex);
        colliderSpeeds.RemoveAt(lastIndex);
        colliderPositions.RemoveAt(lastIndex);
        deltaPositions.RemoveAt(lastIndex);


        collider.Index = -1;
    }

    private void ResizeCollisionMap()
    {
        int capacity = newCollisions.Capacity;
        if (colliderList.Count * COLLISION_MAX <= capacity) return;
        capacity *= 2;
        Debug.Log("capacity: " + capacity);
        var newCols = new NativeParallelMultiHashMap<int, int>(capacity, Allocator.Persistent);
        newCollisions = new NativeParallelMultiHashMap<int, int>(capacity, Allocator.Persistent);

        var keys = collisions.GetKeyArray(Allocator.Temp);
        foreach (var key in keys)
        {
            if (collisions.TryGetFirstValue(key, out var value, out var it))
            {
                do
                {
                    newCols.Add(key, value);
                } while (collisions.TryGetNextValue(out value, ref it));
            }
        }

        keys.Dispose();
        collisions.Dispose();
        collisions = newCols;
    }

    private void ResizeChunk()
    {
        int capacity = chunks.Capacity;
        if (colliderList.Count <= capacity) return;
        capacity *= 2;
        var newChunks = new NativeParallelMultiHashMap<int2, int>(capacity, Allocator.Persistent);
        chunks.Dispose();
        chunks = newChunks;
    }
}