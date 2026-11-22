using HarmonyLib;
using RimWorld;
using Verse;

namespace MinchoCandyWars.Patch;

[HarmonyPatch(typeof(StorytellerComp), "IncidentChanceFinal")]
internal static class Patch_QiaomingIncidentChance
{
    private static void Postfix(IncidentDef def, ref float __result)
    {
        if (Find.Storyteller?.def != MCW_DefOf.MCW_Qiaoming)
            return;

        if (def.modContentPack?.PackageIdPlayerFacing.ToLowerInvariant() is
            "mincho.the.candy.wars" or
            "sutsutman.minchothemintchocoslimeharver" or
            "sutsutman.marilyntheminchoworshipperwitchharver")
            __result *= 2f;
    }
}

