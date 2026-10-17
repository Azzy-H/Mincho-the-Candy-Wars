using System.Linq;
using RimWorld;
using Verse;

namespace MinchoCandyWars
{
    /// <summary>
    /// questB5「珉巧同盟据点」的站点地图生成步骤：
    ///   1. 把 SitePartWorker_MCWAllianceOutpost 生成到 part.things 的珉巧居民
    ///      放到地图中心附近；
    ///   2. 在地图边缘生成回收部队（38 名成员 + 2 名队长）。
    ///
    /// 通过 GenStepDef 的 linkWithSite 挂到 MCW_AllianceOutpost 站点部件上。
    /// </summary>
    public class GenStep_MCWAllianceOutpost : GenStep
    {
        private const string RecycleFactionDefName = "MCW_RecycleCorp_Faction";
        private const string MemberKindDefName = "MCW_RecycleCorp_Member";
        private const string LeaderKindDefName = "MCW_RecycleCorp_Leader";

        private const int AttackerMemberCount = 38;
        private const int AttackerLeaderCount = 2;

        public override int SeedPart => 617384029;

        public override void Generate(Map map, GenStepParams parms)
        {
            // ── 1. 据点里的珉巧居民（从 part.things 取出并放上地图） ──
            if (parms.sitePart?.things != null)
            {
                while (parms.sitePart.things.Count > 0)
                {
                    Thing thing = parms.sitePart.things[0];
                    parms.sitePart.things.Take(thing);
                    if (thing is Pawn pawn)
                    {
                        IntVec3 cell = CellFinder.RandomClosewalkCellNear(map.Center, map, 15);
                        GenSpawn.Spawn(pawn, cell, map);
                    }
                }
            }

            // ── 2. 地图边缘的回收部队 ──
            FactionDef recycleDef = DefDatabase<FactionDef>.GetNamed(RecycleFactionDefName, errorOnFail: false);
            Faction recycleFaction = recycleDef == null ? null : Find.FactionManager.FirstFactionOfDef(recycleDef);
            if (recycleFaction == null)
            {
                Log.Warning("[MCW] GenStep_MCWAllianceOutpost: 找不到回收公司派系 " + RecycleFactionDefName);
                return;
            }

            PawnKindDef memberKind = DefDatabase<PawnKindDef>.GetNamed(MemberKindDefName, errorOnFail: false);
            PawnKindDef leaderKind = DefDatabase<PawnKindDef>.GetNamed(LeaderKindDefName, errorOnFail: false);

            if (memberKind != null)
            {
                SpawnAtEdge(map, recycleFaction, memberKind, AttackerMemberCount);
            }
            if (leaderKind != null)
            {
                SpawnAtEdge(map, recycleFaction, leaderKind, AttackerLeaderCount);
            }
        }

        private static void SpawnAtEdge(Map map, Faction faction, PawnKindDef kind, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                    kind, faction, PawnGenerationContext.NonPlayer, forceGenerateNewPawn: true));
                if (pawn == null)
                {
                    continue;
                }

                IntVec3 cell = CellFinder.RandomEdgeCell(map);
                if (!cell.IsValid)
                {
                    cell = CellFinder.RandomCell(map);
                }
                GenSpawn.Spawn(pawn, cell, map);
            }
        }
    }
}
