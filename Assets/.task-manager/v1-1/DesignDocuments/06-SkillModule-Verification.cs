/// <summary>
/// 技能模块验证脚本
/// 用于测试 SkillModule 的冷却闭环
/// </summary>
using UnityEngine;

namespace FarmGame.Test
{
    /// <summary>
    /// SkillModuleTest: 技能模块测试
    /// 测试技能冷却闭环的三个关键场景
    /// </summary>
    public class SkillModuleTest : MonoBehaviour
    {
        [Header("测试配置")]
        [Tooltip("测试技能 ID")]
        public string testSkillId = "discount_blessing";

        [Tooltip("测试冷却时间（秒）")]
        public float testCooldown = 5f;

        private FarmGame.ActorSystem.Skills.SkillModule _skillModule;
        private bool _testCompleted = false;

        void Start()
        {
            // 获取技能模块
            _skillModule = GetComponent<FarmGame.ActorSystem.Skills.SkillModule>();

            if (_skillModule == null)
            {
                Debug.LogError($"[SkillModuleTest] 未找到 SkillModule，测试失败");
                return;
            }

            Debug.Log($"[SkillModuleTest] 开始测试，技能 ID: {testSkillId}");

            // 开始测试
            StartCoroutine(RunSkillTest());
        }

        /// <summary>
        /// 运行技能测试
        /// </summary>
        private System.Collections.IEnumerator RunSkillTest()
        {
            // 步骤 1: 检查技能是否存在
            Debug.Log($"[SkillModuleTest] 步骤 1: 检查技能 {testSkillId} 是否存在");

            bool hasSkill = _skillModule.HasSkill(testSkillId);
            Debug.Log($"[SkillModuleTest] 技能存在: {hasSkill}");

            if (!hasSkill)
            {
                Debug.LogError($"[SkillModuleTest] 技能 {testSkillId} 不存在，测试终止");
                yield break;
            }

            // 步骤 2: 首次触发成功
            Debug.Log($"[SkillModuleTest] 步骤 2: 首次触发技能 {testSkillId}");

            bool firstCastResult = _skillModule.TryCast(testSkillId);
            Debug.Log($"[SkillModuleTest] 首次施放结果: {firstCastResult}");

            if (!firstCastResult)
            {
                Debug.LogError($"[SkillModuleTest] 首次施放失败，测试终止");
                yield break;
            }

            // 步骤 3: 检查冷却剩余时间
            Debug.Log($"[SkillModuleTest] 步骤 3: 检查冷却剩余时间");

            float cooldownRemaining = _skillModule.GetCooldownRemaining(testSkillId);
            Debug.Log($"[SkillModuleTest] 冷却剩余时间: {cooldownRemaining:F2} 秒");

            if (cooldownRemaining <= 0f)
            {
                Debug.LogWarning($"[SkillModuleTest] 冷却时间异常（应为 {testCooldown} 秒，实际 {cooldownRemaining:F2} 秒）");
            }

            // 步骤 4: 冷却期间施放失败
            Debug.Log($"[SkillModuleTest] 步骤 4: 冷却期间施放技能 {testSkillId}");

            bool inCooldownCastResult = _skillModule.TryCast(testSkillId);
            Debug.Log($"[SkillModuleTest] 冷却期间施放结果: {inCooldownCastResult}");

            if (inCooldownCastResult)
            {
                Debug.LogError($"[SkillModuleTest] 冷却期间不应施放成功，测试失败");
                yield break;
            }

            Debug.Log($"[SkillModuleTest] 冷却期间施放失败（符合预期）");

            // 步骤 5: 等待冷却结束
            Debug.Log($"[SkillModuleTest] 步骤 5: 等待冷却结束（{testCooldown} 秒）");

            yield return new WaitForSeconds(testCooldown + 0.5f);

            // 步骤 6: 冷却结束后再次施放
            Debug.Log($"[SkillModuleTest] 步骤 6: 冷却结束后再次施放技能 {testSkillId}");

            bool afterCooldownCastResult = _skillModule.TryCast(testSkillId);
            Debug.Log($"[SkillModuleTest] 冷却结束后施放结果: {afterCooldownCastResult}");

            if (!afterCooldownCastResult)
            {
                Debug.LogError($"[SkillModuleTest] 冷却结束后施放失败，测试失败");
                yield break;
            }

            Debug.Log($"[SkillModuleTest] 冷却结束后施放成功（符合预期）");

            // 测试完成
            _testCompleted = true;
            Debug.Log($"[SkillModuleTest] 测试完成！所有步骤验证通过。");
        }

        /// <summary>
        /// OnGUI 绘制测试信息
        /// </summary>
        void OnGUI()
        {
            if (_skillModule == null)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(10, 10, 400, 300));
            GUILayout.Box("技能模块测试");
            GUILayout.Space(10);

            GUILayout.Label($"技能 ID: {testSkillId}");
            GUILayout.Label($"技能存在: {_skillModule.HasSkill(testSkillId)}");
            GUILayout.Label($"技能就绪: {_skillModule.IsReady(testSkillId)}");
            GUILayout.Label($"冷却剩余: {_skillModule.GetCooldownRemaining(testSkillId):F2} 秒");
            GUILayout.Space(10);

            if (GUILayout.Button("施放技能"))
            {
                bool result = _skillModule.TryCast(testSkillId);
                Debug.Log($"[SkillModuleTest] 手动施放结果: {result}");
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
