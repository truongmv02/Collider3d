using System;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField]
    public ColliderBase collider;

    public float speed = 5f;
    private void OnEnable()
    {
        collider.AddColliderEnterEvent(OnColliderEnter);
    }

    private void OnColliderEnter(ColliderBase other)
    {
        Debug.Log("OnColliderEnter");
        // collider.Destroy();
        // other.Destroy();
        // EnemyManager.Instance.Remove(other);
        // Destroy(collider.gameObject);
        // Destroy(other.gameObject);
    }
}