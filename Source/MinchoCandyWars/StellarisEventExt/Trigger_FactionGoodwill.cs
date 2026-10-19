using RimWorld;
using Verse;

namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// 判定某个派系对玩家的好感度是否落在 [minGoodwill, maxGoodwill] 区间内。
    ///
    /// 为什么需要它：通讯台原来用 gizmo 的 &lt;minGoodwill&gt; 做门槛（C# 里判），
    /// 把商店/资助并进「联络珉巧同盟」的对话树之后，gizmo 没了，
    /// 而框架自带的 Trigger_* 里没有任何一个能读好感度，所以补这一个。
    ///
    /// XML 示例（好感度 ≥ 50 才显示这个选项）:
    ///   &lt;li Class="MinchoCandyWars.StellarisEvent.Trigger_FactionGoodwill"&gt;
    ///     &lt;factionDef&gt;MCW_Alliance_Faction&lt;/factionDef&gt;
    ///     &lt;minGoodwill&gt;50&lt;/minGoodwill&gt;
    ///   &lt;/li&gt;
    ///
    /// 注意：<c>logic</c>（NOT/OR）只有在写了 <c>idGroup</c> 时才会生效，
    /// 这是 StellarisEventDef.EvaluateTriggers 的既有规则。
    /// </summary>
    public class Trigger_FactionGoodwill : StellarisEventTrigger
    {
        /// <summary>要检查的派系。为空则不做限制（永远通过）。</summary>
        public FactionDef? factionDef;

        /// <summary>好感度下限（含）。</summary>
        public float minGoodwill;

        /// <summary>好感度上限（含）。默认 100 = 不限制上限。</summary>
        public float maxGoodwill = 100f;

        public override bool CanTrigger()
        {
            if (factionDef == null)
            {
                return true;
            }

            Faction? faction = Find.FactionManager?.FirstFactionOfDef(factionDef);
            if (faction == null)
            {
                return false;
            }

            float goodwill = faction.PlayerGoodwill;
            return goodwill >= minGoodwill && goodwill <= maxGoodwill;
        }
    }
}
