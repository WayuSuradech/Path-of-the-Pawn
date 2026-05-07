using UnityEngine;

/// <summary>
/// FlyingEnemy - ม้าบินตาม Waypoints วน Loop
/// 
/// วิธีใช้:
///   1. แนบ Script นี้ที่ม้า
///   2. ตั้ง Waypoints ใน Inspector (ลาก Transform หลายๆ จุดมาใส่)
///   3. ปรับ moveSpeed ตามต้องการ
///   4. ม้าจะบินวน Loop ผ่านทุก Waypoint ไปเรื่อยๆ
///   5. ชน Player → Player ตาย
/// </summary>
public class FlyingEnemy : MonoBehaviour
{
    [Header("Waypoints")]
    [Tooltip("ลาก Empty GameObject หลายๆ จุดมาใส่เป็นเส้นทางบิน")]
    public Transform[] waypoints;

    [Header("Movement")]
    public float moveSpeed = 3f;

    [Tooltip("หันหน้าตามทิศที่บินไหม?")]
    public bool flipSprite = true;

    private int currentWaypointIndex = 0;
    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();

        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogWarning("FlyingEnemy: ยังไม่ได้ตั้ง Waypoints!");
            enabled = false;
            return;
        }

        // เริ่มที่ Waypoint แรก
        transform.position = waypoints[0].position;
    }

    void Update()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        MoveToWaypoint();
    }

    void MoveToWaypoint()
    {
        Transform target = waypoints[currentWaypointIndex];

        // เคลื่อนที่ไปยัง Waypoint ปัจจุบัน
        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            moveSpeed * Time.deltaTime
        );

        // Flip Sprite ตามทิศทาง
        if (flipSprite && sr != null)
        {
            float dirX = target.position.x - transform.position.x;
            if (Mathf.Abs(dirX) > 0.01f)
                sr.flipX = dirX < 0;
        }

        // ถึง Waypoint แล้ว → ไปจุดถัดไป (Loop)
        if (Vector3.Distance(transform.position, target.position) < 0.05f)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth health = other.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.Die();
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        PlayerHealth health = collision.gameObject.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.Die();
        }
    }

    // แสดงเส้นทางบินใน Scene View
    void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;

            Gizmos.DrawSphere(waypoints[i].position, 0.15f);

            // วาดเส้นเชื่อม Waypoints
            int next = (i + 1) % waypoints.Length;
            if (waypoints[next] != null)
                Gizmos.DrawLine(waypoints[i].position, waypoints[next].position);
        }
    }
}
