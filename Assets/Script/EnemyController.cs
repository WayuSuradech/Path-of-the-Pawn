using UnityEngine;

/// <summary>
/// EnemyController - Chess Pawn Style Enemy
/// 
/// ระบบหลัก:
///   1. Detection Circle  → เช็คว่า Player อยู่ในระยะ Aggro หรือยัง
///   2. Attack Raycasts   → 3 เส้น (หน้า / เฉียงหน้า / หลัง) เช็คตำแหน่ง Player
///   3. Backstep          → ถอยหลัง 1 ช่อง หลังโจมตี
/// 
/// การทำงาน:
///   - ถ้า Player อยู่ใน Detection Circle → Enemy จะหันหน้าหา / เดินตาม (Grid-based)
///   - ถ้า Player อยู่ใน Attack Range (Raycast) → โจมตี แล้วถอย 1 ช่อง
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyController : MonoBehaviour
{
    // ══════════════════════════════════════════════════
    //  INSPECTOR FIELDS
    // ══════════════════════════════════════════════════

    [Header("Grid Settings")]
    [Tooltip("ต้องตรงกับ cellSize ของ PlayerController")]
    public float cellSize = 1f;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    [Tooltip("หน่วงเวลาระหว่างการเดินแต่ละก้าว (วินาที)")]
    public float moveInterval = 0.8f;

    [Header("─── 1. Detection Circle ───")]
    [Tooltip("รัศมีวงกลมตรวจจับ Player")]
    public float detectionRadius = 5f;

    [Header("─── 2. Attack Raycasts ───")]
    [Tooltip("ระยะ Raycast โจมตี")]
    public float attackRange = 1.5f;

    [Tooltip("มุมเฉียงของ Raycast ด้านข้าง (องศา)")]
    public float diagonalAngle = 30f;

    [Tooltip("Layer ของ Player")]
    public LayerMask playerLayer;

    [Header("─── 3. Backstep ───")]
    [Tooltip("ดีเลย์ก่อนถอยหลัง (วินาที)")]
    public float backstepDelay = 0.3f;

    [Header("Attack Settings")]
    [Tooltip("Cooldown ระหว่างการโจมตี (วินาที)")]
    public float attackCooldown = 1.2f;

    [Tooltip("Damage ที่ส่งผ่าน SendMessage (ถ้าไม่ใช้ให้ปล่อยว่าง)")]
    public float attackDamage = 10f;

    [Header("References")]
    public Transform player;
    public SpriteRenderer spriteRenderer;

    // ══════════════════════════════════════════════════
    //  PRIVATE STATE
    // ══════════════════════════════════════════════════

    private Rigidbody2D rb;

    // Grid movement
    private Vector3 targetPosition;
    private bool isMoving = false;
    private int facingDirection = 1;   // +1 = ขวา, -1 = ซ้าย

    // Timers
    private float moveTimer = 0f;
    private float attackTimer = 0f;

    // State
    private bool isBackstepping = false;

    // ══════════════════════════════════════════════════
    //  UNITY CALLBACKS
    // ══════════════════════════════════════════════════

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;

        transform.position = SnapToGrid(transform.position);
        targetPosition = transform.position;

        // Auto-find player ถ้าไม่ได้ drag ใน Inspector
        if (player == null)
        {
            GameObject go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) player = go.transform;
        }
    }

    void Update()
    {
        if (player == null) return;

        attackTimer -= Time.deltaTime;
        moveTimer -= Time.deltaTime;

        // ── 1. Detection Circle ──────────────────────
        if (!IsPlayerInDetectionRange()) return;

        // ── 2. Attack Raycasts ───────────────────────
        RaycastHitInfo hitInfo = CheckAttackRaycasts();

        if (hitInfo.hit && attackTimer <= 0f)
        {
            PerformAttack(hitInfo);
            return;   // โจมตีแล้ว ไม่ต้องเดินในเฟรมนี้
        }

        // ── Chase (เดินตาม) ──────────────────────────
        if (!isMoving && !isBackstepping && moveTimer <= 0f)
        {
            ChasePlayer();
        }
    }

    void FixedUpdate()
    {
        if (!isMoving) return;

        float newX = Mathf.MoveTowards(
            transform.position.x,
            targetPosition.x,
            moveSpeed * Time.fixedDeltaTime
        );
        transform.position = new Vector3(newX, transform.position.y, transform.position.z);

        if (Mathf.Abs(transform.position.x - targetPosition.x) < 0.01f)
        {
            transform.position = new Vector3(targetPosition.x, transform.position.y, transform.position.z);
            isMoving = false;
        }
    }

    // ══════════════════════════════════════════════════
    //  1. DETECTION CIRCLE
    // ══════════════════════════════════════════════════

    /// <summary>
    /// เช็คว่า Player อยู่ใน Detection Circle หรือไม่
    /// </summary>
    bool IsPlayerInDetectionRange()
    {
        return Vector2.Distance(transform.position, player.position) <= detectionRadius;
    }

    // ══════════════════════════════════════════════════
    //  2. ATTACK RAYCASTS
    // ══════════════════════════════════════════════════

    struct RaycastHitInfo
    {
        public bool hit;
        public bool playerIsBehind;   // true = Player อยู่ด้านหลัง Enemy
        public RaycastHit2D rayHit;
    }

    /// <summary>
    /// ยิง 3 Raycast: ตรงหน้า / เฉียงหน้า / หลัง
    /// คืนค่า HitInfo ตัวแรกที่โดน
    /// </summary>
    RaycastHitInfo CheckAttackRaycasts()
    {
        RaycastHitInfo info = new RaycastHitInfo();

        // ทิศหลัก
        Vector2 forward = new Vector2(facingDirection, 0f);
        Vector2 backward = new Vector2(-facingDirection, 0f);

        // Raycast 1: ตรงหน้า
        RaycastHit2D front = Physics2D.Raycast(
            transform.position, forward, attackRange, playerLayer
        );

        // Raycast 2: เฉียงหน้า (มุมลง)
        float rad = diagonalAngle * Mathf.Deg2Rad;
        Vector2 diagDir = new Vector2(
            facingDirection * Mathf.Cos(rad),
            -Mathf.Sin(rad)
        );
        RaycastHit2D diagonal = Physics2D.Raycast(
            transform.position, diagDir, attackRange, playerLayer
        );

        // Raycast 3: หลัง
        RaycastHit2D back = Physics2D.Raycast(
            transform.position, backward, attackRange, playerLayer
        );

        if (front.collider != null)
        {
            info.hit = true;
            info.playerIsBehind = false;
            info.rayHit = front;
        }
        else if (diagonal.collider != null)
        {
            info.hit = true;
            info.playerIsBehind = false;
            info.rayHit = diagonal;
        }
        else if (back.collider != null)
        {
            info.hit = true;
            info.playerIsBehind = true;   // Player อยู่ด้านหลัง
            info.rayHit = back;
        }

        return info;
    }

    // ══════════════════════════════════════════════════
    //  ATTACK + BACKSTEP
    // ══════════════════════════════════════════════════

    /// <summary>
    /// โจมตี Player แล้วถอยหลัง 1 ช่อง
    /// </summary>
    void PerformAttack(RaycastHitInfo hitInfo)
    {
        attackTimer = attackCooldown;

        // ถ้า Player อยู่ด้านหลัง → หันหน้าหาก่อนโจมตี
        if (hitInfo.playerIsBehind)
        {
            TurnToFacePlayer();
        }

        // ส่ง Damage (ใช้ SendMessage หรือ Interface ตามต้องการ)
        hitInfo.rayHit.collider.SendMessage(
            "TakeDamage", attackDamage, SendMessageOptions.DontRequireReceiver
        );

        Debug.Log($"[Enemy] โจมตี! Damage = {attackDamage}" +
                  (hitInfo.playerIsBehind ? " (Player อยู่ด้านหลัง)" : ""));

        // ── 3. Backstep ─────────────────────────────
        StartCoroutine(DoBackstep());
    }

    /// <summary>
    /// ถอยหลัง 1 ช่อง หลังโจมตี
    /// </summary>
    System.Collections.IEnumerator DoBackstep()
    {
        isBackstepping = true;
        yield return new WaitForSeconds(backstepDelay);

        // ถอยหลัง = ตรงข้ามทิศที่หันหน้า
        int backstepDir = -facingDirection;
        Vector3 backstepTarget = new Vector3(
            transform.position.x + cellSize * backstepDir,
            transform.position.y,
            transform.position.z
        );

        targetPosition = backstepTarget;
        isMoving = true;

        // รอจนถึงตำแหน่งแล้วค่อย unlock
        yield return new WaitUntil(() => !isMoving);

        isBackstepping = false;
        moveTimer = moveInterval;   // หน่วงก่อนจะเดินตามอีกครั้ง

        Debug.Log("[Enemy] ถอยหลัง 1 ช่อง");
    }

    // ══════════════════════════════════════════════════
    //  CHASE LOGIC
    // ══════════════════════════════════════════════════

    /// <summary>
    /// ตัดสินใจเดินตาม Player หรือหันหน้าหาก่อน
    /// (อิง Logic ของ PlayerController: หันก่อน → ค่อยเดิน)
    /// </summary>
    void ChasePlayer()
    {
        float dx = player.position.x - transform.position.x;
        if (Mathf.Abs(dx) < 0.01f) return;   // อยู่แถวเดียวกันแล้ว

        int dirToPlayer = dx > 0 ? 1 : -1;

        if (dirToPlayer == facingDirection)
        {
            // หันหน้าตรงกันแล้ว → เดิน 1 ช่อง
            MoveOneCell(dirToPlayer);
            moveTimer = moveInterval;
        }
        else
        {
            // หันหน้าไม่ตรง → หันก่อน (ไม่เดิน — เหมือน PlayerController)
            SetFacingDirection(dirToPlayer);
            moveTimer = moveInterval * 0.5f;   // หน่วงสั้นกว่าก่อนเดิน
        }
    }

    // ══════════════════════════════════════════════════
    //  GRID HELPERS
    // ══════════════════════════════════════════════════

    void MoveOneCell(int direction)
    {
        if (isMoving) return;

        targetPosition = new Vector3(
            transform.position.x + cellSize * direction,
            transform.position.y,
            transform.position.z
        );
        isMoving = true;
    }

    void SetFacingDirection(int direction)
    {
        facingDirection = direction;
        if (spriteRenderer != null)
            spriteRenderer.flipX = (direction == -1);
    }

    void TurnToFacePlayer()
    {
        float dx = player.position.x - transform.position.x;
        SetFacingDirection(dx >= 0 ? 1 : -1);
    }

    Vector3 SnapToGrid(Vector3 position)
    {
        return new Vector3(
            Mathf.Round(position.x / cellSize) * cellSize,
            Mathf.Round(position.y / cellSize) * cellSize,
            position.z
        );
    }

    // ══════════════════════════════════════════════════
    //  GIZMOS (Scene View)
    // ══════════════════════════════════════════════════

    void OnDrawGizmosSelected()
    {
        // 1. Detection Circle
        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        Gizmos.DrawSphere(transform.position, detectionRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // 2. Attack Raycasts
        Vector3 forward = new Vector3(facingDirection, 0f, 0f);
        Vector3 backward = new Vector3(-facingDirection, 0f, 0f);
        float rad = diagonalAngle * Mathf.Deg2Rad;
        Vector3 diagDir = new Vector3(
            facingDirection * Mathf.Cos(rad),
            -Mathf.Sin(rad),
            0f
        );

        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, forward * attackRange);     // หน้า

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawRay(transform.position, diagDir * attackRange);     // เฉียงหน้า

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, backward * attackRange);    // หลัง

        // Label Gizmo hint
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(
            transform.position + forward * attackRange, 0.05f
        );
    }
}
