#nullable disable
using System.Collections.Generic;
using Verse;
using RimWorld;

namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// 空投投放位置模式
    /// </summary>
    public enum DropSupplyLocation
    {
        /// <summary>交易空投点（默认，原行为：殖民地边缘贸易落点附近）</summary>
        TradeSpot,

        /// <summary>殖民地（地图）中心</summary>
        MapCenter,

        /// <summary>地图内随机位置</summary>
        Random
    }

    /// <summary>
    /// 向玩家殖民地空投指定数量的物资或单位。
    ///
    /// 若 pawnKindDef 非空 → 生成 Pawn 空投（优先于 thingDef，stuff 仅对物品生效）。
    /// 若仅 thingDef 非空 → 生成物品空投。
    /// stuff 非空且 thingDef 支持材料（MadeFromStuff）时，物品以该材料制造。
    /// dropLocation 控制投放位置：TradeSpot（默认）/ MapCenter / Random。
    /// factionDef 用于设定生成物的派系（Pawn 和部分物品需要）。
    ///
    /// XML 示例:
    ///   &lt;li Class="MinchoCandyWars.StellarisEvent.Effect_DropSupply"&gt;
    ///     &lt;thingDef&gt;Silver&lt;/thingDef&gt;
    ///     &lt;num&gt;500&lt;/num&gt;
    ///   &lt;/li&gt;
    ///
    ///   &lt;!-- 带材料的武器，随机位置投放 --&gt;
    ///   &lt;li Class="MinchoCandyWars.StellarisEvent.Effect_DropSupply"&gt;
    ///     &lt;thingDef&gt;MeleeWeapon_Knife&lt;/thingDef&gt;
    ///     &lt;stuff&gt;Plasteel&lt;/stuff&gt;
    ///     &lt;num&gt;3&lt;/num&gt;
    ///     &lt;dropLocation&gt;Random&lt;/dropLocation&gt;
    ///   &lt;/li&gt;
    ///
    ///   &lt;!-- 物资投放到殖民地中心 --&gt;
    ///   &lt;li Class="MinchoCandyWars.StellarisEvent.Effect_DropSupply"&gt;
    ///     &lt;thingDef&gt;Silver&lt;/thingDef&gt;
    ///     &lt;num&gt;500&lt;/num&gt;
    ///     &lt;dropLocation&gt;MapCenter&lt;/dropLocation&gt;
    ///   &lt;/li&gt;
    /// </summary>
    public class Effect_DropSupply : StellarisEventEffect
    {
        /// <summary>要空投的物品定义</summary>
        public ThingDef thingDef;

        /// <summary>要空投的单位种类（优先级高于 thingDef）</summary>
        public PawnKindDef pawnKindDef;

        /// <summary>是否使用玩家派系</summary>
        public bool usePlayerFaction;

        /// <summary>派系定义。为 Pawn / 部分物品指定所属派系</summary>
        public FactionDef factionDef;

        /// <summary>空投数量（物品 = 总数，Pawn = 人数）</summary>
        public int num = 1;

        /// <summary>制造材料（仅 thingDef 支持材料时生效，Pawn 空投忽略）</summary>
        public ThingDef stuff;

        /// <summary>投放位置模式，默认交易空投点</summary>
        public DropSupplyLocation dropLocation = DropSupplyLocation.TradeSpot;

        public override void Execute()
        {
            Map map = Find.AnyPlayerHomeMap;
            if (map == null)
            {
                Log.Warning("[StellarisEvent] Effect_DropSupply: 未找到玩家殖民地地图");
                return;
            }
            Faction faction = factionDef != null
                ? Find.FactionManager.FirstFactionOfDef(factionDef)
                : null;

            if (usePlayerFaction)
            {
                faction = Find.FactionManager.OfPlayer;
            }

            var things = new List<Thing>();

            if (pawnKindDef != null)
            {
                GeneratePawns(things, faction, map);
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

            IntVec3 dropSpot = ResolveDropSpot(map);
            DropPodUtility.DropThingsNear(dropSpot, map, things, 110, false, false, true, faction: faction);
        }

        /// <summary>
        /// 按 dropLocation 解析投放落点。
        /// </summary>
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
            // stuff 校验：仅当物品支持材料且指定了真正的材料时使用
            ThingDef usedStuff = stuff;
            if (usedStuff != null)
            {
                if (!thingDef.MadeFromStuff)
                {
                    Log.Warning($"[StellarisEvent] Effect_DropSupply: {thingDef.defName} 不支持材料（MadeFromStuff=false），忽略 stuff={usedStuff.defName}");
                    usedStuff = null;
                }
                else if (!usedStuff.IsStuff)
                {
                    Log.Warning($"[StellarisEvent] Effect_DropSupply: {usedStuff.defName} 不是材料（IsStuff=false），忽略 stuff");
                    usedStuff = null;
                }
            }

            int remaining = num;

            if (thingDef.stackLimit > 1 && remaining > thingDef.stackLimit)
            {
                while (remaining > 0)
                {
                    int stackSize = UnityEngine.Mathf.Min(remaining, thingDef.stackLimit);
                    Thing thing = ThingMaker.MakeThing(thingDef, usedStuff);
                    thing.stackCount = stackSize;
                    things.Add(thing);
                    remaining -= stackSize;
                }
            }
            else
            {
                // 不可堆叠 或 总量小于堆叠上限 → 单堆
                Thing thing = ThingMaker.MakeThing(thingDef, usedStuff);
                thing.stackCount = remaining;
                things.Add(thing);
            }
        }
    }
}
