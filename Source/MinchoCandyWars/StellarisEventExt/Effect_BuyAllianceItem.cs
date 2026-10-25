using MinchoCandyWars.StellarisEvent;
using RimWorld;
using UnityEngine;
using Verse;

namespace MinchoCandyWars
{
    /// <summary>
    /// 珉巧同盟商店：用银购买指定物品（糖饰 / 糖饰研究图纸 / 珉巧改造包）。
    ///
    /// 执行时检查玩家主地图上的银是否足够：
    ///   · 足够 → 扣除银，把物品放在地图上；
    ///   · 不足 → 只发提示，不扣不打。
    ///
    /// XML 用法：
    ///   &lt;li Class="MinchoCandyWars.Effect_BuyAllianceItem"&gt;
    ///     &lt;thingDef&gt;MCW_MechaCandyChangeItem&lt;/thingDef&gt;
    ///     &lt;count&gt;1&lt;/count&gt;
    ///     &lt;silverCost&gt;500&lt;/silverCost&gt;
    ///   &lt;/li&gt;
    /// </summary>
    public class Effect_BuyAllianceItem : StellarisEventEffect
    {
        /// <summary>要出售的物品。</summary>
        public ThingDef thingDef;

        /// <summary>物品数量。</summary>
        public int count = 1;

        /// <summary>所需银币。</summary>
        public int silverCost = 100;

        public override void Execute()
        {
            if (thingDef == null)
            {
                Log.Warning("[MCW] Effect_BuyAllianceItem: thingDef 为空，跳过");
                return;
            }

            Map map = Find.AnyPlayerHomeMap;
            if (map == null)
            {
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

            int remaining = Mathf.Max(1, count);
            IntVec3 dropCell = DropCellFinder.RandomDropSpot(map);
            while (remaining > 0)
            {
                int stack = Mathf.Min(remaining, thingDef.stackLimit);
                Thing thing = ThingMaker.MakeThing(thingDef);
                thing.stackCount = stack;
                GenPlace.TryPlaceThing(thing, dropCell, map, ThingPlaceMode.Near);
                remaining -= stack;
            }

            Messages.Message(
                "MinchoCandyWars.UI.Message.Alliance.Bought".Translate(thingDef.LabelCap.Named("ITEM"), count.Named("COUNT")),
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
