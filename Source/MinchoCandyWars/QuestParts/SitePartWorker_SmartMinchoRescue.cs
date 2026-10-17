using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;
using Verse.Grammar;

namespace MinchoCandyWars
{
    /// <summary>
    /// questB2 的囚禁点站点部件：在生成的世界地图站点里放置一位
    /// 「被关押的智慧珉巧」（CompMinchoCore.Active = true 的 MCW_SmartMincho）。
    ///
    /// 复制自 vanilla 的 SitePartWorker_PrisonerWillingToJoin，
    /// 唯一改动是把「随机人类囚犯」换成本 mod 的智慧珉巧。
    /// 玩家到达站点地图后，囚犯由 GenStep_PrisonerWillingToJoin（沿用 vanilla）
    /// 放置在地图中心附近；解救后通过 QuestNode_AddPawnReward 加入殖民地。
    ///
    /// TODO（后续）：根据财富值调整看守数量（目前由 PrisonerRescueQuestThreat
    /// 标签的 Outpost / Turrets 站点部件提供随机敌人，先沿用原版海盗）。
    /// </summary>
    public class SitePartWorker_SmartMinchoRescue : SitePartWorker
    {
        private const string SmartMinchoKindDefName = "MCW_SmartMincho";

        public override void Notify_GeneratedByQuestGen(
            SitePart part,
            Slate slate,
            List<Rule> outExtraDescriptionRules,
            Dictionary<string, string> outExtraDescriptionConstants)
        {
            base.Notify_GeneratedByQuestGen(part, slate, outExtraDescriptionRules, outExtraDescriptionConstants);

            PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamed(SmartMinchoKindDefName, errorOnFail: false);
            if (kind == null)
            {
                Log.Warning("[MCW] SitePartWorker_SmartMinchoRescue: 找不到 PawnKindDef " + SmartMinchoKindDefName);
                return;
            }

            Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                kind,
                part.site.Faction,
                PawnGenerationContext.NonPlayer,
                forceGenerateNewPawn: true));

            if (pawn == null)
            {
                return;
            }

            // 智慧珉巧：核心激活
            CompMinchoCore core = pawn.GetComp<CompMinchoCore>();
            if (core != null && !core.Active)
            {
                core.Active = true;
            }

            // 被关押状态（囚犯）
            if (pawn.guest != null)
            {
                pawn.guest.SetGuestStatus(part.site.Faction, GuestStatus.Prisoner);
            }

            part.things = new ThingOwner<Pawn>(part, oneStackOnly: true);
            part.things.TryAdd(pawn);

            PawnRelationUtility.Notify_PawnsSeenByPlayer(
                Gen.YieldSingle(pawn), out var pawnRelationsInfo,
                informEvenIfSeenBefore: true, writeSeenPawnsNames: false);

            string output = pawnRelationsInfo.NullOrEmpty()
                ? ""
                : ("\n\n" + "PawnHasTheseRelationshipsWithColonists".Translate(pawn.LabelShort, pawn)
                    + "\n\n" + pawnRelationsInfo);

            slate.Set("prisoner", pawn);
            outExtraDescriptionRules.Add(new Rule_String("prisonerFullRelationInfo", output));
        }

        public override string GetPostProcessedThreatLabel(Site site, SitePart sitePart)
        {
            string text = base.GetPostProcessedThreatLabel(site, sitePart);
            if (sitePart.things != null && sitePart.things.Any)
            {
                text = text + ": " + sitePart.things[0].LabelShortCap;
            }
            if (site.HasWorldObjectTimeout)
            {
                text += " (" + "DurationLeft".Translate(site.WorldObjectTimeoutTicksLeft.ToStringTicksToPeriod()) + ")";
            }
            return text;
        }
    }
}
