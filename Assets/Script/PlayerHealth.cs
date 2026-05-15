using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PlayerHealth — จัดการ HP และการตายของ Player
/// 
/// ปรับปรุงจากเดิม: ตายแล้ว Respawn กลับจุดเซฟล่าสุด
/// โดยอัตโนมัติผ่าน SaveSystem
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHP = 100f;
    public float currentHP;

    [Header("Death Settings")]
    [Tooltip("หน่วงเวลากี่วินาทีก่อน Respawn")]
    public float deathDelay = 1f;

    [Header("Audio Settings")]
    [Tooltip("ไฟล์เสียงเวลาตัวละครตาย")]
    public AudioClip deathSound;
    [Range(0f, 1f)] public float deathVolume = 1f;
    private AudioSource audioSource;

    [Header("Respawn Fallback")]
    [Tooltip("ถ้าไม่มี Save File จะ Respawn ที่จุดนี้แทน")]
    public Transform defaultRespawnPoint;

    // ── State ─────────────────────────────────────────
    private bool isDead = false;

    void Awake()
    {
        currentHP = maxHP;
        // เตรียม AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    // ══════════════════════════════════════════════════
    //  DAMAGE / HEAL
    // ══════════════════════════════════════════════════

    /// <summary>
    /// รับ Damage — ถูกเรียกจาก Enemy ผ่าน SendMessage("TakeDamage", amount)
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHP -= amount;
        currentHP = Mathf.Max(currentHP, 0f);

        Debug.Log($"[PlayerHealth] HP: {currentHP}/{maxHP}");

        if (currentHP <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        currentHP = Mathf.Min(currentHP + amount, maxHP);
    }

    // ══════════════════════════════════════════════════
    //  DEATH / RESPAWN
    // ══════════════════════════════════════════════════

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        // เล่นเสียงตาย
        if (deathSound != null && audioSource != null)
            audioSource.PlayOneShot(deathSound, deathVolume);

        // ปิดการควบคุม
        var pc = GetComponent<PlayerController>();
        if (pc != null) pc.enabled = false;

        // Trigger animation (ถ้ามี)
        var anim = GetComponent<Animator>();
        if (anim != null) anim.SetTrigger("Die");

        Debug.Log("[PlayerHealth] Player ตาย → รอ Respawn");

        Invoke(nameof(Respawn), deathDelay);
    }

    void Respawn()
    {
        var data = SaveSystem.Load();

        if (data != null)
        {
            // ── มี Save File → โหลด Scene ที่เซฟ แล้ว Spawn ที่จุดเซฟ ──
            string currentScene = SceneManager.GetActiveScene().name;

            if (data.sceneName == currentScene)
            {
                // Scene เดิม → ย้ายตำแหน่งตรงๆ ไม่ต้องโหลด Scene ใหม่
                RespawnAtSavedPosition(data);
            }
            else
            {
                // Scene ต่างกัน → โหลด Scene ใหม่ แล้วให้ RespawnOnLoad จัดการ
                RespawnOnLoad.pendingRespawn = true;
                SceneManager.LoadScene(data.sceneName);
            }
        }
        else
        {
            // ── ไม่มี Save File → ใช้ Default Respawn Point ──
            if (defaultRespawnPoint != null)
            {
                RespawnAtPosition(defaultRespawnPoint.position);
            }
            else
            {
                // โหลด Scene ใหม่ตั้งแต่ต้น
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }
    }

    void RespawnAtSavedPosition(SaveSystem.SaveData data)
    {
        RespawnAtPosition(new Vector3(data.posX, data.posY, 0f));
        Debug.Log($"[PlayerHealth] Respawn ที่จุด '{data.savePointID}'");
    }

    void RespawnAtPosition(Vector3 pos)
    {
        transform.position = pos;
        currentHP = maxHP;
        isDead = false;

        var pc = GetComponent<PlayerController>();
        if (pc != null) pc.enabled = true;

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    // ══════════════════════════════════════════════════
    //  PUBLIC HELPERS
    // ══════════════════════════════════════════════════

    public bool IsDead() => isDead;
    public float GetHPPercent() => currentHP / maxHP;
}