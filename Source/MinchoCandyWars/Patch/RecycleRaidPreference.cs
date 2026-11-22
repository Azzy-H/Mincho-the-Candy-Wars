using HarmonyLib;
using RimWorld;
using Verse;

namespace MinchoCandyWars.Patch;

[HarmonyPatch(typeof(PawnGroupMakerUtility), nameof(PawnGroupMakerUtility.TryGetRandomFactionForCombatPawnGroupWeighted))]
static class RecycleRaidPreference
{
    static readonly Func<float, Predicate<Faction>?, bool, bool, bool, bool, List<Faction>> UsableFactions =
        AccessTools.MethodDelegate<Func<float, Predicate<Faction>?, bool, bool, bool, bool, List<Faction>>>(
            AccessTools.Method(typeof(PawnGroupMakerUtility), "UsableFactions"));

    static bool Prefix(IncidentParms parms, ref Faction faction, Predicate<Faction>? validator,
        bool allowNonHostileToPlayer, bool allowHidden, bool allowDefeated, bool allowNonHumanlike, ref bool __result)
    {
        if (parms.target is not Map map || !map.mapPawns.AllPawnsSpawned.Any(p =>
            p.def == MCW_ThingDefOf.Mincho_ThingDef && !p.Dead && p.Faction == Faction.OfPlayer)) return true;
        float points = parms.points > 0 ? parms.points : 999999f;
        var candidates = UsableFactions(points, validator, allowNonHostileToPlayer, allowHidden, allowDefeated, allowNonHumanlike);
        __result = candidates.TryRandomElementByWeight(f =>
            f.def.RaidCommonalityFromPoints(points) *
            (f == parms.target.StoryState.lastRaidFaction ? 0.4f : 1f) *
            (f.def == MCW_DefOf.MCW_RecycleCorp_Faction ? 3f : 1f), out faction);
        return false;
    }
}
