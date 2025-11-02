using System.Threading;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float bulletDecay;
    private float timer;

    private void Start()
    {
        timer = bulletDecay;
    }

    private void Update()
    {
        BulletDecay();
    }

    void BulletDecay()
    {
        timer -= Time.deltaTime;

        if(timer <= 0)
        {
            Destroy(gameObject);
        }
    }
}
