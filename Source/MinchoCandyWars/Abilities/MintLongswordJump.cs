using RimWorld;
using Verse;
using Verse.AI;

namespace MinchoCandyWars.Abilities;

// Native jump movement; keep the enemy as the job target instead of accepting ground cells.
public sealed class MintLongswordJump : Verb_CastAbilityJump
{
    public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo target) =>
        CasterPawn?.equipment?.Primary?.def == MCW_ThingDefOf.MCW_MintChocolateLongsword &&
        target.Pawn is { Spawned: true, Dead: false } enemy && enemy.Map == CasterPawn.Map &&
        enemy.HostileTo(CasterPawn) && base.CanHitTargetFrom(root, target);

    public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true) =>
        CasterPawn != null && CanHitTargetFrom(CasterPawn.Position, target) &&
        base.ValidateTarget(target, showMessages);

    public override void OrderForceTarget(LocalTargetInfo target)
    {
        if (!ValidateTarget(target)) return;
        var job = JobMaker.MakeJob(JobDefOf.CastJump, target);
        job.verbToUse = this;
        CasterPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
    }
}
