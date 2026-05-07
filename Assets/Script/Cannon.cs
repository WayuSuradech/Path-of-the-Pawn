using UnityEngine;

/// <summary>
/// Cannon - Block สีชมพู
/// Player เข้าใกล้แล้วกด Space → ยิงกระสุนไฟตามองศาที่กำหนด
/// </summary>
public class Cannon : MonoBehaviour
{
    [Header("References")]
    [Tooltip("ลาก Prefab กระสุนมาใส่")]
    public GameObject projectilePrefab;

    [Tooltip("จุดที่กระสุนจะถูกยิงออกมา (ลาก Empty GameObject มาใส่)")]
    public Transform firePoint;

    [Header("Fire Settings")]
    [Tooltip("องศาการยิง (0 = ขวา, 90 = ขึ้น, 180 = ซ้าย, -45 = เฉียงลงขวา)")]
    public float fireAngle = 45f;

    [Tooltip("ความเร็วกระสุน")]
    public float projectileSpeed = 10f;

    [Tooltip("ยิงได้แค่ครั้งเดียวไหม?")]
    public bool singleUse = true;

    [Header("Interact Settings")]
    public float interactRange = 1.5f;

    [Header("UI (Optional)")]
    public GameObject interactPrompt;

    private PlayerController playerController;
    private bool hasFired = false;

    void Start()
    {
        playerController = FindFirstObjectByType<PlayerController>();

        if (interactPrompt != null)
            interactPrompt.SetActive(false);

        // ถ้าไม่มี firePoint ให้ใช้ตำแหน่งตัวเอง
        if (firePoint == null)
            firePoint = transform;
    }

    void Update()
    {
        if (singleUse && hasFired) return;
        if (playerController == null) return;

        float dist = Vector2.Distance(transform.position, playerController.transform.position);
        bool inRange = dist <= interactRange;

        if (interactPrompt != null)
            interactPrompt.SetActive(inRange);

        if (inRange && Input.GetKeyDown(KeyCode.Space))
        {
            Fire();
        }
    }

    void Fire()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("Cannon: ยังไม่ได้ assign Projectile Prefab!");
            return;
        }

        // คำนวณทิศทางจากองศา
        float rad = fireAngle * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;

        // Spawn กระสุน
        GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);

        // หมุน Sprite กระสุนให้ตรงทิศ
        proj.transform.rotation = Quaternion.Euler(0, 0, fireAngle);

        // ส่งทิศทางและความเร็วให้ Projectile
        Projectile p = proj.GetComponent<Projectile>();
        if (p != null)
        {
            p.Init(direction, projectileSpeed);
        }

        hasFired = true;

        if (interactPrompt != null)
            interactPrompt.SetActive(false);
    }

    void OnDrawGizmosSelected()
    {
        // แสดงทิศการยิง
        float rad = fireAngle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        Gizmos.color = Color.red;
        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        Gizmos.DrawRay(origin, (Vector3)dir * 3f);

        // แสดงระยะ interact
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
