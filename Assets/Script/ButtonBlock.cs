using UnityEngine;
 
/// <summary>
/// ButtonBlock - Block สีแดงที่ยกตัวขึ้นและ trigger สะพานเมื่อโดน PushableBlock ชน
/// </summary>
public class ButtonBlock : MonoBehaviour
{
    [Header("References")]
    public Bridge bridge;
 
    [Header("Rise Settings")]
    [Tooltip("ตำแหน่งที่ Block สีแดงจะเลื่อนขึ้นไป (World Position)")]
    public Vector3 raisedPosition;
 
    [Tooltip("ความเร็วในการยกตัวขึ้น")]
    public float riseSpeed = 5f;
 
    [Header("Visual Feedback")]
    public Color normalColor = Color.red;
    public Color activatedColor = new Color(0.5f, 0f, 0f);
 
    [Header("Safety")]
    public float activationDelay = 0.5f;
 
    private SpriteRenderer sr;
    private bool isActivated = false;
    private bool canReceiveCollision = false;
    private bool isRising = false;
 
    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = normalColor;
 
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.bodyType = RigidbodyType2D.Kinematic;
 
        // ถ้ายังไม่ได้ตั้ง raisedPosition ให้ default ขึ้นไป 3 units
        if (raisedPosition == Vector3.zero)
            raisedPosition = transform.position + new Vector3(0f, 3f, 0f);
 
        Invoke(nameof(EnableCollision), activationDelay);
    }
 
    void EnableCollision()
    {
        canReceiveCollision = true;
    }
 
    void Update()
    {
        if (!isRising) return;
 
        transform.position = Vector3.MoveTowards(
            transform.position,
            raisedPosition,
            riseSpeed * Time.deltaTime
        );
    }
 
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!canReceiveCollision) return;
        if (isActivated) return;
 
        PushableBlock pushable = collision.gameObject.GetComponent<PushableBlock>();
        if (pushable != null && pushable.HasBeenPushed())
        {
            Activate();
        }
    }
 
    void Activate()
    {
        isActivated = true;
        isRising = true;
 
        if (sr != null) sr.color = activatedColor;
 
        if (bridge != null)
            bridge.Activate();
        else
            Debug.LogWarning("ButtonBlock: ยังไม่ได้ assign Bridge!");
    }
 
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(raisedPosition, transform.localScale);
        Gizmos.DrawLine(transform.position, raisedPosition);
    }
}