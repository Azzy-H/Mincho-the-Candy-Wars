using HarmonyLib;
using Verse;
using Verse.AI;

namespace MinchoCandyWars.Patch.Pathing
{
    /// <summary>
    /// 核心5「无视地形减速」：对持有 MCW_PrismTerrainIgnore 的珉巧，把地形路径开销归零。
    /// GetPawnCellBaseCostOverride 返回 0 时，PathGrid 会跳过 terrainDef.pathCost，但不可通行地形仍返回 10000。
    /// </summary>
    [HarmonyPatch(typeof(Pawn_PathFollower), nameof(Pawn_PathFollower.GetPawnCellBaseCostOverride))]
    internal static class Patch_PawnPathFollower_IgnoreTerrain
    {
        public static bool Prefix(Pawn pawn, IntVec3 c, ref int? __result)
        {
            if (pawn?.health?.hediffSet?.HasHediff(MCW_DefOf.MCW_PrismTerrainIgnore) == true)
            {
                __result = 0;
                return false;
            }
            return true;
        }
    }
}
