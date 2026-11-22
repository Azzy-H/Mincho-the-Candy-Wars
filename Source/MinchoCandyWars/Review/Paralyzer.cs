using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace MinchoCandyWars.Review;

/// <summary>瘫痪枪的目标判定：只有「珉巧、没带糖果、活着」的目标才会被麻痹。</summary>
public static class ParalyzerRules
{
    public static bool CanParalyzePawn(Thing target)
    {
        if (target is not Pawn pawn) return false;
        if (pawn.def != MCW_ThingDefOf.Mincho_ThingDef || pawn.Dead) return false;
        // 已经拥有糖饰（糖果类型）的珉巧不会被麻痹。
        return pawn.GetComp<CompMinchoCore>()?.CurrentCandyType == null;
    }
}

/// <summary>瘫痪枪的施放动词。</summary>
public class Verb_Paralyzer : Verb_CastTargetEffect
{
    public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        => ParalyzerRules.CanParalyzePawn(target.Thing) && base.ValidateTarget(target, showMessages);

    /// <summary>蓄力结束后再判一次：期间换了糖饰或目标死亡，不该白白消耗一次充能。</summary>
    protected override bool TryCastShot()
        => ParalyzerRules.CanParalyzePawn(currentTarget.Thing) && base.TryCastShot();
}

/// <summary>命中效果：施加麻痹 Hediff，重复命中时刷新而非叠加。</summary>
public class ParalyzerEffect : CompTargetEffect
{
    public override bool CanApplyOn(Thing target) => ParalyzerRules.CanParalyzePawn(target);

    public override void DoEffectOn(Pawn user, Thing target)
    {
        if (!CanApplyOn(target)) return;

        var pawn = (Pawn)target;
        HediffDef def = MCW_HediffDefOf.MCW_Paralyzed047;
        Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(def);
        if (existing != null) pawn.health.RemoveHediff(existing);
        pawn.health.AddHediff(def);
    }
}

// 只在回收队员的战斗决策中使用瘫痪器，不改变玩家手动施放和糖饰免疫规则。
[HarmonyLib.HarmonyPatch(typeof(JobGiver_AIFightEnemy), "TryGiveJob")]
static class RecycleParalyzerAI
{
    static void Postfix(Pawn pawn, ref Job __result)
    {
        if (pawn.Faction?.def != MCW_DefOf.MCW_RecycleCorp_Faction || !pawn.Spawned || pawn.Downed || !pawn.RaceProps.Humanlike) return;
        foreach (var item in pawn.apparel.WornApparel)
        {
            if (item.def != MCW_ThingDefOf.MCW_Paralyzer) continue;
            var comp = item.TryGetComp<CompApparelReloadable>();
            if (comp == null || !comp.CanBeUsed(out _)) continue;
            var verb = comp.VerbTracker.PrimaryVerb;
            Pawn? target = null;
            float nearest = float.MaxValue;
            foreach (var candidate in pawn.Map.mapPawns.AllPawnsSpawned)
            {
                if (candidate.Downed || !candidate.HostileTo(pawn) || !ParalyzerRules.CanParalyzePawn(candidate)
                    || candidate.health.hediffSet.HasHediff(MCW_HediffDefOf.MCW_Paralyzed047)) continue;
                float distance = candidate.Position.DistanceToSquared(pawn.Position);
                if (distance >= nearest || !verb.CanHitTarget(candidate)) continue;
                nearest = distance; target = candidate;
            }
            if (target == null) continue;
            __result = JobMaker.MakeJob(JobDefOf.UseVerbOnThing, target);
            __result.verbToUse = verb;
            return;
        }
    }
}
