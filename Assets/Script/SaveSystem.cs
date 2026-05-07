using System.IO;
using UnityEngine;

/// <summary>
/// SaveSystem — ระบบบันทึกข้อมูลกลาง
/// 
/// บันทึกด้วย JSON ลง Application.persistentDataPath
/// เรียกใช้ผ่าน SaveSystem.Save() / SaveSystem.Load()
/// 
/// ข้อมูลที่เซฟ:
///   - savePointID  : ID ของจุดเซฟล่าสุด
///   - sceneName    : Scene ที่เซฟ
///   - position     : ตำแหน่ง Player ณ จุดเซฟ
///   - saveTime     : เวลาที่บันทึก (แสดงในหน้า UI)
/// </summary>
public static class SaveSystem
{
    private static readonly string SavePath =
        Path.Combine(Application.persistentDataPath, "savefile.json");

    // ══════════════════════════════════════════════════
    //  DATA STRUCT
    // ══════════════════════════════════════════════════

    [System.Serializable]
    public class SaveData
    {
        public string savePointID;
        public string sceneName;
        public float  posX;
        public float  posY;
        public string saveTime;    // ISO 8601 string
    }

    // ══════════════════════════════════════════════════
    //  PUBLIC API
    // ══════════════════════════════════════════════════

    /// <summary>
    /// บันทึกข้อมูล ณ จุดเซฟที่กำหนด
    /// </summary>
    public static void Save(SavePoint point, Vector3 playerPosition)
    {
        var data = new SaveData
        {
            savePointID = point.savePointID,
            sceneName   = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            posX        = playerPosition.x,
            posY        = playerPosition.y,
            saveTime    = System.DateTime.Now.ToString("o")   // ISO 8601
        };

        string json = JsonUtility.ToJson(data, prettyPrint: true);
        File.WriteAllText(SavePath, json);

        Debug.Log($"[SaveSystem] บันทึกแล้ว → {point.savePointID} @ ({data.posX:F1}, {data.posY:F1})");
    }

    /// <summary>
    /// โหลดข้อมูลที่บันทึกไว้ คืน null ถ้าไม่มีไฟล์
    /// </summary>
    public static SaveData Load()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("[SaveSystem] ไม่พบไฟล์ Save");
            return null;
        }

        string json = File.ReadAllText(SavePath);
        return JsonUtility.FromJson<SaveData>(json);
    }

    /// <summary>
    /// ลบไฟล์ Save ทิ้ง
    /// </summary>
    public static void DeleteSave()
    {
        if (File.Exists(SavePath))
            File.Delete(SavePath);
        Debug.Log("[SaveSystem] ลบ Save แล้ว");
    }

    /// <summary>
    /// มีไฟล์ Save อยู่ไหม
    /// </summary>
    public static bool HasSave() => File.Exists(SavePath);

    /// <summary>
    /// คืน SaveTime แบบ human-readable ("12 พ.ค. 2025, 14:30")
    /// </summary>
    public static string GetFormattedSaveTime()
    {
        var data = Load();
        if (data == null) return "—";

        if (System.DateTime.TryParse(data.saveTime, out System.DateTime dt))
            return dt.ToString("dd MMM yyyy, HH:mm");

        return data.saveTime;
    }
}
