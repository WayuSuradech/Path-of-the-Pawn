using UnityEngine;

/// <summary>
/// GoalTrigger — จุดจบด่าน
/// แนบที่ GameObject ที่ต้องการให้เป็นจุด Win
/// ต้องมี Collider2D และติ๊ก Is Trigger = true
/// </summary>
public class GoalTrigger : MonoBehaviour
{
    [Header("References")]
    [Tooltip("ลาก WinScreen GameObject มาใส่")]
    public WinScreen winScreen;

    [Header("Settings")]
    [Tooltip("หน่วงเวลาก่อนแสดง Win Screen (วินาที)")]
    public float delay = 0.5f;

    private bool triggered = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;

        // ปิดการควบคุม Player
        if (other.TryGetComponent<PlayerController>(out var pc))
            pc.enabled = false;

        Invoke(nameof(ShowWin), delay);
    }

    void ShowWin()
    {
        if (winScreen != null)
            winScreen.Show();
        else
            Debug.LogWarning("GoalTrigger: ไม่ได้ assign WinScreen!");
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.9f, 0f, 0.3f);
        Gizmos.DrawCube(transform.position, transform.localScale);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}
