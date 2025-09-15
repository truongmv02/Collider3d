using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class SpawnBullet : MonoBehaviour
{
    public Bullet bulletPrefab;

    float delayTime = 5f;
    private float time;

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        StartCoroutine(Spawn());
    }

    IEnumerator Spawn()
    {
        for (int i = 0; i < 100; i++)
        {
            var bullet = Instantiate(bulletPrefab);
            bullet.collider.SetSpeed(bullet.speed * Time.deltaTime *
                                     new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)));
            if (i % 20 == 0) yield return null;
        }
    }
}