using HarmonyLib;
using UnityEngine;
using RimWorld;
using Verse;

namespace MinchoCandyWars.Patch;

// The fabric palette is painted into the qipao texture; metal is only a recipe material.
[HarmonyPatch(typeof(Apparel), nameof(Apparel.DrawColor), MethodType.Getter)]
internal static class QipaoMaterialColor
{
    private static void Postfix(Apparel __instance, ref Color __result)
    {
        if (__instance.def == MCW_ThingDefOf.MCW_Qipao)
            __result = Color.white;
    }
}