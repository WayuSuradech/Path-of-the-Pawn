using UnityEngine;

/// <summary>
/// Projectile - กระสุนไฟ
/// พุ่งไปตามทิศที่ Cannon กำหนด ถ้าโดนม้าให้เรียก Die()
/// </summary>
public class Projectile : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("ระยะทางสูงสุดก่อนหายไป")]
    public float maxDistance = 20f;

    [Tooltip("Layer ที่กระสุนจะหายไปเมื่อชน (เช่น Ground, Platform)")]
    public LayerMask destroyOnHitLayer;

    private Vector2 direction;
    private float speed;
    private Vector3 startPosition;

    /// <summary>
    /// เรียกจาก Cannon หลัง Spawn เพื่อตั้งทิศทางและความเร็ว
    /// </summary>
    public void Init(Vector2 dir, float spd)
    {
        direction     = dir.normalized;
        speed         = spd;
        startPosition = transform.position;
    }

    void Update()
    {
        // เคลื่อนที่
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        // หายไปถ้าเกินระยะ
        if (Vector3.Distance(startPosition, transform.position) >= maxDistance)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // โดนม้า (FlyingEnemy หรือ EnemyDrop)
        EnemyDrop enemyDrop = other.GetComponent<EnemyDrop>();
        if (enemyDrop != null)
        {
            enemyDrop.OnHitByProjectile();
            Destroy(gameObject);
            return;
        }

        // โดนม้าที่มี FlyingEnemy
        FlyingEnemy flyEnemy = other.GetComponent<FlyingEnemy>();
        if (flyEnemy != null)
        {
            Destroy(flyEnemy.gameObject);
            Destroy(gameObject);
            return;
        }

        // โดนพื้น/กำแพง
        if (destroyOnHitLayer == (destroyOnHitLayer | (1 << other.gameObject.layer)))
        {
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        EnemyDrop enemyDrop = collision.gameObject.GetComponent<EnemyDrop>();
        if (enemyDrop != null)
        {
            enemyDrop.OnHitByProjectile();
            Destroy(gameObject);
            return;
        }

        if (destroyOnHitLayer == (destroyOnHitLayer | (1 << collision.gameObject.layer)))
        {
            Destroy(gameObject);
        }
    }
}
