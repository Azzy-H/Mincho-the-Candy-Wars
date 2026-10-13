using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// 像 Effect_DropSupply 一样向玩家殖民地空投物品，但可以指定品质。
    ///
    /// 这是 Mincho 侧对 StellarisEvent 框架的扩展（框架的 Effect_DropSupply 无法指定品质）。
    ///
    /// XML 示例:
    ///   &lt;li Class="MinchoCandyWars.StellarisEvent.Effect_DropSupplyQuality"&gt;
    ///     &lt;thingDef&gt;MarineArmor&lt;/thingDef&gt;
    ///     &lt;num&gt;2&lt;/num&gt;
    ///     &lt;quality&gt;Masterwork&lt;/quality&gt;
    ///     &lt;dropLocation&gt;MapCenter&lt;/dropLocation&gt;
    ///   &lt;/li&gt;
    /// </summary>
    public class Effect_DropSupplyQuality : StellarisEventEffect
    {
        /// <summary>要空投的物品</summary>
        public ThingDef? thingDef;

        /// <summary>制造材料（仅当物品支持材料时生效）</summary>
        public ThingDef? stuff;

        /// <summary>品质：Awful / Poor / Normal / Good / Excellent / Masterwork / Legendary</summary>
        public QualityCategory quality = QualityCategory.Normal;

        /// <summary>空投数量</summary>
        public int num = 1;

        /// <summary>投放位置：TradeSpot（默认）/ MapCenter / Random</summary>
        public DropSupplyLocation dropLocation = DropSupplyLocation.TradeSpot;

        /// <summary>是否使用玩家派系</summary>
        public bool usePlayerFaction = true;

        /// <summary>usePlayerFaction 为 false 时使用的派系</summary>
        public FactionDef? factionDef;

        public override void Execute()
        {
            if (thingDef == null)
            {
                Log.Warning("[StellarisEvent] Effect_DropSupplyQuality: thingDef 为空，无操作");
                return;
            }

            Map? map = Find.AnyPlayerHomeMap;
            if (map == null)
            {
                Log.Warning("[StellarisEvent] Effect_DropSupplyQuality: 未找到玩家殖民地地图");
                return;
            }

            Faction? faction = usePlayerFaction
                ? Faction.OfPlayer
                : (factionDef != null ? Find.FactionManager.FirstFactionOfDef(factionDef) : null);

            List<Thing> things = GenerateItems();

            if (things.Count == 0)
            {
                return;
            }

            IntVec3 dropSpot = ResolveDropSpot(map);
            DropPodUtility.DropThingsNear(dropSpot, map, things, 110, false, false, true, faction: faction);
        }

        private IntVec3 ResolveDropSpot(Map map)
        {
            switch (dropLocation)
            {
                case DropSupplyLocation.MapCenter:
                    return map.Center;
                case DropSupplyLocation.Random:
                    return DropCellFinder.RandomDropSpot(map);
                default:
                    return DropCellFinder.TradeDropSpot(map);
            }
        }

        private List<Thing> GenerateItems()
        {
            ThingDef def = thingDef!;

            // stuff 校验：仅当物品支持材料且指定了真正的材料时使用
            ThingDef? usedStuff = stuff;
            if (usedStuff != null)
            {
                if (!def.MadeFromStuff)
                {
                    Log.Warning($"[StellarisEvent] Effect_DropSupplyQuality: {def.defName} 不支持材料（MadeFromStuff=false），忽略 stuff");
                    usedStuff = null;
                }
                else if (!usedStuff.IsStuff)
                {
                    Log.Warning($"[StellarisEvent] Effect_DropSupplyQuality: {usedStuff.defName} 不是材料（IsStuff=false），忽略 stuff");
                    usedStuff = null;
                }
            }

            var things = new List<Thing>();
            int remaining = Mathf.Max(1, num);

            while (remaining > 0)
            {
                int stackSize = def.stackLimit > 1 ? Mathf.Min(remaining, def.stackLimit) : 1;

                Thing thing = ThingMaker.MakeThing(def, usedStuff);
                thing.stackCount = stackSize;

                CompQuality? compQuality = thing.TryGetComp<CompQuality>();
                if (compQuality != null)
                {
                    compQuality.SetQuality(quality, ArtGenerationContext.Outsider);
                }
                else
                {
                    Log.Warning($"[StellarisEvent] Effect_DropSupplyQuality: {def.defName} 没有 CompQuality，忽略品质设置");
                }

                things.Add(thing);
                remaining -= stackSize;
            }

            return things;
        }
    }
}
