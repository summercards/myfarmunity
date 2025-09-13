using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// 统一管理存档路径/存档槽，并提供“旧路径兼容/迁移”帮助方法。
/// 目录：{persistentDataPath}/Saves/slot_{NNN}/
/// </summary>
public static class SavePaths
{
    const string SlotKey = "save.currentSlot";

    static int _currentSlot;
    static SavePaths()
    {
        _currentSlot = PlayerPrefs.GetInt(SlotKey, 1);
        if (_currentSlot < 1) _currentSlot = 1;
    }

    public static string BaseDir => Path.Combine(Application.persistentDataPath, "Saves");

    public static int CurrentSlot
    {
        get => _currentSlot;
        private set
        {
            _currentSlot = Mathf.Max(1, value);
            PlayerPrefs.SetInt(SlotKey, _currentSlot);
            PlayerPrefs.Save();
        }
    }

    public static string SlotDir => Path.Combine(BaseDir, $"slot_{CurrentSlot:000}");

    public static void SetSlot(int slotIndex)
    {
        CurrentSlot = Mathf.Max(1, slotIndex);
        EnsureDir();
    }

    public static void EnsureDir()
    {
        try
        {
            if (!Directory.Exists(BaseDir)) Directory.CreateDirectory(BaseDir);
            if (!Directory.Exists(SlotDir)) Directory.CreateDirectory(SlotDir);
        }
        catch { }
    }

    public static string FileInSlot(string fileName)
    {
        EnsureDir();
        return Path.Combine(SlotDir, fileName);
    }

    /// <summary>兼容旧版本路径：找到则返回旧路径，否则返回当前槽路径。</summary>
    public static string FindExistingOrCurrent(string fileName)
    {
        string cur = FileInSlot(fileName);
        if (File.Exists(cur)) return cur;

        string legacy1 = Path.Combine(BaseDir, fileName);
        if (File.Exists(legacy1)) return legacy1;

        string legacy2 = Path.Combine(Application.persistentDataPath, fileName);
        if (File.Exists(legacy2)) return legacy2;

        try
        {
            if (Directory.Exists(BaseDir))
            {
                var slotDirs = Directory.GetDirectories(BaseDir, "slot_*");
                foreach (var d in slotDirs)
                {
                    var p = Path.Combine(d, fileName);
                    if (File.Exists(p)) return p;
                }
            }
        }
        catch { }

        return cur;
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
