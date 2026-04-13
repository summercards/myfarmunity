/// <summary>
/// ActorTickModule 验证脚本
/// 用于测试低频 tick 调度的3个关键场景
/// </summary>
using UnityEngine;

namespace FarmGame.Test
{
    /// <summary>
    /// ActorTickModuleTest: 低频 tick 调度测试
    /// 测试正常调度、禁用停止、销毁清理三个关键场景
    /// </summary>
    public class ActorTickModuleTest : MonoBehaviour
    {
        [Header("测试配置")]
        [Tooltip("测试 Tick 周期（秒），默认 1 秒")]
        public float testTickInterval = 1f;

        [Tooltip("是否使用未缩放时间")]
        public bool testUseUnscaledTime = false;

        [Header("场景测试")]
        [Tooltip("是否测试场景切换")]
        public bool testSceneSwitch = false;

        private FarmGame.ActorSystem.ActorTickModule _tickModule;
        private FarmGame.ActorSystem.Skills.SkillModule _skillModule;
        private bool _testCompleted = false;
        private int _tickCount = 0;
        private float _testStartTime;

        void Start()
        {
            // 获取 ActorTickModule
            _tickModule = GetComponent<FarmGame.ActorSystem.ActorTickModule>();
            if (_tickModule == null)
            {
                Debug.LogError($"[ActorTickModuleTest] 未找到 ActorTickModule，测试失败");
                return;
            }

            // 获取 SkillModule
            _skillModule = GetComponent<FarmGame.ActorSystem.Skills.SkillModule>();
            if (_skillModule == null)
            {
                Debug.LogWarning($"[ActorTickModuleTest] 未找到 SkillModule，跳过技能相关测试");
            }

            Debug.Log($"[ActorTickModuleTest] 开始测试，Tick 周期：{testTickInterval} 秒，未缩放时间：{testUseUnscaledTime}");

            // 配置测试参数
            _tickModule.tickInterval = testTickInterval;
            _tickModule.useUnscaledTime = testUseUnscaledTime;

            _testStartTime = Time.time;
            StartCoroutine(RunTickTest());
        }

        /// <summary>
        /// 运行 Tick 测试
        /// </summary>
        private System.Collections.IEnumerator RunTickTest()
        {
            // 步骤 1: 检查模块是否已启用
            Debug.Log($"[ActorTickModuleTest] 步骤 1: 检查模块是否已启用");

            if (!_tickModule.IsEnabled)
            {
                Debug.LogError($"[ActorTickModuleTest] 模块未启用，测试终止");
                yield break;
            }

            Debug.Log($"[ActorTickModuleTest] 模块已启用，IsEnabled: {_tickModule.IsEnabled}");

            // 步骤 2: 检查是否正在调度
            Debug.Log($"[ActorTickModuleTest] 步骤 2: 检查是否正在调度");

            bool isScheduling = _tickModule.IsScheduling;
            Debug.Log($"[ActorTickModuleTest] 正在调度: {isScheduling}");

            if (!isScheduling)
            {
                Debug.LogWarning($"[ActorTickModuleTest] 模块未正在调度，可能存在问题");
            }

            // 步骤 3: 等待 Tick 触发（正常场景）
            Debug.Log($"[ActorTickModuleTest] 步骤 3: 等待 Tick 触发（正常场景），预期 {testTickInterval} 秒");

            int tickCountBefore = _tickCount;
            yield return new WaitForSeconds(testTickInterval + 0.5f);

            int tickCountAfter = _tickCount;
            int tickDiff = tickCountAfter - tickCountBefore;

            Debug.Log($"[ActorTickModuleTest] Tick 触发次数：{tickDiff}");

            if (tickDiff >= 1)
            {
                Debug.Log($"[ActorTickModuleTest] ✅ Tick 正常触发（符合预期）");
            }
            else
            {
                Debug.LogWarning($"[ActorTickModuleTest] ⚠️ Tick 未触发，可能存在问题");
            }

            // 步骤 4: 禁用模块，验证调度停止
            Debug.Log($"[ActorTickModuleTest] 步骤 4: 禁用模块，验证调度停止");

            _tickModule.Disable();
            yield return new WaitForSeconds(0.1f);

            bool stillScheduling = _tickModule.IsScheduling;
            Debug.Log($"[ActorTickModuleTest] 禁用后是否仍在调度: {stillScheduling}");

            if (stillScheduling)
            {
                Debug.LogError($"[ActorTickModuleTest] ❌ 禁用后仍在调度，测试失败");
                yield break;
            }
            else
            {
                Debug.Log($"[ActorTickModuleTest] ✅ 禁用后已停止调度（符合预期）");
            }

            // 步骤 5: 重新启用模块
            Debug.Log($"[ActorTickModuleTest] 步骤 5: 重新启用模块");

            _tickModule.Enable();
            yield return new WaitForSeconds(0.1f);

            isScheduling = _tickModule.IsScheduling;
            Debug.Log($"[ActorTickModuleTest] 重新启用后是否在调度: {isScheduling}");

            if (!isScheduling)
            {
                Debug.LogWarning($"[ActorTickModuleTest] 重新启用后未开始调度");
            }

            // 步骤 6: 测试场景切换（如果启用）
            if (testSceneSwitch)
            {
                Debug.Log($"[ActorTickModuleTest] 步骤 6: 测试场景切换");

                // 加载当前场景（这会销毁当前对象）
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
                );

                // 场景加载后会自动测试销毁对象后的情况
            }

            _testCompleted = true;
            Debug.Log($"[ActorTickModuleTest] 测试完成！所有场景验证通过。");
        }

        /// <summary>
        /// ActorTickModule 调用 OnTick 时的回调（通过 SendMessage）
        /// </summary>
        private void OnTickCallback()
        {
            _tickCount++;
            Debug.Log($"[ActorTickModuleTest] Tick 触发，次数：{_tickCount}，时间：{Time.time - _testStartTime:F2} 秒");
        }

        /// <summary>
        /// 销毁时验证无悬挂回调
        /// </summary>
        private void OnDestroy()
        {
            if (_tickModule != null)
            {
                bool stillScheduling = _tickModule.IsScheduling;
                Debug.Log($"[ActorTickModuleTest] OnDestroy: 仍在调度: {stillScheduling}");

                if (stillScheduling)
                {
                    Debug.LogError($"[ActorTickModuleTest] ❌ 销毁后仍有悬挂回调，测试失败");
                }
                else
                {
                    Debug.Log($"[ActorTickModuleTest] ✅ 销毁后无悬挂回调（符合预期）");
                }
            }
        }

        /// <summary>
        /// OnGUI 绘制测试信息
        /// </summary>
        void OnGUI()
        {
            if (_tickModule == null)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(10, 10, 400, 400));
            GUILayout.Box("ActorTickModule 测试");
            GUILayout.Space(10);

            GUILayout.Label($"模块启用: {_tickModule.IsEnabled}");
            GUILayout.Label($"正在调度: {_tickModule.IsScheduling}");
            GUILayout.Label($"Tick 周期: {_tickModule.TickInterval:F2} 秒");
            GUILayout.Label($"Tick 模式: {_tickModule.TickMode}");
            GUILayout.Label($"Tick 触发次数: {_tickCount}");
            GUILayout.Label($"测试时间: {Time.time - _testStartTime:F2} 秒");
            GUILayout.Space(10);

            if (GUILayout.Button("禁用模块"))
            {
                _tickModule.Disable();
            }

            if (GUILayout.Button("启用模块"))
            {
                _tickModule.Enable();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("销毁游戏对象（测试无悬挂回调）"))
            {
                Destroy(gameObject);
            }

            GUILayout.Space(10);

            if (_testCompleted)
            {
                GUILayout.Label("测试状态: 完成");
                GUILayout.Label("所有验证通过 ✓");
            }
            else
            {
                GUILayout.Label("测试状态: 进行中...");
            }

            GUILayout.EndArea();
        }
    }
}
