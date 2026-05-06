using UnityEngine;
 
/// <summary>
/// PlayerController - Chess Pawn Style Movement (No Jump, Auto Step-Up, Grid Gravity)
/// 
/// กฎการเดิน:
///   - กด A หรือ D ครั้งแรก → หันหน้าไปทิศนั้น (ไม่ขยับ)
///   - กด A หรือ D ครั้งที่สอง (ทิศเดิม) → เดิน 1 ช่อง
///   - ถ้าช่องข้างหน้ามี platform สูงขึ้น 1 ช่อง → ขึ้นไปอัตโนมัติ (Step Up)
///   - หลังเคลื่อนที่ทุกครั้ง → เช็ค Gravity ตกลงจนถึงพื้น
///
/// Layer ที่ต้องตั้งใน Unity:
///   - Base (พื้นยาว) → Layer: "Ground"
///   - กล่อง/Platform → Layer: "Platform"
///   - ใส่ทั้ง Ground และ Platform ใน groundLayer และ platformLayer ของ Script
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Grid Settings")]
    public float cellSize = 1f;
 
    [Header("Movement Settings")]
    public float moveSpeed = 10f;
 
    [Header("Layer Settings")]
    [Tooltip("Layer ของพื้นหลัก (Base) — ใช้เช็คว่าตกถึงพื้นหรือยัง")]
    public LayerMask groundLayer;
 
    [Tooltip("Layer ของ Platform/กล่อง — ใช้เช็คการชนขณะเดิน\nถ้าต้องการให้ทั้งสองทำงาน ให้ tick ทั้ง Ground และ Platform")]
    public LayerMask obstacleLayer;
 
    [Header("Gizmo / Overlap Tuning")]
    [Tooltip("ขนาดของ IsObstacle box (กี่เท่าของ cellSize)\nกล่องสีฟ้า (Flat) และสีเขียว (Step Up) ใน Gizmos")]
    [Range(0.1f, 1.2f)] public float obstacleBoxSize = 0.8f;
 
    [Tooltip("ระยะห่าง X ของกล่องสีฟ้า/เขียว/เหลืองจาก Player (กี่เท่าของ cellSize)\n1.0 = ห่าง 1 ช่องพอดี (ค่าปกติ)")]
    [Range(0.5f, 3.0f)] public float obstacleCheckDistance = 1.0f;
 
    [Tooltip("Y offset ของ HasFloorAt check (กี่เท่าของ cellSize ลงด้านล่าง)\nกล่องสีแดงใน Gizmos")]
    [Range(0.0f, 0.8f)] public float floorCheckOffsetY = 0.4f;
 
    [Tooltip("ความกว้างของ HasFloorAt box (กี่เท่าของ cellSize)\nกล่องสีแดงใน Gizmos")]
    [Range(0.1f, 1.2f)] public float floorCheckWidth = 0.6f;
 
    [Tooltip("ความสูงของ HasFloorAt box (กี่เท่าของ cellSize)\nกล่องสีแดงใน Gizmos")]
    [Range(0.01f, 0.5f)] public float floorCheckHeight = 0.2f;
 
    private int facingDirection = 1;
    private Vector3 targetPosition;
    private bool isMoving = false;
 
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
 
    // รวม Layer ทั้งสองเพื่อใช้เช็คการตก
    private LayerMask combinedLayer;
 
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
 
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.linearVelocity = Vector2.zero;
 
        combinedLayer = groundLayer | obstacleLayer;
 
        transform.position = SnapToGrid(transform.position);
        targetPosition = transform.position;
 
        ApplyGridGravity();
    }
 
    void Update()
    {
        HandleMovementInput();
    }
 
    void FixedUpdate()
    {
        if (isMoving)
        {
            Vector3 newPos = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.fixedDeltaTime);
            rb.MovePosition(newPos);
 
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                rb.MovePosition(targetPosition);
                transform.position = targetPosition;
                isMoving = false;
                ApplyGridGravity();
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
        Vector3 cur = transform.position;
        float nextX = cur.x + (cellSize * obstacleCheckDistance * direction);
 
        // ช่อง Flat — เช็คเฉพาะ obstacleLayer (ไม่นับพื้น Base)
        Vector3 flatPos = new Vector3(nextX, cur.y, cur.z);
        if (!IsObstacle(flatPos))
        {
            targetPosition = flatPos;
            isMoving = true;
            return;
        }
 
        // Step Up — เช็คเฉพาะ obstacleLayer
        Vector3 stepPos = new Vector3(nextX, cur.y + cellSize, cur.z);
        if (!IsObstacle(stepPos))
        {
            targetPosition = stepPos;
            isMoving = true;
            return;
        }
 
        // บล็อคทั้งหมด → ไม่ขยับ
    }
 
    void ApplyGridGravity()
    {
        int maxFallSteps = 50;
        for (int i = 0; i < maxFallSteps; i++)
        {
            Vector3 below = new Vector3(transform.position.x, transform.position.y - cellSize, transform.position.z);
 
            if (!HasFloorAt(below))
            {
                targetPosition = below;
                isMoving = true;
                return;
            }
            else
            {
                break;
            }
        }
    }
 
    /// <summary>
    /// เช็คการชนสำหรับการเดิน (obstacleLayer เท่านั้น — ไม่รวม Base)
    /// ใช้ค่า obstacleBoxSize ที่ปรับได้จาก Inspector
    /// </summary>
    bool IsObstacle(Vector3 position)
    {
        Collider2D hit = Physics2D.OverlapBox(
            new Vector2(position.x, position.y),
            Vector2.one * (cellSize * obstacleBoxSize),
            0f,
            obstacleLayer
        );
        return hit != null;
    }
 
    /// <summary>
    /// เช็คว่ามีพื้น (Ground หรือ Platform) อยู่ที่ตำแหน่งนั้นไหม
    /// ใช้ค่า floorCheck* ที่ปรับได้จาก Inspector
    /// </summary>
    bool HasFloorAt(Vector3 position)
    {
        Vector2 checkCenter = new Vector2(position.x, position.y - cellSize * floorCheckOffsetY);
        Collider2D hit = Physics2D.OverlapBox(
            checkCenter,
            new Vector2(cellSize * floorCheckWidth, cellSize * floorCheckHeight),
            0f,
            combinedLayer
        );
        return hit != null;
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
 
    public int GetFacingDirection() => facingDirection;
    public bool IsMoving() => isMoving;
 
    void OnDrawGizmosSelected()
    {
        // กล่องสีเหลือง — targetPosition
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(targetPosition, Vector3.one * (cellSize * obstacleBoxSize));
 
        float nx = transform.position.x + cellSize * obstacleCheckDistance * facingDirection;
 
        // กล่องสีฟ้า — Flat check (IsObstacle)
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(new Vector3(nx, transform.position.y, 0), Vector3.one * (cellSize * obstacleBoxSize));
 
        // กล่องสีเขียว — Step Up check (IsObstacle)
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(new Vector3(nx, transform.position.y + cellSize, 0), Vector3.one * (cellSize * obstacleBoxSize));
 
        // กล่องสีแดง — HasFloorAt check zone
        Vector3 below = new Vector3(transform.position.x, transform.position.y - cellSize, 0);
        Vector3 floorCheckPos = new Vector3(below.x, below.y - cellSize * floorCheckOffsetY, 0);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(floorCheckPos, new Vector3(cellSize * floorCheckWidth, cellSize * floorCheckHeight, 0));
    }
}