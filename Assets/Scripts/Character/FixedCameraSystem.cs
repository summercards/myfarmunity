using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM && !UNITY_INPUT_SYSTEM_DISABLE
using UnityEngine.InputSystem;
#endif

public class FixedCameraSystem : MonoBehaviour
{
    public static FixedCameraSystem instance { get; private set; }

    [System.Serializable]
    public class FixedView
    {
        public string viewName;
        public float pitchAngle = 45f;      // 俯仰角度（45度为顶视角）
        public float yawAngle = 0f;          // 偏航角度
        public float distance = 10f;         // 距离目标
        public float followSpeed = 5f;       // 跟随速度
    }

    [Header("固定视角列表")]
    public FixedView[] fixedViews = new FixedView[4];  // 预设4个视角

    [Header("当前设置")]
    public int currentViewIndex = 0;
    public Transform target;

    [Header("相机偏移（相对于Player）- 在编辑器调整这个值")]
    public Vector3 cameraOffset = new Vector3(0, 8, -8);  // Y=高度，Z=后方距离

    [Header("旋转角度（编辑器调整或代码控制）")]
    public float pitchAngle = 45f;   // 俯视角度
    public float yawAngle = 0f;       // 偏航角度

    [Header("滚轮缩放")]
    public bool enableZoom = true;
    public float zoomSpeed = 2.0f;
    public float minZoomDistance = 4f;
    public float maxZoomDistance = 15f;
    public float currentZoomDistance = 8f;

    [Header("碰撞（可选）")]
    public bool enableCollision = true;
    public LayerMask collisionLayer = ~0;
    public float collisionRadius = 0.3f;

    private float currentYaw;
    private float currentPitch;
    private float currentDistance;

    // 缓存
    private Vector3 currentVelocity;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);  // 跨场景保持
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 场景加载后，重新查找Player
        Debug.Log($"[FixedCamera] 场景加载: {scene.name}，正在重新绑定Player...");
        TryFindPlayer();
    }

    void TryFindPlayer()
    {
        // 方法1：通过tag查找
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            target = player.transform;
            Debug.Log($"[FixedCamera] 成功绑定Player (by tag): {player.name}");
            return;
        }

        // 方法2：通过TPSCharacter组件查找
        TPSCharacter tps = FindObjectOfType<TPSCharacter>();
        if (tps != null)
        {
            target = tps.transform;
            Debug.Log($"[FixedCamera] 成功绑定Player (by TPSCharacter): {tps.name}");
            return;
        }

        // 方法3：通过名称查找
        player = GameObject.Find("Player");
        if (player != null)
        {
            target = player.transform;
            Debug.Log($"[FixedCamera] 成功绑定Player (by name): {player.name}");
            return;
        }

        Debug.LogWarning("[FixedCamera] 找不到Player，将在其他帧重试...");
        // 延迟重试
        StartCoroutine(RetryFindPlayer());
    }

    System.Collections.IEnumerator RetryFindPlayer()
    {
        yield return new WaitForSeconds(0.5f);
        if (target == null)
        {
            TryFindPlayer();
        }
    }

    void Start()
    {
        // 初始化预设视角
        InitializeDefaultViews();

        // 默认激活第一个视角（45度顶视角）
        if (fixedViews != null && fixedViews.Length > 0)
        {
            SetView(0);
        }

        // 尝试获取Player
        TryFindPlayer();
    }

    void InitializeDefaultViews()
    {
        // 直接创建新的数组实例，确保有4个元素
        fixedViews = new FixedView[4];

        // 预设视角配置，类似动物之森
        fixedViews[0] = new FixedView
        {
            viewName = "45度顶视角",
            pitchAngle = 45f,
            yawAngle = 0f,  // 恢复原样
            distance = 15f,
            followSpeed = 5f
        };

        fixedViews[1] = new FixedView
        {
            viewName = "俯视视角",
            pitchAngle = 60f,
            yawAngle = 0f,  // 恢复原样
            distance = 12f,
            followSpeed = 5f
        };

        fixedViews[2] = new FixedView
        {
            viewName = "侧面视角",
            pitchAngle = 30f,
            yawAngle = 90f,
            distance = 12f,
            followSpeed = 5f
        };

        fixedViews[3] = new FixedView
        {
            viewName = "对角视角",
            pitchAngle = 35f,
            yawAngle = 45f,
            distance = 14f,
            followSpeed = 5f
        };
    }

    public void SetView(int index)
    {
        if (fixedViews == null || index < 0 || index >= fixedViews.Length) return;

        currentViewIndex = index;
        var view = fixedViews[index];

        // 设置目标角度
        currentYaw = view.yawAngle;
        currentPitch = view.pitchAngle;
        currentDistance = view.distance;

        Debug.Log($"[FixedCamera] 切换到视角: {view.viewName}");
    }

    public void SetView(string viewName)
    {
        if (fixedViews == null) return;

        for (int i = 0; i < fixedViews.Length; i++)
        {
            if (fixedViews[i].viewName == viewName)
            {
                SetView(i);
                return;
            }
        }

        Debug.LogWarning($"[FixedCamera] 未找到视角: {viewName}");
    }

    public FixedView GetCurrentView()
    {
        if (fixedViews != null && currentViewIndex >= 0 && currentViewIndex < fixedViews.Length)
            return fixedViews[currentViewIndex];
        return null;
    }

    void LateUpdate()
    {
        if (target == null)
        {
            Debug.LogWarning("[FixedCamera] Target is null, trying to find Player...");
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
            return;
        }

        if (fixedViews == null || fixedViews.Length == 0) return;

        var view = fixedViews[currentViewIndex];

        // ===== 处理滚轮缩放 =====
        if (enableZoom)
        {
            float scroll = 0f;
#if ENABLE_INPUT_SYSTEM && !UNITY_INPUT_SYSTEM_DISABLE
            if (Mouse.current != null)
                scroll = Mouse.current.scroll.ReadValue().y;
#else
            scroll = Input.mouseScrollDelta.y;
#endif

            if (Mathf.Abs(scroll) > 0.01f)
            {
                // 滚轮滚动调整缩放距离
                currentZoomDistance -= scroll * 0.01f * zoomSpeed;
                currentZoomDistance = Mathf.Clamp(currentZoomDistance, minZoomDistance, maxZoomDistance);
            }
        }

        // ===== 计算基于缩放距离的视角 =====
        // 缩放比例 (0 = 最近, 1 = 最远)
        float zoomRatio = (currentZoomDistance - minZoomDistance) / (maxZoomDistance - minZoomDistance);

        // 拉近时视角抬高（pitch增加），拉远时视角降低
        float pitchRange = 20f;
        float dynamicPitch = pitchAngle + (1f - zoomRatio) * pitchRange;

        // 使用cameraOffset作为基础偏移
        // cameraOffset.x = 左右偏移（正=右）
        // cameraOffset.y = 高度（正=上）
        // cameraOffset.z = 前后偏移（正=玩家前方，负=玩家后方）
        Vector3 offset = new Vector3(cameraOffset.x, cameraOffset.y, cameraOffset.z);

        // 滚轮缩放：调整前后距离（只在Z轴上缩放）
        offset.z *= (0.5f + zoomRatio * 0.5f);  // 缩放范围0.5~1.0倍
        // 拉近时高度微降，拉远时高度微升
        offset.y *= (0.8f + zoomRatio * 0.2f);

        // 相机只跟随Player位置，不跟随Player旋转
        Vector3 targetPos = target.position;
        Vector3 currentPos = transform.position;
        Vector3 desiredPos = targetPos + offset;

        // 平滑移动到目标位置
        transform.position = Vector3.SmoothDamp(currentPos, desiredPos, ref currentVelocity, 1f / view.followSpeed);

        // 相机保持固定角度（不随Player旋转）
        Quaternion targetRot = Quaternion.Euler(dynamicPitch, yawAngle, 0);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * view.followSpeed);
    }

    // 切换到下一个视角
    public void NextView()
    {
        if (fixedViews == null || fixedViews.Length == 0) return;
        SetView((currentViewIndex + 1) % fixedViews.Length);
    }

    // 切换到上一个视角
    public void PreviousView()
    {
        if (fixedViews == null || fixedViews.Length == 0) return;
        int newIndex = currentViewIndex - 1;
        if (newIndex < 0) newIndex = fixedViews.Length - 1;
        SetView(newIndex);
    }
}
#if ENABLE_INPUT_SYSTEM && !UNITY_INPUT_SYSTEM_DISABLE
#endif
