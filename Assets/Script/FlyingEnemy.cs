using UnityEngine;
 
/// <summary>
/// FlyingEnemy - ม้าบินตาม Waypoints วน Loop
///
/// useTurnSystem = true  → บินตาม Player Turn (ค่อยๆ พุ่งไปทีละจุด)
/// useTurnSystem = false → บินอิสระ Update ปกติ
/// </summary>
public class FlyingEnemy : MonoBehaviour, ITurnTaker
{
    [Header("Waypoints")]
    [Tooltip("ลาก Empty GameObject หลายๆ จุดมาใส่เป็นเส้นทางบิน")]
    public Transform[] waypoints;
 
    [Header("Movement")]
    public float moveSpeed = 3f;
    public bool  flipSprite = true;
 
    [Header("Turn Settings")]
    [Tooltip("เปิด → บินตาม Player Turn (ค่อยๆ พุ่งไปทีละจุด)\nปิด → บินอิสระแบบปกติ")]
    public bool useTurnSystem = false;
 
    private int            currentWaypointIndex = 0;
    private SpriteRenderer sr;
    private bool           isMovingToWaypoint   = false;
 
    // ══════════════════════════════════════════════
    //  UNITY
    // ══════════════════════════════════════════════
 
    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
 
        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogWarning("FlyingEnemy: ยังไม่ได้ตั้ง Waypoints!");
            enabled = false;
            return;
        }
 
        transform.position = waypoints[0].position;
 
        if (useTurnSystem)
            TurnManager.Instance?.Register(this);
    }
 
    void OnDestroy()
    {
        if (useTurnSystem)
            TurnManager.Instance?.Unregister(this);
    }
 
    void Update()
    {
        if (useTurnSystem)
        {
            // Turn Mode → แค่เคลื่อนที่ไปยัง Waypoint ปัจจุบัน (OnTurn เซต Waypoint ถัดไปให้)
            if (isMovingToWaypoint)
                MoveToCurrentWaypoint();
        }
        else
        {
            // Free Mode → บินอิสระแบบปกติ
            MoveToCurrentWaypoint();
        }
    }
 
    // ══════════════════════════════════════════════
    //  ITURNTAKER
    // ══════════════════════════════════════════════
 
    public void OnTurn()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        if (isMovingToWaypoint) return;   // ยังบินไม่ถึงจุดก่อน → รอก่อน
 
        // ตั้ง Waypoint ถัดไปแล้วให้ Update() พาบินไปเอง
        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        isMovingToWaypoint   = true;
    }
 
    // ══════════════════════════════════════════════
    //  MOVEMENT
    // ══════════════════════════════════════════════
 
    void MoveToCurrentWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return;
 
        Transform target = waypoints[currentWaypointIndex];
 
        // ค่อยๆ เคลื่อนที่ไปยัง Waypoint
        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            moveSpeed * Time.deltaTime
        );
 
        // Flip Sprite ตามทิศทาง
        if (flipSprite && sr != null)
        {
            float dx = target.position.x - transform.position.x;
            if (Mathf.Abs(dx) > 0.01f) sr.flipX = dx < 0;
        }
 
        // ถึง Waypoint แล้ว
        if (Vector3.Distance(transform.position, target.position) < 0.05f)
        {
            transform.position = target.position;
 
            if (useTurnSystem)
            {
                // Turn Mode → หยุดรอ Turn ถัดไป
                isMovingToWaypoint = false;
            }
            else
            {
                // Free Mode → ไปจุดถัดไปเลย
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            }
        }
    }
 
    // ══════════════════════════════════════════════
    //  COLLISION
    // ══════════════════════════════════════════════
 
    void OnTriggerEnter2D(Collider2D other)
    {
        other.GetComponent<PlayerHealth>()?.Die();
    }
 
    void OnCollisionEnter2D(Collision2D col)
    {
        col.gameObject.GetComponent<PlayerHealth>()?.Die();
    }
 
    // ══════════════════════════════════════════════
    //  GIZMOS
    // ══════════════════════════════════════════════
 
    void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length == 0) return;
 
        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            Gizmos.DrawSphere(waypoints[i].position, 0.15f);
            int next = (i + 1) % waypoints.Length;
            if (waypoints[next] != null)
                Gizmos.DrawLine(waypoints[i].position, waypoints[next].position);
        }
    }
}