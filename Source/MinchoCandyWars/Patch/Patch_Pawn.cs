using HarmonyLib;
using Verse;

namespace MinchoCandyWars.Patch;

[HarmonyPatch(typeof(Pawn), nameof(Pawn.GetDisabledWorkTypes))]
internal static class Patch_Pawn
{
    [HarmonyPostfix]
    public static void Postfix_GetDisabledWorkTypes(Pawn __instance)
        => __instance.GetComp<CompMinchoCore>()?.TryRemoveDisabledWorkTypes();
}

// Research benches and techprints must see the same intellectual work capability.
[HarmonyPatch(typeof(Pawn), nameof(Pawn.CombinedDisabledWorkTags), MethodType.Getter)]
internal static class SmartMinchoResearchTag
{
    private static void Postfix(Pawn __instance, ref WorkTags __result)
    {
        if (__instance.GetComp<CompMinchoCore>()?.Active == true)
            __result &= ~WorkTags.Intellectual;
    }
}
