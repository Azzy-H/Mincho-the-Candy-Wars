using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MinchoCandyWars
{
    /// <summary>
    /// 场景部件：开局把指定 PawnKindDef 的起始珉巧转变为「智慧珉巧」。
    ///
    /// 背景：智慧珉巧与普通珉巧是**同一个种族**（Mincho_ThingDef），
    /// 区别只在 <see cref="CompMinchoCore"/> 的 Active 字段 ——
    ///   Active = false：等级 0/0、无 Intellectual 热情（普通珉巧）
    ///   Active = true ：等级 1/1、有 Intellectual 热情（智慧珉巧）
    /// Active 是运行时状态（存档字段，默认 false），
    /// CompProperties_MinchoCore 里没有任何可配置字段，因此 PawnKindDef
    /// 本身无法让新生成的 pawn 变成智慧珉巧 —— 必须由代码在这时置位。
    ///
    /// 注意：MCW_Intel hediff 是 Active = true 的**结果**（由
    /// CompMinchoCandyBuffApply 自动施加），不是原因；所以不能用
    /// ScenPart_ForcedHediff 来造智慧珉巧（那样 Active 仍是 false，状态不一致）。
    ///
    /// 场景 XML 用法：
    ///   &lt;li Class="MinchoCandyWars.ScenPart_StartWithSmartMincho"&gt;
    ///     &lt;def&gt;MCW_StartWithSmartMincho&lt;/def&gt;
    ///     &lt;kindDefs&gt;
    ///       &lt;li&gt;MCW_SmartMincho&lt;/li&gt;
    ///     &lt;/kindDefs&gt;
    ///   &lt;/li&gt;
    /// </summary>
    public class ScenPart_StartWithSmartMincho : ScenPart
    {
        /// <summary>需要被转变为智慧珉巧的起始 PawnKindDef 列表。</summary>
        public List<PawnKindDef> kindDefs = new List<PawnKindDef>();

        public override void Notify_PawnGenerated(Pawn pawn, PawnGenerationContext context, bool redressed)
        {
            base.Notify_PawnGenerated(pawn, context, redressed);

            // 只处理玩家起始 pawn（世界生成、袭击等其它上下文的 pawn 不受影响）
            if (context != PawnGenerationContext.PlayerStarter)
            {
                return;
            }

            if (pawn?.kindDef == null || kindDefs == null || !kindDefs.Contains(pawn.kindDef))
            {
                return;
            }

            CompMinchoCore core = pawn.GetComp<CompMinchoCore>();
            if (core != null && !core.Active)
            {
                core.Active = true;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref kindDefs, "kindDefs", LookMode.Def);
        }

        public override string Summary(Scenario scen)
        {
            return "MCW_ScenPart_StartWithSmartMincho".Translate();
        }
    }
}
