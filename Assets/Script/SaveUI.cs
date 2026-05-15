using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SaveUI — หน้า UI สำหรับ Save / Load
///
/// Setup (ทำใน Inspector):
///   1. สร้าง Canvas → ใส่ Script นี้
///   2. สร้าง Panel ต่างๆ แล้ว Assign ใน Inspector
///      - savePromptPanel   : "กด E เพื่อบันทึก"
///      - savedFeedbackPanel: "บันทึกแล้ว ✓"
///      - mainMenuPanel     : หน้า Title / Load Game
///   3. Assign Text components ต่างๆ
/// </summary>
public class SaveUI : MonoBehaviour
{
    [Header("── Save Prompt (ตอนยืนที่จุดเซฟ) ──")]
    public GameObject savePromptPanel;

    public TextMeshProUGUI savePromptText;

    [Header("── Saved Feedback ──")] public GameObject savedFeedbackPanel;
    public TextMeshProUGUI savedFeedbackText;
    public float feedbackDuration = 2f;

    [Header("── Main Menu / Load Screen ──")]
    public GameObject mainMenuPanel;

    public TextMeshProUGUI lastSaveInfoText;
    public Button continueButton;
    public Button newGameButton;
    public Button deleteSaveButton;

    // ─────────────────────────────────────────────────

    void Start()
    {
        HideSavePrompt();
        HideSavedFeedback();
        SetupMainMenu();
    }

    // ══════════════════════════════════════════════════
    //  SAVE PROMPT
    // ══════════════════════════════════════════════════

    public void ShowSavePrompt(SavePoint point)
    {
        if (savePromptPanel == null) return;
        savePromptPanel.SetActive(true);

        if (savePromptText != null)
            savePromptText.text = $"Press [E] save at\n<b>{point.displayName}</b>";
    }

    public void HideSavePrompt()
    {
        if (savePromptPanel != null)
            savePromptPanel.SetActive(false);
    }

    // ══════════════════════════════════════════════════
    //  SAVED FEEDBACK
    // ══════════════════════════════════════════════════

    public void ShowSavedFeedback(string pointName)
    {
        StopAllCoroutines();
        StartCoroutine(FeedbackRoutine(pointName));
    }

    void HideSavedFeedback()
    {
        if (savedFeedbackPanel != null)
            savedFeedbackPanel.SetActive(false);
    }

    IEnumerator FeedbackRoutine(string pointName)
    {
        if (savedFeedbackPanel == null) yield break;

        savedFeedbackPanel.SetActive(true);

        if (savedFeedbackText != null)
            savedFeedbackText.text = $"Save at <b>{pointName}</b>\n{System.DateTime.Now:HH:mm}";

        yield return new WaitForSeconds(feedbackDuration);
        savedFeedbackPanel.SetActive(false);
    }

    // ══════════════════════════════════════════════════
    //  MAIN MENU / LOAD SCREEN
    // ══════════════════════════════════════════════════

    void SetupMainMenu()
    {
        if (mainMenuPanel == null) return;

        bool hasSave = SaveSystem.HasSave();

        // ── แสดงข้อมูล Save ล่าสุด ──────────────────
        if (lastSaveInfoText != null)
        {
            if (hasSave)
            {
                var data = SaveSystem.Load();
                lastSaveInfoText.text =
                    $"Last save <b>{data.savePointID}</b>\n{SaveSystem.GetFormattedSaveTime()}";
            }
            else
            {
                lastSaveInfoText.text = "No data Found!";
            }
        }

        // ── ปุ่ม Continue ────────────────────────────
        if (continueButton != null)
        {
            continueButton.interactable = hasSave;
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinueClicked);
        }

        // ── ปุ่ม New Game ────────────────────────────
        if (newGameButton != null)
        {
            newGameButton.onClick.RemoveAllListeners();
            newGameButton.onClick.AddListener(OnNewGameClicked);
        }

        // ── ปุ่ม Delete Save ─────────────────────────
        if (deleteSaveButton != null)
        {
            deleteSaveButton.interactable = hasSave;
            deleteSaveButton.onClick.RemoveAllListeners();
            deleteSaveButton.onClick.AddListener(OnDeleteSaveClicked);
        }
    }

    // ── Button Handlers ───────────────────────────────

    void OnContinueClicked()
    {
        Time.timeScale = 1f;
        var data = SaveSystem.Load();
        if (data == null) return;

        mainMenuPanel.SetActive(false);

        // โหลด Scene ใหม่เสมอ — Reset puzzle และ object ทุกอย่างใน Scene
        // RespawnOnLoad จะจัดการย้าย Player ไปจุดเซฟให้
        RespawnOnLoad.pendingRespawn = true;
        UnityEngine.SceneManagement.SceneManager.LoadScene(data.sceneName);
    }

    void OnNewGameClicked()
    {
        Time.timeScale = 1f;
        mainMenuPanel.SetActive(false);
        // โหลด Scene แรก (index 0) — ปรับเป็น Scene ที่ต้องการ
        UnityEngine.SceneManagement.SceneManager.LoadScene(1);
    }

    void OnDeleteSaveClicked()
    {
        SaveSystem.DeleteSave();
        SetupMainMenu(); // refresh UI
    }

    // ══════════════════════════════════════════════════
    //  PUBLIC: เปิด/ปิด Main Menu จาก Script อื่น
    // ══════════════════════════════════════════════════

    public void ShowMainMenu()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
            SetupMainMenu();

            // --- หยุดเวลาเกม ---
            Time.timeScale = 0f;
        }
    }

    public void HideMainMenu()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);

            // --- ให้เกมกลับมาเดินต่อ ---
            Time.timeScale = 1f;
        }
    }
}