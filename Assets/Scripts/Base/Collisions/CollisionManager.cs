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
    private Dictionary<Vector2Int, Chunk<ColliderBase>> chuckDict = new Dictionary<Vector2Int, Chunk<ColliderBase>>();

    private NativeList<float3> colliderSpeeds;
    private NativeList<float3> colliderPositions;


    #region UNITY EVENT METHODS

    protected override void Awake()
    {
        base.Awake();
        colliderSpeeds = new NativeList<float3>(OBJECT_CAPACITY, Allocator.Persistent);
        colliderPositions = new NativeList<float3>(OBJECT_CAPACITY, Allocator.Persistent);
    }

    private void OnDestroy()
    {
        colliderPositions.Dispose();
        colliderSpeeds.Dispose();
    }

    #endregion


    public void AddCollider(ColliderBase newCollider)
    {
        newCollider.Index = colliderList.Count;
        colliderList.Add(newCollider);
        colliderSpeeds.Add(newCollider.Speed);
        colliderPositions.Add(newCollider.transform.position);
    }

    private void AddColliderToChunk(ColliderBase newCollider)
    {
        var chunkKey = WorldToChunk(newCollider.transform.position);
        var chunk = GetChunk(chunkKey);
        chunk.Add(newCollider);
    }

    public Chunk<ColliderBase> GetChunk(Vector2Int position)
    {
        if (!chuckDict.TryGetValue(position, out var chunk))
        {
            return new Chunk<ColliderBase>(position);
        }
        return chunk;
    }

    private Vector2Int WorldToChunk(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x / CHUNK_SIZE);
        int z = Mathf.FloorToInt(position.z / CHUNK_SIZE);
        return new Vector2Int(x, z);
    }
}