using System.Collections;
using UnityEngine;

/// <summary>
/// EnemyHealth — ระบบเลือดของ Enemy
///
/// Features:
///   - รับ Damage จาก PlayerAttack
///   - Flash สีขาวตอนโดนตี (Silksong style)
///   - Invincibility frames กัน damage ซ้ำในเฟรมเดียวกัน
///   - Knockback แบบ Grid-based ผ่าน EnemyController.ApplyKnockback()
///   - ตายแล้ว Destroy พร้อม DeathFlicker
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHP = 60f;
    public float currentHP;

    [Header("Hit Flash (Silksong Style)")]
    public SpriteRenderer spriteRenderer;
    public Color hitColor = Color.white;
    public float flashDuration = 0.08f;

    [Header("Invincibility Frames")]
    [Tooltip("หน่วงหลังโดนตีก่อนจะรับ Damage ครั้งถัดไปได้ (วินาที)")]
    public float iFrameDuration = 0.15f;

    [Header("Knockback")]
    [Tooltip("จำนวนช่องที่กระเด็นในแนวนอน")]
    public float knockbackForceX = 2f;
    [Tooltip("จำนวนช่องที่กระเด็นขึ้น")]
    public float knockbackForceY = 1f;
    [Tooltip("ระยะเวลา Knockback (วินาที)")]
    public float knockbackDuration = 0.25f;

    [Header("Death")]
    [Tooltip("หน่วงก่อน Destroy (ให้เวลา animation ตาย)")]
    public float deathDelay = 0.4f;
    [Tooltip("เล่น animation ตาย ถ้ามี Animator")]
    public bool useDeathAnimation = true;

    // ── Events ───────────────────────────────────────
    public System.Action OnDeath;

    // ── Private ──────────────────────────────────────
    private Color originalColor;
    private bool isDead = false;
    private bool isInvincible = false;

    // ══════════════════════════════════════════════════
    //  UNITY
    // ══════════════════════════════════════════════════

    void Awake()
    {
        currentHP = maxHP;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    // ══════════════════════════════════════════════════
    //  PUBLIC API
    // ══════════════════════════════════════════════════

    /// <summary>
    /// รับ Damage พร้อม Knockback
    /// direction: +1 = ดันไปขวา, -1 = ดันไปซ้าย (ทิศที่ Player หันอยู่)
    /// </summary>
    public void TakeDamage(float amount, int direction = 0)
    {
        if (isDead || isInvincible) return;

        currentHP -= amount;
        currentHP = Mathf.Max(currentHP, 0f);

        Debug.Log($"[EnemyHealth] {gameObject.name} HP: {currentHP}/{maxHP} (-{amount})");

        StartCoroutine(HitFlash());
        StartCoroutine(InvincibilityFrames());

        // Knockback ผ่าน EnemyController แบบ Grid-based
        if (direction != 0)
        {
            var ec = GetComponent<EnemyController>();
            if (ec != null)
                ec.ApplyKnockback(direction, knockbackForceX, knockbackForceY, knockbackDuration);
        }

        if (currentHP <= 0f)
            StartCoroutine(Die());
    }

    public float GetHPPercent() => currentHP / maxHP;
    public bool IsDead() => isDead;

    // ══════════════════════════════════════════════════
    //  PRIVATE COROUTINES
    // ══════════════════════════════════════════════════

    IEnumerator HitFlash()
    {
        if (spriteRenderer == null) yield break;

        spriteRenderer.color = hitColor;
        yield return new WaitForSeconds(flashDuration);

        if (!isDead)
            spriteRenderer.color = originalColor;
    }

    IEnumerator InvincibilityFrames()
    {
        isInvincible = true;
        yield return new WaitForSeconds(iFrameDuration);
        isInvincible = false;
    }

    IEnumerator Die()
    {
        isDead = true;

        var ec = GetComponent<EnemyController>();
        if (ec != null) ec.enabled = false;

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        if (useDeathAnimation)
        {
            var anim = GetComponent<Animator>();
            if (anim != null) anim.SetTrigger("Die");
        }

        if (spriteRenderer != null)
            StartCoroutine(DeathFlicker());

        OnDeath?.Invoke();

        Debug.Log($"[EnemyHealth] {gameObject.name} ตายแล้ว");

        yield return new WaitForSeconds(deathDelay);

        Destroy(gameObject);
    }

    IEnumerator DeathFlicker()
    {
        if (spriteRenderer == null) yield break;

        int flickers = 6;
        float interval = deathDelay / (flickers * 2f);

        for (int i = 0; i < flickers; i++)
        {
            spriteRenderer.enabled = false;
            yield return new WaitForSeconds(interval);
            spriteRenderer.enabled = true;
            yield return new WaitForSeconds(interval);
        }
    }

    // ══════════════════════════════════════════════════
    //  GIZMO
    // ══════════════════════════════════════════════════

    void OnDrawGizmosSelected()
    {
#if UNITY_EDITOR
        float pct = Application.isPlaying ? currentHP / maxHP : 1f;
        float barW = 1f;
        float barH = 0.1f;
        float yOffset = 0.8f;

        Vector3 barBG = transform.position + Vector3.up * yOffset;

        Gizmos.color = Color.black;
        Gizmos.DrawCube(barBG, new Vector3(barW, barH, 0.01f));

        Gizmos.color = Color.Lerp(Color.red, Color.green, pct);
        Gizmos.DrawCube(
            barBG + Vector3.left * (barW * (1f - pct) * 0.5f),
            new Vector3(barW * pct, barH, 0.01f)
        );
#endif
    }
}