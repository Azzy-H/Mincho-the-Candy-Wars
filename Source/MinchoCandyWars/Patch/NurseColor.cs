using HarmonyLib;
using UnityEngine;
using Verse;

namespace MinchoCandyWars;

// White multiplication preserves the source texture's colors and black outline.
[HarmonyPatch(typeof(BuildableDef), nameof(BuildableDef.GetColorForStuff))]
static class MaterialPreview
{
    static void Postfix(BuildableDef __instance, ref Color __result)
    {
        if (__instance.defName is "MCW_NurseOutfit" or "MCW_NurseCap" or "MCW_MechaArmor" or "MCW_HoloVisor" or "MCW_GorgeousDress" or "MCW_BrownDress" or "MCW_CookiesShield" or "MCW_TowerShield" or "MCW_MW_CandyCane") __result = Color.white;
    }
}

[HarmonyPatch(typeof(ThingWithComps), nameof(ThingWithComps.DrawColor), MethodType.Getter)]
static class WornAndItemColor
{
    static void Postfix(ThingWithComps __instance, ref Color __result)
    {
        if (__instance.def.defName is "MCW_NurseOutfit" or "MCW_NurseCap" or "MCW_MechaArmor" or "MCW_HoloVisor" or "MCW_GorgeousDress" or "MCW_BrownDress" or "MCW_CookiesShield" or "MCW_TowerShield" or "MCW_MW_CandyCane") __result = Color.white;
    }
}


