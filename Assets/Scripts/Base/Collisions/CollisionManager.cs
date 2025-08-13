using System;
using System.Collections.Generic;
using TMV.Base;
using Unity.Collections;
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
    private NativeHashMap<int2, NativeList<int>> chunkDict;


    #region UNITY EVENT METHODS

    protected override void Awake()
    {
        base.Awake();
        colliderSpeeds = new NativeList<float3>(OBJECT_CAPACITY, Allocator.Persistent);
        colliderPositions = new NativeList<float3>(OBJECT_CAPACITY, Allocator.Persistent);
        colliderOffsets = new NativeList<float3>(OBJECT_CAPACITY, Allocator.Persistent);
        colliderSizes = new NativeList<float3>(OBJECT_CAPACITY, Allocator.Persistent);
        colliderRotations = new NativeList<quaternion>(OBJECT_CAPACITY, Allocator.Persistent);
        chunkDict = new NativeHashMap<int2, NativeList<int>>(64, Allocator.Persistent);
    }

    private void OnDestroy()
    {
        colliderPositions.Dispose();
        colliderSpeeds.Dispose();
        colliderOffsets.Dispose();
        colliderSizes.Dispose();
        colliderRotations.Dispose();
        
        var keys = chunkDict.GetKeyArray(Allocator.Temp);
        foreach (var key in keys)
        {
            if (chunkDict.TryGetValue(key, out var list))
            {
                list.Dispose(); 
            }
        }
        keys.Dispose();

        chunkDict.Dispose();
    }

    private void Update()
    {
    }

    #endregion


    public void AddCollider(ColliderBase collider)
    {
        if (collider == null) return;
        collider.Index = colliderList.Count;
        colliderList.Add(collider);
        colliderSpeeds.Add(collider.Speed);
        colliderPositions.Add(collider.transform.position);
        AddColliderToChunk(collider);
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

        RemoveColliderFromChunk(collider);

        int lastIndex = colliderList.Count - 1;
        colliderPositions[index] = colliderPositions[lastIndex];
        colliderSpeeds[index] = colliderSpeeds[lastIndex];

        var lastCollider = colliderList[lastIndex];
        colliderList[index] = lastCollider;
        colliderList[lastIndex] = collider;
        lastCollider.Index = index;

        colliderList.RemoveAt(lastIndex);
        colliderSpeeds.RemoveAt(lastIndex);
        colliderPositions.RemoveAt(lastIndex);

        collider.Index = -1;
    }

    private void AddColliderToChunk(ColliderBase collider)
    {
        var chunkKey = WorldToChunk(collider.transform.position);
        var chunk = GetChunk(chunkKey);
        chunk.Add(collider.Index);
    }

    private void RemoveColliderFromChunk(ColliderBase collider)
    {
        var chunkKey = WorldToChunk(collider.transform.position);

        if (!chunkDict.TryGetValue(chunkKey, out var chunk)) return;
        if (chunk.Length <= 1)
        {
            chunk.Dispose();
            chunkDict.Remove(chunkKey);
            return;
        }

        var index = chunk.IndexOf(collider.Index);
        chunk.RemoveAtSwapBack(index);
    }

    public NativeList<int> GetChunk(int2 position)
    {
        if (!chunkDict.TryGetValue(position, out var chunk))
        {
            var newChunk = new NativeList<int>(8, Allocator.Persistent);
            chunkDict.Add(position, newChunk);
            return newChunk;
        }

        return chunk;
    }

    private int2 WorldToChunk(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x / CHUNK_SIZE);
        int z = Mathf.FloorToInt(position.z / CHUNK_SIZE);
        return new int2(x, z);
    }
}