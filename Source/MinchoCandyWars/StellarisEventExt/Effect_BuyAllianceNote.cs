using MinchoCandyWars.StellarisEvent;
using RimWorld;
using UnityEngine;
using Verse;

namespace MinchoCandyWars
{
    /// <summary>
    /// 珉巧同盟商店：用银购买「研究图纸」（直接解锁对应研究项目）。
    ///
    /// 执行时检查玩家主地图上的银是否足够：
    ///   · 足够 → 扣除银并完成研究；
    ///   · 不足 → 只发提示，不扣不打。
    ///
    /// XML 用法：
    ///   &lt;li Class="MinchoCandyWars.Effect_BuyAllianceNote"&gt;
    ///     &lt;silverCost&gt;1200&lt;/silverCost&gt;
    ///     &lt;researchDef&gt;MCW_Research_MechaCandy&lt;/researchDef&gt;
    ///   &lt;/li&gt;
    /// </summary>
    public class Effect_BuyAllianceNote : StellarisEventEffect
    {
        /// <summary>所需银币。</summary>
        public int silverCost = 1200;

        /// <summary>要解锁的研究项目。</summary>
        public ResearchProjectDef researchDef;

        public override void Execute()
        {
            if (researchDef == null)
            {
                Log.Warning("[MCW] Effect_BuyAllianceNote: researchDef 为空，跳过");
                return;
            }

            Map map = Find.AnyPlayerHomeMap;
            if (map == null)
            {
                return;
            }

            if (researchDef.IsFinished)
            {
                Messages.Message(
                    "MinchoCandyWars.UI.Message.Alliance.AlreadyResearched".Translate(researchDef.LabelCap.Named("RESEARCH")),
                    MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            if (silverCost > 0 && !HasEnoughSilver(map, silverCost))
            {
                Messages.Message(
                    "MinchoCandyWars.UI.Message.Alliance.NotEnoughSilver".Translate(silverCost.Named("SILVER")),
                    MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            if (silverCost > 0)
            {
                TakeSilver(map, silverCost);
            }

            Find.ResearchManager.FinishProject(researchDef, doCompletionDialog: false, doCompletionLetter: true);

            Messages.Message(
                "MinchoCandyWars.UI.Message.Alliance.NoteUnlocked".Translate(researchDef.LabelCap.Named("RESEARCH")),
                MessageTypeDefOf.PositiveEvent, historical: false);
        }

        private static bool HasEnoughSilver(Map map, int amount)
        {
            int total = 0;
            foreach (Thing silver in map.listerThings.ThingsOfDef(ThingDefOf.Silver))
            {
                total += silver.stackCount;
                if (total >= amount)
                {
                    return true;
                }
            }
            return false;
        }

        private static void TakeSilver(Map map, int amount)
        {
            int remaining = amount;
            foreach (Thing silver in map.listerThings.ThingsOfDef(ThingDefOf.Silver).ToList())
            {
                if (remaining <= 0)
                {
                    break;
                }

                int take = Mathf.Min(silver.stackCount, remaining);
                silver.stackCount -= take;
                remaining -= take;

                if (silver.stackCount <= 0)
                {
                    silver.Destroy(DestroyMode.Vanish);
                }
            }
        }
    }
}
