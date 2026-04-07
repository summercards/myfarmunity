# 我的农场 v1.2 Implementation Document

## Version Summary

完成主角私人卧室的完整制作，包括美术、传送、DIY系统和灵之空间入口功能

---

## Version Tasks

> 注：本版本的任务将由系统自动创建，基于以下阶段拆分

---

## Stages

### 设计与拆解

- **Status**: todo
- **Summary**: 拆分 v1.2 的模块、接口与开发顺序

#### 数据模型设计

**1. 卧室数据模型**

创建 `Assets/Scripts/BedroomSystem/BedroomData.cs`：
```csharp
[System.Serializable]
public class BedroomData
{
    public List<FurniturePlacement> furniturePlacements = new();
    public Vector3 playerSpawnPosition;
    public Quaternion playerSpawnRotation;
}

[System.Serializable]
public class FurniturePlacement
{
    public string furnitureId;  // 家具SO的ID
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
}
```

**2. 家具SO模型**

创建 `Assets/Scripts/BedroomSystem/FurnitureSO.cs`：
```csharp
[CreateAssetMenu(fileName = "NewFurniture", menuName = "MyFarm/Furniture")]
public class FurnitureSO : ScriptableObject
{
    public string furnitureId;
    public string displayName;
    public GameObject prefab;
    public bool canPlaceOnFloor = true;
    public bool canPlaceOnWall = false;
    public Vector2Int gridSize = new(1, 1);  // 占用网格大小
}
```

**3. 传送点数据模型**

创建 `Assets/Scripts/BedroomSystem/TeleportPoint.cs`：
```csharp
public class TeleportPoint : MonoBehaviour
{
    public string targetScene;
    public string targetTeleportId;
    public Vector3 spawnOffset;
    public bool useFadeTransition = true;
    public float fadeDuration = 1f;

    public void TeleportPlayer()
    {
        // 传送逻辑
    }
}
```

#### 技术方案设计

**1. 场景管理方案**

- 卧室场景路径：`Assets/Scenes/Bedroom.unity`
- 场景加载方式：`SceneManager.LoadSceneAsync()`
- 场景管理器：`SceneTransitionManager.cs`

**2. DIY系统方案**

- 家具放置系统：`FurniturePlacementManager.cs`
- 交互系统：`BedroomInteractionManager.cs`
- 数据持久化：`BedroomDataManager.cs`

**3. 传送系统方案**

- 传送动画：使用 `ScreenFadeEffect.cs`
- 传送管理器：`TeleportManager.cs`
- 状态保存：使用 `PlayerPrefs` 或 JSON

#### 开发顺序建议

```
1. 数据模型 → 2. 白模场景 → 3. 传送系统 → 4. DIY系统 → 5. 灵之空间入口 → 6. 素材替换 → 7. 细节打磨
```

---

### 卧室场景制作 开发

- **Status**: todo
- **Summary**: 完成主角私人卧室的场景白膜制作、DIY系统和传送功能

#### 任务1：创建白模场景

**文件路径**：
- 场景文件：`Assets/Scenes/Bedroom.unity`
- 场景数据：`Assets/Scenes/Bedroom.unity.meta`

**实现步骤**：
1. 在Unity中创建新场景 `Bedroom.unity`
2. 创建房间基础结构（立方体）
3. 放置床、窗户、门、家具位（立方体占位）
4. 设置玩家出生点
5. 设置传送出口点
6. 添加基础光照（Directional Light）
7. 保存场景

**验收标准**：
- [ ] 卧室场景可以正常加载
- [ ] 房间布局合理（大小、位置）
- [ ] 家具位位置明确
- [ ] 玩家出生点设置正确
- [ ] 传送出口点设置正确

---

#### 任务2：实现传送系统

**文件路径**：
- `Assets/Scripts/BedroomSystem/SceneTransitionManager.cs`
- `Assets/Scripts/BedroomSystem/ScreenFadeEffect.cs`
- `Assets/Scripts/BedroomSystem/TeleportPoint.cs`

**实现步骤**：

**2.1 创建 ScreenFadeEffect.cs**
```csharp
using UnityEngine;
using UnityEngine.UI;

public class ScreenFadeEffect : MonoBehaviour
{
    public static ScreenFadeEffect Instance;

    [SerializeField] private Image fadeImage;
    [SerializeField] private float defaultFadeDuration = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public async System.Threading.Tasks.Task FadeIn(float duration = 0)
    {
        duration = duration <= 0 ? defaultFadeDuration : duration;
        float alpha = 1f;

        while (alpha > 0)
        {
            alpha -= Time.deltaTime / duration;
            fadeImage.color = new Color(0, 0, 0, alpha);
            await System.Threading.Tasks.Task.Yield();
        }

        fadeImage.color = new Color(0, 0, 0, 0);
    }

    public async System.Threading.Tasks.Task FadeOut(float duration = 0)
    {
        duration = duration <= 0 ? defaultFadeDuration : duration;
        float alpha = 0f;

        while (alpha < 1)
        {
            alpha += Time.deltaTime / duration;
            fadeImage.color = new Color(0, 0, 0, alpha);
            await System.Threading.Tasks.Task.Yield();
        }

        fadeImage.color = new Color(0, 0, 0, 1);
    }
}
```

**2.2 创建 TeleportPoint.cs**
```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

public class TeleportPoint : MonoBehaviour
{
    [SerializeField] private string targetScene;
    [SerializeField] private string targetTeleportId;
    [SerializeField] private Vector3 spawnOffset = Vector3.zero;
    [SerializeField] private bool useFadeTransition = true;
    [SerializeField] private float fadeDuration = 1f;

    private bool isTeleporting = false;

    public async void TeleportPlayer()
    {
        if (isTeleporting) return;
        isTeleporting = true;

        if (useFadeTransition)
        {
            await ScreenFadeEffect.Instance.FadeOut(fadeDuration);
        }

        // 保存当前场景和位置（用于返回）
        PlayerPrefs.SetString("LastScene", SceneManager.GetActiveScene().name);
        PlayerPrefs.SetString("LastTeleportId", gameObject.name);

        // 加载目标场景
        await SceneManager.LoadSceneAsync(targetScene);

        // 查找目标传送点
        var targetPoints = FindObjectsOfType<TeleportPoint>();
        var targetPoint = System.Array.Find(targetPoints, tp => tp.gameObject.name == targetTeleportId);

        if (targetPoint != null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                player.transform.position = targetPoint.transform.position + spawnOffset;
                player.transform.rotation = targetPoint.transform.rotation;
            }
        }

        if (useFadeTransition)
        {
            await ScreenFadeEffect.Instance.FadeIn(fadeDuration);
        }

        isTeleporting = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            TeleportPlayer();
        }
    }
}
```

**验收标准**：
- [ ] 云层电梯→卧室传送正常
- [ ] 卧室→云层电梯传送正常
- [ ] 淡入淡出动画流畅
- [ ] 玩家在正确的位置出生
- [ ] 传送状态正确保存

---

#### 任务3：实现DIY系统

**文件路径**：
- `Assets/Scripts/BedroomSystem/FurnitureSO.cs`
- `Assets/Scripts/BedroomSystem/FurniturePlacementManager.cs`
- `Assets/Scripts/BedroomSystem/BedroomInteractionManager.cs`
- `Assets/Scripts/BedroomSystem/BedroomDataManager.cs`

**实现步骤**：

**3.1 创建 FurnitureSO.cs**（见设计部分）

**3.2 创建 FurniturePlacementManager.cs**
```csharp
using UnityEngine;
using System.Collections.Generic;

public class FurniturePlacementManager : MonoBehaviour
{
    public static FurniturePlacementManager Instance;

    [SerializeField] private Layer placementLayer;
    [SerializeField] private GameObject placementPreview;
    private GameObject currentPreview;
    private FurnitureSO selectedFurniture;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartPlacement(FurnitureSO furniture)
    {
        selectedFurniture = furniture;
        if (currentPreview != null)
        {
            Destroy(currentPreview);
        }
        currentPreview = Instantiate(furniture.prefab);
        currentPreview.GetComponent<Collider>().enabled = false;
    }

    public void CancelPlacement()
    {
        if (currentPreview != null)
        {
            Destroy(currentPreview);
            currentPreview = null;
        }
        selectedFurniture = null;
    }

    public void ConfirmPlacement()
    {
        if (currentPreview == null || selectedFurniture == null) return;

        GameObject placedFurniture = Instantiate(selectedFurniture.prefab, currentPreview.transform.position, currentPreview.transform.rotation);
        placedFurniture.AddComponent<PlacedFurniture>().Initialize(selectedFurniture);

        Destroy(currentPreview);
        currentPreview = null;
        selectedFurniture = null;

        // 保存数据
        BedroomDataManager.Instance.SaveFurniturePlacement(selectedFurniture.furnitureId, placedFurniture.transform.position, placedFurniture.transform.rotation, placedFurniture.transform.localScale);
    }

    private void Update()
    {
        if (currentPreview != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, placementLayer))
            {
                currentPreview.transform.position = hit.point;
            }
        }
    }
}
```

**3.3 创建 BedroomDataManager.cs**
```csharp
using UnityEngine;
using System.Collections.Generic;

public class BedroomDataManager : MonoBehaviour
{
    public static BedroomDataManager Instance;

    private BedroomData bedroomData;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        LoadBedroomData();
    }

    public void SaveBedroomData()
    {
        string json = JsonUtility.ToJson(bedroomData, true);
        PlayerPrefs.SetString("BedroomData", json);
        PlayerPrefs.Save();
    }

    public void LoadBedroomData()
    {
        string json = PlayerPrefs.GetString("BedroomData", "");
        if (!string.IsNullOrEmpty(json))
        {
            bedroomData = JsonUtility.FromJson<BedroomData>(json);
            ApplyBedroomData();
        }
        else
        {
            bedroomData = new BedroomData();
        }
    }

    public void SaveFurniturePlacement(string furnitureId, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        FurniturePlacement placement = new FurniturePlacement
        {
            furnitureId = furnitureId,
            position = position,
            rotation = rotation,
            scale = scale
        };

        bedroomData.furniturePlacements.Add(placement);
        SaveBedroomData();
    }

    private void ApplyBedroomData()
    {
        // 根据保存的数据重新放置家具
        foreach (var placement in bedroomData.furniturePlacements)
        {
            // 加载家具并放置
        }
    }
}
```

**验收标准**：
- [ ] 家具可以正确放置
- [ ] 家具可以正确移动
- [ ] 家具可以正确删除
- [ ] DIY数据可以正确保存
- [ ] DIY数据可以正确加载
- [ ] 重新加载后数据正确恢复

---

#### 任务4：实现灵之空间入口

**文件路径**：
- `Assets/Scripts/BedroomSystem/SpiritSpaceEntrance.cs`
- `Assets/Scripts/BedroomSystem/SleepAnimationController.cs`

**实现步骤**：

**4.1 创建 SpiritSpaceEntrance.cs**
```csharp
using UnityEngine;

public class SpiritSpaceEntrance : MonoBehaviour
{
    [SerializeField] private float sleepDelay = 2f;

    private bool isSleeping = false;

    public async void EnterSpiritSpace()
    {
        if (isSleeping) return;
        isSleeping = true;

        // 播放睡觉动画
        var animController = GetComponent<SleepAnimationController>();
        if (animController != null)
        {
            await animController.PlaySleepAnimation();
        }

        // 等待一段时间
        await System.Threading.Tasks.Task.Delay((int)(sleepDelay * 1000));

        // 传送到灵之空间
        await ScreenFadeEffect.Instance.FadeOut(1f);
        UnityEngine.SceneManagement.SceneManager.LoadScene("SpiritSpace");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 显示"按E键睡觉"提示
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && Input.GetKeyDown(KeyCode.E))
        {
            EnterSpiritSpace();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 隐藏提示
        }
    }
}
```

**4.2 创建 SleepAnimationController.cs**
```csharp
using UnityEngine;
using System.Threading.Tasks;

public class SleepAnimationController : MonoBehaviour
{
    [SerializeField] private Animator animator;

    public async Task PlaySleepAnimation()
    {
        if (animator == null) return;

        animator.SetBool("IsSleeping", true);

        // 等待动画播放完成
        await Task.Delay(2000);

        // 关闭屏幕
        await ScreenFadeEffect.Instance.FadeOut(1f);

        animator.SetBool("IsSleeping", false);
    }
}
```

**验收标准**：
- [ ] 主角躺在床上可以触发睡觉
- [ ] 睡觉动画播放正常
- [ ] 睡觉后正确传送到灵之空间
- [ ] 传送动画流畅

---

#### 任务5：素材替换

**文件路径**：
- 家具模型：`Assets/Models/Furniture/`
- 装饰物品模型：`Assets/Models/Decorations/`
- 材质：`Assets/Materials/`
- 纹理：`Assets/Textures/`

**实现步骤**：
1. 导入家具模型（床、衣柜、桌子、椅子等）
2. 导入装饰物品模型（画、地毯、植物等）
3. 创建材质和纹理
4. 在场景中替换白模为实际模型
5. 调整模型位置、旋转、缩放
6. 调整光照和阴影

**验收标准**：
- [ ] 所有白模替换为实际模型
- [ ] 模型位置、旋转、缩放正确
- [ ] 材质和纹理正确应用
- [ ] 光照和阴影效果良好

---

#### 任务6：细节打磨

**文件路径**：
- 音效：`Assets/Audio/Bedroom/`
- 性能优化脚本：`Assets/Scripts/BedroomSystem/`

**实现步骤**：
1. 添加音效（走路声、床声、放置家具声）
2. 优化光照（减少动态阴影、使用光照贴图）
3. 添加LOD（距离细节层次）
4. 添加遮挡剔除
5. 优化摄像机视角
6. 添加粒子效果（如必要）

**验收标准**：
- [ ] 音效正常播放
- [ ] 场景加载时间 < 2秒
- [ ] 帧率稳定（>30fps）
- [ ] 内存占用合理

---

### 联调与发布

- **Status**: todo
- **Summary**: 完成 v1.2 的联调、回归与发布收口

#### 联调清单

- [ ] 传送系统与云层场景联调
- [ ] DIY系统与游戏主循环联调
- [ ] 灵之空间入口与灵之空间场景联调
- [ ] 数据持久化与加载流程联调

#### 回归测试

- [ ] 云层功能不受影响
- [ ] NPC对话系统不受影响
- [ ] 商店功能不受影响
- [ ] 玩家移动不受影响

#### 发布准备

- [ ] 代码审查通过
- [ ] 文档整理完成
- [ ] 测试报告无严重bug
- [ ] CHANGELOG更新
- [ ] 版本打标签

---

## 附录

### 文件结构

```
Assets/
├── Scenes/
│   └── Bedroom.unity                    # 卧室场景
├── Scripts/
│   └── BedroomSystem/
│       ├── BedroomData.cs              # 卧室数据模型
│       ├── FurnitureSO.cs              # 家具SO
│       ├── FurniturePlacementManager.cs  # 家具放置管理器
│       ├── BedroomInteractionManager.cs # 卧室交互管理器
│       ├── BedroomDataManager.cs       # 卧室数据管理器
│       ├── SceneTransitionManager.cs    # 场景过渡管理器
│       ├── ScreenFadeEffect.cs          # 屏幕淡入淡出效果
│       ├── TeleportPoint.cs             # 传送点
│       ├── SpiritSpaceEntrance.cs       # 灵之空间入口
│       └── SleepAnimationController.cs  # 睡觉动画控制器
├── Models/
│   ├── Furniture/                       # 家具模型
│   └── Decorations/                     # 装饰物品模型
├── Materials/                           # 材质
├── Textures/                            # 纹理
└── Audio/
    └── Bedroom/                         # 卧室音效
```

### 参考资源

**Unity文档**：
- [Scene Management](https://docs.unity3d.com/Manual/SceneManagement.html)
- [ScriptableObject](https://docs.unity3d.com/ScriptReference/ScriptableObject.html)
- [Coroutines](https://docs.unity3d.com/Manual/Coroutines.html)

**最佳实践**：
- 使用ScriptableObject管理家具数据
- 使用JSON进行数据持久化
- 使用异步加载场景
- 使用协程管理异步操作
