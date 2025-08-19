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