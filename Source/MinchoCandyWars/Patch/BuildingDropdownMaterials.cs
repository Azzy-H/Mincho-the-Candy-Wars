using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace MinchoCandyWars.Patch;

// The native dropdown selects a designator directly, skipping Designator_Build's stuff menu.
[HarmonyPatch(typeof(Designator_Dropdown), "GetDesignatorSelectAction")]
static class BuildingDropdownMaterials
{
    static void Postfix(Event ev, Designator des, ref Action __result)
    {
        if (des is not Designator_Build build || build.PlacingDef is not ThingDef { MadeFromStuff: true } def
            || def.designatorDropdown?.defName?.StartsWith("MCW_", StringComparison.Ordinal) != true) return;
        var select = __result;
        __result = () =>
        {
            select();
            build.ProcessInput(ev);
        };
    }
}
