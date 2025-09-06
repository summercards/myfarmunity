using System.IO;
using UnityEngine;

/// <summary>
/// 统一管理存档路径/存档槽。
/// 目录结构：
///   {persistentDataPath}/Saves/slot_{NNN}/
/// 例如： C:/Users/你/AppData/LocalLow/公司/游戏名/Saves/slot_001/
/// </summary>
public static class SavePaths
{
    /// <summary>存档基础目录 …/Saves</summary>
    public static string BaseDir =>
        Path.Combine(Application.persistentDataPath, "Saves");

    /// <summary>当前存档槽编号（从1开始）。你可以在主菜单切换它。</summary>
    public static int CurrentSlot { get; private set; } = 1;

    /// <summary>当前槽的目录 …/Saves/slot_001</summary>
    public static string SlotDir =>
        Path.Combine(BaseDir, $"slot_{CurrentSlot:000}");

    /// <summary>切换存档槽（会确保目录存在）。</summary>
    public static void SetSlot(int slotIndex)
    {
        if (slotIndex < 1) slotIndex = 1;
        CurrentSlot = slotIndex;
        EnsureDir();
    }

    /// <summary>确保当前槽目录已创建。</summary>
    public static void EnsureDir()
    {
        if (!Directory.Exists(BaseDir)) Directory.CreateDirectory(BaseDir);
        if (!Directory.Exists(SlotDir)) Directory.CreateDirectory(SlotDir);
    }

    /// <summary>获取当前槽下的某个文件的完整路径。</summary>
    public static string FileInSlot(string fileName)
    {
        EnsureDir();
        return Path.Combine(SlotDir, fileName);
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/Open Saves Folder")]
    static void OpenSavesFolder()
    {
        EnsureDir();
        Application.OpenURL(SlotDir);
    }
#endif
}
