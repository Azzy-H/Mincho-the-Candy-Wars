using HarmonyLib;
using UnityEngine;
using Verse;

namespace MinchoCandyWars.Review;

/// <summary>糖果手杖固定白色，不跟随材质着色。</summary>
[HarmonyPatch(typeof(BuildableDef), nameof(BuildableDef.GetColorForStuff))]
public static class CandyCaneColor
{
    public static void Postfix(BuildableDef __instance, ref Color __result)
    {
        if (__instance == MCW_DefOf.MCW_MW_CandyCane) __result = Color.white;
    }
}
