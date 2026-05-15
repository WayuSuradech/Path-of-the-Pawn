using System.Collections;
using UnityEngine;
 
/// <summary>
/// EnemyController — Silksong Grunt Style
/// ใช้ ITurnTaker → ขยับตาม Player ทีละก้าว
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyController : MonoBehaviour, ITurnTaker
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
    public float cellSize = 1f;
 
    [Header("Movement")]
    public float moveSpeed      = 4f;
    public float aggroMoveSpeed = 6f;
    public float patrolStepInterval = 0.6f;
    public float aggroStepInterval  = 0.35f;
 
    [Header("── Patrol ──")]
    public int   patrolRange    = 4;
    public float patrolTurnPause = 0.5f;
 
    [Header("── Detection ──")]
    public float detectionRadius = 5f;
    public float deaggroRadius   = 7f;
    public float aggroWakeDelay  = 0.25f;
 
    [Header("── Attack Raycasts ──")]
    public float     attackRange    = 1.5f;
    public float     diagonalAngle  = 30f;
    public LayerMask playerLayer;
    public float     attackWindup   = 0.2f;
    public float     attackCooldown = 1.4f;
    public float     attackDamage   = 10f;
 
    [Header("── Backstep ──")]
    public float backstepDelay = 0.15f;
 
    [Header("── Block / Jump Detection ──")]
    public LayerMask groundLayer;
    public LayerMask platformLayer;
    public float     groundCheckRadius = 0.1f;
    public int       maxJumpableHeight = 1;
    public float     jumpForce = 8f;
 
    private LayerMask solidLayer => groundLayer | platformLayer;
 
    [Header("Turn Settings")]
    [Tooltip("เปิด/ปิดระบบ Turn-based สำหรับ Enemy ตัวนี้")]
    public bool useTurnSystem = true;
 
    [Header("References")]
    public Transform player;
 
    // ══════════════════════════════════════════════════
    //  PRIVATE
    // ══════════════════════════════════════════════════
 
    private Rigidbody2D    rb;
    private SpriteRenderer sr;
 
    private Vector3 targetPosition;
    private bool    isMoving        = false;
    private int     facingDirection = 1;
 
    private float patrolOriginX;
    private int   patrolDirection = 1;
 
    private float stepTimer   = 0f;
    private float attackTimer = 0f;
    private bool  isBusy      = false;
    private bool  isGrounded  = false;
 
    // ══════════════════════════════════════════════════
    //  UNITY
    // ══════════════════════════════════════════════════
 
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        rb.freezeRotation = true;
 
        transform.position = SnapToGrid(transform.position);
        targetPosition     = transform.position;
        patrolOriginX      = transform.position.x;
 
        if (player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) player = go.transform;
        }
 
        // ลงทะเบียนกับ TurnManager
        if (useTurnSystem)
            TurnManager.Instance?.Register(this);
    }
 
    void OnDestroy()
    {
        // ยกเลิกจาก TurnManager เมื่อ Enemy ตาย
        TurnManager.Instance?.Unregister(this);
    }
 
    void Update()
    {
        // ถ้าใช้ Turn System → ไม่ Update ตัวเอง รอ OnTurn() แทน
        if (useTurnSystem) return;
 
        TickLogic();
    }
 
    void FixedUpdate()
    {
        if (!isMoving) return;
 
        float speed = (currentState == EnemyState.Aggro) ? aggroMoveSpeed : moveSpeed;
        float newX  = Mathf.MoveTowards(transform.position.x, targetPosition.x, speed * Time.fixedDeltaTime);
        transform.position = new Vector3(newX, transform.position.y, transform.position.z);
 
        if (Mathf.Abs(transform.position.x - targetPosition.x) < 0.01f)
        {
            transform.position = new Vector3(targetPosition.x, transform.position.y, transform.position.z);
            isMoving = false;
        }
    }
 
    // ══════════════════════════════════════════════════
    //  ITURNTAKER — เรียกทุกครั้งที่ Player เดิน
    // ══════════════════════════════════════════════════
 
    public void OnTurn()
    {
        if (isBusy || isMoving) return;
        TickLogic();
    }
 
    // ── Logic หลัก ────────────────────────────────────
 
    void TickLogic()
    {
        if (player == null || isBusy) return;
 
        attackTimer -= useTurnSystem ? 1f : Time.deltaTime;
        stepTimer   -= useTurnSystem ? 1f : Time.deltaTime;
 
        CheckGround();
 
        switch (currentState)
        {
            case EnemyState.Patrol:   UpdatePatrol(); break;
            case EnemyState.Aggro:    UpdateAggro();  break;
        }
    }
 
    // ══════════════════════════════════════════════════
    //  STATE: PATROL
    // ══════════════════════════════════════════════════
 
    void UpdatePatrol()
    {
        if (IsPlayerInRange(detectionRadius)) { StartCoroutine(WakeUpAggro()); return; }
        if (isMoving || stepTimer > 0f) return;
 
        float distFromOrigin = transform.position.x - patrolOriginX;
        bool  atRightEdge    = distFromOrigin >=  patrolRange * cellSize - 0.05f;
        bool  atLeftEdge     = distFromOrigin <= -patrolRange * cellSize + 0.05f;
 
        if ((patrolDirection == 1 && atRightEdge) || (patrolDirection == -1 && atLeftEdge))
            StartCoroutine(PatrolTurn());
        else
        {
            if (facingDirection != patrolDirection) SetFacing(patrolDirection);
            else MoveOneCell(patrolDirection);
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
        if (!IsPlayerInRange(deaggroRadius)) { currentState = EnemyState.Patrol; stepTimer = patrolStepInterval; return; }
 
        var hit = CheckAttackRaycasts();
        if (hit.collider != null && attackTimer <= 0f) { StartCoroutine(PerformAttack(hit)); return; }
        if (isMoving || stepTimer > 0f) return;
 
        ChasePlayer();
        stepTimer = aggroStepInterval;
    }
 
    void ChasePlayer()
    {
        float dx        = player.position.x - transform.position.x;
        int   dirNeeded = dx > 0 ? 1 : -1;
 
        if (Mathf.Abs(dx) < cellSize * 0.5f) return;
        if (facingDirection != dirNeeded) { SetFacing(dirNeeded); return; }
 
        int blockHeight = GetBlockHeightAhead(dirNeeded);
        if (blockHeight == 0)
            MoveOneCell(dirNeeded);
        else if (blockHeight <= maxJumpableHeight && isGrounded)
            StartCoroutine(JumpMove(dirNeeded));
    }
 
    // ══════════════════════════════════════════════════
    //  BLOCK / JUMP
    // ══════════════════════════════════════════════════
 
    int GetBlockHeightAhead(int dir)
    {
        float rayLength = cellSize * 0.75f;
        float feetY     = transform.position.y - cellSize * 0.5f;
 
        for (int h = 0; h <= maxJumpableHeight + 1; h++)
        {
            var hit = Physics2D.Raycast(
                new Vector2(transform.position.x, feetY + cellSize * h + 0.05f),
                new Vector2(dir, 0f),
                rayLength, solidLayer);
            if (hit.collider == null) return h;
        }
        return maxJumpableHeight + 2;
    }
 
    bool HasFloorAt(Vector3 position)
    {
        return Physics2D.OverlapCircle(
            new Vector2(position.x, position.y - cellSize * 0.6f),
            groundCheckRadius * 2f, solidLayer) != null;
    }
 
    IEnumerator JumpMove(int dir)
    {
        isBusy = true;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        float targetY = transform.position.y + cellSize;
        yield return new WaitUntil(() => transform.position.y >= targetY || rb.linearVelocity.y < 0);
        MoveOneCell(dir);
        yield return new WaitUntil(() => !isMoving);
        isBusy    = false;
        stepTimer = aggroStepInterval;
    }
 
    void CheckGround()
    {
        isGrounded = Physics2D.OverlapCircle(
            transform.position + Vector3.down * (cellSize * 0.5f),
            groundCheckRadius, solidLayer);
    }
 
    // ══════════════════════════════════════════════════
    //  ATTACK + BACKSTEP
    // ══════════════════════════════════════════════════
 
    RaycastHit2D CheckAttackRaycasts()
    {
        var   o   = (Vector2)transform.position;
        float rad = diagonalAngle * Mathf.Deg2Rad;
 
        var h = Physics2D.Raycast(o, new Vector2(facingDirection, 0), attackRange, playerLayer);
        if (h.collider != null) return h;
 
        h = Physics2D.Raycast(o, new Vector2(facingDirection * Mathf.Cos(rad), -Mathf.Sin(rad)), attackRange, playerLayer);
        if (h.collider != null) return h;
 
        h = Physics2D.Raycast(o, new Vector2(-facingDirection, 0), attackRange, playerLayer);
        if (h.collider != null) { TurnToFacePlayer(); return h; }
 
        return default;
    }
 
    IEnumerator PerformAttack(RaycastHit2D hit)
    {
        isBusy       = true;
        currentState = EnemyState.Attack;
        attackTimer  = attackCooldown;
 
        yield return new WaitForSeconds(attackWindup);
        hit.collider.SendMessage("TakeDamage", attackDamage, SendMessageOptions.DontRequireReceiver);
 
        currentState = EnemyState.Backstep;
        yield return new WaitForSeconds(backstepDelay);
 
        float backstepTargetX = transform.position.x + cellSize * -facingDirection;
        targetPosition = new Vector3(backstepTargetX, transform.position.y, transform.position.z);
        isMoving = true;
        yield return new WaitUntil(() => !isMoving);
 
        currentState = IsPlayerInRange(detectionRadius) ? EnemyState.Aggro : EnemyState.Patrol;
        stepTimer    = aggroStepInterval;
        isBusy       = false;
    }
 
    // ══════════════════════════════════════════════════
    //  HELPERS
    // ══════════════════════════════════════════════════
 
    bool IsPlayerInRange(float r) => player != null && Vector2.Distance(transform.position, player.position) <= r;
 
    void MoveOneCell(int dir)
    {
        if (isMoving) return;
        targetPosition = new Vector3(transform.position.x + cellSize * dir, transform.position.y, transform.position.z);
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
 
    public EnemyState GetState()  => currentState;
    public bool       IsAggro()   => currentState == EnemyState.Aggro || currentState == EnemyState.Attack;
    public int        GetFacing() => facingDirection;
 
    public void ApplyKnockback(int direction, float forceX, float forceY, float duration)
    {
        if (!enabled) return;
        StartCoroutine(KnockbackGrid(direction, forceX, forceY, duration));
    }
 
    IEnumerator KnockbackGrid(int direction, float forceX, float forceY, float duration)
    {
        isBusy   = true;
        isMoving = false;
        rb.linearVelocity = Vector2.zero;
 
        Vector3 dest = SnapToGrid(new Vector3(
            transform.position.x + cellSize * direction * Mathf.Max(1, Mathf.RoundToInt(forceX)),
            transform.position.y + cellSize * Mathf.Max(0, Mathf.RoundToInt(forceY)),
            transform.position.z));
 
        targetPosition = dest;
        isMoving       = true;
 
        float t = 0f;
        while (isMoving && t < duration + 0.5f) { t += Time.deltaTime; yield return null; }
 
        isMoving           = false;
        transform.position = SnapToGrid(transform.position);
 
        for (int i = 0; i < 10; i++)
        {
            if (!HasFloorAt(transform.position))
            {
                targetPosition = new Vector3(transform.position.x, transform.position.y - cellSize, transform.position.z);
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
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, deaggroRadius);
 
        float ox = Application.isPlaying ? patrolOriginX : transform.position.x;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(ox - patrolRange * cellSize, transform.position.y),
                        new Vector3(ox + patrolRange * cellSize, transform.position.y));
 
        int   fd  = Application.isPlaying ? facingDirection : 1;
        float rad = diagonalAngle * Mathf.Deg2Rad;
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, new Vector3(fd, 0f) * attackRange);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawRay(transform.position, new Vector3(fd * Mathf.Cos(rad), -Mathf.Sin(rad)) * attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, new Vector3(-fd, 0f) * attackRange);
 
        Gizmos.color = new Color(0.8f, 0.4f, 1f);
        for (int h = 0; h <= maxJumpableHeight + 1; h++)
        {
            Vector3 o = transform.position + new Vector3(0f, -cellSize * 0.5f + cellSize * h + 0.05f, 0f);
            Gizmos.DrawRay(o, new Vector3(fd, 0f) * cellSize * 0.75f);
        }
 
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position + Vector3.down * (cellSize * 0.5f), groundCheckRadius);
    }
}