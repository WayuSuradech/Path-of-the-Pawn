using UnityEngine;

/// <summary>
/// EnemyDrop - แนบที่ม้าบิน
/// เมื่อโดนกระสุน → ม้าตาย → ปล่อย Block สีฟ้าให้ตกลงมาด้วย Physics
///
/// วิธีใช้:
///   1. แนบ Script นี้ที่ม้า (GameObject เดียวกับ FlyingEnemy)
///   2. ลาก Block สีฟ้า (PushableBlock) มาใส่ช่อง dropBlock
///   3. Block สีฟ้าตั้งค่าเริ่มต้น: ซ่อนอยู่เหนือม้า, Rigidbody2D Kinematic, SetActive(false)
/// </summary>
public class EnemyDrop : MonoBehaviour
{
    [Header("Drop Settings")]
    [Tooltip("ลาก Block สีฟ้า (PushableBlock) มาใส่\nBlock ควร SetActive(false) ไว้ก่อน")]
    public GameObject dropBlock;

    [Tooltip("ตำแหน่ง Spawn ของ Block (ถ้าไม่ได้ตั้ง จะ Spawn ที่ตำแหน่งม้า)")]
    public Transform dropPoint;

    [Header("Death Effects (Optional)")]
    [Tooltip("Effect ตอนม้าตาย เช่น Particle")]
    public GameObject deathEffect;

    private bool isDead = false;

    /// <summary>
    /// เรียกจาก Projectile เมื่อกระสุนโดนม้า
    /// </summary>
    public void OnHitByProjectile()
    {
        if (isDead) return;
        isDead = true;

        Die();
    }

    void Die()
    {
        // Spawn death effect
        if (deathEffect != null)
        {
            Vector3 pos = dropPoint != null ? dropPoint.position : transform.position;
            Instantiate(deathEffect, pos, Quaternion.identity);
        }

        // ปล่อย Block สีฟ้าให้ตกลงมา
        DropBlock();

        // ทำลายม้า
        Destroy(gameObject);
    }

    void DropBlock()
    {
        if (dropBlock == null)
        {
            Debug.LogWarning("EnemyDrop: ยังไม่ได้ assign Drop Block!");
            return;
        }

        Vector3 spawnPos = dropPoint != null ? dropPoint.position : transform.position;
        dropBlock.transform.position = spawnPos;

        // เปิด Block
        dropBlock.SetActive(true);

        // เปิด Physics ให้ตกลงมา
        Rigidbody2D rb = dropBlock.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType    = RigidbodyType2D.Dynamic;
            rb.gravityScale = 3f;   // ตกเร็วๆ ดูดี
            rb.linearVelocity = Vector2.zero;
        }
    }
}
