using System.Collections.Generic;
using UnityEngine;

// ══════════════════════════════════════════════════════════════
//
//  PuzzleManager — จัดการ Puzzle ทั้งหมดในฉาก
//
//  วิธีใช้:
//    1. สร้าง Empty GameObject ชื่อ "PuzzleManager"
//    2. แนบ Script นี้
//    3. กด + เพื่อเพิ่ม Puzzle ใหม่
//    4. ลาก Key Item / Button / Bridge มาใส่แต่ละ Puzzle
//    5. ปรับ Settings ของ Bridge และ Button ในแต่ละ Puzzle ได้เลย
//
//  1 Script จัดการได้ไม่จำกัด Puzzle!
//
// ══════════════════════════════════════════════════════════════

public class PuzzleManager : MonoBehaviour
{
    [System.Serializable]
    public class PuzzleEntry
    {
        [Tooltip("ชื่อ Puzzle (แสดงใน Inspector เฉยๆ)")]
        public string puzzleName = "Puzzle";

        [Header("Objects")]
        [Tooltip("Block ที่ต้องผลักหรือ drop มาชน Button (PushableBlock หรือ Rigidbody2D ก็ได้)")]
        public GameObject keyItem;

        [Tooltip("Button ที่รอรับ Key Item")]
        public GameObject button;

        [Tooltip("Bridge ที่จะเลื่อนออกมา")]
        public GameObject bridge;

        [Header("Bridge Settings")]
        public Vector3 bridgeExtendedPosition;
        public float   bridgeSpeed = 4f;

        [Header("Button Settings")]
        public Vector3 buttonRaisedPosition;
        public float   buttonSpeed = 4f;
        public float   activationDelay = 0.5f;

        // Runtime state
        [HideInInspector] public bool isActivated        = false;
        [HideInInspector] public bool canReceiveCollision = false;
        [HideInInspector] public bool isButtonRising     = false;
        [HideInInspector] public bool isBridgeMoving     = false;
        [HideInInspector] public bool isBridgeDone       = false;
    }

    [Header("Puzzles")]
    public List<PuzzleEntry> puzzles = new List<PuzzleEntry>();

    // ══════════════════════════════════════════════
    //  INIT
    // ══════════════════════════════════════════════

    void Start()
    {
        foreach (var p in puzzles)
        {
            // Default raised position ถ้าปล่อยว่าง
            if (p.button != null && p.buttonRaisedPosition == Vector3.zero)
                p.buttonRaisedPosition = p.button.transform.position + new Vector3(0f, 3f, 0f);

            // ตั้ง Button Rigidbody เป็น Kinematic
            if (p.button != null && p.button.TryGetComponent<Rigidbody2D>(out var rb))
                rb.bodyType = RigidbodyType2D.Kinematic;

            // หน่วงก่อนรับ Collision
            float delay = p.activationDelay;
            var   entry = p;
            StartCoroutine(EnableCollisionAfterDelay(entry, delay));
        }
    }

    System.Collections.IEnumerator EnableCollisionAfterDelay(PuzzleEntry p, float delay)
    {
        yield return new WaitForSeconds(delay);
        p.canReceiveCollision = true;
    }

    // ══════════════════════════════════════════════
    //  UPDATE — ขยับ Button และ Bridge ทุก Frame
    // ══════════════════════════════════════════════

    void Update()
    {
        foreach (var p in puzzles)
        {
            if (!p.isActivated) continue;

            // ยก Button ขึ้น
            if (p.isButtonRising && p.button != null)
            {
                p.button.transform.position = Vector3.MoveTowards(
                    p.button.transform.position,
                    p.buttonRaisedPosition,
                    p.buttonSpeed * Time.deltaTime
                );
            }

            // เลื่อน Bridge ออกมา
            if (p.isBridgeMoving && !p.isBridgeDone && p.bridge != null)
            {
                p.bridge.transform.position = Vector3.MoveTowards(
                    p.bridge.transform.position,
                    p.bridgeExtendedPosition,
                    p.bridgeSpeed * Time.deltaTime
                );

                if (Vector3.Distance(p.bridge.transform.position, p.bridgeExtendedPosition) < 0.01f)
                {
                    p.bridge.transform.position = p.bridgeExtendedPosition;
                    p.isBridgeDone = true;
                }
            }
        }
    }

    // ══════════════════════════════════════════════
    //  COLLISION DETECTION
    // ══════════════════════════════════════════════

    void OnEnable()
    {
        // Subscribe Collision events จากทุก Button
        // ใช้ PuzzleButtonListener ที่แนบอัตโนมัติ
        foreach (var p in puzzles)
        {
            if (p.button == null) continue;

            var listener = p.button.GetComponent<PuzzleButtonListener>();
            if (listener == null) listener = p.button.AddComponent<PuzzleButtonListener>();

            listener.Setup(this, p);
        }
    }

    /// <summary>
    /// เรียกจาก PuzzleButtonListener เมื่อ Key Item ชน Button
    /// </summary>
    public void OnButtonTriggered(PuzzleEntry p)
    {
        if (!p.canReceiveCollision || p.isActivated) return;
        p.isActivated    = true;
        p.isButtonRising = true;
        p.isBridgeMoving = true;

        // เปลี่ยนสี Button
        if (p.button != null && p.button.TryGetComponent<SpriteRenderer>(out var sr))
            sr.color = new Color(0.5f, 0f, 0f);

        Debug.Log($"[PuzzleManager] Puzzle '{p.puzzleName}' Activated!");
    }

    // ══════════════════════════════════════════════
    //  GIZMOS
    // ══════════════════════════════════════════════

    void OnDrawGizmos()
    {
        foreach (var p in puzzles)
        {
            if (p.bridge != null && p.bridgeExtendedPosition != Vector3.zero)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(p.bridgeExtendedPosition, p.bridge.transform.localScale);
                Gizmos.DrawLine(p.bridge.transform.position, p.bridgeExtendedPosition);
            }

            if (p.button != null && p.buttonRaisedPosition != Vector3.zero)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(p.buttonRaisedPosition, p.button.transform.localScale);
                Gizmos.DrawLine(p.button.transform.position, p.buttonRaisedPosition);
            }

            // เส้นเชื่อม Key Item → Button → Bridge
            if (p.keyItem != null && p.button != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(p.keyItem.transform.position, p.button.transform.position);
            }
            if (p.button != null && p.bridge != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(p.button.transform.position, p.bridge.transform.position);
            }
        }
    }
}


// ══════════════════════════════════════════════════════════════
//  PuzzleButtonListener — แนบที่ Button อัตโนมัติ
//  รับ Collision แล้วส่งต่อให้ PuzzleManager
// ══════════════════════════════════════════════════════════════

public class PuzzleButtonListener : MonoBehaviour
{
    private PuzzleManager           manager;
    private PuzzleManager.PuzzleEntry entry;

    public void Setup(PuzzleManager m, PuzzleManager.PuzzleEntry e)
    {
        manager = m;
        entry   = e;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (manager == null || entry == null) return;

        // รับจาก PushableBlock
        if (col.gameObject.TryGetComponent<PushableBlock>(out var pb))
        {
            if (!pb.HasBeenPushed()) return;
            if (entry.keyItem != null && col.gameObject != entry.keyItem) return;
            manager.OnButtonTriggered(entry);
            return;
        }

        // รับจาก Rigidbody2D ทั่วไป (เช่น Block ที่ drop ลงมา)
        if (entry.keyItem != null && col.gameObject == entry.keyItem)
        {
            manager.OnButtonTriggered(entry);
        }
    }
}
