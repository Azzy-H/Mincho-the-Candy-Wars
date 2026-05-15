using System.Collections.Generic;
using Verse;
using RimWorld;

namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// Pawn 到达殖民地的方式
    /// </summary>
    public enum PawnArrivalMode
    {
        /// <summary>通过空投舱直接空投到殖民地</summary>
        DropPod,
        /// <summary>从地图边缘步行进入</summary>
        EdgeWalkIn
    }

    /// <summary>
    /// 生成指定 PawnKindDef 的 Pawn，并为其创建一个 Thing 类型的 flag 以供后续追踪。
    ///
    /// 生成后自动调用 StellarisEventFlagManager.SetFlag(trackFlagName, pawn)，
    /// 之后可通过 GetThingFlagStatus(trackFlagName) 查询该 Pawn 的 Normal / Despawn / DiedOrDisappeared 状态。
    /// 若被追踪的 Pawn 死亡或消失，flag 会由 Manager 的定期检查自动清理。
    ///
    /// XML 示例:
    ///   &lt;li Class="MinchoCandyWars.StellarisEvent.Effect_SpawnPawnFromFlag"&gt;
    ///     &lt;pawnKindDefName&gt;ARA_Warrior&lt;/pawnKindDefName&gt;
    ///     &lt;trackFlagName&gt;elite_warrior&lt;/trackFlagName&gt;
    ///     &lt;count&gt;1&lt;/count&gt;
    ///     &lt;factionDef&gt;ARA_MinchoCandyWars&lt;/factionDef&gt;
    ///     &lt;arrivalMode&gt;DropPod&lt;/arrivalMode&gt;
    ///   &lt;/li&gt;
    /// </summary>
    public class Effect_SpawnPawnFromFlag : StellarisEventEffect
    {
        /// <summary>
        /// 要生成的 PawnKindDef 名称（defName）。
        /// 优先读取同名字段；若为空则从 sourceFlagName 指定的 String flag 中读取。
        /// </summary>
        public string? pawnKindDefName;

        /// <summary>
        /// 可选：从指定的 String flag 中读取 PawnKindDef 名称（覆盖 pawnKindDefName）。
        /// 若此 flag 为 Thing 类型，则克隆该 Thing 的 kindDef。
        /// </summary>
        public string? sourceFlagName;

        /// <summary>
        /// 生成后为 Pawn 创建的 Thing 追踪 flag 名。
        /// 若 count &gt; 1，仅为首个 Pawn 创建 flag。
        /// </summary>
        public string? trackFlagName;

        /// <summary>到达模式：DropPod（空投）或 EdgeWalkIn（边缘进入）</summary>
        public PawnArrivalMode arrivalMode = PawnArrivalMode.DropPod;

        /// <summary>派系定义</summary>
        public FactionDef? factionDef;

        /// <summary>是否使用玩家派系（优先级高于 factionDef）</summary>
        public bool usePlayerFaction;

        /// <summary>追踪 flag 的到期 tick。null = 永久</summary>
        public int? ticks;

        public override void Execute()
        {
            Map map = Find.AnyPlayerHomeMap;
            if (map == null)
            {
                Log.Warning("[StellarisEvent] Effect_SpawnPawnFromFlag: 未找到玩家殖民地地图");
                return;
            }

            PawnKindDef kindDef = ResolvePawnKindDef();
            if (kindDef == null)
            {
                Log.Warning("[StellarisEvent] Effect_SpawnPawnFromFlag: 无法解析 PawnKindDef");
                return;
            }

            Faction faction = ResolveFaction();

            // 生成 pawn
            var pawns = new List<Pawn>();
            Pawn pawn = PawnGenerator.GeneratePawn(kindDef, faction);
            if (pawn != null)
                pawns.Add(pawn);

            if (pawns.Count == 0) return;

            // 执行到达
            switch (arrivalMode)
            {
                case PawnArrivalMode.DropPod:
                    SpawnByDropPod(map, pawns, faction);
                    break;
                case PawnArrivalMode.EdgeWalkIn:
                    SpawnByEdgeWalkIn(map, pawns);
                    break;
            }

            // 创建 Thing 追踪 flag——不检查 Spawned
            // thingIDNumber 在 PawnGenerator.GeneratePawn 时已分配，
            // DropPod 方式中 pawn 在空投舱内（Spawned==false），但 flag 仍可凭 thingID 追踪
            if (!trackFlagName.NullOrEmpty() && pawns.Count > 0)
            {
                StellarisEventFlagManager.SetFlag(trackFlagName!, pawns[0], ticks);
            }
        }

        // ──────────────────── PawnKindDef 解析 ────────────────────

        private PawnKindDef ResolvePawnKindDef()
        {
            // 优先从 sourceFlagName 读取
            if (!sourceFlagName.NullOrEmpty())
            {
                var flag = StellarisEventFlagManager.GetFlag(sourceFlagName!);
                if (flag != null)
                {
                    if (flag.valueType == FlagValueType.String && !flag.stringValue.NullOrEmpty())
                        return DefDatabase<PawnKindDef>.GetNamedSilentFail(flag.stringValue);

                    if (flag.valueType == FlagValueType.Thing)
                    {
                        Thing thing = flag.ResolveThing();
                        if (thing is Pawn p && p.kindDef != null)
                            return p.kindDef;
                    }
                }
            }

            // 后备：直接使用 pawnKindDefName
            if (!pawnKindDefName.NullOrEmpty())
                return DefDatabase<PawnKindDef>.GetNamedSilentFail(pawnKindDefName);

            return null!;
        }

        // ──────────────────── 派系解析 ────────────────────

        private Faction ResolveFaction()
        {
            if (usePlayerFaction)
                return Faction.OfPlayer;
            if (factionDef != null)
                return Find.FactionManager.FirstFactionOfDef(factionDef);
            return null!;
        }

        // ──────────────────── 空投 ────────────────────

        private void SpawnByDropPod(Map map, List<Pawn> pawns, Faction faction)
        {
            var things = new List<Thing>();
            foreach (var p in pawns) things.Add(p);

            IntVec3 dropSpot = DropCellFinder.TradeDropSpot(map);
            DropPodUtility.DropThingsNear(dropSpot, map, things, 110, false, false, true, faction: faction);
        }

        // ──────────────────── 边缘进入 ────────────────────

        private void SpawnByEdgeWalkIn(Map map, List<Pawn> pawns)
        {
            foreach (Pawn pawn in pawns)
            {
                IntVec3 entryCell;
                if (!RCellFinder.TryFindRandomPawnEntryCell(out entryCell, map, CellFinder.EdgeRoadChance_Neutral))
                {
                    if (!CellFinder.TryFindRandomEdgeCellWith(
                        c => c.Standable(map) && !c.Filled(map), map,
                        CellFinder.EdgeRoadChance_Ignore, out entryCell))
                        continue;
                }

                GenSpawn.Spawn(pawn, entryCell, map, Rot4.Random);
            }
        }
    }
}
