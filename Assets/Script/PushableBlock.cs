using UnityEngine;

/// <summary>
/// PushableBlock - Block สีฟ้าที่ถูกผลักได้
/// 
/// วิธีใช้:
///   1. แนบ Script นี้ที่ Block สีฟ้า
///   2. ตรวจสอบว่ามี Rigidbody2D และ Collider2D
///   3. ตั้ง pushForce ตามต้องการ
///   4. Player ต้องอยู่ติดกัน แล้วกด Space เพื่อผลัก
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PushableBlock : MonoBehaviour
{
    [Header("Push Settings")]
    [Tooltip("แรงผลัก — ยิ่งมากยิ่งแรง")]
    public float pushForce = 20f;

    [Tooltip("ระยะที่ player ต้องอยู่ใกล้เพื่อผลักได้ (units)")]
    public float interactRange = 1.5f;

    [Header("Drag Settings")]
    [Tooltip("แรงเสียดทาน — ยิ่งมากหยุดเร็วขึ้น")]
    public float linearDrag = 3f;

    [Header("Interaction UI")]
    [Tooltip("ลาก GameObject ที่เป็น prompt [Space] มาใส่ (optional)")]
    public GameObject interactPrompt;

    private Rigidbody2D rb;
    private Transform playerTransform;
    private PlayerController playerController;

    private bool hasBeenPushed = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 1f;
        rb.freezeRotation = true;
        rb.linearDamping = linearDrag;

        // หา Player อัตโนมัติ
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerController = playerObj.GetComponent<PlayerController>();
        }

        if (interactPrompt != null)
            interactPrompt.SetActive(false);
    }

    void Update()
    {
        if (hasBeenPushed) return;
        if (playerTransform == null) return;

        float distance = Vector2.Distance(transform.position, playerTransform.position);
        bool inRange = distance <= interactRange;

        // แสดง/ซ่อน prompt
        if (interactPrompt != null)
            interactPrompt.SetActive(inRange);

        if (inRange && Input.GetKeyDown(KeyCode.Space))
        {
            Push();
        }
    }

    void Push()
    {
        if (playerController == null) return;

        int direction = playerController.GetFacingDirection();
        Vector2 force = new Vector2(direction * pushForce, 0f);

        rb.AddForce(force, ForceMode2D.Impulse);
        hasBeenPushed = true;

        if (interactPrompt != null)
            interactPrompt.SetActive(false);
    }

    /// <summary>
    /// เรียกจากภายนอกเพื่อ reset block (optional)
    /// </summary>
    public bool HasBeenPushed() => hasBeenPushed;
}
