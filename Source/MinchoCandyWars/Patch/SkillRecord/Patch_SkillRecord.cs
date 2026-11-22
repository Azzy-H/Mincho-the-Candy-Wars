using HarmonyLib;
using RimWorld;
using System.Text.RegularExpressions;
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

        internal static StatDef? GetSkillLearningFactorStat(SkillDef skillDef)
        {
            //if (skillDef == SkillDefOf.Animals) return MCW_StatDefOf.MCW_AnimalsLearningFactor;
            if (skillDef == SkillDefOf.Artistic) return MCW_StatDefOf.MCW_ArtisticLearningFactor;
            if (skillDef == SkillDefOf.Construction) return MCW_StatDefOf.MCW_ConstructionLearningFactor;
            if (skillDef == SkillDefOf.Cooking) return MCW_StatDefOf.MCW_CookingLearningFactor;
            if (skillDef == SkillDefOf.Crafting) return MCW_StatDefOf.MCW_CraftingLearningFactor;
            if (skillDef == SkillDefOf.Intellectual) return MCW_StatDefOf.MCW_IntellectualLearningFactor;
            if (skillDef == SkillDefOf.Medicine) return MCW_StatDefOf.MCW_MedicalLearningFactor;
            if (skillDef == SkillDefOf.Melee) return MCW_StatDefOf.MCW_MeleeLearningFactor;
            if (skillDef == SkillDefOf.Mining) return MCW_StatDefOf.MCW_MiningLearningFactor;
            if (skillDef == SkillDefOf.Plants) return MCW_StatDefOf.MCW_PlantsLearningFactor;
            if (skillDef == SkillDefOf.Shooting) return MCW_StatDefOf.MCW_ShootingLearningFactor;
            if (skillDef == SkillDefOf.Social) return MCW_StatDefOf.MCW_SocialLearningFactor;
            return null;
        }
    }

    [HarmonyPatch(typeof(SkillUI), "GetSkillDescription")]
    internal static class Patch_SkillUI_GetSkillDescription
    {
        static void Postfix(RimWorld.SkillRecord sk, ref string __result)
        {
            if (sk.TotallyDisabled) return;
            var stat = Patch_SkillRecord_LearnRateFactor.GetSkillLearningFactorStat(sk.def);
            if (stat == null) return;
            float factor = sk.Pawn.GetStatValue(stat);
            if (UnityEngine.Mathf.Approximately(factor, 1f)) return;
            string detail = "  - " + stat.LabelCap + ": ×" + factor.ToStringPercent();
            string label = "LearningSpeed".Translate().RawText.Trim();
            var lines = __result.Split('\n').ToList();
            int header = lines.FindIndex(line =>
                Regex.IsMatch(Regex.Replace(line, "<[^>]+>", ""),
                    "^" + Regex.Escape(label) + @"\s*[:：]"));
            if (header < 0)
            {
                __result += "\n\n" + detail;
                return;
            }

            int separator = lines[header].IndexOfAny([':', '：']);
            int closingTag = lines[header].IndexOf("</color>", separator, StringComparison.Ordinal);
            int valueStart = closingTag < 0 ? separator + 1 : closingTag + "</color>".Length;
            while (valueStart < lines[header].Length && char.IsWhiteSpace(lines[header][valueStart]))
                valueStart++;
            lines[header] = lines[header][..valueStart] + sk.LearnRateFactor(false).ToStringPercent();
            int last = header + 1;
            while (last < lines.Count && lines[last].StartsWith("  - ", StringComparison.Ordinal))
                last++;
            lines.Insert(last, detail);
            __result = string.Join("\n", lines);
        }
    }
}