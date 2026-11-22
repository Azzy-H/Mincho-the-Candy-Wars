using HarmonyLib;
using RimWorld;
using Verse;
using MinchoCandyWars.Abilities.MechaCandy;
using UnityEngine;

namespace MinchoCandyWars.Patch;

// Only the controller physically held by MechBoost uses the host as a command centre.
[HarmonyPatch(typeof(Pawn_MechanitorTracker), nameof(Pawn_MechanitorTracker.CanControlMechs), MethodType.Getter)]
static class BoostControllerAvailable
{
    static bool Prefix(Pawn ___pawn, ref AcceptanceReport __result)
    {
        if (Hediff_MechBoost.HostFor(___pawn) == null) return true;
        __result = !___pawn.Dead && !___pawn.Downed && !___pawn.IsPrisoner && !___pawn.InMentalState;
        return false;
    }
}

[HarmonyPatch(typeof(Pawn_MechanitorTracker), nameof(Pawn_MechanitorTracker.CanCommandTo))]
static class BoostCommandCentre
{
    static bool Prefix(Pawn ___pawn, LocalTargetInfo target, ref bool __result)
    {
        Pawn? host = Hediff_MechBoost.HostFor(___pawn);
        if (host == null) return true;
        __result = target.Cell.InBounds(host.MapHeld) &&
            (!target.HasThing || target.Thing.MapHeld == host.MapHeld) &&
            host.PositionHeld.DistanceToSquared(target.Cell) < 620.01f;
        return false;
    }
}

[HarmonyPatch(typeof(Pawn_MechanitorTracker), nameof(Pawn_MechanitorTracker.DrawCommandRadius))]
static class BoostCommandRing
{
    static bool Prefix(Pawn ___pawn)
    {
        Pawn? host = Hediff_MechBoost.HostFor(___pawn);
        if (host == null) return true;
        GenDraw.DrawRadiusRing(host.PositionHeld, 24.9f, Color.white);
        return false;
    }
}

[HarmonyPatch(typeof(MechanitorUtility), nameof(MechanitorUtility.CanDraftMech))]
static class BoostHostDraft
{
    static bool Prefix(Pawn mech, ref AcceptanceReport __result)
    {
        if (mech.health?.hediffSet.GetFirstHediffOfDef(MCW_HediffDefOf.MCW_MechBoost)
            is not Hediff_MechBoost { Pilot: { Dead: false, Downed: false } pilot }) return true;
        __result = mech.Faction == Faction.OfPlayer && !mech.Dead && !mech.Downed &&
            !pilot.IsPrisoner && !pilot.InMentalState &&
            mech.needs?.energy?.IsLowEnergySelfShutdown != true;
        return false;
    }
}

// The pilot is held off-map, so selection must request its command ring from the host or a subject.
[HarmonyPatch(typeof(Pawn), nameof(Pawn.DrawExtraSelectionOverlays))]
static class BoostSelectedCommandRing
{
    static void Postfix(Pawn __instance)
    {
        if (!__instance.Spawned || __instance.Map != Find.CurrentMap) return;
        var boost = __instance.health?.hediffSet.GetFirstHediffOfDef(MCW_HediffDefOf.MCW_MechBoost) as Hediff_MechBoost;
        Pawn? pilot = boost?.Pilot ?? __instance.GetOverseer();
        if (pilot?.mechanitor is { } tracker && Hediff_MechBoost.HostFor(pilot) != null)
            tracker.DrawCommandRadius();
    }
}
