using System.IO;
using UnityEngine;

/// <summary>
/// ͳһ����浵·��/�浵�ۡ�
/// Ŀ¼�ṹ��
///   {persistentDataPath}/Saves/slot_{NNN}/
/// ���磺 C:/Users/��/AppData/LocalLow/��˾/��Ϸ��/Saves/slot_001/
/// </summary>
public static class SavePaths
{
    /// <summary>�浵����Ŀ¼ ��/Saves</summary>
    public static string BaseDir =>
        Path.Combine(Application.persistentDataPath, "Saves");

    /// <summary>��ǰ�浵�۱�ţ���1��ʼ��������������˵��л�����</summary>
    public static int CurrentSlot { get; private set; } = 1;

    /// <summary>��ǰ�۵�Ŀ¼ ��/Saves/slot_001</summary>
    public static string SlotDir =>
        Path.Combine(BaseDir, $"slot_{CurrentSlot:000}");

    /// <summary>�л��浵�ۣ���ȷ��Ŀ¼���ڣ���</summary>
    public static void SetSlot(int slotIndex)
    {
        if (slotIndex < 1) slotIndex = 1;
        CurrentSlot = slotIndex;
        EnsureDir();
    }

    /// <summary>ȷ����ǰ��Ŀ¼�Ѵ�����</summary>
    public static void EnsureDir()
    {
        if (!Directory.Exists(BaseDir)) Directory.CreateDirectory(BaseDir);
        if (!Directory.Exists(SlotDir)) Directory.CreateDirectory(SlotDir);
    }

    /// <summary>��ȡ��ǰ���µ�ĳ���ļ�������·����</summary>
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
