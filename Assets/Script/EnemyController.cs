using System.Collections;
using UnityEngine;
 
/// <summary>
/// EnemyController — Silksong Grunt Style
///
/// State Machine:
///   PATROL   → เดินไปมาระหว่าง 2 จุด (Grid-based), หยุดสั้นๆ ที่ขอบ
///   AGGRO    → ตรวจจับ Player ด้วย Detection Circle แล้วไล่ตาม
///   ATTACK   → ยิง 3 Raycast เช็คตำแหน่ง Player แล้วโจมตี
///   BACKSTEP → ถอยหลัง 1 ช่อง หลังโจมตี
///
/// การขึ้น Platform:
///   ใช้ Grid Step Up เหมือน PlayerController
///   ถ้าช่องข้างหน้าบล็อค แต่ช่องบนว่าง → ขึ้นทันที (ไม่ใช้ Physics Jump)
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyController : MonoBehaviour
{
    // ══════════════════════════════════════════════════
    //  STATE
    // ══════════════════════════════════════════════════
 
    public enum EnemyState { Patrol, Aggro, Attack, Backstep }
    public EnemyState currentState = EnemyState.Patrol;
 
    // ══════════════════════════════════════════════════
    //  INSPECTOR
    // ══════════════════════════════════════════════════
 
    [Header("Grid Settings")]
    [Tooltip("ต้องตรงกับ cellSize ของ PlayerController")]
    public float cellSize = 1f;
 
    [Header("Movement")]
    public float moveSpeed      = 4f;
    public float aggroMoveSpeed = 6f;
 
    [Tooltip("หน่วงระหว่างก้าว (วินาที)")]
    public float patrolStepInterval = 0.6f;
    public float aggroStepInterval  = 0.35f;
 
    [Header("── Patrol ──")]
    [Tooltip("จำนวนช่องที่เดินออกจากจุดเริ่มต้น (ซ้าย/ขวา)")]
    public int patrolRange = 4;
 
    [Tooltip("หน่วงเวลาหยุดที่ขอบก่อนหันกลับ")]
    public float patrolTurnPause = 0.5f;
 
    [Header("── Detection ──")]
    public float detectionRadius = 5f;
    public float deaggroRadius   = 7f;
 
    [Tooltip("Pause สั้นๆ ตอน 'ตื่น' เจอ Player")]
    public float aggroWakeDelay = 0.25f;
 
    [Header("── Attack Raycasts ──")]
    public float attackRange   = 1.5f;
    public float diagonalAngle = 30f;
    public LayerMask playerLayer;
 
    [Tooltip("Windup ก่อนตี")]
    public float attackWindup   = 0.2f;
    public float attackCooldown = 1.4f;
    public float attackDamage   = 10f;
 
    [Header("── Backstep ──")]
    public float backstepDelay = 0.15f;
 
    [Header("── Collision Layers ──")]
    [Tooltip("Layer พื้นหลัก (Base)")]
    public LayerMask groundLayer;
 
    [Tooltip("Layer Platform / กล่อง — ใช้เช็ค Step Up")]
    public LayerMask obstacleLayer;
 
    [Header("References")]
    public Transform player;
 
    // ══════════════════════════════════════════════════
    //  PRIVATE
    // ══════════════════════════════════════════════════
 
    private Rigidbody2D    rb;
    private SpriteRenderer sr;
 
    private Vector3 targetPosition;
    private bool    isMoving = false;
    private int     facingDirection = 1;
 
    private float patrolOriginX;
    private int   patrolDirection = 1;
 
    private float stepTimer   = 0f;
    private float attackTimer = 0f;
    private bool  isBusy      = false;
 
    // รวม Layer ทั้งสองสำหรับ Gravity check
    private LayerMask combinedLayer => groundLayer | obstacleLayer;
 
    // ══════════════════════════════════════════════════
    //  UNITY CALLBACKS
    // ══════════════════════════════════════════════════
 
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
 
        rb.gravityScale   = 0f;   // Grid-based ปิด gravity เหมือน Player
        rb.freezeRotation = true;
        rb.linearVelocity = Vector2.zero;
 
        transform.position = SnapToGrid(transform.position);
        targetPosition     = transform.position;
        patrolOriginX      = transform.position.x;
 
        if (player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) player = go.transform;
        }
 
        ApplyGridGravity();
    }
 
    void Update()
    {
        if (player == null || isBusy) return;
 
        attackTimer -= Time.deltaTime;
        stepTimer   -= Time.deltaTime;
 
        switch (currentState)
        {
            case EnemyState.Patrol:   UpdatePatrol(); break;
            case EnemyState.Aggro:    UpdateAggro();  break;
            case EnemyState.Attack:   break;
            case EnemyState.Backstep: break;
        }
    }
 
    void FixedUpdate()
    {
        if (!isMoving) return;
 
        float speed = (currentState == EnemyState.Aggro) ? aggroMoveSpeed : moveSpeed;
 
        // เคลื่อนที่ทั้ง X และ Y (รองรับ Step Up)
        Vector3 newPos = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.fixedDeltaTime);
        rb.MovePosition(newPos);
 
        if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
        {
            rb.MovePosition(targetPosition);
            transform.position = targetPosition;
            isMoving = false;
 
            // หลังถึงเป้าหมาย → เช็ค Gravity
            ApplyGridGravity();
        }
    }
 
    // ══════════════════════════════════════════════════
    //  GRID GRAVITY (เหมือน PlayerController)
    // ══════════════════════════════════════════════════
 
    void ApplyGridGravity()
    {
        for (int i = 0; i < 50; i++)
        {
            Vector3 below = new Vector3(transform.position.x, transform.position.y - cellSize, transform.position.z);
            if (!HasFloorAt(below))
            {
                targetPosition = below;
                isMoving = true;
                return;
            }
            else break;
        }
    }
 
    bool IsObstacle(Vector3 position)
    {
        // เช็คทั้ง Ground และ Platform — ป้องกันกรณี Platform อยู่ใน Layer Ground
        return Physics2D.OverlapBox(
            new Vector2(position.x, position.y + cellSize * 0.1f),
            Vector2.one * (cellSize * 0.8f),
            0f,
            combinedLayer
        ) != null;
    }
 
    bool HasFloorAt(Vector3 position)
    {
        Vector2 checkCenter = new Vector2(position.x, position.y - cellSize * 0.4f);
        return Physics2D.OverlapBox(
            checkCenter,
            new Vector2(cellSize * 0.6f, cellSize * 0.2f),
            0f,
            combinedLayer
        ) != null;
    }
 
    // ══════════════════════════════════════════════════
    //  GRID MOVEMENT (Step Up เหมือน PlayerController)
    // ══════════════════════════════════════════════════
 
    /// <summary>
    /// เดิน 1 ช่องไปทิศ dir พร้อม Step Up อัตโนมัติ
    /// เหมือน TryMove() ใน PlayerController
    /// </summary>
    void TryMoveGrid(int dir)
    {
        if (isMoving) return;
 
        Vector3 cur    = transform.position;
        float   nextX  = cur.x + cellSize * dir;
 
        // 1) Flat — ช่องข้างหน้าว่าง
        Vector3 flatPos = new Vector3(nextX, cur.y, cur.z);
        if (!IsObstacle(flatPos))
        {
            targetPosition = flatPos;
            isMoving = true;
            return;
        }
 
        // 2) Step Up — ช่องข้างหน้าบล็อค แต่ช่องบนว่าง
        Vector3 stepPos = new Vector3(nextX, cur.y + cellSize, cur.z);
        if (!IsObstacle(stepPos))
        {
            targetPosition = stepPos;
            isMoving = true;
            return;
        }
 
        // 3) บล็อคทั้งหมด → ไม่ขยับ
    }
 
    // ══════════════════════════════════════════════════
    //  STATE: PATROL
    // ══════════════════════════════════════════════════
 
    void UpdatePatrol()
    {
        if (IsPlayerInRange(detectionRadius))
        {
            StartCoroutine(WakeUpAggro());
            return;
        }
 
        if (isMoving || stepTimer > 0f) return;
 
        float distFromOrigin = transform.position.x - patrolOriginX;
        bool  atRightEdge    = distFromOrigin >= patrolRange * cellSize - 0.05f;
        bool  atLeftEdge     = distFromOrigin <= -patrolRange * cellSize + 0.05f;
 
        if ((patrolDirection == 1 && atRightEdge) || (patrolDirection == -1 && atLeftEdge))
        {
            StartCoroutine(PatrolTurn());
        }
        else
        {
            if (facingDirection != patrolDirection)
                SetFacing(patrolDirection);
            else
                TryMoveGrid(patrolDirection);
 
            stepTimer = patrolStepInterval;
        }
    }
 
    IEnumerator PatrolTurn()
    {
        isBusy = true;
        yield return new WaitForSeconds(patrolTurnPause);
        patrolDirection = -patrolDirection;
        SetFacing(patrolDirection);
        isBusy    = false;
        stepTimer = patrolStepInterval;
    }
 
    // ══════════════════════════════════════════════════
    //  STATE: AGGRO
    // ══════════════════════════════════════════════════
 
    IEnumerator WakeUpAggro()
    {
        isBusy = true;
        TurnToFacePlayer();
        yield return new WaitForSeconds(aggroWakeDelay);
        currentState = EnemyState.Aggro;
        isBusy       = false;
    }
 
    void UpdateAggro()
    {
        if (!IsPlayerInRange(deaggroRadius))
        {
            currentState = EnemyState.Patrol;
            stepTimer    = patrolStepInterval;
            return;
        }
 
        var hit = CheckAttackRaycasts();
        if (hit.collider != null && attackTimer <= 0f)
        {
            StartCoroutine(PerformAttack(hit));
            return;
        }
 
        if (isMoving || stepTimer > 0f) return;
 
        ChasePlayer();
        stepTimer = aggroStepInterval;
    }
 
    void ChasePlayer()
    {
        float dx       = player.position.x - transform.position.x;
        int   dirNeeded = dx > 0 ? 1 : -1;
 
        if (Mathf.Abs(dx) < cellSize * 0.5f) return;
 
        if (facingDirection != dirNeeded)
        {
            SetFacing(dirNeeded);
            return;
        }
 
        // ใช้ TryMoveGrid แทน Physics Jump
        // Step Up จะทำงานอัตโนมัติถ้าช่องข้างหน้าบล็อค
        TryMoveGrid(dirNeeded);
    }
 
    // ══════════════════════════════════════════════════
    //  ATTACK RAYCASTS
    // ══════════════════════════════════════════════════
 
    RaycastHit2D CheckAttackRaycasts()
    {
        var   origin  = (Vector2)transform.position;
        float rad     = diagonalAngle * Mathf.Deg2Rad;
        var   forward = new Vector2(facingDirection, 0f);
        var   back    = new Vector2(-facingDirection, 0f);
        var   diag    = new Vector2(facingDirection * Mathf.Cos(rad), -Mathf.Sin(rad));
 
        var h = Physics2D.Raycast(origin, forward, attackRange, playerLayer);
        if (h.collider != null) return h;
 
        h = Physics2D.Raycast(origin, diag, attackRange, playerLayer);
        if (h.collider != null) return h;
 
        h = Physics2D.Raycast(origin, back, attackRange, playerLayer);
        if (h.collider != null) { TurnToFacePlayer(); return h; }
 
        return default;
    }
 
    // ══════════════════════════════════════════════════
    //  ATTACK + BACKSTEP
    // ══════════════════════════════════════════════════
 
    IEnumerator PerformAttack(RaycastHit2D hit)
    {
        isBusy       = true;
        currentState = EnemyState.Attack;
        attackTimer  = attackCooldown;
 
        yield return new WaitForSeconds(attackWindup);
 
        hit.collider.SendMessage("TakeDamage", attackDamage, SendMessageOptions.DontRequireReceiver);
        Debug.Log($"[Enemy] โจมตี {attackDamage} dmg");
 
        currentState = EnemyState.Backstep;
        yield return new WaitForSeconds(backstepDelay);
 
        // Backstep ใช้ TryMoveGrid ถอยหลัง
        TryMoveGrid(-facingDirection);
        yield return new WaitUntil(() => !isMoving);
 
        currentState = IsPlayerInRange(detectionRadius) ? EnemyState.Aggro : EnemyState.Patrol;
        stepTimer    = aggroStepInterval;
        isBusy       = false;
    }
 
    // ══════════════════════════════════════════════════
    //  HELPERS
    // ══════════════════════════════════════════════════
 
    bool IsPlayerInRange(float radius)
        => player != null && Vector2.Distance(transform.position, player.position) <= radius;
 
    void SetFacing(int dir)
    {
        facingDirection = dir;
        if (sr != null) sr.flipX = (dir == -1);
    }
 
    void TurnToFacePlayer()
    {
        if (player == null) return;
        SetFacing(player.position.x >= transform.position.x ? 1 : -1);
    }
 
    Vector3 SnapToGrid(Vector3 p)
        => new Vector3(
            Mathf.Round(p.x / cellSize) * cellSize,
            Mathf.Round(p.y / cellSize) * cellSize,
            p.z);
 
    // ══════════════════════════════════════════════════
    //  PUBLIC API
    // ══════════════════════════════════════════════════
 
    public EnemyState GetState()  => currentState;
    public bool       IsAggro()   => currentState == EnemyState.Aggro || currentState == EnemyState.Attack;
    public int        GetFacing() => facingDirection;
 
    // ══════════════════════════════════════════════════
    //  GIZMOS
    // ══════════════════════════════════════════════════
 
    void OnDrawGizmosSelected()
    {
        // Detection
        Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.12f);
        Gizmos.DrawSphere(transform.position, detectionRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
 
        // Deaggro
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, deaggroRadius);
 
        // Patrol range
        float ox = Application.isPlaying ? patrolOriginX : transform.position.x;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(
            new Vector3(ox - patrolRange * cellSize, transform.position.y),
            new Vector3(ox + patrolRange * cellSize, transform.position.y));
 
        // Attack rays
        int   fd  = Application.isPlaying ? facingDirection : 1;
        float rad = diagonalAngle * Mathf.Deg2Rad;
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, new Vector3(fd, 0f) * attackRange);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawRay(transform.position, new Vector3(fd * Mathf.Cos(rad), -Mathf.Sin(rad)) * attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, new Vector3(-fd, 0f) * attackRange);
 
        // Step Up check zones (ม่วง)
        float nx = transform.position.x + cellSize * fd;
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(new Vector3(nx, transform.position.y, 0), Vector3.one * (cellSize * 0.8f));
        Gizmos.color = new Color(1f, 0f, 1f, 0.5f);
        Gizmos.DrawWireCube(new Vector3(nx, transform.position.y + cellSize, 0), Vector3.one * (cellSize * 0.8f));
    }
}