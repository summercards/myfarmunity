using System;
using System.Collections.Generic;
using FarmGame.Core.Contracts;
using UnityEngine;

/// <summary>
/// 运行时引用注册表。
/// 统一维护高频运行时对象，减少运行时全局查找 / Tag 查找 / 场景重绑。
/// </summary>
public static class RuntimeRefs
{
    private static readonly Dictionary<string, SpawnPoint> spawnPoints = new Dictionary<string, SpawnPoint>();
    private static readonly HashSet<CropPlant> cropPlants = new HashSet<CropPlant>();

    private static Transform playerTransform;

    public static TPSCharacter PlayerCharacter { get; private set; }
    public static PlayerInventoryHolder InventoryHolder { get; private set; }
    public static ActiveItemController ActiveItemController { get; private set; }
    public static PlayerStats PlayerStats { get; private set; }
    public static PlayerWallet PlayerWallet { get; private set; }
    public static InventoryBridge InventoryBridge { get; private set; }
    public static MiniShop MiniShopUI { get; private set; }
    public static SimpleShopUI SimpleShopUI { get; private set; }
    public static Transform PlayerTransform => playerTransform;

    public static NPCDialogUI DialogUI { get; private set; }
    public static NPCDialogWorldBridge DialogWorldBridge { get; private set; }
    public static IDialogUI DialogUIContract => DialogUI;
    public static IDialogWorldBridge DialogWorldBridgeContract => DialogWorldBridge;

    public static TPSOrbitCamera TpsOrbitCamera { get; private set; }
    public static FixedCameraSystem FixedCameraSystem { get; private set; }
    public static Camera TpsCamera => TpsOrbitCamera ? TpsOrbitCamera.GetComponent<Camera>() : null;
    public static Camera FixedCamera => FixedCameraSystem ? FixedCameraSystem.GetComponent<Camera>() : null;

    public static GameTimeSystem TimeSystem { get; private set; }
    public static ISaveService SaveService { get; private set; }
    public static IEnumerable<CropPlant> CropPlants => cropPlants;

    public static event Action<TPSCharacter> PlayerCharacterChanged;
    public static event Action<Transform> PlayerTransformChanged;
    public static event Action<PlayerInventoryHolder> InventoryHolderChanged;
    public static event Action<ActiveItemController> ActiveItemControllerChanged;
    public static event Action<PlayerStats> PlayerStatsChanged;
    public static event Action<PlayerWallet> PlayerWalletChanged;
    public static event Action<InventoryBridge> InventoryBridgeChanged;
    public static event Action<MiniShop> MiniShopUIChanged;
    public static event Action<SimpleShopUI> SimpleShopUIChanged;
    public static event Action<NPCDialogUI> DialogUIChanged;
    public static event Action<NPCDialogWorldBridge> DialogWorldBridgeChanged;
    public static event Action<IDialogUI> DialogUIContractChanged;
    public static event Action<IDialogWorldBridge> DialogWorldBridgeContractChanged;
    public static event Action<TPSOrbitCamera> TpsOrbitCameraChanged;
    public static event Action<FixedCameraSystem> FixedCameraSystemChanged;
    public static event Action<GameTimeSystem> TimeSystemChanged;
    public static event Action<ISaveService> SaveServiceChanged;
    public static event Action SpawnPointsChanged;
    public static event Action CropPlantsChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        PlayerCharacter = null;
        InventoryHolder = null;
        ActiveItemController = null;
        PlayerStats = null;
        PlayerWallet = null;
        InventoryBridge = null;
        MiniShopUI = null;
        SimpleShopUI = null;
        playerTransform = null;

        DialogUI = null;
        DialogWorldBridge = null;
        TpsOrbitCamera = null;
        FixedCameraSystem = null;
        TimeSystem = null;
        SaveService = null;

        spawnPoints.Clear();
        cropPlants.Clear();

        PlayerCharacterChanged = null;
        PlayerTransformChanged = null;
        InventoryHolderChanged = null;
        ActiveItemControllerChanged = null;
        PlayerStatsChanged = null;
        PlayerWalletChanged = null;
        InventoryBridgeChanged = null;
        MiniShopUIChanged = null;
        SimpleShopUIChanged = null;
        DialogUIChanged = null;
        DialogWorldBridgeChanged = null;
        DialogUIContractChanged = null;
        DialogWorldBridgeContractChanged = null;
        TpsOrbitCameraChanged = null;
        FixedCameraSystemChanged = null;
        TimeSystemChanged = null;
        SaveServiceChanged = null;
        SpawnPointsChanged = null;
        CropPlantsChanged = null;
    }

    public static void RegisterPlayerCharacter(TPSCharacter player)
    {
        if (PlayerCharacter == player)
        {
            return;
        }

        PlayerCharacter = player;
        PlayerCharacterChanged?.Invoke(PlayerCharacter);
        RefreshPlayerTransform();
    }

    public static void UnregisterPlayerCharacter(TPSCharacter player)
    {
        if (PlayerCharacter != player)
        {
            return;
        }

        PlayerCharacter = null;
        PlayerCharacterChanged?.Invoke(null);
        RefreshPlayerTransform();
    }

    public static void RegisterInventoryHolder(PlayerInventoryHolder holder)
    {
        if (InventoryHolder == holder)
        {
            return;
        }

        InventoryHolder = holder;
        InventoryHolderChanged?.Invoke(InventoryHolder);
        RefreshPlayerTransform();
    }

    public static void UnregisterInventoryHolder(PlayerInventoryHolder holder)
    {
        if (InventoryHolder != holder)
        {
            return;
        }

        InventoryHolder = null;
        InventoryHolderChanged?.Invoke(null);
        RefreshPlayerTransform();
    }

    public static void RegisterActiveItemController(ActiveItemController controller)
    {
        if (ActiveItemController == controller)
        {
            return;
        }

        ActiveItemController = controller;
        ActiveItemControllerChanged?.Invoke(ActiveItemController);
        RefreshPlayerTransform();
    }

    public static void UnregisterActiveItemController(ActiveItemController controller)
    {
        if (ActiveItemController != controller)
        {
            return;
        }

        ActiveItemController = null;
        ActiveItemControllerChanged?.Invoke(null);
        RefreshPlayerTransform();
    }

    public static void RegisterPlayerStats(PlayerStats stats)
    {
        if (PlayerStats == stats)
        {
            return;
        }

        PlayerStats = stats;
        PlayerStatsChanged?.Invoke(PlayerStats);
        RefreshPlayerTransform();
    }

    public static void UnregisterPlayerStats(PlayerStats stats)
    {
        if (PlayerStats != stats)
        {
            return;
        }

        PlayerStats = null;
        PlayerStatsChanged?.Invoke(null);
        RefreshPlayerTransform();
    }

    public static void RegisterPlayerWallet(PlayerWallet wallet)
    {
        if (PlayerWallet == wallet)
        {
            return;
        }

        PlayerWallet = wallet;
        PlayerWalletChanged?.Invoke(PlayerWallet);
        RefreshPlayerTransform();
    }

    public static void UnregisterPlayerWallet(PlayerWallet wallet)
    {
        if (PlayerWallet != wallet)
        {
            return;
        }

        PlayerWallet = null;
        PlayerWalletChanged?.Invoke(null);
        RefreshPlayerTransform();
    }

    public static void RegisterInventoryBridge(InventoryBridge bridge)
    {
        if (InventoryBridge == bridge)
        {
            return;
        }

        InventoryBridge = bridge;
        InventoryBridgeChanged?.Invoke(InventoryBridge);
    }

    public static void UnregisterInventoryBridge(InventoryBridge bridge)
    {
        if (InventoryBridge != bridge)
        {
            return;
        }

        InventoryBridge = null;
        InventoryBridgeChanged?.Invoke(null);
    }

    public static void RegisterMiniShopUI(MiniShop miniShop)
    {
        if (MiniShopUI == miniShop)
        {
            return;
        }

        MiniShopUI = miniShop;
        MiniShopUIChanged?.Invoke(MiniShopUI);
    }

    public static void UnregisterMiniShopUI(MiniShop miniShop)
    {
        if (MiniShopUI != miniShop)
        {
            return;
        }

        MiniShopUI = null;
        MiniShopUIChanged?.Invoke(null);
    }

    public static void RegisterSimpleShopUI(SimpleShopUI simpleShopUI)
    {
        if (SimpleShopUI == simpleShopUI)
        {
            return;
        }

        SimpleShopUI = simpleShopUI;
        SimpleShopUIChanged?.Invoke(SimpleShopUI);
    }

    public static void UnregisterSimpleShopUI(SimpleShopUI simpleShopUI)
    {
        if (SimpleShopUI != simpleShopUI)
        {
            return;
        }

        SimpleShopUI = null;
        SimpleShopUIChanged?.Invoke(null);
    }

    public static void RegisterDialogUI(NPCDialogUI ui)
    {
        if (DialogUI == ui)
        {
            return;
        }

        DialogUI = ui;
        DialogUIChanged?.Invoke(DialogUI);
        DialogUIContractChanged?.Invoke(DialogUI);
    }

    public static void UnregisterDialogUI(NPCDialogUI ui)
    {
        if (DialogUI != ui)
        {
            return;
        }

        DialogUI = null;
        DialogUIChanged?.Invoke(null);
        DialogUIContractChanged?.Invoke(null);
    }

    public static void RegisterDialogWorldBridge(NPCDialogWorldBridge bridge)
    {
        if (DialogWorldBridge == bridge)
        {
            return;
        }

        DialogWorldBridge = bridge;
        DialogWorldBridgeChanged?.Invoke(DialogWorldBridge);
        DialogWorldBridgeContractChanged?.Invoke(DialogWorldBridge);
    }

    public static void UnregisterDialogWorldBridge(NPCDialogWorldBridge bridge)
    {
        if (DialogWorldBridge != bridge)
        {
            return;
        }

        DialogWorldBridge = null;
        DialogWorldBridgeChanged?.Invoke(null);
        DialogWorldBridgeContractChanged?.Invoke(null);
    }

    public static void RegisterTpsOrbitCamera(TPSOrbitCamera camera)
    {
        if (TpsOrbitCamera == camera)
        {
            return;
        }

        TpsOrbitCamera = camera;
        TpsOrbitCameraChanged?.Invoke(TpsOrbitCamera);
    }

    public static void UnregisterTpsOrbitCamera(TPSOrbitCamera camera)
    {
        if (TpsOrbitCamera != camera)
        {
            return;
        }

        TpsOrbitCamera = null;
        TpsOrbitCameraChanged?.Invoke(null);
    }

    public static void RegisterFixedCameraSystem(FixedCameraSystem cameraSystem)
    {
        if (FixedCameraSystem == cameraSystem)
        {
            return;
        }

        FixedCameraSystem = cameraSystem;
        FixedCameraSystemChanged?.Invoke(FixedCameraSystem);
    }

    public static void UnregisterFixedCameraSystem(FixedCameraSystem cameraSystem)
    {
        if (FixedCameraSystem != cameraSystem)
        {
            return;
        }

        FixedCameraSystem = null;
        FixedCameraSystemChanged?.Invoke(null);
    }

    public static void RegisterTimeSystem(GameTimeSystem timeSystem)
    {
        if (TimeSystem == timeSystem || timeSystem == null)
        {
            return;
        }

        TimeSystem = timeSystem;
        TimeSystemChanged?.Invoke(TimeSystem);
    }

    public static void ClearTimeSystem(GameTimeSystem timeSystem)
    {
        if (TimeSystem != timeSystem)
        {
            return;
        }

        TimeSystem = null;
        TimeSystemChanged?.Invoke(null);
    }

    public static void RegisterSaveService(ISaveService saveService)
    {
        if (SaveService == saveService || saveService == null)
        {
            return;
        }

        SaveService = saveService;
        SaveServiceChanged?.Invoke(SaveService);
    }

    public static void UnregisterSaveService(ISaveService saveService)
    {
        if (SaveService != saveService)
        {
            return;
        }

        SaveService = null;
        SaveServiceChanged?.Invoke(null);
    }

    public static void RegisterCropPlant(CropPlant cropPlant)
    {
        if (cropPlant == null || !cropPlants.Add(cropPlant))
        {
            return;
        }

        CropPlantsChanged?.Invoke();
    }

    public static void UnregisterCropPlant(CropPlant cropPlant)
    {
        if (cropPlant == null || !cropPlants.Remove(cropPlant))
        {
            return;
        }

        CropPlantsChanged?.Invoke();
    }

    public static void RegisterSpawnPoint(SpawnPoint point)
    {
        if (point == null || string.IsNullOrWhiteSpace(point.spawnID))
        {
            return;
        }

        spawnPoints[point.spawnID] = point;
        SpawnPointsChanged?.Invoke();
    }

    public static void UnregisterSpawnPoint(SpawnPoint point)
    {
        if (point == null || string.IsNullOrWhiteSpace(point.spawnID))
        {
            return;
        }

        if (spawnPoints.TryGetValue(point.spawnID, out SpawnPoint current) && current == point)
        {
            spawnPoints.Remove(point.spawnID);
            SpawnPointsChanged?.Invoke();
        }
    }

    public static bool TryGetSpawnPoint(string spawnId, out SpawnPoint point)
    {
        if (string.IsNullOrWhiteSpace(spawnId))
        {
            point = null;
            return false;
        }

        if (spawnPoints.TryGetValue(spawnId, out point) && point != null)
        {
            return true;
        }

        point = null;
        return false;
    }

    public static string[] GetSpawnIds()
    {
        string[] ids = new string[spawnPoints.Count];
        int index = 0;
        foreach (KeyValuePair<string, SpawnPoint> pair in spawnPoints)
        {
            ids[index++] = pair.Key;
        }

        return ids;
    }

    private static void RefreshPlayerTransform()
    {
        Transform next = null;

        if (PlayerCharacter != null)
        {
            next = PlayerCharacter.transform;
        }
        else if (InventoryHolder != null)
        {
            next = InventoryHolder.transform;
        }
        else if (PlayerStats != null)
        {
            next = PlayerStats.transform;
        }
        else if (ActiveItemController != null)
        {
            next = ActiveItemController.transform;
        }

        if (playerTransform == next)
        {
            return;
        }

        playerTransform = next;
        PlayerTransformChanged?.Invoke(playerTransform);
    }
}
