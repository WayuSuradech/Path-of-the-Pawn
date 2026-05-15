using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
 
/// <summary>
/// WinScreen — UI You Win
/// 
/// วิธีติดตั้ง:
///   1. แนบ Script นี้ที่ WinScreen Panel
///   2. ปิด Panel ใน Editor เองโดยคลิก checkbox ชื่อ Object ให้ติ๊กออก
///   3. GoalTrigger จะเรียก Show() เมื่อ Player ถึงจุดจบ
/// </summary>
public class WinScreen : MonoBehaviour
{
    [Header("Scene Names")]
    [Tooltip("ชื่อ Scene ด่านถัดไป (ถ้าว่างจะซ่อนปุ่ม Next Level)")]
    public string nextLevelScene = "";
 
    [Tooltip("ชื่อ Scene Main Menu")]
    public string mainMenuScene = "MainMenu";
 
    [Header("UI References (Optional)")]
    public Button nextLevelButton;
 
    public void Show()
    {
        gameObject.SetActive(true);
        Time.timeScale = 0f;
 
        if (nextLevelButton != null)
            nextLevelButton.gameObject.SetActive(!string.IsNullOrEmpty(nextLevelScene));
    }
 
    public void OnNextLevel()
    {
        Time.timeScale = 1f;
        if (!string.IsNullOrEmpty(nextLevelScene))
            SceneManager.LoadScene(nextLevelScene);
    }
 
    public void OnRestart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
 
    public void OnMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene);
    }
}