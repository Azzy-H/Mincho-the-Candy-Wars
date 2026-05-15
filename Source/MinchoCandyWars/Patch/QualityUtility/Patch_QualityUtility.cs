using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace MinchoCandyWars.Patch.QualityUtility
{
    [HarmonyPatch(typeof(RimWorld.QualityUtility), nameof(RimWorld.QualityUtility.GenerateQualityCreatedByPawn), new[] { typeof(Pawn), typeof(SkillDef), typeof(bool) })]
    internal static class Patch_QualityUtility_GenerateQualityCreatedByPawn
    {
        public static void Postfix(Pawn pawn, ref QualityCategory __result)
        {
            if (pawn?.health?.hediffSet == null)
            {
                return;
            }

            if (pawn.health.hediffSet.HasHediff(MCW_DefOf.MCW_ProductionQualityBoost))
            {
                __result = AddLevels(__result, 1);
            }
        }

        private static QualityCategory AddLevels(QualityCategory quality, int levels)
        {
            return (QualityCategory)Mathf.Min((int)quality + levels, (int)QualityCategory.Legendary);
        }
    }
}