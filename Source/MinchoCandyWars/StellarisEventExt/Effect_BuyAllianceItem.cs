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
    ///   · 足够 → 生成购买物品、扣除银并空投；
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
            if (QuestStory.Relations.Number < 50)
            {
                Messages.Message("MinchoCandyWars.UI.Message.Alliance.TechprintGoodwill".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            if (thingDef == null || count <= 0 || thingDef.stackLimit <= 0)
            {
                Log.Warning("[MCW] Effect_BuyAllianceItem: 无效物品或购买数量，跳过且不扣银");
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

            int remaining = count;
            var cargo = new List<Thing>();
            while (remaining > 0)
            {
                int stack = Mathf.Min(remaining, thingDef.stackLimit);
                Thing thing = ThingMaker.MakeThing(thingDef);
                thing.stackCount = stack;
                cargo.Add(thing);
                remaining -= stack;
            }
            if (silverCost > 0)
            {
                TakeSilver(map, silverCost);
            }
            DropPodUtility.DropThingsNear(DropCellFinder.TradeDropSpot(map), map, cargo, openDelay: 110, canInstaDropDuringInit: false, leaveSlag: false, canRoofPunch: false, forbid: false, allowFogged: false);

            Messages.Message(
                "MinchoCandyWars.UI.Message.Alliance.Bought".Translate(thingDef.LabelCap.Named("ITEM"), count.Named("COUNT")),
                MessageTypeDefOf.PositiveEvent, historical: false);
        }

        private static bool HasEnoughSilver(Map map, int amount)
        {
            int total = 0;
            foreach (Thing silver in map.listerThings.ThingsOfDef(ThingDefOf.Silver))
            {
                if (silver.IsForbidden(Faction.OfPlayer)) continue;
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
                if (silver.IsForbidden(Faction.OfPlayer)) continue;
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

