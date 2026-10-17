using System.Collections.Generic;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace MinchoCandyWars.QuestNodes
{
    /// <summary>
    /// 给任务挂上「珉巧同盟任务奖励」：
    ///   · 任务生成时（本节点 RunInt）按玩家财富算出奖励，随机抽取 4 种物品；
    ///   · 任务结束时由 QuestPart_GiveAllianceReward.Cleanup 结算好感度 / 终局进度 / 物品。
    ///
    /// XML 用法：
    ///   &lt;li Class="MinchoCandyWars.QuestNode_GiveAllianceReward"&gt;
    ///     &lt;factionDef&gt;MCW_Alliance_Faction&lt;/factionDef&gt;
    ///     &lt;goodwillChange&gt;20&lt;/goodwillChange&gt;
    ///     &lt;endgameProgress&gt;1&lt;/endgameProgress&gt;
    ///   &lt;/li&gt;
    /// </summary>
    public class QuestNode_GiveAllianceReward : QuestNode
    {
        /// <summary>好感度变化的派系。</summary>
        public SlateRef<FactionDef> factionDef;

        /// <summary>好感度增减量（对应任务的点数奖励）。</summary>
        public SlateRef<int> goodwillChange = 0;

        /// <summary>终局进度增量。</summary>
        public SlateRef<int> endgameProgress = 1;

        /// <summary>可选：任务结束时停用该派系（终局 questFF1 用）。</summary>
        public SlateRef<FactionDef> disableFactionDef;

        /// <summary>奖励表：随机抽取的种类数量。</summary>
        public SlateRef<int> pickCount = 4;

        /// <summary>财富换算系数：rewardValue = wealth * this（夹在 minValue~maxValue）。</summary>
        public SlateRef<float> wealthFactor = 0.015f;
        public SlateRef<float> minValue = 300f;
        public SlateRef<float> maxValue = 3000f;

        private const string MintchocoDefName = "Mincho_Mintchoco";
        private const string MintchocoBlocksDefName = "Mincho_BlocksMintchoco";
        private const string ChocoStickDefName = "Mincho_ChocoStick";
        private const string RiceDefName = "RawRice";
        private const string MushroomClothDefName = "Hyperweave";
        private const string BetaFluidDefName = "MCW_Beta_NanoShapingFluid";
        private const string ArchiteCapsuleDefName = "ArchiteCapsule";

        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }

        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;

            QuestParts.QuestPart_GiveAllianceReward part = new QuestParts.QuestPart_GiveAllianceReward
            {
                factionDef = factionDef.GetValue(slate),
                goodwillChange = goodwillChange.GetValue(slate),
                endgameProgress = endgameProgress.GetValue(slate),
                disableFactionDef = disableFactionDef.GetValue(slate),
            };

            BuildRewards(slate, part);

            QuestGen.quest.AddPart(part);
        }

        /// <summary>按财富算出奖励表，随机取 pickCount 种写入 part。</summary>
        private void BuildRewards(Slate slate, QuestParts.QuestPart_GiveAllianceReward part)
        {
            Map playerMap = Find.AnyPlayerHomeMap;
            float wealth = playerMap?.wealthWatcher?.WealthTotal ?? 0f;
            float baseValue = Mathf.Clamp(
                wealth * wealthFactor.GetValue(slate),
                minValue.GetValue(slate),
                maxValue.GetValue(slate));

            float steelPrice = ThingDefOf.Steel.BaseMarketValue;
            int steelCount = steelPrice > 0f ? Mathf.RoundToInt(baseValue / steelPrice) : 0;

            // 奖励表（defName, 数量）
            List<(string defName, int count)> table = new List<(string, int)>
            {
                (MintchocoDefName, steelCount * 4),          // 冰淇淋 = 钢铁 × 4
                (MintchocoBlocksDefName, steelCount * 2),    // 冰淇淋砖块 = 钢铁 × 2
                (ChocoStickDefName, Mathf.RoundToInt(steelCount * 0.5f)), // 巧克力棒 = 钢铁 × 0.5
                (RiceDefName, steelCount * 2),               // 稻米 = 钢铁 × 2
                (MushroomClothDefName, ValueToCount(baseValue, MushroomClothDefName)), // 蘑菇布 = 超织物数量
                ("Steel", steelCount),                       // 钢铁 = 基准
                ("Plasteel", ValueToCount(baseValue, "Plasteel")), // 玻璃钢 = 玻璃钢数量
                (BetaFluidDefName, 1),                       // β型纳米塑形液
                (ArchiteCapsuleDefName, 1),                  // 超凡胶囊
            };

            int pickCountValue = Mathf.Clamp(pickCount.GetValue(slate), 1, table.Count);
            for (int i = 0; i < pickCountValue; i++)
            {
                int idx = Rand.Range(0, table.Count);
                (string defName, int count) entry = table[idx];
                table.RemoveAt(idx);

                ThingDef def = DefDatabase<ThingDef>.GetNamed(entry.defName, errorOnFail: false);
                if (def == null || entry.count <= 0)
                {
                    continue;
                }

                part.rewardDefs.Add(def);
                part.rewardCounts.Add(entry.count);
            }
        }

        /// <summary>按等价值换算某物品的数量。</summary>
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
