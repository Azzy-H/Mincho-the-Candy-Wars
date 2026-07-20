using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MinchoCandyWars.Buildings
{
    /// <summary>
    /// Harmony 补丁：让 Building_MintIceCreamCommsConsole 在通讯台系统（CommsConsoleUtility）中被正确识别。
    /// JobDriver 的类型兼容通过 XML Patch 替换 driverClass 实现，无需反射/Transpiler。
    /// </summary>
    [HarmonyPatch]
    public static class Patch_MintIceCreamCommsConsole
    {
        /// <summary>
        /// Postfix：让 CommsConsoleUtility.PlayerHasPoweredCommsConsole 也识别我们的通讯台
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CommsConsoleUtility), nameof(CommsConsoleUtility.PlayerHasPoweredCommsConsole),
            typeof(Map))]
        public static void PlayerHasPoweredCommsConsole_Postfix(Map map, ref bool __result)
        {
            if (__result)
            {
                return;
            }

            foreach (Building_MintIceCreamCommsConsole item in
                     map.listerBuildings.AllBuildingsColonistOfClass<Building_MintIceCreamCommsConsole>())
            {
                if (item.Faction == Faction.OfPlayer)
                {
                    CompPowerTrader comp = item.TryGetComp<CompPowerTrader>();
                    if (comp == null || comp.PowerOn)
                    {
                        __result = true;
                        return;
                    }
                }
            }
        }
    }
}
