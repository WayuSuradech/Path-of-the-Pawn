using System.Collections;
using UnityEngine;

/// <summary>
/// PlayerAttack — ระบบโจมตีแบบ Grid
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [Tooltip("Damage ที่ส่งให้ Enemy")]
    public float attackDamage = 20f;

    [Tooltip("Cooldown ระหว่างการโจมตี (วินาที)")]
    public float attackCooldown = 0.4f;

    [Tooltip("ขนาด HitBox (ควรเท่ากับ cellSize ของ Grid)")]
    public float cellSize = 1f;

    [Tooltip("Layer ของ Enemy")]
    public LayerMask enemyLayer;

    [Header("Anticipation (Windup)")]
    [Tooltip("หน่วงก่อนตีจริง — ให้ Enemy มีเวลา react (0 = ตีทันที)")]
    public float attackWindup = 0.08f;

    [Header("Visual Feedback")]
    [Tooltip("GameObject ที่แสดง HitBox ขณะโจมตี (optional)")]
    public GameObject attackVFX;
    [Tooltip("ให้ VFX หันตามทิศทางที่ตัวละครหันหน้าหรือไม่")]
    public bool flipVFXWithPlayer = true;

    [Header("Audio Settings")]
    [Tooltip("ไฟล์เสียงตอนเหวี่ยงอาวุธ")]
    public AudioClip attackSound;
    [Range(0f, 1f)] public float attackVolume = 1f;

    [Tooltip("ไฟล์เสียงตอนตีโดนศัตรู")]
    public AudioClip hitSound;
    [Range(0f, 1f)] public float hitVolume = 1f;

    private AudioSource audioSource;

    // ── Private ──────────────────────────────────────
    private PlayerController pc;
    private float cooldownTimer = 0f;
    private bool  isAttacking   = false;

    // ══════════════════════════════════════════════════
    //  UNITY
    // ══════════════════════════════════════════════════

    void Awake()
    {
        pc = GetComponent<PlayerController>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        cooldownTimer -= Time.deltaTime;

        if (Input.GetMouseButton(0) && !isAttacking && cooldownTimer <= 0f)
        {
            StartCoroutine(DoAttack());
        }
    }

    // ══════════════════════════════════════════════════
    //  ATTACK
    // ══════════════════════════════════════════════════

    IEnumerator DoAttack()
    {
        isAttacking   = true;
        cooldownTimer = attackCooldown;

        var anim = GetComponent<Animator>();
        if (anim != null) anim.SetTrigger("Attack");

        if (attackSound != null) audioSource.PlayOneShot(attackSound, attackVolume);

        if (attackWindup > 0f)
            yield return new WaitForSeconds(attackWindup);

        int     facing    = pc.GetFacingDirection();
        Vector2 hitCenter = new Vector2(
            transform.position.x + cellSize * facing,
            transform.position.y
        );

        if (attackVFX != null)
            StartCoroutine(ShowVFX(hitCenter, facing)); // ส่งค่าทิศทางไปที่ VFX

        Vector2 boxSize = new Vector2(cellSize * 0.9f, cellSize * 0.9f);
        Collider2D[] hits = Physics2D.OverlapBoxAll(hitCenter, boxSize, 0f, enemyLayer);

        if (hits.Length > 0)
        {
            if (hitSound != null) audioSource.PlayOneShot(hitSound, hitVolume);
            Debug.Log($"[PlayerAttack] ตีโดน {hits.Length} ตัว, damage = {attackDamage}");
        }

        foreach (var col in hits)
        {
            var enemyHP = col.GetComponent<EnemyHealth>();
            if (enemyHP != null)
                enemyHP.TakeDamage(attackDamage, facing);
        }

        isAttacking = false;
    }

    IEnumerator ShowVFX(Vector2 pos, int facing)
    {
        attackVFX.transform.position = pos;

        // --- ส่วนที่เพิ่มสำหรับ Flip VFX ---
        if (flipVFXWithPlayer)
        {
            // ปรับ scale ของ VFX ตามทิศทางที่หัน (1 หรือ -1)
            attackVFX.transform.localScale = new Vector3(facing, 1, 1);
        }
        // --------------------------------

        attackVFX.SetActive(true);
        yield return new WaitForSeconds(0.1f);
        attackVFX.SetActive(false);
    }

    void OnDrawGizmosSelected()
    {
        int facing = 1;
        if (pc != null) facing = pc.GetFacingDirection();
        else if (Application.isPlaying == false) facing = 1;

        Vector3 hitCenter = new Vector3(
            transform.position.x + cellSize * facing,
            transform.position.y,
            0f
        );

        Gizmos.color = new Color(1f, 0f, 0f, 0.35f);
        Gizmos.DrawCube(hitCenter, new Vector3(cellSize * 0.9f, cellSize * 0.9f, 0.1f));

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(hitCenter, new Vector3(cellSize * 0.9f, cellSize * 0.9f, 0.1f));
    }
}