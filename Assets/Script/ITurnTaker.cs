/// <summary>
/// ITurnTaker — Interface สำหรับ Object ที่ขยับตาม Player Turn
///
/// วิธีใช้:
///   ใส่ : ITurnTaker ใน class แล้ว implement OnTurn()
///   จากนั้นเรียก TurnManager.Instance.Register(this) ใน Start()
///
/// ตัวอย่าง Object ที่ใช้ได้:
///   - EnemyController  (เดินไล่ Player ทีละก้าว)
///   - FlyingEnemy      (บินไป Waypoint ถัดไป)
///   - MovingPlatform   (เลื่อนทีละช่อง)
///   - Trap             (ยิงลูกธนูทุก N turn)
/// </summary>
public interface ITurnTaker
{
    void OnTurn();
}
