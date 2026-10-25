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
        if (pawn.def != MCW_DefOf.Mincho_ThingDef || pawn.Dead) return false;
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
        HediffDef def = MCW_DefOf.MCW_Paralyzed047;
        Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(def);
        if (existing != null) pawn.health.RemoveHediff(existing);
        pawn.health.AddHediff(def);
    }
}
