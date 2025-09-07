using UnityEngine;

[DisallowMultipleComponent]
public class CropPlant : MonoBehaviour
{
    [Header("Runtime (ReadOnly)")]
    public bool isMature = false;

    // 配置
    SeedPlantDataSO.Entry _cfg;

    // 生长计时
    float _timer = 0f;            // 当前阶段累计
    int _stageIndex = 0;        // 当前阶段索引
    float _elapsedTotal = 0f;     // 所有阶段累计
    float _totalGrowth = 0f;      // 总生长时长
    float[] _stageDur;            // 每阶段时长（已处理默认值）

    // 阶段可视
    GameObject[] _stageVisuals;

    // —— 初始化标记（防止 Update 抢跑）——
    bool _inited = false;

    [Header("无阶段模型时的缩放兜底")]
    public bool enableScaleTween = true;
    public Vector3 startScale = Vector3.one * 0.45f;
    public Vector3 endScale = Vector3.one * 1.00f;
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("交互 / 收获")]
    public KeyCode interactKey = KeyCode.E;
    public float interactRadius = 1.6f;
    PlayerInventoryHolder _player;

    // —— 3D 进度条（两个 Quad） ——
    [Header("3D Progress Bar (Quad)")]
    public bool showBar = true;
    public Vector3 barOffset = new Vector3(0, 1.1f, 0);
    public float barWidth = 0.8f;
    public float barHeight = 0.12f;
    public Color barBgColor = new Color(0f, 0f, 0f, 0.6f);
    public Color barFillColor = new Color(0.2f, 0.9f, 0.2f, 0.95f);
    Transform _barRoot; Transform _bgQuad; Transform _fillQuad;
    static Material _matBG, _matFill;

    // —— 树：生产改为“入库存”，收获一次性清空库存 —— 
    float _produceTimer = 0f;   // 距离下次入库计时
    int _storedYield = 0;    // 已入库的可收获数量

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


    // 强制打开/关闭某个阶段下所有渲染器，防止被误关导致“看不见”
    void ForceEnableRenderers(GameObject go, bool enable)
    {
        if (!go) return;
        var rends = go.GetComponentsInChildren<Renderer>(true);
        foreach (var r in rends) r.enabled = enable;
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

    // ===== 生命周期 =====
    public void Init(SeedPlantDataSO.Entry cfg)
    {
        _cfg = cfg;
        BuildStageDurations();
        SetupStageVisuals();
        BuildBar();
        ApplyStage(0);
        _player = FindObjectOfType<PlayerInventoryHolder>();
        UpdateBar(0f);
        _inited = true;
    }

    void Update()
    {
        // 防守
        if (!_inited)
        {
            if (_cfg != null && (_stageDur == null || _stageDur.Length == 0))
            {
                BuildStageDurations();
                SetupStageVisuals();
                BuildBar();
                ApplyStage(0);
                UpdateBar(0f);
                _inited = true;
            }
            else return;
        }
        if (_stageDur == null || _stageDur.Length == 0) return;

        // 在线生长
        if (!isMature)
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
        else
        {
            // 成熟后逻辑
            if (_cfg.keepAfterHarvest && _cfg.periodicProduce)
            {
                float interval = Mathf.Max(0.1f, _cfg.produceInterval);
                int perTick = Mathf.Max(1, _cfg.producePerTick);
                int cap = (_cfg.maxOnGround > 0) ? _cfg.maxOnGround : int.MaxValue;

                _produceTimer += Time.deltaTime;

                // 定时把产量写入“库存”
                while (_produceTimer >= interval)
                {
                    _produceTimer -= interval;
                    _storedYield = Mathf.Min(cap, _storedYield + perTick);
                }

                // ★ 进度条规则：
                // 有库存 ⇒ 显示满格（准备好可收）
                // 无库存 ⇒ 显示“距离首次入库”的进度
                if (_storedYield > 0) UpdateBar(1f);
                else UpdateBar(_produceTimer / interval);
            }
            else
            {
                // 一次性作物成熟后保持满条
                UpdateBar(1f);
            }
        }

        FaceCamera();

        // 成熟后交互（按 E）
        if (_player && CanHarvestNow() &&
            Vector3.Distance(_player.transform.position, transform.position) <= interactRadius &&
            Input.GetKeyDown(interactKey))
        {
            HarvestTo(_player);
        }
    }

    bool CanHarvestNow()
    {
        if (!isMature) return false;
        if (!_cfg.keepAfterHarvest) return true;        // 一次性作物：成熟即可收
        return _storedYield > 0;                        // 树：必须有库存才允许收
    }

    // 阶段可视
    void ApplyStage(int index)
    {
        if (_stageVisuals == null || _stageVisuals.Length == 0) return;

        for (int i = 0; i < _stageVisuals.Length; i++)
        {
            var go = _stageVisuals[i];
            if (!go) continue;

            bool on = (i == index);
            go.SetActive(on);

            // ★ 开启当前阶段的渲染器；关闭其它阶段（更保险）
            ForceEnableRenderers(go, on);
        }
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
                // 实例化阶段模型到根下面
                var inst = Instantiate(v, transform);
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = Quaternion.identity;
                inst.transform.localScale = Vector3.one;

                // 先全部关掉，后面再按当前阶段打开
                inst.SetActive(false);

                // ★ 关键：强制打开该阶段内所有渲染器，防止预制里被禁用/透明
                ForceEnableRenderers(inst, true);

                _stageVisuals[i] = inst;
                any = true;
            }
            else
            {
                _stageVisuals[i] = null;
            }
        }

        // 若没有阶段模型，则启用缩放补间作为兜底
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

        // ★ 关键修正：
        // 树：成熟后开始“第一次产出倒计时”，进度条从 0 开始
        // 一次性：成熟即满条
        if (_cfg != null && _cfg.keepAfterHarvest && _cfg.periodicProduce)
        {
            _produceTimer = 0f;
            _storedYield = 0;
            UpdateBar(0f);
        }
        else
        {
            UpdateBar(1f);
        }
    }

    // ======= 收获 =======
    public void HarvestTo(PlayerInventoryHolder holder)
    {
        if (!isMature || _cfg == null) return;

        string dropId = string.IsNullOrEmpty(_cfg.produceId) ? _cfg.plantItemId : _cfg.produceId;

        if (_cfg.keepAfterHarvest)
        {
            // 树：只有当 _storedYield > 0 才会进来
            int amount = Mathf.Max(0, _storedYield);
            if (amount <= 0) return;

            if (_cfg.produceWorldPrefab)
                SpawnProduceToGround(amount, gentle: true);
            else if (holder && !string.IsNullOrEmpty(dropId))
                holder.AddItem(dropId, amount);

            // 清空库存并重置倒计时，进度条从 0 重新启动
            _storedYield = 0;
            _produceTimer = 0f;
            UpdateBar(0f);
        }
        else
        {
            // 一次性作物：成熟后直接结算随机数量并销毁
            int amount = Mathf.Clamp(Random.Range(_cfg.produceMin, _cfg.produceMax + 1), 0, 999);

            if (_cfg.produceWorldPrefab && amount > 0)
                SpawnProduceBurst(amount);
            else if (holder && !string.IsNullOrEmpty(dropId) && amount > 0)
                holder.AddItem(dropId, amount);

            Destroy(gameObject);
        }
    }

    // —— 在树周围环形掉落 ——
    // 说明：_cfg.dropRadius 作为外半径，内半径按 0.6R 兜底；会尝试多次避免与其它碰撞重叠。
    void SpawnProduceToGround(int amount, bool gentle)
    {
        float R = Mathf.Max(0.35f, _cfg.dropRadius);           // 外半径
        float inner = Mathf.Clamp(R * 0.6f, 0.15f, R - 0.05f);  // 内半径，避免贴树
        LayerMask any = ~0;

        for (int i = 0; i < amount; i++)
        {
            Vector3 pos = PickRingPosition(transform.position, inner, R);

            // 往下打个射线，尽量贴地
            if (Physics.Raycast(pos + Vector3.up * 3f, Vector3.down, out var h, 6f, any, QueryTriggerInteraction.Ignore))
                pos = h.point + Vector3.up * 0.05f;

            // 尝试几次避免与其它碰撞重叠
            int tries = 0;
            while (tries++ < 4 && Physics.CheckSphere(pos, 0.15f, any, QueryTriggerInteraction.Ignore))
                pos = PickRingPosition(transform.position, inner, R);

            var go = Instantiate(_cfg.produceWorldPrefab, pos, Quaternion.identity);
            var rb = go.GetComponent<Rigidbody>();
            if (rb)
            {
                if (gentle)
                    rb.AddForce(Vector3.up * 0.8f, ForceMode.VelocityChange);
                else
                    rb.AddForce(new Vector3(Random.Range(-0.5f, 0.5f), 1f, Random.Range(-0.5f, 0.5f)) * 1.2f, ForceMode.VelocityChange);
            }
        }
    }

    void SpawnProduceBurst(int amount)
    {
        float R = Mathf.Max(0.35f, _cfg.dropRadius);
        float inner = Mathf.Clamp(R * 0.6f, 0.15f, R - 0.05f);
        LayerMask any = ~0;

        for (int i = 0; i < amount; i++)
        {
            Vector3 pos = PickRingPosition(transform.position, inner, R);

            if (Physics.Raycast(pos + Vector3.up * 3f, Vector3.down, out var h, 6f, any, QueryTriggerInteraction.Ignore))
                pos = h.point + Vector3.up * 0.05f;

            int tries = 0;
            while (tries++ < 4 && Physics.CheckSphere(pos, 0.15f, any, QueryTriggerInteraction.Ignore))
                pos = PickRingPosition(transform.position, inner, R);

            var go = Instantiate(_cfg.produceWorldPrefab, pos, Quaternion.identity);
            var rb = go.GetComponent<Rigidbody>();
            if (rb)
            {
                var dir = (pos - transform.position).normalized * Random.Range(0.6f, 1.1f) + Vector3.up;
                rb.AddForce(dir * Random.Range(1.0f, 2.0f), ForceMode.VelocityChange);
            }
        }
    }

    // 工具：从外半径 R、内半径 inner 的环形随机一个点（均匀分布）
    Vector3 PickRingPosition(Vector3 center, float inner, float outer)
    {
        float ang = Random.value * Mathf.PI * 2f;
        // 让半径采样更均匀：sqrt 随机
        float t = Mathf.Sqrt(Random.value);
        float r = Mathf.Lerp(inner, outer, t);
        Vector3 off = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * r;
        return center + off + Vector3.up * 0.35f;
    }




    // ======= 生长时间表 =======
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

    // ======= 存档结构 =======
    [System.Serializable]
    public struct GrowthState
    {
        public int stageIndex;     // 当前阶段
        public float stageTimer;   // 当前阶段累计秒
        public bool mature;        // 是否成熟
        public float produceTimer; // 树：生产计时器
        public int storedYield;    // 树：库存（可收获数量）
    }

    public GrowthState GetSaveState()
    {
        int si = Mathf.Clamp(_stageIndex, 0, Mathf.Max(0, _stageDur.Length - 1));
        float curDur = (_stageDur != null && _stageDur.Length > 0) ? _stageDur[si] : 0f;
        float st = Mathf.Clamp(_timer, 0f, Mathf.Max(0.0001f, curDur));
        return new GrowthState
        {
            stageIndex = si,
            stageTimer = st,
            mature = isMature,
            produceTimer = _produceTimer,
            storedYield = _storedYield
        };
    }

    public void ApplySaveState(GrowthState s)
    {
        if (_stageDur == null || _stageDur.Length == 0) BuildStageDurations();
        if (_stageVisuals == null || _stageVisuals.Length == 0) SetupStageVisuals();

        if (s.mature || s.stageIndex >= (_stageDur?.Length ?? 0))
        {
            _elapsedTotal = _totalGrowth;
            _stageIndex = Mathf.Max(0, (_stageDur?.Length ?? 1) - 1);
            _timer = 0f;
            BecomeMature();

            // ★ 按保存状态恢复树的生产条
            _produceTimer = Mathf.Max(0f, s.produceTimer);
            _storedYield = Mathf.Max(0, s.storedYield);

            if (_cfg != null && _cfg.keepAfterHarvest && _cfg.periodicProduce)
            {
                if (_storedYield > 0) UpdateBar(1f);
                else
                {
                    float interval = Mathf.Max(0.1f, _cfg.produceInterval);
                    UpdateBar(_produceTimer / interval);
                }
            }

            _inited = true;
            return;
        }

        _stageIndex = Mathf.Clamp(s.stageIndex, 0, _stageDur.Length - 1);
        float curDur = Mathf.Max(0.0001f, _stageDur[_stageIndex]);
        _timer = Mathf.Clamp(s.stageTimer, 0f, curDur - 0.0001f);
        isMature = false;

        _elapsedTotal = 0f;
        for (int i = 0; i < _stageIndex; i++) _elapsedTotal += _stageDur[i];

        ApplyStage(_stageIndex);
        float totalP = (_elapsedTotal + _timer) / Mathf.Max(0.0001f, _totalGrowth);
        ApplyScaleTween(totalP);
        UpdateBar(totalP);

        _produceTimer = Mathf.Max(0f, s.produceTimer);
        _storedYield = Mathf.Max(0, s.storedYield);
        _inited = true;
    }

    // ======= 离线推进：生长 + 生产入库 =======
    public void AdvanceBy(float seconds)
    {
        if (seconds <= 0f) return;
        if (_stageDur == null || _stageDur.Length == 0) BuildStageDurations();
        if (_stageDur == null || _stageDur.Length == 0) return;

        // 1) 先把生长推进到成熟
        while (seconds > 0f && !isMature)
        {
            float curDur = Mathf.Max(0.0001f, _stageDur[_stageIndex]);
            float remain = curDur - _timer;

            if (seconds < remain)
            {
                _timer += seconds;
                _elapsedTotal += seconds;
                seconds = 0f;
            }
            else
            {
                _elapsedTotal += remain;
                seconds -= remain;
                _timer = 0f;
                _stageIndex++;
                if (_stageIndex >= _stageDur.Length)
                {
                    BecomeMature();
                    break;
                }
                else
                {
                    ApplyStage(_stageIndex);
                }
            }

            float totalP = (_elapsedTotal + _timer) / Mathf.Max(0.0001f, _totalGrowth);
            ApplyScaleTween(totalP);
            UpdateBar(totalP);
        }

        // 2) 已成熟 & 树 → 按间隔入库
        if (isMature && _cfg.keepAfterHarvest && _cfg.periodicProduce)
        {
            float interval = Mathf.Max(0.1f, _cfg.produceInterval);
            int perTick = Mathf.Max(1, _cfg.producePerTick);
            int cap = (_cfg.maxOnGround > 0) ? _cfg.maxOnGround : int.MaxValue;

            float acc = _produceTimer + seconds;
            int ticks = Mathf.FloorToInt(acc / interval);
            _produceTimer = acc - ticks * interval;

            if (ticks > 0)
            {
                long add = (long)ticks * perTick;
                long target = (long)_storedYield + add;
                _storedYield = (int)Mathf.Clamp(target, 0, cap);
            }

            // 离线推进后也按规则刷新进度条
            if (_storedYield > 0) UpdateBar(1f);
            else UpdateBar(_produceTimer / interval);
        }
    }
}
