using RimWorld;
using Verse;
using Verse.AI;
using System.Collections.Generic;

namespace MinchoCandyWars.Anomaly;

public class BucketDiscoveryProperties : CompProperties
{
    public BucketDiscoveryProperties() => compClass = typeof(BucketDiscovery);
}

public class BucketDiscovery : ThingComp
{
    public override string GetDescriptionPart()
    {
        var notes = parent.TryGetComp<CompStudyUnlocks>();
        if (notes?.Completed == true)
            return "MinchoCandyWars.BucketDiscovery.Completed".Translate().RawText;
        return notes?.Progress > 0
            ? "MinchoCandyWars.BucketDiscovery.Started".Translate().RawText
            : null!;
    }

    int checkTicks;
    public override void CompTick() => CheckAttraction(1);
    public override void CompTickInterval(int delta) => CheckAttraction(delta);

    void CheckAttraction(int delta)
    {
        checkTicks += delta;
        if (checkTicks < 250)
            return;
        int elapsed = checkTicks;
        checkTicks = 0;
        if (!parent.Spawned || !Rand.MTBEventOccurs(0.5f, 60000f, elapsed))
            return;

        var candidates = parent.Map.mapPawns.FreeColonistsSpawned.FindAll(p =>
            !p.Dead && !p.Downed && !p.Drafted && p.Awake() && !p.InMentalState &&
            p.needs?.food != null && p.CurJob?.playerForced != true &&
            p.CurJobDef != JobDefOf.Ingest && !parent.IsForbidden(p) &&
            p.CanReserveAndReach(parent, PathEndMode.ClosestTouch, Danger.Some));
        if (!candidates.TryRandomElement(out Pawn pawn))
            return;

        var job = JobMaker.MakeJob(JobDefOf.Ingest, parent);
        job.count = 1;
        pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
    }
}
