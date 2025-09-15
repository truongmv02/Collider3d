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
        collider.AddColliderStayEvent(OnColliderStay);
        collider.AddColliderExitEvent(OnColliderExit);

    }

    private void OnColliderEnter(ColliderBase other)
    {
        // Debug.Log("OnColliderEnter");
        // collider.Destroy();
        // other.Destroy();
        // EnemyManager.Instance.Remove(other);
        // Destroy(collider.gameObject);
        // Destroy(other.gameObject);
    }
    
    private void OnColliderStay(ColliderBase other)
    {
        // Debug.Log("OnColliderStay");
        // collider.Destroy();
        // other.Destroy();
        // EnemyManager.Instance.Remove(other);
        // Destroy(collider.gameObject);
        // Destroy(other.gameObject);
    }
    private void OnColliderExit(ColliderBase other)
    {
        // Debug.Log("OnColliderExit");
        // collider.Destroy();
        // other.Destroy();
        // EnemyManager.Instance.Remove(other);
        // Destroy(collider.gameObject);
        // Destroy(other.gameObject);
    }
}