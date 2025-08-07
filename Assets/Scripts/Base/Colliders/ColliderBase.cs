using System;
using UnityEngine;

public abstract class ColliderBase : MonoBehaviour
{
    public int Index { set; get; }

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

    // Collider Events
    private event Action<ColliderBase> onColliderEnter;
    private event Action<ColliderBase> onColliderStay;
    private event Action<ColliderBase> onColliderExit;

    #region UNITY EVENT METHODS

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

    
    #region CALL EVENTS

    public virtual void OnColliderEnter(ColliderBase other)
    {
        onColliderEnter?.Invoke(other);
    }

    public virtual void OnColliderStay(ColliderBase other)
    {
        onColliderStay?.Invoke(other);
    }

    public virtual void OnColliderExit(ColliderBase other)
    {
        onColliderExit?.Invoke(other);
    }

    #endregion


    #region ADD AND REMOVE COLLIDER EVENTS

    public virtual void AddColliderEnterEvent(Action<ColliderBase> onEnter)
    {
        onColliderEnter += onEnter;
    }

    public virtual void AddColliderStayEvent(Action<ColliderBase> onStay)
    {
        onColliderStay += onStay;
    }

    public virtual void AddColliderExitEvent(Action<ColliderBase> onExit)
    {
        onColliderExit += onExit;
    }

    public virtual void RemoveColliderEnterEvent(Action<ColliderBase> onEnter)
    {
        onColliderEnter -= onEnter;
    }

    public virtual void RemoveColliderStayEvent(Action<ColliderBase> onStay)
    {
        onColliderStay -= onStay;
    }

    public virtual void RemoveColliderExitEvent(Action<ColliderBase> onExit)
    {
        onColliderExit -= onExit;
    }

    #endregion

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.matrix = transform.localToWorldMatrix;
    }
}