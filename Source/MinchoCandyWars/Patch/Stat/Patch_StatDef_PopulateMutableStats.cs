using System;
using System.Collections.Generic;
using System.Reflection;
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

            foreach (CandyTypeStage stage in stages)
            {
                if (stage == null)
                {
                    continue;
                }

                AddStats(stage.statOffsets, mutableStats);
                AddStats(stage.statFactors, mutableStats);
            }
        }

        private static void AddStats(List<StatModifier>? statModifiers, HashSet<StatDef> mutableStats)
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
                    Log.Message($"MCW: Added stat {statModifier.stat.defName} from CandyTypeStage to StatDef.mutableStats.");
                }
            }
        }
    }
}