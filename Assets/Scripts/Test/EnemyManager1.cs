using System;
using System.Collections.Generic;
using TMV.Base;
using UnityEngine;

public class EnemyManager1 : SingletonMonoBehaviour<EnemyManager1>
{
    public GameObject target;

    public List<Rigidbody> colliders = new List<Rigidbody>(1000);


    private void Update()
    {
        for (int i = 0, count = colliders.Count; i < count; i++)
        {
            var collider = colliders[i];
            var direction = target.transform.position - collider.transform.position;
            direction.Normalize();
            direction.y = 0;
            collider.velocity = 500f * Time.deltaTime * direction;
            // CollisionManager.Instance.SetSpeed(collider, collider.Speed);
        }
    }
}