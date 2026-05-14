using System.Collections.Generic;
using Verse;
using RimWorld;

namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// 向玩家殖民地空投指定数量的物资或单位。
    ///
    /// 若 pawnKindDef 非空 → 生成 Pawn 空投（优先于 thingDef）。
    /// 若仅 thingDef 非空 → 生成物品空投。
    /// factionDef 用于设定生成物的派系（Pawn 和部分物品需要）。
    ///
    /// XML 示例:
    ///   &lt;li Class="MinchoCandyWars.StellarisEvent.Effect_DropSupply"&gt;
    ///     &lt;thingDef&gt;Silver&lt;/thingDef&gt;
    ///     &lt;num&gt;500&lt;/num&gt;
    ///   &lt;/li&gt;
    ///
    ///   &lt;li Class="MinchoCandyWars.StellarisEvent.Effect_DropSupply"&gt;
    ///     &lt;pawnKindDef&gt;ARA_Warrior&lt;/pawnKindDef&gt;
    ///     &lt;factionDef&gt;ARA_MinchoCandyWars&lt;/factionDef&gt;
    ///     &lt;num&gt;3&lt;/num&gt;
    ///   &lt;/li&gt;
    /// </summary>
    public class Effect_DropSupply : StellarisEventEffect
    {
        /// <summary>要空投的物品定义</summary>
        public ThingDef? thingDef;

        /// <summary>要空投的单位种类（优先级高于 thingDef）</summary>
        public PawnKindDef? pawnKindDef;

        public bool usePlayerFaction;

        /// <summary>派系定义。为 Pawn / 部分物品指定所属派系</summary>
        public FactionDef? factionDef;

        /// <summary>空投数量（物品 = 总数，Pawn = 人数）</summary>
        public int num = 1;

        public override void Execute()
        {
            Map map = Find.AnyPlayerHomeMap;
            if (map == null)
            {
                Log.Warning("[StellarisEvent] Effect_DropSupply: 未找到玩家殖民地地图");
                return;
            }
            Faction? faction = factionDef != null
                ? Find.FactionManager.FirstFactionOfDef(factionDef)
                : null;

            if (usePlayerFaction)
            {
                faction = Find.FactionManager.OfPlayer;
            }

            var things = new List<Thing>();

            if (pawnKindDef != null)
            {
                GeneratePawns(things, faction!, map);
            }
            else if (thingDef != null)
            {
                GenerateItems(things);
            }
            else
            {
                Log.Warning("[StellarisEvent] Effect_DropSupply: thingDef 和 pawnKindDef 均为空，无操作");
                return;
            }

            if (things.Count == 0) return;

            IntVec3 dropSpot = DropCellFinder.TradeDropSpot(map);
            DropPodUtility.DropThingsNear(dropSpot, map, things, 110, false, false, true , faction : faction);
        }

        private void GeneratePawns(List<Thing> things, Faction faction, Map map)
        {
            for (int i = 0; i < num; i++)
            {
                Pawn pawn = PawnGenerator.GeneratePawn(pawnKindDef, faction);
                if (pawn != null)
                    things.Add(pawn);
            }
        }

        private void GenerateItems(List<Thing> things)
        {
            int remaining = num;

            if (thingDef!.stackLimit > 1 && remaining > thingDef.stackLimit)
            {
                while (remaining > 0)
                {
                    int stackSize = UnityEngine.Mathf.Min(remaining, thingDef.stackLimit);
                    Thing thing = ThingMaker.MakeThing(thingDef);
                    thing.stackCount = stackSize;
                    things.Add(thing);
                    remaining -= stackSize;
                }
            }
            else
            {
                // 不可堆叠 或 总量小于堆叠上限 → 单堆
                Thing thing = ThingMaker.MakeThing(thingDef);
                thing.stackCount = remaining;
                things.Add(thing);
            }
        }
    }
}
