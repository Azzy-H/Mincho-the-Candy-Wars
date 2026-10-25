using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace MinchoCandyWars.Review;

/// <summary>
/// 回收公司部队：身上有瘫痪枪、且当前目标可被麻痹时，优先用枪而不是普通攻击。
///
/// 独立的 ThinkNode，不再补丁原版 <c>JobGiver_AIFightEnemy.TryGiveJob</c>。
/// 注入点是 ThinkTreeDef 的 <c>insertTag</c>（见 1.7/Defs/ThinkTreeDefs/MCW_ParalyzerThinkTree.xml），
/// 挂在原版给 modder 预留的 <c>Humanlike_PostMentalState</c> 钩子上。
/// 该钩子位于「精神崩溃 → 领主职责（LordDuty）」之间，也就是战斗职责之前，
/// 所以这里的判定先于原版 JobGiver_AIFightEnemies 执行；返回 null 时自然落回原版逻辑。
/// </summary>
public class JobGiver_UseParalyzer : ThinkNode_JobGiver
{
    protected override Job? TryGiveJob(Pawn pawn)
    {
        if (pawn.Downed || pawn.WorkTagIsDisabled(WorkTags.Violent)) return null;

        Thing target = pawn.mindState.enemyTarget;
        if (!ParalyzerRules.CanParalyzePawn(target)) return null;
        if (target is Pawn victim && victim.Downed) return null;
        if (!target.HostileTo(pawn)) return null;

        Apparel? item = pawn.apparel?.WornApparel.FirstOrDefault(a => a.def == MCW_DefOf.MCW_Paralyzer);
        Verb? verb = item?.GetComp<CompApparelReloadable>()?.VerbTracker.PrimaryVerb;
        if (verb == null || !verb.Available() || !verb.CanHitTarget(target) || !verb.ValidateTarget(target, false))
        {
            return null;
        }

        Job job = JobMaker.MakeJob(JobDefOf.UseVerbOnThing, target);
        job.verbToUse = verb;
        return job;
    }
}
