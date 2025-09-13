using UnityEngine;

public enum BuildCategory { Furniture, Outdoor, Wall, Surface }
public enum BuildSnapMode { Ground, Wall, Surface }

[CreateAssetMenu(menuName = "Farm/Build Catalog", fileName = "BuildCatalogSO")]
public class BuildCatalogSO : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        [Header("Key")]
        public string itemId;                 // ƥ�� ItemSO.id

        [Header("Prefab")]
        public GameObject prefab;

        [Header("Rules")]
        public BuildCategory category = BuildCategory.Furniture;
        public BuildSnapMode snapMode = BuildSnapMode.Ground;

        [Tooltip("����ڷŵı����")]
        public LayerMask surfaceLayers = ~0;

        [Tooltip("��������ࣨGround/Surfaceʱ���ã�")]
        public float grid = 0.5f;

        [Tooltip("�����е�ĸ߶ȣ�����Up����ƫ��")]
        public float yOffset = 0.0f;

        [Tooltip("Y����ת��ɢ�������ȣ����� 90 ��ʾֻ�� 0/90/180/270")]
        public float yawStep = 90f;

        [Header("Overlap ���")]
        [Tooltip("��ߴ磻���Ϊ (0,0,0) �������Զ����������Զ��� Prefab �İ�Χ�гߴ�")]
        public Vector3 checkBoxSize = Vector3.zero;

        [Tooltip("�Ƿ��Զ��� Prefab �� Collider/Renderer �����Χ�а�ߴ�")]
        public bool autoBoundsFromPrefab = true;

        [Tooltip("�Զ���Χ�еķŴ�ϵ������������ʱ���У�")]
        public float boundsInflation = 0.02f;

        [Tooltip("�赲�㣻����������Щ�����ײ�弴�ж�Ϊ���ɰڷţ����Զ���������Ԥ������ң�")]
        public LayerMask blockerLayers = ~0;

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
