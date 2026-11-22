using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MinchoCandyWars.Patch;

// Filter the health UI only; keep apparel render nodes and saved Hediff instances intact.
[HarmonyPatch(typeof(HealthCardUtility), "VisibleHediffs")]
static class EquipmentVisualHealthRows
{
    static IEnumerable<Hediff> Postfix(IEnumerable<Hediff> __result, bool ___showAllHediffs)
    {
        foreach (var hediff in __result)
            if (___showAllHediffs || hediff.def.defName is not
                ("MCW_MechaArmor_AddonHediff" or "MCW_MechaWings_Visual"))
                yield return hediff;
    }
}
