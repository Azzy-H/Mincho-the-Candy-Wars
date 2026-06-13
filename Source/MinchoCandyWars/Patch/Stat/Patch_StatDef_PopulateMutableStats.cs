using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MinchoCandyWars.Patch.Stat
{
    [HarmonyPatch(typeof(StatDef))]
    internal static class Patch_StatDef_PopulateMutableStats
    {
        [HarmonyPostfix]
        [HarmonyPatch("PopulateMutableStats")]
        private static void Postfix_PopulateMutableStats()
        {
            Log.Message("MCW: Patching StatDef.PopulateMutableStats to include stats from CandyTypeStages.");
            FieldInfo? mutableStatsField = typeof(StatDef).GetField("mutableStats", BindingFlags.Static | BindingFlags.NonPublic);
            if (mutableStatsField == null)
            {
                Log.Error("MCW: Unable to find StatDef.mutableStats.");
                return;
            }

            HashSet<StatDef>? mutableStats = mutableStatsField.GetValue(null) as HashSet<StatDef>;
            if (mutableStats == null)
            {
                Log.Error("MCW: StatDef.mutableStats is null.");
                return;
            }

            foreach (CandyTypeDef candyTypeDef in DefDatabase<CandyTypeDef>.AllDefsListForReading)
            {
                if (candyTypeDef == null)
                {
                    continue;
                }

                AddStageStats(candyTypeDef.coreStages, mutableStats);
                AddStageStats(candyTypeDef.bodyStages, mutableStats);
            }
        }

        private static void AddStageStats(List<CandyTypeStage> stages, HashSet<StatDef> mutableStats)
        {
            if (stages == null)
            {
                return;
            }
            StringBuilder stringBuilder = new StringBuilder($"MCW: Added stats from CandyTypeStage to StatDef.mutableStats.");
            foreach (CandyTypeStage stage in stages)
            {
                if (stage == null)
                {
                    continue;
                }

                AddStats(stage.statOffsets, mutableStats, stringBuilder);
                AddStats(stage.statFactors, mutableStats, stringBuilder);
            }
            Log.Message(stringBuilder.ToString());
        }

        private static void AddStats(List<StatModifier>? statModifiers, HashSet<StatDef> mutableStats, StringBuilder stringBuilder)
        {
            if (statModifiers == null)
            {
                return;
            }
            foreach (StatModifier statModifier in statModifiers)
            {
                if (statModifier?.stat != null)
                {
                    mutableStats.Add(statModifier.stat);
                    stringBuilder.AppendLine($"- {statModifier.stat.defName}");
                }
            }
        }
    }
}