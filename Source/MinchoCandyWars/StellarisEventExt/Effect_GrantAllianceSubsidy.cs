using System.Collections.Generic;
using MinchoCandyWars.StellarisEvent;
using RimWorld;
using UnityEngine;
using Verse;

namespace MinchoCandyWars
{
    /// <summary>
    /// 珉巧同盟资助：从奖励表里随机抽 pickCount 个物品，直接落在玩家主地图上，
    /// 并设置 cooldownTicks 的冷却 flag（默认 10 天）。
    ///
    /// 用于沁心糖好感度 ≥ 30 后的「领取资助」通讯事件。
    /// 配合 gizmo 的 flagAsCooldown（同一个 flag）即可实现按钮冷却显示。
    /// </summary>
    public class Effect_GrantAllianceSubsidy : StellarisEventEffect
    {
        /// <summary>抽取奖励的种类数量。</summary>
        public int pickCount = 2;

        /// <summary>冷却 flag 名（与 gizmo 的 flagAsCooldown 保持一致）。</summary>
        public string cooldownFlag = "MCW_AllianceSubsidyCooldown";

        /// <summary>冷却时长（tick），默认 10 天。</summary>
        public int cooldownTicks = 600000;

        /// <summary>奖励表（defName, 数量系数说明见 QuestPart_GiveAllianceReward）。</summary>
        private static readonly string[] Table =
        {
            "Mincho_Mintchoco",
            "Mincho_BlocksMintchoco",
            "Mincho_ChocoStick",
            "RawRice",
            "Hyperweave",
            "Steel",
            "Plasteel",
            "MCW_Beta_NanoShapingFluid",
            "ArchiteCapsule",
        };

        public override void Execute()
        {
            Map map = Find.AnyPlayerHomeMap;
            if (map == null)
            {
                Log.Warning("[MCW] Effect_GrantAllianceSubsidy: 没有玩家主地图，跳过");
                return;
            }

            float wealth = map.wealthWatcher?.WealthTotal ?? 0f;
            float baseValue = Mathf.Clamp(wealth * 0.015f, 300f, 3000f);
            float steelPrice = ThingDefOf.Steel.BaseMarketValue;
            int steelCount = steelPrice > 0f ? Mathf.RoundToInt(baseValue / steelPrice) : 0;

            List<(string defName, int count)> table = new List<(string, int)>
            {
                ("Mincho_Mintchoco", steelCount * 4),
                ("Mincho_BlocksMintchoco", steelCount * 2),
                ("Mincho_ChocoStick", Mathf.RoundToInt(steelCount * 0.5f)),
                ("RawRice", steelCount * 2),
                ("Hyperweave", ValueToCount(baseValue, "Hyperweave")),
                ("Steel", steelCount),
                ("Plasteel", ValueToCount(baseValue, "Plasteel")),
                ("MCW_Beta_NanoShapingFluid", 1),
                ("ArchiteCapsule", 1),
            };

            IntVec3 dropCell = DropCellFinder.RandomDropSpot(map);
            int picks = Mathf.Clamp(pickCount, 1, table.Count);

            for (int i = 0; i < picks && table.Count > 0; i++)
            {
                int idx = Rand.Range(0, table.Count);
                (string defName, int count) entry = table[idx];
                table.RemoveAt(idx);

                ThingDef def = DefDatabase<ThingDef>.GetNamed(entry.defName, errorOnFail: false);
                if (def == null || entry.count <= 0)
                {
                    continue;
                }

                int remaining = entry.count;
                while (remaining > 0)
                {
                    int stack = System.Math.Min(remaining, def.stackLimit);
                    Thing thing = ThingMaker.MakeThing(def);
                    thing.stackCount = stack;
                    GenPlace.TryPlaceThing(thing, dropCell, map, ThingPlaceMode.Near);
                    remaining -= stack;
                }
            }

            // 冷却 flag（浮点值 = 总时长，ticks = 剩余时间）
            if (!cooldownFlag.NullOrEmpty() && cooldownTicks > 0)
            {
                StellarisEventFlagManager.SetFlag(cooldownFlag, cooldownTicks, cooldownTicks);
            }

            Find.LetterStack.ReceiveLetter(
                "MinchoCandyWars.UI.Letter.Subsidy.Label".Translate(),
                "MinchoCandyWars.UI.Letter.Subsidy.Text".Translate(),
                LetterDefOf.PositiveEvent);
        }

        private static int ValueToCount(float value, string defName)
        {
            ThingDef def = DefDatabase<ThingDef>.GetNamed(defName, errorOnFail: false);
            if (def == null || def.BaseMarketValue <= 0f)
            {
                return 0;
            }
            return Mathf.RoundToInt(value / def.BaseMarketValue);
        }
    }
}
