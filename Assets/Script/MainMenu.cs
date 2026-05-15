using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// MainMenu - จัดการ Main Menu
///
/// วิธีใช้:
///   1. สร้าง Scene ใหม่ชื่อ "MainMenu"
///   2. สร้าง Canvas → Button 2 อัน (Play, Quit)
///   3. แนบ Script นี้ที่ GameObject ใดก็ได้
///   4. ผูก Button:
///      - Play  → OnClick → MainMenu.PlayGame()
///      - Quit  → OnClick → MainMenu.QuitGame()
///   5. ใน Build Settings → เพิ่ม MainMenu เป็น Scene index 0
///      และ Scene เกมเป็น index 1
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("ชื่อ Scene ที่จะโหลดตอนกด Play")]
    public string gameSceneName = "Map1";

    // ── ปุ่ม Play ──────────────────────────────────
    public void PlayGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    // ── ปุ่ม Quit ──────────────────────────────────
    public void QuitGame()
    {
        Application.Quit();

        // ใช้ใน Editor (Application.Quit ไม่ทำงานใน Editor)
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
