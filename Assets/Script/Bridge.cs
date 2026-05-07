using UnityEngine;
 
/// <summary>
/// Bridge - สะพานที่เลื่อนออกมาเมื่อถูก Trigger จาก ButtonBlock
///
/// วิธีตั้งค่าใน Inspector:
///   1. วางสะพานไว้ที่ตำแหน่ง "ซ่อน" (เช่น ซ่อนอยู่ในกำแพง)
///   2. ตั้ง Extended Position = ตำแหน่งที่อยากให้สะพานเลื่อนไปถึง
///   3. ButtonBlock จะเรียก Activate() เมื่อโดน PushableBlock ชน
/// </summary>
public class Bridge : MonoBehaviour
{
    [Header("Bridge Settings")]
    [Tooltip("ตำแหน่งปลายทางที่สะพานจะเลื่อนไปถึง (World Position)\nต้องตั้งให้ถูกต้อง — ห้ามปล่อยว่าง!")]
    public Vector3 extendedPosition;
 
    [Tooltip("ความเร็วในการเลื่อน")]
    public float moveSpeed = 3f;
 
    private bool isActivated = false;
    private bool isExtended = false;
 
    void Update()
    {
        if (!isActivated || isExtended) return;
 
        transform.position = Vector3.MoveTowards(
            transform.position,
            extendedPosition,
            moveSpeed * Time.deltaTime
        );
 
        if (Vector3.Distance(transform.position, extendedPosition) < 0.01f)
        {
            transform.position = extendedPosition;
            isExtended = true;
        }
    }
 
    public void Activate()
    {
        if (!isActivated)
            isActivated = true;
    }
 
    public bool IsExtended() => isExtended;
    public bool IsActivated() => isActivated;
 
    // แสดง extendedPosition ใน Scene View
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(extendedPosition, transform.localScale);
        Gizmos.DrawLine(transform.position, extendedPosition);
    }
}