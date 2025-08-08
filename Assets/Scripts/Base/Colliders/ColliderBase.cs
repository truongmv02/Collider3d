using System;
using UnityEngine;

public abstract class ColliderBase : MonoBehaviour
{
    public int Index { set; get; }

    [SerializeField]
    protected CollisionListener eventListener;

    [SerializeField]
    protected Vector3 speed;

    public Vector3 Speed
    {
        get => speed;
        set => speed = value;
    }

    [SerializeField]
    protected Vector3 center;

    public Vector3 Center
    {
        get => center;
        set => center = value;
    }

    public Vector3 CenterReal => transform.rotation * center;

    public Vector3 WorldCenter => transform.position + CenterReal;


    #region UNITY EVENT METHODS

    protected void Awake()
    {
        
    }

    protected virtual void OnEnable()
    {
        CollisionManager.Instance.AddCollider(this);
    }

    protected virtual void OnDestroy()
    {
        CollisionManager.Instance.RemoveCollider(this);
    }

    protected virtual void OnDisable()
    {
        CollisionManager.Instance.RemoveCollider(this);
    }

    #endregion

    public CollisionListener GetCollisionListener()
    {
        return eventListener;
    }
    

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.matrix = transform.localToWorldMatrix;
    }
}