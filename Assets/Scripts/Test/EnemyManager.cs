using System;
using System.Collections.Generic;
using TMV.Base;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemyManager : SingletonMonoBehaviour<EnemyManager>
{
    public GameObject target;

    public List<ColliderBase> colliders = new List<ColliderBase>(1000);
    private CollisionManager collisionManager;
    private void Start()
    {
         collisionManager = CollisionManager.Instance;
    }

    public void Remove(ColliderBase collider)
    {
        colliders.Remove(collider);
    }

    private void Update()
    {
        for (int i = 0, count = colliders.Count; i < count; i++)
        {
            var collider = colliders[i];
            var direction = target.transform.position - collider.transform.position;
            direction.Normalize();
            direction.y = 0;
            collider.Speed =Random.Range(1f, 2f) * Time.fixedDeltaTime * direction;
            collisionManager.SetSpeed(collider, collider.Speed);
        }
    }
}