using UnityEngine;
 
/// <summary>
/// PlayerController - Chess Pawn Style Movement (No Jump, Auto Step-Up, Grid Gravity)
/// 
/// กฎการเดิน:
///   - กด A หรือ D ครั้งแรก → หันหน้าไปทิศนั้น (ไม่ขยับ)
///   - กด A หรือ D ครั้งที่สอง (ทิศเดิม) → เดิน 1 ช่อง
///   - ถ้าช่องข้างหน้ามี platform สูงขึ้น 1 ช่อง → ขึ้นไปอัตโนมัติ (Step Up)
///   - หลังเคลื่อนที่ทุกครั้ง → เช็ค Gravity ตกลงจนถึงพื้น
///   - ทุกครั้งที่เดินสำเร็จ → เรียก TurnManager.EndPlayerTurn() ให้ Enemy ขยับตาม
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Grid Settings")]
    public float cellSize = 1f;
 
    [Header("Movement Settings")]
    public float moveSpeed = 10f;
 
    [Header("Layer Settings")]
    [Tooltip("Layer ของพื้นหลัก (Base)")]
    public LayerMask groundLayer;
 
    [Tooltip("Layer ของ Platform/กล่อง")]
    public LayerMask obstacleLayer;
 
    [Header("Gizmo / Overlap Tuning")]
    [Range(0.1f, 1.2f)] public float obstacleBoxSize = 0.8f;
    [Range(0.5f, 3.0f)] public float obstacleCheckDistance = 1.0f;
    [Range(0.0f, 0.8f)] public float floorCheckOffsetY = 0.4f;
    [Range(0.1f, 1.2f)] public float floorCheckWidth = 0.6f;
    [Range(0.01f, 0.5f)] public float floorCheckHeight = 0.2f;
 
    [Header("Turn Settings")]
    [Tooltip("เปิด/ปิดระบบ Turn-based\nถ้าปิด Enemy จะขยับอิสระไม่ตาม Player")]
    public bool useTurnSystem = true;
 
    private int     facingDirection = 1;
    private Vector3 targetPosition;
    private bool    isMoving = false;
    private bool    hasMoved = false;   // true เฉพาะตอน Player กดเดินจริง (ไม่นับ Gravity)
 
    private Rigidbody2D    rb;
    private SpriteRenderer spriteRenderer;
    private LayerMask      combinedLayer;
 
    void Start()
    {
        rb             = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
 
        rb.gravityScale   = 0f;
        rb.freezeRotation = true;
        rb.linearVelocity = Vector2.zero;
 
        combinedLayer = groundLayer | obstacleLayer;
 
        transform.position = SnapToGrid(transform.position);
        targetPosition     = transform.position;
 
        ApplyGridGravity();
    }
 
    void Update()
    {
        HandleMovementInput();
    }
 
    void FixedUpdate()
    {
        if (!isMoving) return;
 
        Vector3 newPos = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.fixedDeltaTime);
        rb.MovePosition(newPos);
 
        if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
        {
            rb.MovePosition(targetPosition);
            transform.position = targetPosition;
            isMoving           = false;
            ApplyGridGravity();
 
            // เรียก Turn หลังเดินถึงเป้าหมาย
            // ใช้ hasMoved เพื่อกัน Gravity ตกไปเรียก Turn ซ้ำ
            if (hasMoved && useTurnSystem)
            {
                hasMoved = false;
                TurnManager.Instance?.EndPlayerTurn();
            }
        }
    }
 
    void HandleMovementInput()
    {
        if (isMoving) return;
 
        int inputDirection = 0;
        if (Input.GetKeyDown(KeyCode.A))      inputDirection = -1;
        else if (Input.GetKeyDown(KeyCode.D)) inputDirection = 1;
 
        if (inputDirection == 0) return;
 
        if (inputDirection != facingDirection)
        {
            SetFacingDirection(inputDirection);
            return;
        }
 
        TryMove(inputDirection);
    }
 
    void TryMove(int direction)
    {
        Vector3 cur   = transform.position;
        float   nextX = cur.x + (cellSize * obstacleCheckDistance * direction);
 
        // Flat
        Vector3 flatPos = new Vector3(nextX, cur.y, cur.z);
        if (!IsObstacle(flatPos))
        {
            targetPosition = flatPos;
            isMoving       = true;
            hasMoved       = true;
            return;
        }
 
        // Step Up
        Vector3 stepPos = new Vector3(nextX, cur.y + cellSize, cur.z);
        if (!IsObstacle(stepPos))
        {
            targetPosition = stepPos;
            isMoving       = true;
            hasMoved       = true;
            return;
        }
    }
 
    void ApplyGridGravity()
    {
        for (int i = 0; i < 50; i++)
        {
            Vector3 below = new Vector3(transform.position.x, transform.position.y - cellSize, transform.position.z);
            if (!HasFloorAt(below))
            {
                targetPosition = below;
                isMoving       = true;
                // ไม่ set hasMoved = true → Gravity ไม่ trigger Turn
                return;
            }
            else break;
        }
    }
 
    bool IsObstacle(Vector3 position)
    {
        return Physics2D.OverlapBox(
            new Vector2(position.x, position.y),
            Vector2.one * (cellSize * obstacleBoxSize),
            0f,
            obstacleLayer
        ) != null;
    }
 
    bool HasFloorAt(Vector3 position)
    {
        Vector2 checkCenter = new Vector2(position.x, position.y - cellSize * floorCheckOffsetY);
        return Physics2D.OverlapBox(
            checkCenter,
            new Vector2(cellSize * floorCheckWidth, cellSize * floorCheckHeight),
            0f,
            combinedLayer
        ) != null;
    }
 
    void SetFacingDirection(int direction)
    {
        facingDirection = direction;
        if (spriteRenderer != null)
            spriteRenderer.flipX = (direction == -1);
    }
 
    Vector3 SnapToGrid(Vector3 pos)
    {
        return new Vector3(
            Mathf.Round(pos.x / cellSize) * cellSize,
            Mathf.Round(pos.y / cellSize) * cellSize,
            pos.z
        );
    }
 
    public int  GetFacingDirection() => facingDirection;
    public bool IsMoving()           => isMoving;
 
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(targetPosition, Vector3.one * (cellSize * obstacleBoxSize));
 
        float nx = transform.position.x + cellSize * obstacleCheckDistance * facingDirection;
 
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(new Vector3(nx, transform.position.y, 0), Vector3.one * (cellSize * obstacleBoxSize));
 
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(new Vector3(nx, transform.position.y + cellSize, 0), Vector3.one * (cellSize * obstacleBoxSize));
 
        Vector3 below         = new Vector3(transform.position.x, transform.position.y - cellSize, 0);
        Vector3 floorCheckPos = new Vector3(below.x, below.y - cellSize * floorCheckOffsetY, 0);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(floorCheckPos, new Vector3(cellSize * floorCheckWidth, cellSize * floorCheckHeight, 0));
    }
    public void ResetMovementState()
    {
        isMoving = false;
        hasMoved = false;
        targetPosition = transform.position; // ย้ายเป้าหมายมาอยู่ที่ตำแหน่งปัจจุบันทันที
    }
}
 