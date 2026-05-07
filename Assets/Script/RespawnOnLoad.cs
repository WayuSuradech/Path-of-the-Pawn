using UnityEngine;

/// <summary>
/// RespawnOnLoad — จัดการ Respawn ข้าม Scene
/// วาง Script นี้ไว้ใน Scene (ทุก Scene ที่ Player อาจ Respawn ที่นั่น)
/// </summary>
public class RespawnOnLoad : MonoBehaviour
{
    public static bool pendingRespawn = false;

    void Start()
    {
        if (!pendingRespawn) return;
        pendingRespawn = false;

        var data = SaveSystem.Load();
        if (data == null) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        player.transform.position = new Vector3(data.posX, data.posY, 0f);

        var hp = player.GetComponent<PlayerHealth>();
        if (hp != null) hp.currentHP = hp.maxHP;

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        var pc = player.GetComponent<PlayerController>();
        if (pc != null) pc.enabled = true;

        Debug.Log($"[RespawnOnLoad] Respawn ที่ '{data.savePointID}'");
    }
}
