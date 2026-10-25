using MinchoCandyWars.StellarisEvent;
using RimWorld;
using Verse;

namespace MinchoCandyWars
{
    /// <summary>
    /// StellarisEvent Gizmo 的单条配置（Thing 版本）。
    ///
    /// 移植自 ProdrakeRace 的 PDR.StellarisEventGizmoEntry，保留同样的 XML 结构，
    /// 使其可以直接写在 ThingDef 的 comps 里。
    /// </summary>
    public class StellarisEventGizmoEntry
    {
        /// <summary>要触发的 StellarisEventDef。</summary>
        public StellarisEventDef eventDef;

        /// <summary>Gizmo 按钮显示的标签文字。</summary>
        [MustTranslate]
        public string label;

        /// <summary>Gizmo 按钮悬停时的描述/提示文字。</summary>
        [MustTranslate]
        public string desc;

        /// <summary>Gizmo 按钮图标路径，如 "UI/Commands/MyIcon"。</summary>
        public string iconPath;

        /// <summary>
        /// 指定用作冷却计时器的 Float Flag 名。
        /// 为空或 null 表示无冷却。Flag 存在期间 Gizmo 禁用并显示冷却进度条。
        /// Flag.floatValue = 冷却总时长（tick），remainingTicks = 剩余冷却 tick。
        /// </summary>
        [NoTranslate]
        public string flagAsCooldown;

        /// <summary>
        /// 可选：指定一个 Bool Flag 名。若设置，仅当该 Flag 存在、
        /// 为 Bool 类型且值与 existFlagValue 匹配时，Gizmo 才显示。
        /// 为空或 null 表示不检查。
        /// </summary>
        [NoTranslate]
        public string existFlag;

        /// <summary>
        /// existFlag 期望的布尔值，默认 true。
        /// existFlagValue=true  → Flag 存在且为 true 时显示
        /// existFlagValue=false → Flag 存在且为 false 时显示
        /// </summary>
        public bool existFlagValue = true;

        /// <summary>
        /// 可选：好感度门槛。指定 factionDef 后，只有当玩家与该派系的
        /// 好感度 &gt;= minGoodwill 时，按钮才显示。
        /// </summary>
        public FactionDef factionDef;

        /// <summary>最低好感度（默认 -100，相当于不限制）。</summary>
        public float minGoodwill = -100f;

        /// <summary>
        /// 可选：浮点 flag 阈值。指定 minFloatFlag 后，只有当该 float flag
        /// 存在且值 &gt;= minFloatValue 时按钮才显示（用于"终局进度 10"这类门槛）。
        /// </summary>
        [NoTranslate]
        public string minFloatFlag;

        /// <summary>minFloatFlag 的最低值。</summary>
        public float minFloatValue = 0f;
    }
}
