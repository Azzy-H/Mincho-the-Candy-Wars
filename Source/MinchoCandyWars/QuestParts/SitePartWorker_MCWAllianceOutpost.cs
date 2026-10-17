using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;
using Verse.Grammar;

namespace MinchoCandyWars
{
    /// <summary>
    /// questB5 的「珉巧同盟据点」站点部件：
    /// 生成据点里的珉巧居民（5~10 殖民者、2~5 守护者、1 长老、1 皇族、
    /// 2 精英骑士、2~5 太空难民）到 part.things，由配套 GenStep 放上地图。
    ///
    /// 地图边缘的回收部队（38 成员 + 2 队长）由 GenStep_MCWAllianceOutpost 生成。
    /// </summary>
    public class SitePartWorker_MCWAllianceOutpost : SitePartWorker
    {
        /// <summary>要生成到据点里的珉巧（kindDefName, 最小数量, 最大数量）。</summary>
        private static readonly (string defName, int min, int max)[] Guards =
        {
            ("Mincho_Colonist", 5, 10),
            ("Mincho_Defender", 2, 5),
            ("Mincho_Elder", 1, 1),
            ("Mincho_Royal", 1, 1),
            ("Mincho_Knight_Elite", 2, 2),
            ("Mincho_SpaceRefugee", 2, 5),
        };

        public override void Notify_GeneratedByQuestGen(
            SitePart part,
            Slate slate,
            List<Rule> outExtraDescriptionRules,
            Dictionary<string, string> outExtraDescriptionConstants)
        {
            base.Notify_GeneratedByQuestGen(part, slate, outExtraDescriptionRules, outExtraDescriptionConstants);

            part.things = new ThingOwner<Pawn>(part, oneStackOnly: true);

            Faction faction = Faction.OfPlayer;

            foreach ((string defName, int min, int max) in Guards)
            {
                PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamed(defName, errorOnFail: false);
                if (kind == null)
                {
                    Log.Warning("[MCW] SitePartWorker_MCWAllianceOutpost: 找不到 PawnKindDef " + defName);
                    continue;
                }

                int count = Rand.RangeInclusive(min, max);
                for (int i = 0; i < count; i++)
                {
                    Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                        kind, faction, PawnGenerationContext.NonPlayer, forceGenerateNewPawn: true));
                    if (pawn != null)
                    {
                        part.things.TryAdd(pawn);
                    }
                }
            }

            // 让任务文案能引用据点里的一位珉巧
            Pawn first = null;
            foreach (Thing t in part.things)
            {
                if (t is Pawn p)
                {
                    first = p;
                    break;
                }
            }
            if (first != null)
            {
                slate.Set("outpostMincho", first);
            }
        }
    }
}
