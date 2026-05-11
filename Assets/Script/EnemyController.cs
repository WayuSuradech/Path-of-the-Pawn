using System.Collections;
using UnityEngine;

/// <summary>
/// EnemyController — Silksong Grunt Style
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
    public float moveSpeed = 4f;
    public float aggroMoveSpeed = 6f;

    [Tooltip("หน่วงระหว่างก้าว (วินาที)")]
    public float patrolStepInterval = 0.6f;
    public float aggroStepInterval = 0.35f;

    [Header("── Patrol ──")]
    [Tooltip("จำนวนช่องที่เดินออกจากจุดเริ่มต้น (ซ้าย/ขวา)")]
    public int patrolRange = 4;

    [Tooltip("หน่วงเวลาหยุดที่ขอบก่อนหันกลับ")]
    public float patrolTurnPause = 0.5f;

    [Header("── 1. Detection Circle ──")]
    public float detectionRadius = 5f;

    [Tooltip("ระยะ Deaggro (ใหญ่กว่า Detection เพื่อกัน oscillation)")]
    public float deaggroRadius = 7f;

    [Tooltip("Pause สั้นๆ ตอน 'ตื่น' เจอ Player (Silksong trait)")]
    public float aggroWakeDelay = 0.25f;

    [Header("── 2. Attack Raycasts ──")]
    public float attackRange = 1.5f;
    public float diagonalAngle = 30f;
    public LayerMask playerLayer;

    [Tooltip("Windup ก่อนตี (Anticipation — Silksong trait)")]
    public float attackWindup = 0.2f;
    public float attackCooldown = 1.4f;
    public float attackDamage = 10f;

    [Header("── 3. Backstep ──")]
    public float backstepDelay = 0.15f;

    [Header("── 4. Block / Jump Detection ──")]
    [Tooltip("Layer ของพื้นเรียบ (Ground)")]
    public LayerMask groundLayer;

    [Tooltip("Layer ของบล็อคชั้นๆ (Platform)")]
    public LayerMask platformLayer;

    [Tooltip("รัศมีเช็คพื้นใต้เท้า")]
    public float groundCheckRadius = 0.1f;

    [Tooltip("ความสูงสูงสุด (ช่อง) ที่ Enemy กระโดดข้ามได้")]
    public int maxJumpableHeight = 1;

    public float jumpForce = 8f;

    private LayerMask solidLayer => groundLayer | platformLayer;

    [Header("References")]
    public Transform player;

    // ══════════════════════════════════════════════════
    //  PRIVATE
    // ══════════════════════════════════════════════════

    private Rigidbody2D rb;
    private SpriteRenderer sr;

    private Vector3 targetPosition;
    private bool isMoving = false;
    private int facingDirection = 1;

    private float patrolOriginX;
    private int patrolDirection = 1;

    private float stepTimer = 0f;
    private float attackTimer = 0f;
    private bool isBusy = false;
    private bool isGrounded = false;

    // ══════════════════════════════════════════════════
    //  UNITY CALLBACKS
    // ══════════════════════════════════════════════════

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        rb.freezeRotation = true;

        transform.position = SnapToGrid(transform.position);
        targetPosition = transform.position;
        patrolOriginX = transform.position.x;

        if (player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) player = go.transform;
        }
    }

    void Update()
    {
        if (player == null || isBusy) return;

        attackTimer -= Time.deltaTime;
        stepTimer -= Time.deltaTime;

        CheckGround();

        switch (currentState)
        {
            case EnemyState.Patrol: UpdatePatrol(); break;
            case EnemyState.Aggro: UpdateAggro(); break;
            case EnemyState.Attack: break;
            case EnemyState.Backstep: break;
        }
    }

    void FixedUpdate()
    {
        if (!isMoving) return;

        float speed = (currentState == EnemyState.Aggro) ? aggroMoveSpeed : moveSpeed;
        float newX = Mathf.MoveTowards(
            transform.position.x, targetPosition.x, speed * Time.fixedDeltaTime
        );
        transform.position = new Vector3(newX, transform.position.y, transform.position.z);

        if (Mathf.Abs(transform.position.x - targetPosition.x) < 0.01f)
        {
            transform.position = new Vector3(targetPosition.x, transform.position.y, transform.position.z);
            isMoving = false;
        }
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
        bool atRightEdge = distFromOrigin >= patrolRange * cellSize - 0.05f;
        bool atLeftEdge = distFromOrigin <= -patrolRange * cellSize + 0.05f;

        if ((patrolDirection == 1 && atRightEdge) ||
            (patrolDirection == -1 && atLeftEdge))
        {
            StartCoroutine(PatrolTurn());
        }
        else
        {
            if (facingDirection != patrolDirection)
                SetFacing(patrolDirection);
            else
                MoveOneCell(patrolDirection);

            stepTimer = patrolStepInterval;
        }
    }

    IEnumerator PatrolTurn()
    {
        isBusy = true;
        yield return new WaitForSeconds(patrolTurnPause);
        patrolDirection = -patrolDirection;
        SetFacing(patrolDirection);
        isBusy = false;
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
        isBusy = false;
    }

    void UpdateAggro()
    {
        if (!IsPlayerInRange(deaggroRadius))
        {
            currentState = EnemyState.Patrol;
            stepTimer = patrolStepInterval;
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
        float dx = player.position.x - transform.position.x;
        int dirNeeded = dx > 0 ? 1 : -1;

        if (Mathf.Abs(dx) < cellSize * 0.5f) return;

        if (facingDirection != dirNeeded)
        {
            SetFacing(dirNeeded);
            return;
        }

        int blockHeight = GetBlockHeightAhead(dirNeeded);

        if (blockHeight == 0)
            MoveOneCell(dirNeeded);
        else if (blockHeight <= maxJumpableHeight && isGrounded)
            StartCoroutine(JumpMove(dirNeeded));
    }

    // ══════════════════════════════════════════════════
    //  BLOCK / JUMP DETECTION
    // ══════════════════════════════════════════════════

    int GetBlockHeightAhead(int dir)
    {
        float rayLength = cellSize * 0.75f;
        float feetY = transform.position.y - cellSize * 0.5f;
        float originX = transform.position.x;

        for (int h = 0; h <= maxJumpableHeight + 1; h++)
        {
            Vector2 origin = new Vector2(originX, feetY + cellSize * h + 0.05f);
            Vector2 direction = new Vector2(dir, 0f);

            var hit = Physics2D.Raycast(origin, direction, rayLength, solidLayer);
            if (hit.collider == null)
                return h;
        }

        return maxJumpableHeight + 2;
    }

    /// <summary>
    /// เช็คว่ามีพื้นอยู่ใต้ตำแหน่งที่กำหนดหรือไม่
    /// ใช้ใน KnockbackGrid เพื่อให้ Enemy ตกลงพื้นหลังกระเด็น
    /// </summary>
    bool HasFloorAt(Vector3 position)
    {
        Vector2 checkCenter = new Vector2(position.x, position.y - cellSize * 0.6f);
        return Physics2D.OverlapCircle(checkCenter, groundCheckRadius * 2f, solidLayer) != null;
    }

    IEnumerator JumpMove(int dir)
    {
        isBusy = true;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

        float targetY = transform.position.y + cellSize;
        yield return new WaitUntil(() => transform.position.y >= targetY || rb.linearVelocity.y < 0);

        MoveOneCell(dir);
        yield return new WaitUntil(() => !isMoving);

        isBusy = false;
        stepTimer = aggroStepInterval;
    }

    void CheckGround()
    {
        isGrounded = Physics2D.OverlapCircle(
            transform.position + Vector3.down * (cellSize * 0.5f),
            groundCheckRadius,
            solidLayer
        );
    }

    // ══════════════════════════════════════════════════
    //  2. ATTACK RAYCASTS
    // ══════════════════════════════════════════════════

    RaycastHit2D CheckAttackRaycasts()
    {
        var origin = (Vector2)transform.position;
        var forward = new Vector2(facingDirection, 0f);
        var back = new Vector2(-facingDirection, 0f);
        float rad = diagonalAngle * Mathf.Deg2Rad;
        var diag = new Vector2(facingDirection * Mathf.Cos(rad), -Mathf.Sin(rad));

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
        isBusy = true;
        currentState = EnemyState.Attack;
        attackTimer = attackCooldown;

        yield return new WaitForSeconds(attackWindup);

        hit.collider.SendMessage("TakeDamage", attackDamage, SendMessageOptions.DontRequireReceiver);
        Debug.Log($"[Enemy] โจมตี {attackDamage} dmg");

        currentState = EnemyState.Backstep;
        yield return new WaitForSeconds(backstepDelay);

        float backstepTargetX = transform.position.x + cellSize * -facingDirection;
        targetPosition = new Vector3(backstepTargetX, transform.position.y, transform.position.z);
        isMoving = true;

        yield return new WaitUntil(() => !isMoving);

        currentState = IsPlayerInRange(detectionRadius) ? EnemyState.Aggro : EnemyState.Patrol;
        stepTimer = aggroStepInterval;
        isBusy = false;
    }

    // ══════════════════════════════════════════════════
    //  HELPERS
    // ══════════════════════════════════════════════════

    bool IsPlayerInRange(float radius)
        => player != null && Vector2.Distance(transform.position, player.position) <= radius;

    void MoveOneCell(int dir)
    {
        if (isMoving) return;
        targetPosition = new Vector3(
            transform.position.x + cellSize * dir,
            transform.position.y, transform.position.z);
        isMoving = true;
    }

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

    public EnemyState GetState() => currentState;
    public bool IsAggro() => currentState == EnemyState.Aggro || currentState == EnemyState.Attack;
    public int GetFacing() => facingDirection;

    /// <summary>
    /// เรียกจาก EnemyHealth — กระเด็นแบบ Grid ไม่ใช้ Physics velocity
    /// forceX = จำนวนช่องแนวนอน, forceY = จำนวนช่องขึ้น
    /// </summary>
    public void ApplyKnockback(int direction, float forceX, float forceY, float duration)
    {
        if (!enabled) return;
        StartCoroutine(KnockbackGrid(direction, forceX, forceY, duration));
    }

    IEnumerator KnockbackGrid(int direction, float forceX, float forceY, float duration)
    {
        isBusy = true;
        isMoving = false;
        rb.linearVelocity = Vector2.zero;

        int cellsX = Mathf.Max(1, Mathf.RoundToInt(forceX));
        int cellsY = Mathf.Max(0, Mathf.RoundToInt(forceY));

        Vector3 dest = SnapToGrid(new Vector3(
            transform.position.x + cellSize * direction * cellsX,
            transform.position.y + cellSize * cellsY,
            transform.position.z
        ));

        targetPosition = dest;
        isMoving = true;

        float t = 0f;
        while (isMoving && t < duration + 0.5f)
        {
            t += Time.deltaTime;
            yield return null;
        }

        isMoving = false;
        transform.position = SnapToGrid(transform.position);

        // ตกลงพื้นถ้าอยู่กลางอากาศ
        for (int i = 0; i < 10; i++)
        {
            if (!HasFloorAt(transform.position))
            {
                targetPosition = new Vector3(
                    transform.position.x,
                    transform.position.y - cellSize,
                    transform.position.z
                );
                isMoving = true;
                yield return new WaitUntil(() => !isMoving);
            }
            else break;
        }

        isBusy = false;
    }

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
        Gizmos.color = new Color(1f, 1f, 0f, 0.06f);
        Gizmos.DrawSphere(transform.position, deaggroRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, deaggroRadius);

        // Patrol range
        float ox = Application.isPlaying ? patrolOriginX : transform.position.x;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(
            new Vector3(ox - patrolRange * cellSize, transform.position.y),
            new Vector3(ox + patrolRange * cellSize, transform.position.y));
        Gizmos.DrawWireSphere(new Vector3(ox - patrolRange * cellSize, transform.position.y), 0.12f);
        Gizmos.DrawWireSphere(new Vector3(ox + patrolRange * cellSize, transform.position.y), 0.12f);

        // Attack raycasts
        int fd = Application.isPlaying ? facingDirection : 1;
        float rad = diagonalAngle * Mathf.Deg2Rad;
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, new Vector3(fd, 0f) * attackRange);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawRay(transform.position, new Vector3(fd * Mathf.Cos(rad), -Mathf.Sin(rad)) * attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, new Vector3(-fd, 0f) * attackRange);

        // Block detection rays (ม่วง)
        Gizmos.color = new Color(0.8f, 0.4f, 1f);
        float rayLen = cellSize * 0.75f;
        for (int h = 0; h <= maxJumpableHeight + 1; h++)
        {
            Vector3 origin = transform.position + new Vector3(0f, -cellSize * 0.5f + cellSize * h + 0.05f, 0f);
            Gizmos.DrawRay(origin, new Vector3(fd, 0f) * rayLen);
        }

        // Ground check (ขาว)
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(
            transform.position + Vector3.down * (cellSize * 0.5f), groundCheckRadius
        );
    }
}