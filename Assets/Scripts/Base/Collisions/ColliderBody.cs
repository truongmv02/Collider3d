using System;
using System.Collections.Generic;
using UnityEngine;

public class ColliderBody : MonoBehaviour
{
    private static int id = 0;
    public int ID { set; get; }

    private ColliderBase[] colliders;

    // Collider Events
    private event Action<ColliderBase> onColliderEnter;
    private event Action<ColliderBase> onColliderStay;
    private event Action<ColliderBase> onColliderExit;


    #region UNITY EVENT METHODS

    private void Awake()
    {
        ID = id++;
        Debug.Log("body id: " + ID);
        colliders = GetComponentsInChildren<ColliderBase>();
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
}