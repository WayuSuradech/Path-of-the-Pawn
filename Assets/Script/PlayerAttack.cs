using System.Collections;
using UnityEngine;

/// <summary>
/// PlayerAttack — ระบบโจมตีแบบ Grid
///
/// ตี 1 ช่องตรงหน้า Player (ทิศที่หันอยู่)
/// ใช้ OverlapBox เช็ค Collider ใน cell นั้น
/// 
/// Setup:
///   - ใส่ Script นี้บน Player GameObject เดียวกับ PlayerController
///   - ตั้ง enemyLayer ให้ตรงกับ Layer ของ Enemy
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
        // เพิ่ม AudioSource อัตโนมัติถ้าไม่มี
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

        // Trigger animation windup (ถ้ามี Animator)
        var anim = GetComponent<Animator>();
        if (anim != null) anim.SetTrigger("Attack");

        // เล่นเสียงเหวี่ยงอาวุธพร้อมปรับ Volume
        if (attackSound != null) audioSource.PlayOneShot(attackSound, attackVolume);

        // รอ Windup
        if (attackWindup > 0f)
            yield return new WaitForSeconds(attackWindup);

        // คำนวณตำแหน่ง cell ข้างหน้า
        int     facing    = pc.GetFacingDirection();
        Vector2 hitCenter = new Vector2(
            transform.position.x + cellSize * facing,
            transform.position.y
        );

        // แสดง VFX
        if (attackVFX != null)
            StartCoroutine(ShowVFX(hitCenter));

        // OverlapBox เช็ค Enemy ใน cell นั้น
        Vector2 boxSize = new Vector2(cellSize * 0.9f, cellSize * 0.9f);
        Collider2D[] hits = Physics2D.OverlapBoxAll(hitCenter, boxSize, 0f, enemyLayer);

        if (hits.Length > 0)
        {
            // เล่นเสียงเมื่อตีโดนศัตรูพร้อมปรับ Volume
            if (hitSound != null) audioSource.PlayOneShot(hitSound, hitVolume);
            Debug.Log($"[PlayerAttack] ตีโดน {hits.Length} ตัว, damage = {attackDamage}");
        }

        foreach (var col in hits)
        {
            var enemyHP = col.GetComponent<EnemyHealth>();
            if (enemyHP != null)
                enemyHP.TakeDamage(attackDamage, facing);  // ส่งทิศให้ Knockback
        }

        isAttacking = false;
    }

    IEnumerator ShowVFX(Vector2 pos)
    {
        attackVFX.transform.position = pos;
        attackVFX.SetActive(true);
        yield return new WaitForSeconds(0.1f);
        attackVFX.SetActive(false);
    }

    // ══════════════════════════════════════════════════
    //  GIZMO — แสดง HitBox ใน Scene View
    // ══════════════════════════════════════════════════

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

        // สีแดงโปร่งแสง = zone โจมตี
        Gizmos.color = new Color(1f, 0f, 0f, 0.35f);
        Gizmos.DrawCube(hitCenter, new Vector3(cellSize * 0.9f, cellSize * 0.9f, 0.1f));

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(hitCenter, new Vector3(cellSize * 0.9f, cellSize * 0.9f, 0.1f));
    }
}