// Assets/Scripts/Build/BuildCatalogSO.cs
using UnityEngine;

public enum BuildCategory { Furniture, Outdoor, Wall, Surface }   // �򻯵İڷŷ���
public enum BuildSnapMode { Ground, Wall, Surface }               // ��Ӧ�������ж��뷽ʽ

[CreateAssetMenu(menuName = "Farm/Build Catalog", fileName = "BuildCatalogSO")]
public class BuildCatalogSO : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        [Header("Key")]
        public string itemId;                // ��Ӧ ItemSO.id���ֳ���ƷID��

        [Header("Prefab")]
        public GameObject prefab;            // ʵ�ʰڷŵ�Ԥ���壨����ʱʵ������

        [Header("Rules")]
        public BuildCategory category = BuildCategory.Furniture;
        public BuildSnapMode snapMode = BuildSnapMode.Ground;

        [Tooltip("����ڷŵı����")]
        public LayerMask surfaceLayers = ~0;

        [Tooltip("��������ࣨGround/Surfaceʱ���ã�")]
        public float grid = 0.5f;

        [Tooltip("�����е�ĸ߶ȣ�����Up����ƫ��")]
        public float yOffset = 0.0f;

        [Tooltip("Y����ת��ɢ�������ȣ������� 90 ��ʾֻ�� 0/90/180/270 ��")]
        public float yawStep = 90f;

        [Header("Overlap ���")]
        public Vector3 checkBoxSize = new Vector3(0.5f, 0.5f, 0.5f);    // ��ߴ�
        public LayerMask blockerLayers = ~0;                             // ��ײ�赲�㣨���ʧ�ܼ����ɷ��ã�

        [Header("��ѡ�����ʱ�Զ����뵽���棨����Ҿ߽Ų���أ�")]
        public bool alignToGroundAfterPlace = true;
        public float groundProbeDistance = 3f;

        [Header("��ѡ��ʵ������ĸ��ڵ㣨Ϊ����ŵ���������")]
        public Transform optionalParentAtRuntime;
    }

    public Entry[] entries;

    public Entry Get(string itemId)
    {
        if (entries == null) return null;
        for (int i = 0; i < entries.Length; i++)
        {
            var e = entries[i];
            if (e != null && !string.IsNullOrEmpty(e.itemId) && e.itemId == itemId)
                return e;
        }
        return null;
    }
}
