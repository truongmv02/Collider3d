using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class SpawnBullet : MonoBehaviour
{
    public Bullet bulletPrefab;

    float delayTime = 0.05f;
    private float time;

    private void Update()
    {
        if (!Input.GetMouseButton(0)) return;
        if (time == 0)
        {
            var bullet = Instantiate(bulletPrefab);
            bullet.collider.SetSpeed(bullet.speed * Time.deltaTime * new Vector3(Random.Range(-1f,1f), 0f, Random.Range(-1f, 1f)));
            Debug.Log("set bullet speed");
        }

        else
        {
            time += Time.deltaTime;
            if (time >= delayTime)
            {
                time = 0;
            }
        }
    }
}