using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
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
    } = -1;

    [SerializeField]
    protected bool isTrigger;

    [SerializeField]
    protected bool isKinematic;

    [SerializeField]
    protected CollisionLayer layer;

    [SerializeField]
    protected CollisionLayer collisionMask;

    [SerializeField]
    protected CollisionLayer interactionMask;

    [SerializeField]
    protected Vector3 speed;

    [SerializeField]
    protected int priority ;

    public bool IsTrigger
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => isTrigger;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => isTrigger = value;
    }

    public bool IsKinematic
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => isKinematic;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => isKinematic = value;
    }

    public Vector3 Speed
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => speed;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            speed = value;
        }
    }
    
    public int Priority
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => priority;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            priority = value;
        }
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


    // Collision Event
    private event Action<ColliderBase> onColliderEnter;
    private event Action<ColliderBase> onColliderStay;
    private event Action<ColliderBase> onColliderExit;

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

    public void SetSpeed(Vector3 speed)
    {
        this.speed = speed;
        CollisionManager.Instance.SetSpeed(this, speed);
    }
    public void Destroy()
    {
        CollisionManager.Instance.RemoveCollider(this);
    }
    public override int GetHashCode()
    {
        return Index.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        return obj is ColliderBase other && this.Index == other.Index;
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.matrix = transform.localToWorldMatrix;
    }
}