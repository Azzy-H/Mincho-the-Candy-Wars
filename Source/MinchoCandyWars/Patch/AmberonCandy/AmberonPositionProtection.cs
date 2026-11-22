using HarmonyLib;
using RimWorld;
using Verse;

namespace MinchoCandyWars.Patch;

[HarmonyPatch(typeof(Pawn), nameof(Pawn.PreApplyDamage))]
internal static class AmberonPositionProtection
{
    private static bool Prefix(Pawn __instance, ref bool absorbed)
    {
        if (!__instance.Spawned || __instance.Faction == null)
            return true;

        foreach (var shield in __instance.Map.listerThings.ThingsOfDef(MCW_ThingDefOf.MCW_AmberonPositionShield))
        {
            if (shield.Faction != __instance.Faction || !shield.Position.InHorDistOf(__instance.Position, 4f))
                continue;

            absorbed = true;
            return false;
        }
        return true;
    }
}
