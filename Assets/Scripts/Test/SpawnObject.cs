using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class SpawnObject : MonoBehaviour
{
    public ColliderBase prefab;
    public int count = 500;
    private void Start()
    {
        count =PlayerPrefs.GetInt("count", count);
        for (int i = 0; i< count; i++)
        {
            var positionx = Random.Range(-30f, 30f);
            var positionz = Random.Range(-30f, 30f);
            var obj = Instantiate(prefab, new Vector3(positionx, 0, positionz), Quaternion.identity);
            EnemyManager.Instance.colliders.Add(obj);
            obj.transform.SetParent(transform);
        }
    }
}