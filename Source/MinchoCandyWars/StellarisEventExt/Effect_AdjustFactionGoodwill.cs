using RimWorld;
using Verse;

namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// 相对修改某派系与玩家的好感度（+/- 指定值）。
    ///
    /// 与 Effect_SetFactionRelation 的区别：
    ///   - Effect_SetFactionRelation 把关系设为「目标档位」（Ally=100 / Neutral=0 / Hostile=-100）
    ///   - 本效果在此基础上按「增减量」调整，用于剧情里的「降低 10 好感度」这类需求
    ///
    /// XML 用法：
    ///   &lt;li Class="MinchoCandyWars.StellarisEvent.Effect_AdjustFactionGoodwill"&gt;
    ///     &lt;factionDef&gt;MCW_Alliance_Faction&lt;/factionDef&gt;
    ///     &lt;goodwillDelta&gt;-10&lt;/goodwillDelta&gt;
    ///   &lt;/li&gt;
    /// </summary>
    public class Effect_AdjustFactionGoodwill : StellarisEventEffect
    {
        /// <summary>目标派系。</summary>
        public FactionDef? factionDef;

        /// <summary>好感度增减量（负数为降低）。</summary>
        public int goodwillDelta = 0;

        /// <summary>好感度变化时是否弹提示消息。</summary>
        public bool canSendMessage = true;

        /// <summary>好感度降到敌对时是否发敌对信件。</summary>
        public bool canSendHostilityLetter = true;

        public override void Execute()
        {
            if (factionDef == null)
            {
                Log.Warning("[StellarisEvent] Effect_AdjustFactionGoodwill: factionDef 为空，无操作");
                return;
            }

            Faction? faction = Find.FactionManager.FirstFactionOfDef(factionDef);
            if (faction == null)
            {
                Log.Warning($"[StellarisEvent] Effect_AdjustFactionGoodwill: 找不到派系 {factionDef.defName}");
                return;
            }

            if (faction == Faction.OfPlayer)
            {
                return;
            }

            // 没有好感度系统的派系（永久敌对 / 隐藏派系）无法按数值调整
            if (!faction.HasGoodwill)
            {
                Log.Warning($"[StellarisEvent] Effect_AdjustFactionGoodwill: {faction.def.defName} 没有好感度系统，跳过");
                return;
            }

            // 关系记录缺失（运行时新建派系）时先补上，避免 GoodwillWith 出错
            if (faction.RelationWith(Faction.OfPlayer, allowNull: true) == null)
            {
                faction.TryMakeInitialRelationsWith(Faction.OfPlayer);
            }

            if (!faction.TryAffectGoodwillWith(
                    Faction.OfPlayer, goodwillDelta,
                    canSendMessage: canSendMessage,
                    canSendHostilityLetter: canSendHostilityLetter))
            {
                Log.Warning($"[StellarisEvent] Effect_AdjustFactionGoodwill: 无法改变与 {faction.def.defName} 的好感度"
                    + "（可能被任务锁定或该派系已被击败）");
            }
        }
    }
}
