using UnityEngine;

[DisallowMultipleComponent]
public class CropPlant : MonoBehaviour
{
    [Header("Runtime (ReadOnly)")] public bool isMature = false;

    // ����
    SeedPlantDataSO.Entry _cfg;

    // ������ʱ
    float _timer = 0f;              // ��ǰ�׶��ۼ�
    int _stageIndex = 0;          // ��ǰ�׶�����
    float _elapsedTotal = 0f;       // ���н׶��ۼ�
    float _totalGrowth = 0f;        // ������ʱ��
    float[] _stageDur;              // ÿ�׶�ʱ�����Ѵ���Ĭ��ֵ��

    // �׶ο��ӣ�����Ŀ��� stages[i].visual ���ɣ�
    GameObject[] _stageVisuals;

    // ���� ��ʼ����ǣ����ڷ�ֹ Update ���ܵ��¿�ָ�룩����
    bool _inited = false;

    [Header("�޽׶�ģ��ʱ�����Ŷ���")]
    public bool enableScaleTween = true;
    public Vector3 startScale = Vector3.one * 0.45f;
    public Vector3 endScale = Vector3.one * 1.00f;
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("���� / �ջ�")]
    public KeyCode interactKey = KeyCode.E;
    public float interactRadius = 1.6f;
    PlayerInventoryHolder _player;

    // ���� ���� 3D ������������ Quad�� ����
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

    // ===== �������� =====
    public void Init(SeedPlantDataSO.Entry cfg)
    {
        _cfg = cfg;
        BuildStageDurations();
        SetupStageVisuals();
        BuildBar();
        ApplyStage(0);            // ��ʼ��ʾ��0�׶�
        _player = FindObjectOfType<PlayerInventoryHolder>();
        UpdateBar(0f);
        _inited = true;
    }

    void Update()
    {
        // ���� ���أ�δ��ʼ����׶�����δ����ʱֱ�ӷ��أ������ָ�� ����
        if (!_inited)
        {
            // ���ԡ�����ʼ����һ�Σ���������� Init ��û�����ã�
            if (_cfg != null && (_stageDur == null || _stageDur.Length == 0))
            {
                BuildStageDurations();
                SetupStageVisuals();
                BuildBar();
                ApplyStage(0);
                UpdateBar(0f);
                _inited = true;
            }
            else
            {
                return;
            }
        }
        if (_stageDur == null || _stageDur.Length == 0) return;

        // ���������ƽ�
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

        FaceCamera();

        // ����󽻻�
        if (isMature && _player)
        {
            if (Vector3.Distance(_player.transform.position, transform.position) <= interactRadius
                && Input.GetKeyDown(interactKey))
            {
                HarvestTo(_player);
            }
        }
    }

    // �׶ο��ӣ��ѳ���ǰ�׶������ģ��ȫ�����أ�
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

    // �ջ�
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
            }
        }
        else if (holder && !string.IsNullOrEmpty(dropId) && amount > 0)
        {
            holder.AddItem(dropId, amount);
        }
        Destroy(gameObject);
    }

    // ����׶�ʱ�� & ��ʱ������0�滻ΪĬ��ֵ��
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

    // ======= �����л��ġ������浵״̬�� =======
    [System.Serializable]
    public struct GrowthState
    {
        public int stageIndex;     // ��ǰ�׶Σ�0~N-1��
        public float stageTimer;   // ��ǰ�׶������ۼ�����
        public bool mature;        // �Ƿ��ѳ���
    }

    // ��������״̬�����棩
    public GrowthState GetSaveState()
    {
        int si = Mathf.Clamp(_stageIndex, 0, Mathf.Max(0, _stageDur.Length - 1));
        float curDur = (_stageDur != null && _stageDur.Length > 0) ? _stageDur[si] : 0f;
        float st = Mathf.Clamp(_timer, 0f, Mathf.Max(0.0001f, curDur));
        return new GrowthState { stageIndex = si, stageTimer = st, mature = isMature };
    }

    // Ӧ������״̬�����أ�
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
        _inited = true;
    }

    // ======= �����ƽ�����N��ֱ�ӳԵ��� =======
    public void AdvanceBy(float seconds)
    {
        if (seconds <= 0f) return;
        if (_stageDur == null || _stageDur.Length == 0) BuildStageDurations();
        if (_stageDur == null || _stageDur.Length == 0) return;

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
    }
}
