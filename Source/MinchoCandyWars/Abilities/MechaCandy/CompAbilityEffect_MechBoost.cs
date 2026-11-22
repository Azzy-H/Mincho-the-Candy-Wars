using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace MinchoCandyWars.Abilities.MechaCandy;

public class CompAbilityEffect_MechBoost : CompAbilityEffect
{
    public override bool Valid(LocalTargetInfo target, bool throwMessages = false) =>
        target.Pawn is { Spawned: true, Dead: false, Downed: false } host &&
        host.RaceProps.IsMechanoid && host.Faction == parent.pawn.Faction &&
        !host.health.hediffSet.HasHediff(MCW_HediffDefOf.MCW_MechBoost);

    public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest) => Valid(target);

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        if (!Valid(target)) return;
        var pilot = parent.pawn;
        var boost = (Hediff_MechBoost)target.Pawn.health.AddHediff(MCW_HediffDefOf.MCW_MechBoost);
        // Keep already-drafted subjects while transferring the controller into the host.
        pilot.DeSpawn(DestroyMode.WillReplace);
        boost.GetDirectlyHeldThings().TryAddOrTransfer(pilot);
        pilot.mechanitor?.Notify_BandwidthChanged();
        boost.EnsureHostAbility();
    }
}

public class Hediff_MechBoost : Hediff, IThingHolder
{
    ThingOwner<Thing> occupants = null!;
    public Pawn? Pilot => occupants?.OfType<Pawn>().FirstOrDefault();
    public static Pawn? HostFor(Pawn pilot) =>
        pilot.ParentHolder is Hediff_MechBoost boost && boost.pawn is { Dead: false, Downed: false } host && host.MapHeld != null
            ? host : null;

    public IThingHolder ParentHolder => pawn;
    public ThingOwner GetDirectlyHeldThings() => occupants;
    public void GetChildHolders(List<IThingHolder> children) =>
        ThingOwnerUtility.AppendThingHoldersFromThings(children, occupants);
    public override void PostMake()
    {
        base.PostMake();
        occupants = new ThingOwner<Thing>(this, false, LookMode.Deep);
    }
    public override void ExposeData()
    {
        base.ExposeData();
        // Preserve the existing save key.
        Scribe_Deep.Look(ref occupants, "innerContainers", this);
    }
    public void EnsureHostAbility()
    {
        if (pawn.def != MCW_ThingDefOf.MCW_Mech_Golem || Pilot == null) return;
        var jump = MCW_DefOf.MCW_BoostGolemJump;
        pawn.abilities ??= new Pawn_AbilityTracker(pawn);
        if (pawn.abilities.GetAbility(jump) == null)
            pawn.abilities.GainAbility(jump);
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        EnsureHostAbility();
        yield return new Command_Action
        {
            defaultLabel = "MinchoCandyWars.MechBoostExitLabel".Translate(),
            defaultDesc = "MinchoCandyWars.MechBoostExitDesc".Translate(),
            icon = ContentFinder<Texture2D>.Get("UI/Gizmos/Abilities/Mecha/MCW_MechBoostExit"),
            action = Exit
        };
        if (Pilot?.mechanitor is { } tracker)
            foreach (Gizmo gizmo in tracker.GetGizmos())
                yield return gizmo;
    }
    public void Exit()
    {
        if (pawn.MapHeld == null) return;
        Pawn? pilot = Pilot;
        occupants.TryDropAll(pawn.PositionHeld, pawn.MapHeld, ThingPlaceMode.Near);
        pilot?.mechanitor?.Notify_BandwidthChanged();
    }
    public override void PostRemoved()
    {
        Exit();
        base.PostRemoved();
    }
    public override void PostTickInterval(int delta)
    {
        base.PostTickInterval(delta);
        if (pawn.Downed && pawn.Spawned) Exit();
    }
    public override void Notify_PawnCorpseSpawned()
    {
        base.Notify_PawnCorpseSpawned();
        Exit();
    }
    public override bool ShouldRemove => occupants != null && occupants.Count == 0;
}

public class BoostGolemJumpCheck : CompAbilityEffect
{
    public override bool Valid(LocalTargetInfo target, bool throwMessages = false) =>
        parent.pawn.health.hediffSet.GetFirstHediffOfDef(MCW_HediffDefOf.MCW_MechBoost) is Hediff_MechBoost { Pilot: not null };

    public override bool GizmoDisabled(out string reason)
    {
        reason = "MinchoCandyWars.Reason.GolemPilotRequired".Translate().RawText;
        return !Valid(default(LocalTargetInfo));
    }
}

