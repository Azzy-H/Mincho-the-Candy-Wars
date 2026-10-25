using HarmonyLib;
using UnityEngine;
using Verse;

namespace MinchoCandyWars.Review
{
    /// <summary>子个体专用武器固定白色，不跟随材质着色（保留材质本身的属性）。</summary>
    [HarmonyPatch(typeof(BuildableDef), nameof(BuildableDef.GetColorForStuff))]
    public static class MaterialTint
    {
        public static void Postfix(BuildableDef __instance, ref Color __result)
        {
            if (__instance.HasModExtension<ChildMechWeaponExtension>()) __result = Color.white;
        }
    }
}
