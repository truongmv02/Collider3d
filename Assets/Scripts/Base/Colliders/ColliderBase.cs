using System;
using System.Runtime.CompilerServices;
using UnityEngine;

public abstract class ColliderBase : MonoBehaviour
{
    [field: SerializeField]
    public int Index
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    }

    [SerializeField]
    protected CollisionLayer layer;

    [SerializeField]
    protected CollisionLayer collisionMask;

    [SerializeField]
    protected CollisionLayer interactionMask;

    [SerializeField]
    protected Vector3 speed;


    public Vector3 Speed
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => speed;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => speed = value;
    }

    private Transform transformCache;

    public Transform Transform
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => transformCache;
    }

    public CollisionLayer Layer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => layer;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => layer = value;
    }

    public CollisionLayer CollisionMask
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => collisionMask;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => collisionMask = value;
    }

    public CollisionLayer InteractionMask
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => interactionMask;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => interactionMask = value;
    }


    public Vector3 WorldCenter => transform.position;


    #region UNITY EVENT METHODS

    protected virtual void Awake()
    {
        transformCache = transform;
    }

    protected virtual void OnEnable()
    {
        CollisionManager.Instance.AddCollider(this);
    }

    protected virtual void OnDestroy()
    {
        // CollisionManager.Instance.RemoveCollider(this);
    }

    protected virtual void OnDisable()
    {
        // CollisionManager.Instance.RemoveCollider(this);
    }

    #endregion


    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.matrix = transform.localToWorldMatrix;
    }
}