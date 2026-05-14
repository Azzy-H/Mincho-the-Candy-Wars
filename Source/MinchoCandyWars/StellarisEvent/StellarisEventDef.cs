using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// 立绘定义 —— 包含路径与展示触发条件
    /// </summary>
    public class StellarisEventPictureDef
    {
        /// <summary>立绘纹理路径，如 "UI/StellarisEvent/Portrait_Queen"</summary>
        public string? path;

        /// <summary>该立绘可被选中的触发条件；为空则始终可用</summary>
        public StellarisEventTrigger? trigger;

        /// <summary>立绘 X 偏移（像素，正值右移，负值左移）</summary>
        public float portraitOffsetX = 0f;

        /// <summary>立绘 Y 偏移（像素，正值下移，负值上移）</summary>
        public float portraitOffsetY = 0f;

        public bool CanShow()
        {
            return trigger?.CanTrigger() ?? true;
        }
    }

    /// <summary>
    /// 事件回应选项
    /// </summary>
    public class StellarisEventOption
    {
        /// <summary>选项显示文本</summary>
        public string? label;

        /// <summary>鼠标悬停时显示的额外描述</summary>
        public string? desc;

        /// <summary>选择后执行的 effect 列表</summary>
        public StellarisEventEffectList? action;

        /// <summary>该选项是否可用的触发条件</summary>
        public StellarisEventTrigger? trigger;

        /// <summary>若 trigger 不通过，是否仍然在界面上构建该选项（但置灰）</summary>
        public bool hideWhenDisabled = false;

        /// <summary>禁用时显示的替代描述文本</summary>
        public string? disabledReason;

        public bool IsAvailable()
        {
            return trigger?.CanTrigger() ?? true;
        }
    }

    /// <summary>
    /// Stellaris 风格事件定义 —— 在 XML 中通过 &lt;StellarisEventDef&gt; 配置
    /// </summary>
    public class StellarisEventDef : Def
    {
        /// <summary>事件标题（显示在窗口顶部）</summary>
        public string? title;

        /// <summary>事件描述文本（显示在标题下方）</summary>
        public string? desc;

        /// <summary>事件窗口背景纹理路径，如 "UI/StellarisEvent/Bg_Nebula"。为空则使用默认暗色背景</summary>
        public string? backgroundPath;

        /// <summary>立绘列表，构建窗口时从满足 trigger 的项中随机选取</summary>
        public List<StellarisEventPictureDef> pictures = new List<StellarisEventPictureDef>();

        /// <summary>事件本体触发条件列表。无 idGroup 的 trigger 默认 AND；有 idGroup 的按分组逻辑判定。</summary>
        public List<StellarisEventTrigger> triggers = new List<StellarisEventTrigger>();

        /// <summary>事件触发时立即执行的 effect 列表（在选择 option 之前）</summary>
        public StellarisEventEffectList? immediate;

        /// <summary>供玩家选择的回应选项列表</summary>
        public List<StellarisEventOption> options = new List<StellarisEventOption>();

        /// <summary>所有 option 的 action 执行完毕后执行的 effect 列表</summary>
        public StellarisEventEffectList? after;

        /// <summary>本地化参数列表。由 Effect_FetchFlagAsParam 在 immediate 阶段填充。</summary>
        public List<StellarisEventLocalParam> localParams = new List<StellarisEventLocalParam>();

        // ──────────────────── 内部运行时状态 ────────────────────

        /// <summary>本次弹窗选中的立绘纹理（在 Fire 时缓存）</summary>
        [Unsaved]
        public Texture2D? selectedPicture;

        /// <summary>本次弹窗选中的立绘路径（用于调试）</summary>
        [Unsaved]
        public string? selectedPicturePath;

        /// <summary>本次弹窗选中的立绘 X 偏移</summary>
        [Unsaved]
        public float selectedPictureOffsetX;

        /// <summary>本次弹窗选中的立绘 Y 偏移</summary>
        [Unsaved]
        public float selectedPictureOffsetY;

        /// <summary>本次弹窗使用的背景纹理（在 Fire 时缓存）</summary>
        [Unsaved]
        public Texture2D? backgroundTexture;

        // ──────────────────── 静态执行上下文 ────────────────────

        /// <summary>
        /// 当前正在执行 effect 的事件 Def（供 Effect_FetchFlagAsParam 等需要访问父 Def 的 Effect 使用）。
        /// 在 TryFire / OnOptionSelected 中设置和清除。
        /// </summary>
        public static StellarisEventDef? ExecutingInstance { get; private set; }

        // ──────────────────── 方法 ────────────────────

        /// <summary>
        /// 事件是否满足触发条件
        /// </summary>
        public bool CanTrigger()
        {
            if (triggers.NullOrEmpty())
                return true;

            // 按 idGroup 分组：空 idGroup 视为独立 trigger
            var groups = new Dictionary<string, List<StellarisEventTrigger>>();
            var ungrouped = new List<StellarisEventTrigger>();

            foreach (var t in triggers)
            {
                if (t.idGroup.NullOrEmpty())
                    ungrouped.Add(t);
                else
                {
                    if (!groups.ContainsKey(t.idGroup??""))
                        groups[t.idGroup ?? ""] = new List<StellarisEventTrigger>();
                    groups[t.idGroup ?? ""].Add(t);
                }
            }

            // 无 idGroup 的 trigger：全部必须为 true（默认 AND）
            foreach (var t in ungrouped)
            {
                if (!t.CanTrigger())
                    return false;
            }

            // 有 idGroup 的 trigger：按分组逻辑判定
            foreach (var kvp in groups)
            {
                if (!EvaluateTriggerGroup(kvp.Value))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 评估同一 idGroup 的 trigger 列表。
        /// 规则: NOT 翻转 → 任一 OR 为 true 则所有 OR 视为 true → 全部必须为 true
        /// </summary>
        private static bool EvaluateTriggerGroup(List<StellarisEventTrigger> group)
        {
            // Step 1: 收集原始结果
            var rawResults = new Dictionary<StellarisEventTrigger, bool>();
            var finalResults = new Dictionary<StellarisEventTrigger, bool>();

            foreach (var t in group)
                rawResults[t] = t.CanTrigger();

            // Step 2: NOT 翻转
            foreach (var t in group)
            {
                if (t.logic == TriggerLogic.NOT)
                    finalResults[t] = !rawResults[t];
                else
                    finalResults[t] = rawResults[t];
            }

            // Step 3: OR 覆盖 —— 任一 OR 为 true，则所有 OR 视为 true
            bool anyOrTrue = group.Any(t => t.logic == TriggerLogic.OR && rawResults[t]);
            if (anyOrTrue)
            {
                foreach (var t in group)
                {
                    if (t.logic == TriggerLogic.OR)
                        finalResults[t] = true;
                }
            }

            // Step 4: 全部为 true 才算通过
            return finalResults.Values.All(v => v);
        }

        /// <summary>
        /// 从 pictures 列表中随机选取一张满足条件的立绘并缓存
        /// </summary>
        public void SelectRandomPicture()
        {
            var validPictures = new List<StellarisEventPictureDef>();
            foreach (var pic in pictures)
            {
                if (pic.CanShow())
                    validPictures.Add(pic);
            }

            if (validPictures.Count > 0)
            {
                var chosen = validPictures.RandomElement();
                selectedPicturePath = chosen.path;
                selectedPicture = ContentFinder<Texture2D>.Get(chosen.path);
                selectedPictureOffsetX = chosen.portraitOffsetX;
                selectedPictureOffsetY = chosen.portraitOffsetY;
            }
            else
            {
                selectedPicturePath = null;
                selectedPicture = null;
                selectedPictureOffsetX = 0f;
                selectedPictureOffsetY = 0f;
            }
        }

        /// <summary>
        /// 加载背景纹理（如果指定了 backgroundPath）
        /// </summary>
        public void LoadBackgroundTexture()
        {
            backgroundTexture = null;
            if (!string.IsNullOrEmpty(backgroundPath))
            {
                backgroundTexture = ContentFinder<Texture2D>.Get(backgroundPath, false);
            }
        }

        /// <summary>
        /// 执行完整事件流程：
        /// 1. trigger 判定 → 2. immediate → 3. 选立绘 → 4. 构建窗口
        /// </summary>
        public bool TryFire()
        {
            if (!CanTrigger())
                return false;

            ExecutingInstance = this;
            try
            {
                immediate?.Execute();
            }
            finally
            {
                ExecutingInstance = null!;
            }

            SelectRandomPicture();
            LoadBackgroundTexture();
            Find.WindowStack.Add(new StellarisEventWindow(this));
            return true;
        }

        // ──────────────────── 本地化参数 ────────────────────

        /// <summary>
        /// 设置 / 覆写一个本地化参数。若同名 key 已存在则覆盖值。
        /// </summary>
        public void SetParam(string key, string value)
        {
            for (int i = 0; i < localParams.Count; i++)
            {
                if (localParams[i].key == key)
                {
                    localParams[i].value = value;
                    return;
                }
            }
            localParams.Add(new StellarisEventLocalParam(key, value));
        }

        /// <summary>
        /// 将文本中所有 {key} 占位替换为对应的参数值。
        /// 无匹配 key 的占位保持原样。
        /// 若 localParams 为空则直接返回原文本。
        /// </summary>
        public string InjectParams(string text)
        {
            if (string.IsNullOrEmpty(text) || localParams.Count == 0)
                return text ?? "";

            foreach (var param in localParams)
            {
                if (param.key == null) continue;
                text = text.Replace("{" + param.key + "}", param.value ?? "");
            }
            return text;
        }
    }
}
