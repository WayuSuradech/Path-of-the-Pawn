using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// TurnManager — ระบบ Turn-based
///
/// ทุกครั้งที่ Player เดิน 1 ก้าว → TurnManager จะเรียก OnTurn()
/// ให้ทุก Object ที่ implement ITurnTaker ขยับตาม
///
/// วิธีใช้:
///   1. สร้าง Empty GameObject ชื่อ "TurnManager" แนบ Script นี้
///   2. Enemy หรือ Object ที่อยากขยับตาม → implement ITurnTaker
///   3. PlayerController จะเรียก TurnManager.Instance.EndPlayerTurn() ให้เอง
/// </summary>
public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    // รายชื่อ Object ที่จะขยับตาม Player
    private List<ITurnTaker> turnTakers = new List<ITurnTaker>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── ลงทะเบียน / ยกเลิก ────────────────────────

    public void Register(ITurnTaker t)
    {
        if (!turnTakers.Contains(t)) turnTakers.Add(t);
    }

    public void Unregister(ITurnTaker t)
    {
        turnTakers.Remove(t);
    }

    // ── เรียกจาก PlayerController หลัง Player เดินเสร็จ ──

    public void EndPlayerTurn()
    {
        // เรียก OnTurn() ให้ทุก Object ที่ลงทะเบียน
        foreach (var t in turnTakers)
        {
            if (t != null) t.OnTurn();
        }
    }
}
