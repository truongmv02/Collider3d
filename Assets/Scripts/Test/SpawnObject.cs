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
        var range = 60f;
        for (int i = 0; i< count; i++)
        {
            var positionx = Random.Range(-range, range);
            var positionz = Random.Range(-range, range);
            var obj = Instantiate(prefab, new Vector3(positionx, 0, positionz), Quaternion.identity);
            EnemyManager.Instance.colliders.Add(obj);
            obj.transform.SetParent(transform);
        }
    }
}