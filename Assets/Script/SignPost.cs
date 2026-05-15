using UnityEngine;

public class SignPost : MonoBehaviour
{
    [Header("UI Settings")]
    [Tooltip("Panel สอนการเล่นที่จะให้แสดง")]
    public GameObject tutorialPanel;
    
    [Tooltip("GameObject ที่เป็นปุ่ม Space/E เพื่อบอกให้ผู้เล่นกด")]
    public GameObject interactPrompt;

    private bool playerInRange = false;
    private bool isDialogOpen = false;

    void Start()
    {
        if (interactPrompt != null) interactPrompt.SetActive(false);
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
    }

    void Update()
    {
        // ถ้าผู้เล่นอยู่ในระยะ และไม่ได้เปิดหน้าต่างอยู่ ให้กด Space เพื่ออ่าน
        if (playerInRange && !isDialogOpen)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                OpenTutorial();
            }
        }
    }

    public void OpenTutorial()
    {
        if (tutorialPanel == null) return;

        isDialogOpen = true;
        tutorialPanel.SetActive(true);
        if (interactPrompt != null) interactPrompt.SetActive(false);

        // หยุดเวลาเกมเพื่อให้ผู้เล่นค่อยๆ อ่าน
        Time.timeScale = 0f;
    }

    public void CloseTutorial()
    {
        if (tutorialPanel == null) return;

        isDialogOpen = false;
        tutorialPanel.SetActive(false);
        if (playerInRange && interactPrompt != null) interactPrompt.SetActive(true);

        // ให้เวลาเดินต่อ
        Time.timeScale = 1f;
    }

    // --- ตรวจสอบระยะ ---
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            if (!isDialogOpen && interactPrompt != null) 
                interactPrompt.SetActive(true);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (interactPrompt != null) interactPrompt.SetActive(false);
            
            // ป้องกันกรณีเดินหนีป้ายโดยไม่กดปิด (ถ้าต้องการให้ปิดอัตโนมัติ)
            // CloseTutorial(); 
        }
    }
}