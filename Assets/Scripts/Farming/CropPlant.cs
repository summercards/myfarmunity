using UnityEngine;

[DisallowMultipleComponent]
public class CropPlant : MonoBehaviour
{
    [Header("Runtime (ReadOnly)")] public bool isMature = false;

    // 配置来源
    SeedPlantDataSO.Entry _cfg;

    // 生长计时
    float _timer = 0f;              // 当前阶段累计
    int _stageIndex = 0;          // 当前阶段索引
    float _elapsedTotal = 0f;       // 所有阶段累计（前面阶段之和 + 当前阶段累计）
    float _totalGrowth = 0f;        // 总生长时长
    float[] _stageDur;              // 每阶段时长（已处理默认值）

    // 阶段可视
    GameObject[] _stageVisuals;

    [Header("无阶段模型时的缩放兜底")]
    public bool enableScaleTween = true;
    public Vector3 startScale = Vector3.one * 0.45f;
    public Vector3 endScale = Vector3.one * 1.00f;
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("交互 / 收获")]
    public KeyCode interactKey = KeyCode.E;
    public float interactRadius = 1.6f;
    PlayerInventoryHolder _player;

    // ---------------- 极简 3D 进度条：两个 Quad ----------------
    [Header("3D Progress Bar (Quad)")]
    public bool showBar = true;
    public Vector3 barOffset = new Vector3(0, 1.1f, 0);
    public float barWidth = 0.8f;
    public float barHeight = 0.12f;
    public Color barBgColor = new Color(0f, 0f, 0f, 0.6f);
    public Color barFillColor = new Color(0.2f, 0.9f, 0.2f, 0.95f);
    Transform _barRoot; Transform _bgQuad; Transform _fillQuad;
    static Material _matBG, _matFill;

    static Material MakeMat(Color c)
    {
        Shader sh = Shader.Find("Unlit/Color");
        if (!sh) sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (!sh) sh = Shader.Find("Standard");
        var m = new Material(sh);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        m.renderQueue = 3000;
        return m;
    }

    void BuildBar()
    {
        if (!showBar) return;
        if (_matBG == null) _matBG = MakeMat(barBgColor);
        if (_matFill == null) _matFill = MakeMat(barFillColor);

        _barRoot = new GameObject("Progress3D").transform;
        _barRoot.SetParent(transform, false);
        _barRoot.localPosition = barOffset;

        _bgQuad = GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
        _bgQuad.name = "BG"; _bgQuad.SetParent(_barRoot, false);
        _bgQuad.localScale = new Vector3(barWidth, barHeight, 1);
        var bgmr = _bgQuad.GetComponent<MeshRenderer>(); bgmr.sharedMaterial = _matBG;
        Destroy(_bgQuad.GetComponent<Collider>());

        _fillQuad = GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
        _fillQuad.name = "Fill"; _fillQuad.SetParent(_barRoot, false);
        _fillQuad.localPosition = new Vector3(-barWidth * 0.5f, 0, -0.001f);
        _fillQuad.localScale = new Vector3(0f, barHeight, 1);
        var fmr = _fillQuad.GetComponent<MeshRenderer>(); fmr.sharedMaterial = _matFill;
        Destroy(_fillQuad.GetComponent<Collider>());
    }

    void UpdateBar(float p)
    {
        if (!showBar || _fillQuad == null) return;
        p = Mathf.Clamp01(p);
        float x = barWidth * p;
        _fillQuad.localScale = new Vector3(x, barHeight, 1);
        _fillQuad.localPosition = new Vector3(-barWidth * 0.5f + x * 0.5f, 0, -0.001f);
    }

    void FaceCamera()
    {
        if (_barRoot == null) return;
        var cam = Camera.main; if (!cam) return;
        _barRoot.rotation = Quaternion.LookRotation(_barRoot.position - cam.transform.position);
    }

    // ============ 生命周期 ============
    public void Init(SeedPlantDataSO.Entry cfg)
    {
        _cfg = cfg;
        BuildStageDurations();     // 把0时长替换为默认值
        SetupStageVisuals();
        BuildBar();
        ApplyStage(0);
        _player = FindObjectOfType<PlayerInventoryHolder>();
        UpdateBar(0f);
    }

    void Update()
    {
        // 生长推进
        if (!isMature && _stageDur.Length > 0)
        {
            _timer += Time.deltaTime;

            float curDur = Mathf.Max(0.0001f, _stageDur[_stageIndex]);
            float totalP = (_elapsedTotal + _timer) / Mathf.Max(0.0001f, _totalGrowth);

            ApplyScaleTween(totalP);
            UpdateBar(totalP);

            if (_timer >= curDur)
            {
                _elapsedTotal += _timer;
                _timer = 0f;
                _stageIndex++;
                if (_stageIndex >= _stageDur.Length) BecomeMature();
                else ApplyStage(_stageIndex);
            }
        }

        FaceCamera();

        // 成熟后：距离交互
        if (isMature && _player)
        {
            if (Vector3.Distance(_player.transform.position, transform.position) <= interactRadius
                && Input.GetKeyDown(interactKey))
            {
                HarvestTo(_player);
            }
        }
    }

    // 阶段可视
    void ApplyStage(int index)
    {
        if (_stageVisuals == null) return;
        for (int i = 0; i < _stageVisuals.Length; i++)
            if (_stageVisuals[i]) _stageVisuals[i].SetActive(i == index);
    }

    void SetupStageVisuals()
    {
        int n = (_cfg?.stages != null) ? _cfg.stages.Length : 0;
        _stageVisuals = new GameObject[n];
        bool any = false;
        for (int i = 0; i < n; i++)
        {
            var v = _cfg.stages[i].visual;
            if (v)
            {
                var inst = Instantiate(v, transform);
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = Quaternion.identity;
                _stageVisuals[i] = inst;
                any = true;
            }
        }
        if (enableScaleTween && !any) transform.localScale = startScale;
    }

    void ApplyScaleTween(float t01)
    {
        if (!enableScaleTween) return;
        float k = Mathf.Clamp01(t01);
        float w = (scaleCurve != null) ? scaleCurve.Evaluate(k) : k;
        transform.localScale = Vector3.LerpUnclamped(startScale, endScale, w);
    }

    void BecomeMature()
    {
        isMature = true;
        _timer = 0f;
        _stageIndex = Mathf.Max(0, _stageDur.Length - 1);
        _elapsedTotal = _totalGrowth;
        ApplyStage(_stageIndex);
        UpdateBar(1f);
    }

    // 收获（掉落或进背包，按你之前的配置）
    public void HarvestTo(PlayerInventoryHolder holder)
    {
        if (!isMature || _cfg == null) return;

        string dropId = string.IsNullOrEmpty(_cfg.produceId) ? _cfg.plantItemId : _cfg.produceId;
        int amount = Mathf.Clamp(Random.Range(_cfg.produceMin, _cfg.produceMax + 1), 0, 999);

        if (_cfg.produceWorldPrefab && amount > 0)
        {
            float r = 0.35f; Vector2 v = new Vector2(1.2f, 2.2f);
            for (int i = 0; i < amount; i++)
            {
                var pos = transform.position + Vector3.up * 0.4f
                        + new Vector3(Random.Range(-r, r), 0, Random.Range(-r, r));
                var go = Instantiate(_cfg.produceWorldPrefab, pos, Quaternion.identity);
                var rb = go.GetComponent<Rigidbody>();
                if (rb)
                {
                    var dir = new Vector3(Random.Range(-1f, 1f), 1f, Random.Range(-1f, 1f)).normalized;
                    rb.AddForce(dir * Random.Range(v.x, v.y), ForceMode.VelocityChange);
                }
                // go.GetComponent<ItemWorld>()?.Set(dropId, 1);
            }
        }
        else if (holder && !string.IsNullOrEmpty(dropId) && amount > 0)
        {
            holder.AddItem(dropId, amount);
        }
        Destroy(gameObject);
    }

    // 阶段时长（把0替换为默认值），并计算总时长
    void BuildStageDurations()
    {
        int n = (_cfg?.stages != null) ? _cfg.stages.Length : 0;
        float def = Mathf.Max(0.5f, (_cfg != null && _cfg.defaultStageDuration > 0)
                                       ? _cfg.defaultStageDuration : 4f);

        if (n <= 0)
        {
            _stageDur = new float[] { def };
        }
        else
        {
            _stageDur = new float[n];
            for (int i = 0; i < n; i++)
            {
                float d = (_cfg.stages[i] != null && _cfg.stages[i].duration > 0f)
                        ? _cfg.stages[i].duration : def;
                _stageDur[i] = Mathf.Max(0.01f, d);
            }
        }
        _totalGrowth = 0f;
        foreach (var d in _stageDur) _totalGrowth += d;
        if (_totalGrowth <= 0f) _totalGrowth = def;
    }

    void OnDestroy()
    {
        if (_barRoot) Destroy(_barRoot.gameObject);
    }

    // =========== ① 新增：可序列化的生长存档结构 ===========
    [System.Serializable]
    public struct GrowthState
    {
        public int stageIndex;     // 当前阶段
        public float stageTimer;   // 当前阶段内已累计的秒数
        public bool mature;        // 是否已成熟
    }

    // =========== ② 新增：导出当前生长状态 ===========
    public GrowthState GetSaveState()
    {
        int si = Mathf.Clamp(_stageIndex, 0, Mathf.Max(0, _stageDur.Length - 1));
        float curDur = (_stageDur != null && _stageDur.Length > 0) ? _stageDur[si] : 0f;
        float st = Mathf.Clamp(_timer, 0f, Mathf.Max(0.0001f, curDur));
        return new GrowthState { stageIndex = si, stageTimer = st, mature = isMature };
    }

    // =========== ③ 新增：应用生长状态（加载时调用） ===========
    public void ApplySaveState(GrowthState s)
    {
        // 确保时长表已就绪
        if (_stageDur == null || _stageDur.Length == 0)
        {
            BuildStageDurations();
            if (_stageDur == null || _stageDur.Length == 0) return;
        }

        if (s.mature || s.stageIndex >= _stageDur.Length)
        {
            // 直接成熟
            _elapsedTotal = _totalGrowth;
            _stageIndex = Mathf.Max(0, _stageDur.Length - 1);
            _timer = 0f;
            BecomeMature();
            return;
        }

        _stageIndex = Mathf.Clamp(s.stageIndex, 0, _stageDur.Length - 1);
        float curDur = Mathf.Max(0.0001f, _stageDur[_stageIndex]);
        _timer = Mathf.Clamp(s.stageTimer, 0f, curDur - 0.0001f);
        isMature = false;

        // 重新计算累计
        _elapsedTotal = 0f;
        for (int i = 0; i < _stageIndex; i++) _elapsedTotal += _stageDur[i];

        // 套用可视与进度
        ApplyStage(_stageIndex);
        float totalP = (_elapsedTotal + _timer) / Mathf.Max(0.0001f, _totalGrowth);
        ApplyScaleTween(totalP);
        UpdateBar(totalP);
    }
}
