using UnityEngine;

/// <summary>
/// SavePoint — จุดเซฟในโลกเกม
///
/// วางหลายจุดได้ ตั้ง savePointID ไม่ซ้ำกัน
/// เมื่อ Player เข้าใกล้ → แสดง Prompt กด [E] เพื่อบันทึก
/// 
/// Setup:
///   1. สร้าง GameObject ใส่ Script นี้
///   2. เพิ่ม Collider2D แบบ Trigger
///   3. ตั้ง savePointID ให้ไม่ซ้ำกันแต่ละจุด
/// </summary>
public class SavePoint : MonoBehaviour
{
    [Header("Save Point Settings")]
    [Tooltip("ID ไม่ซ้ำกันแต่ละจุด เช่น 'cave_entrance', 'boss_room', 'village'")]
    public string savePointID = "savepoint_01";

    [Tooltip("บันทึกอัตโนมัติเมื่อ Player เข้าใกล้ (ไม่ต้องกด E)")]
    public bool autoSave = false;

    [Tooltip("ชื่อที่แสดงใน UI เช่น 'ทางเข้าถ้ำ'")]
    public string displayName = "จุดบันทึก";

    [Header("Visual Feedback")]
    public SpriteRenderer spriteRenderer;
    public Color inactiveColor = new Color(0.5f, 0.5f, 0.5f);
    public Color activeColor   = Color.yellow;
    public Color savedColor    = Color.cyan;

    // ── State ─────────────────────────────────────────
    private bool playerInRange = false;
    private bool isSaved       = false;   // เคยเซฟจุดนี้แล้วหรือยัง

    void Start()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = inactiveColor;

        // ถ้าจุดนี้คือจุดที่เซฟล่าสุด → แสดงสี saved
        var data = SaveSystem.Load();
        if (data != null && data.savePointID == savePointID)
        {
            isSaved = true;
            if (spriteRenderer != null)
                spriteRenderer.color = savedColor;
        }
    }

    void Update()
    {
        if (!playerInRange) return;

        // กด E เพื่อเซฟ (ถ้าไม่ใช่ autoSave)
        if (!autoSave && Input.GetKeyDown(KeyCode.E))
            TriggerSave();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;

        if (spriteRenderer != null)
            spriteRenderer.color = activeColor;

        if (autoSave)
        {
            TriggerSave();
        }
        else
        {
            // แจ้ง SaveUI ให้แสดง Prompt
            SaveUI ui = FindObjectOfType<SaveUI>();
            if (ui != null) ui.ShowSavePrompt(this);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;

        if (spriteRenderer != null)
            spriteRenderer.color = isSaved ? savedColor : inactiveColor;

        // ซ่อน Prompt
        SaveUI ui = FindObjectOfType<SaveUI>();
        if (ui != null) ui.HideSavePrompt();
    }

    /// <summary>
    /// บันทึกเกม ณ จุดนี้
    /// </summary>
    public void TriggerSave()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        SaveSystem.Save(this, player.transform.position);

        isSaved = true;
        if (spriteRenderer != null)
            spriteRenderer.color = savedColor;

        // แจ้ง SaveUI ให้แสดง feedback "บันทึกแล้ว"
        SaveUI ui = FindObjectOfType<SaveUI>();
        if (ui != null) ui.ShowSavedFeedback(displayName);

        Debug.Log($"[SavePoint] บันทึกที่ '{displayName}' (ID: {savePointID})");
    }

    // Gizmo แสดงใน Scene View
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.8f);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.6f,
            $"{savePointID}"
        );
#endif
    }
}
