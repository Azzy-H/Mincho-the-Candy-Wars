using HarmonyLib;
using RimWorld;
using Verse;

namespace MinchoCandyWars.Patch.SkillRecord
{
    [HarmonyPatch(typeof(RimWorld.SkillRecord), nameof(RimWorld.SkillRecord.LearnRateFactor))]
    internal static class Patch_SkillRecord_LearnRateFactor
    {
        public static void Postfix(RimWorld.SkillRecord __instance, bool direct, ref float __result)
        {
            StatDef? statDef = GetSkillLearningFactorStat(__instance.def);
            if(statDef != null)
            {
                __result *= __instance.Pawn.GetStatValue(statDef);
            }
        }

        private static StatDef? GetSkillLearningFactorStat(SkillDef skillDef)
        {
            //if (skillDef == SkillDefOf.Animals) return MCW_DefOf.MCW_AnimalsLearningFactor;
            if (skillDef == SkillDefOf.Artistic) return MCW_DefOf.MCW_ArtisticLearningFactor;
            if (skillDef == SkillDefOf.Construction) return MCW_DefOf.MCW_ConstructionLearningFactor;
            if (skillDef == SkillDefOf.Cooking) return MCW_DefOf.MCW_CookingLearningFactor;
            if (skillDef == SkillDefOf.Crafting) return MCW_DefOf.MCW_CraftingLearningFactor;
            if (skillDef == SkillDefOf.Intellectual) return MCW_DefOf.MCW_IntellectualLearningFactor;
            if (skillDef == SkillDefOf.Medicine) return MCW_DefOf.MCW_MedicalLearningFactor;
            if (skillDef == SkillDefOf.Melee) return MCW_DefOf.MCW_MeleeLearningFactor;
            if (skillDef == SkillDefOf.Mining) return MCW_DefOf.MCW_MiningLearningFactor;
            if (skillDef == SkillDefOf.Plants) return MCW_DefOf.MCW_PlantsLearningFactor;
            if (skillDef == SkillDefOf.Shooting) return MCW_DefOf.MCW_ShootingLearningFactor;
            if (skillDef == SkillDefOf.Social) return MCW_DefOf.MCW_SocialLearningFactor;
            return null;
        }
    }
}