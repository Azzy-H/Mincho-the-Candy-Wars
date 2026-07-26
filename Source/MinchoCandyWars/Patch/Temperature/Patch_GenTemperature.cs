using HarmonyLib;
using MinchoCandyWars.Buildings;
using Verse;

namespace MinchoCandyWars.Patch.Temperature
{
    [HarmonyPatch(typeof(GenTemperature))]
    internal static class Patch_GenTemperature
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(GenTemperature.TryGetTemperatureForCell))]
        private static bool Prefix_TryGetTemperatureForCell(IntVec3 c, Map map, ref float tempResult, ref bool __result)
        {
            if (map == null || !c.InBounds(map))
            {
                return true;
            }

            MapComponent_CellTemperatureOverrideRegistry registry = map.GetComponent<MapComponent_CellTemperatureOverrideRegistry>();
            if (registry.TryGetTemperatureOverride(c, out float overrideTemperature))
            {
                tempResult = overrideTemperature;
                __result = true;
                return false;
            }

            return true;
        }
    }

}
