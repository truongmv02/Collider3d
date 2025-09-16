using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMV.Base;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[DefaultExecutionOrder(-100)] 
public class CollisionManager : SingletonMonoBehaviour<CollisionManager>
{
    private const int OBJECT_CAPACITY = 1000;
    private const float CHUNK_SIZE = 1f;

    private const int COLLISION_MAX = 16;

    private List<ColliderBase> colliderList = new List<ColliderBase>(OBJECT_CAPACITY);
    private HashSet<ColliderBase> colliderRemoves = new HashSet<ColliderBase>(OBJECT_CAPACITY);

    private NativeList<CollisionLayer> layers;
    private NativeList<CollisionLayer> collisionMasks;
    private NativeList<CollisionLayer> interactionMasks;

    private NativeList<bool> isTriggers;
    private NativeList<bool> isKinematics;

    private NativeList<float3> colliderSpeeds;
    private NativeList<int> priorities;
    private NativeList<float3> colliderPositions; 
    private NativeList<float3> colliderSizes;
    private NativeList<float3> deltaPositions;
    private NativeList<float> colliderAngles; // deg
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
        priorities =  new NativeList<int>(OBJECT_CAPACITY, Allocator.Persistent);
        colliderSizes = new NativeList<float3>(OBJECT_CAPACITY, Allocator.Persistent);
        colliderAngles = new NativeList<float>(OBJECT_CAPACITY, Allocator.Persistent);
        deltaPositions = new NativeList<float3>(OBJECT_CAPACITY, Allocator.Persistent);
        chunks = new NativeParallelMultiHashMap<int2, int>(OBJECT_CAPACITY, Allocator.Persistent);
        collisions = new NativeParallelMultiHashMap<int, int>(OBJECT_CAPACITY * COLLISION_MAX, Allocator.Persistent);
        newCollisions = new NativeParallelMultiHashMap<int, int>(OBJECT_CAPACITY * COLLISION_MAX, Allocator.Persistent);
        Debug.Log("init list");
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
        priorities.Dispose();
        colliderPositions.Dispose();
        colliderSizes.Dispose();
        colliderAngles.Dispose();
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

        // for (int i = 0; i < count; i++)
        // {
        //     // colliderList[i].Transform.position = new Vector3(colliderPositions[i].x, 0, colliderPositions[i].z);
        //     colliderPositions[i] = colliderList[i].Transform.position;
        // }

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
            angles = colliderAngles,
            speeds = colliderSpeeds,
            priorities = priorities,
            chunkSize = CHUNK_SIZE,
            chunks = chunks,
            collisions = newCollisions.AsParallelWriter(),
        };
        JobHandle handle = job.Schedule(count, 32);
        handle.Complete();

        ApplyDeltaJob applyDeltaJob = new ApplyDeltaJob()
        {
            positions = colliderPositions,
            deltaPositions = deltaPositions,
        };
        applyDeltaJob.Schedule(count, 128).Complete();
        UpdateColliderPositions(colliderPositions);
        CheckCollision();
        RemoveCollisions();
        // chunks.Dispose();
        // deltaPositions.Dispose();
    }

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateColliderPositions(NativeList<float3> positions)
    {
        for (int i = 0, count = colliderPositions.Length; i < count; i++)
        {
            colliderList[i].Transform.position = new Vector3(positions[i].x, 0, positions[i].z);
            deltaPositions[i] = float3.zero;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckCollision()
    {
        
    //      int count = colliderList.Count;
    //
    // // Tập hợp tất cả cặp va chạm cũ + mới
    // var allPairs = new NativeHashSet<long>(count * 4, Allocator.Temp);
    //
    // // Add từ newCollisions
    // using (var keys = newCollisions.GetKeyArray(Allocator.Temp))
    // {
    //     foreach (var i in keys)
    //     {
    //         NativeParallelMultiHashMapIterator<int> it;
    //         int other;
    //         if (newCollisions.TryGetFirstValue(i, out other, out it))
    //         {
    //             do
    //             {
    //                 int a = math.min(i, other);
    //                 int b = math.max(i, other);
    //                 allPairs.Add(((long)a << 32) | (uint)b);
    //             }
    //             while (newCollisions.TryGetNextValue(out other, ref it));
    //         }
    //     }
    // }
    //
    // // Add từ collisions (cũ)
    // using (var keys = collisions.GetKeyArray(Allocator.Temp))
    // {
    //     foreach (var i in keys)
    //     {
    //         NativeParallelMultiHashMapIterator<int> it;
    //         int other;
    //         if (collisions.TryGetFirstValue(i, out other, out it))
    //         {
    //             do
    //             {
    //                 int a = math.min(i, other);
    //                 int b = math.max(i, other);
    //                 allPairs.Add(((long)a << 32) | (uint)b);
    //             }
    //             while (collisions.TryGetNextValue(out other, ref it));
    //         }
    //     }
    // }
    //
    // // Duyệt 1 lần duy nhất
    // foreach (var pair in allPairs)
    // {
    //     int a = (int)(pair >> 32);
    //     int b = (int)(pair & 0xffffffff);
    //
    //     bool inNew = newCollisions.ContainsKeyValue(a, b) || newCollisions.ContainsKeyValue(b, a);
    //     bool inOld = collisions.ContainsKeyValue(a, b) || collisions.ContainsKeyValue(b, a);
    //
    //     var col1 = colliderList[a];
    //     var col2 = colliderList[b];
    //
    //     if (inNew)
    //     {
    //         if (inOld)
    //         {
    //             col1.OnColliderStay(col2);
    //             col2.OnColliderStay(col1);
    //         }
    //         else
    //         {
    //             col1.OnColliderEnter(col2);
    //             col2.OnColliderEnter(col1);
    //         }
    //     }
    //     else if (inOld) // Exit
    //     {
    //         col1.OnColliderExit(col2);
    //         col2.OnColliderExit(col1);
    //     }
    // }
    //
    // allPairs.Dispose();
        
    
    
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
                    var col1 = colliderList[i];
                    var col2 = colliderList[other];
                    if (collisions.ContainsKeyValue(i, other))
                    {
                        col1.OnColliderStay(col2);
                        col2.OnColliderStay(col1);
                    }
                    else
                    {
                        col1.OnColliderEnter(col2);
                        col2.OnColliderEnter(col1);
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
                        var col1 = colliderList[i];
                        var col2 = colliderList[other];
                        col1.OnColliderExit(col2);
                        col2.OnColliderExit(col1);
                    }
                } while (collisions.TryGetNextValue(out other, ref it));
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RemoveCollisions()
    {
        foreach (var collider in colliderRemoves)
        {
            RemoveCollider(collider.Index);
        }

        colliderRemoves.Clear();
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
        priorities.Add(collider.Priority);
        colliderPositions.Add(collider.Transform.position);
        colliderAngles.Add(collider.Transform.localEulerAngles.y);
        ResizeChunk();
        ResizeCollisionMap();
    }

    public void RemoveCollider(ColliderBase collider)
    {
        if (collider == null) return;
        if (collider.Index == -1) return;
        colliderRemoves.Add(collider);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RemoveCollider(int index)
    {
        if (index >= colliderList.Count)
        {
            Debug.Log($"Collider index: {index} out of range");
            return;
        }

        ColliderBase collider = colliderList[index];

        RemoveCollisionsOf(index, ref collisions);
        int lastIndex = colliderList.Count - 1;
        isTriggers[index] = isTriggers[lastIndex];
        isKinematics[index] = isKinematics[lastIndex];
        layers[index] = layers[lastIndex];
        collisionMasks[index] = collisionMasks[lastIndex];
        interactionMasks[index] = interactionMasks[lastIndex];
        colliderPositions[index] = colliderPositions[lastIndex];
        colliderSpeeds[index] = colliderSpeeds[lastIndex];
        priorities[index] = priorities[lastIndex];
        colliderSizes[index] = colliderSizes[lastIndex];
        deltaPositions[index] = deltaPositions[lastIndex];
        colliderAngles[index] = colliderAngles[lastIndex];

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
        priorities.RemoveAt(lastIndex);
        colliderPositions.RemoveAt(lastIndex);
        deltaPositions.RemoveAt(lastIndex);

        collider.Index = -1;
    }

    private void RemoveCollisionsOf(int index, ref NativeParallelMultiHashMap<int, int> map)
    {
        var keys = map.GetKeyArray(Allocator.Temp);
        var pairList = new NativeList<int2>(map.Count(), Allocator.TempJob);

        foreach (var key in keys)
        {
            if (map.TryGetFirstValue(key, out var value, out var it))
            {
                do
                {
                    pairList.Add(new int2(key, value));
                } while (map.TryGetNextValue(out value, ref it));
            }
        }

        keys.Dispose();

        // Create new map
        var newMap = new NativeParallelMultiHashMap<int, int>(map.Capacity, Allocator.Persistent);

        // Run job
        var job = new RemoveCollisionJob
        {
            pairs = pairList.AsDeferredJobArray(),
            removeIndex = index,
            newMap = newMap.AsParallelWriter()
        };
        job.Schedule(pairList.Length, 64).Complete();

        // Swap
        map.Dispose();
        map = newMap;

        pairList.Dispose();
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