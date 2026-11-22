using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MinchoCandyWars.Recreation;

public class Building_CocoaBath : Building
{
    public Pawn? Bather;
    public int Phase;
    private const string TextureRoot = "Things/Building/CocoaBath/";
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref Bather, "bather");
        Scribe_Values.Look(ref Phase, "bathPhase");
    }
    public bool InUse => Bather?.CurJob?.targetA.Thing == this && Bather.CurJob.def == MCW_DefOf.MCW_UseCocoaBath;
    public int DisplayPhase => InUse ? Phase : 0;
    public int BubbleFrame => (Find.TickManager.TicksGame / 12) % 2;
    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        bool horizontal = Rotation.IsHorizontal;
        string axis = horizontal ? "Horizontal" : "Vertical";
        var size = horizontal ? new Vector2(2.4f, 1.2f) : new Vector2(1.2f, 2.4f);
        GraphicDatabase.Get<Graphic_Single>(TextureRoot + axis + DisplayPhase,
            ShaderDatabase.Cutout, size, Color.white).Draw(drawLoc, Rot4.North, this);
        if (DisplayPhase != 2) return;
        var foam = GraphicDatabase.Get<Graphic_Single>(TextureRoot + "Bubble" + (BubbleFrame + (horizontal ? 2 : 0)),
            ShaderDatabase.Transparent, size, Color.white);
        // The supplied foam images use the same canvas as the tub. Keep them
        // aligned, below the exposed head rather than over the whole pawn.
        drawLoc.y = AltitudeLayer.Pawn.AltitudeFor() - .01f;
        foam.Draw(drawLoc, Rot4.North, this);
    }
    public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
    {
        foreach (var option in base.GetFloatMenuOptions(selPawn)) yield return option;
        if (selPawn.needs?.joy == null || !selPawn.CanReserveAndReach(this, PathEndMode.OnCell, Danger.Some)) yield break;
        yield return new FloatMenuOption("MCW_CocoaBathUse".Translate(), () =>
            selPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(MCW_DefOf.MCW_UseCocoaBath, this)));
    }
}

public class JoyGiver_CocoaBath : JoyGiver
{
    public override Job? TryGiveJob(Pawn pawn)
    {
        if (pawn.needs?.joy == null) return null;
        var bath = pawn.Map.listerThings.ThingsOfDef(MCW_ThingDefOf.MCW_CocoaBath)
            .Where(t => t.Faction == pawn.Faction && !t.IsForbidden(pawn) && pawn.CanReserveAndReach(t, PathEndMode.OnCell, Danger.None))
            .OrderBy(t => t.Position.DistanceToSquared(pawn.Position)).FirstOrDefault();
        return bath == null ? null : JobMaker.MakeJob(def.jobDef, bath);
    }
}

public class JobDriver_CocoaBath : JobDriver
{
    private int soakTicks;
    private Building_CocoaBath Bath => (Building_CocoaBath)job.targetA.Thing;
    public override Rot4 ForcedLayingRotation => Bath.Phase == 2 ? (Bath.Rotation.IsHorizontal ? Rot4.East : Rot4.North) : Rot4.Invalid;
    public override Vector3 ForcedBodyOffset
    {
        get { if (Bath.Phase != 2) return Vector3.zero; var offset = Bath.TrueCenter() - pawn.Position.ToVector3Shifted(); offset.y = 0f; return offset; }
    }
    public override bool TryMakePreToilReservations(bool errorOnFailed) => pawn.Reserve(Bath, job, 1, -1, null, errorOnFailed);
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref soakTicks, "soakTicks");
    }
    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        AddFinishAction(condition =>
        {
            // 结束即退出“游泳”渲染状态。
            job.swimming = false;
            if (soakTicks >= 300 && pawn.needs?.mood != null)
                pawn.needs.mood.thoughts.memories.TryGainMemory(MCW_DefOf.MCW_CocoaBathRelaxed);
            if (Bath.Bather == pawn) { Bath.Bather = null; Bath.Phase = 0; }
        });
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
        var fill = Toils_General.Wait(180, TargetIndex.A);
        fill.initAction = () => { Bath.Bather = pawn; Bath.Phase = 1; };
        yield return fill;
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell);
        var soak = Toils_General.Wait(2500, TargetIndex.A);
        soak.initAction = () => { Bath.Bather = pawn; Bath.Phase = 2; pawn.jobs.posture = PawnPosture.LayingOnGroundNormal; };
        // job.swimming 不进存档，所以每 tick 重设一次。原版据 Pawn.Swimming 在绘制时
        // 清除 Headgear/Clothes 并置 NoBody，同时把头部下移 0.5——泡澡“只露头”全靠它。
        soak.tickAction = () =>
        {
            job.swimming = true;
            soakTicks++;
            JoyUtility.JoyTickCheckEnd(pawn, 1, JoyTickFullJoyAction.EndJob, 1f, Bath);
        };
        yield return soak;
    }
}
