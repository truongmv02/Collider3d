using System;
using System.Runtime.CompilerServices;
using UnityEngine;

public abstract class ColliderBase : MonoBehaviour
{
    public int Index
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    }

    [SerializeField]
    protected Vector3 speed;

    public Vector3 Speed
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => speed;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => speed = value;
    }


    public Vector3 WorldCenter => transform.position;


    #region UNITY EVENT METHODS

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