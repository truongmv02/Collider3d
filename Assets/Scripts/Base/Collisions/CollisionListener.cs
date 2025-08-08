using System;
using UnityEngine;

public class CollisionListener : MonoBehaviour
{

    // Collider Events
    private event Action<ColliderBase> onColliderEnter;
    private event Action<ColliderBase> onColliderStay;
    private event Action<ColliderBase> onColliderExit;
    
    
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

}
