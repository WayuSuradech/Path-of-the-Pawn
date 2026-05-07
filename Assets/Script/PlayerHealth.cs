using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PlayerHealth - จัดการการตายของ Player
/// แนบ Script นี้ที่ Player
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Death Settings")]
    [Tooltip("หน่วงเวลากี่วินาทีก่อน Respawn")]
    public float deathDelay = 1f;

    [Tooltip("Respawn แบบไหน?")]
    public RespawnType respawnType = RespawnType.ReloadScene;

    [Header("Respawn Point (ถ้าเลือก RespawnAtPoint)")]
    public Transform respawnPoint;

    public enum RespawnType
    {
        ReloadScene,    // โหลด Scene ใหม่
        RespawnAtPoint  // กลับไปที่จุด Respawn
    }

    private bool isDead = false;

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        // ปิดการควบคุม Player
        PlayerController pc = GetComponent<PlayerController>();
        if (pc != null) pc.enabled = false;

        // เล่น animation ตาย (ถ้ามี)
        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.SetTrigger("Die");

        // รอแล้ว Respawn
        Invoke(nameof(Respawn), deathDelay);
    }

    void Respawn()
    {
        if (respawnType == RespawnType.ReloadScene)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        else if (respawnType == RespawnType.RespawnAtPoint)
        {
            if (respawnPoint != null)
                transform.position = respawnPoint.position;

            isDead = false;

            PlayerController pc = GetComponent<PlayerController>();
            if (pc != null) pc.enabled = true;
        }
    }

    public bool IsDead() => isDead;
}
