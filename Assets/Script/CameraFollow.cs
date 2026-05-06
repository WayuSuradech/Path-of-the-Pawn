using UnityEngine;

/// <summary>
/// CameraFollow - กล้องติดตาม Player
/// แนบ Script นี้ไว้ที่ Main Camera
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("ลาก Player มาใส่ช่องนี้")]
    public Transform target;

    [Header("Follow Settings")]
    [Tooltip("ความนุ่มนวลในการติดตาม (ค่าน้อย = นุ่มกว่า)")]
    [Range(0.01f, 1f)]
    public float smoothSpeed = 0.15f;

    [Header("Offset")]
    [Tooltip("ระยะห่างจาก Player (Z ต้องเป็นลบเสมอ เช่น -10)")]
    public Vector3 offset = new Vector3(0f, 1f, -10f);

    [Header("Bounds (Optional)")]
    [Tooltip("เปิดถ้าอยากจำกัดขอบเขตกล้อง")]
    public bool useBounds = false;
    public float minX, maxX, minY, maxY;

    void LateUpdate()
    {
        if (target == null) return;

        // ตำแหน่งที่อยากให้กล้องไป
        Vector3 desiredPosition = target.position + offset;

        // Smooth ด้วย Lerp
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        // จำกัดขอบเขตถ้าเปิดใช้
        if (useBounds)
        {
            smoothedPosition.x = Mathf.Clamp(smoothedPosition.x, minX, maxX);
            smoothedPosition.y = Mathf.Clamp(smoothedPosition.y, minY, maxY);
        }

        // Z คงที่เสมอ (กล้อง 2D)
        smoothedPosition.z = offset.z;

        transform.position = smoothedPosition;
    }
}
