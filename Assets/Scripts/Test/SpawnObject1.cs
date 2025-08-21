using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class SpawnObject1 : MonoBehaviour
{
    public Rigidbody prefab;
    public int count = 500;
    private void Start()
    {
        for (int i = 0; i< count; i++)
        {
            var positionx = Random.Range(-12f, 12f);
            var positionz = Random.Range(-12f, 12f);
            var obj = Instantiate(prefab, new Vector3(positionx, 0, positionz), Quaternion.identity);
            EnemyManager1.Instance.colliders.Add(obj);
            obj.transform.SetParent(transform);
        }
    }
}