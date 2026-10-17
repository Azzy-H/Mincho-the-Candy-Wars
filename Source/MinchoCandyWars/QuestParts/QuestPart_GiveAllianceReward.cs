using System.Collections.Generic;
using MinchoCandyWars.StellarisEvent;
using RimWorld;
using Verse;

namespace MinchoCandyWars.QuestParts
{
    /// <summary>
    /// 珉巧同盟任务完成奖励（B2~B5 通用）：
    ///   · 沁心糖好感度变化（对应任务的点数奖励：B2 +20 / B3 +10 / B4 +30 / B5 +50）
    ///   · 终局进度 flag（MCW_EndgameProgress）+1
    ///   · 物品奖励：任务生成时从奖励表里随机抽 4 种，任务结束时发放。
    ///
    /// 奖励表（数量按玩家财富换算，参考普通任务奖励的数量级）：
    ///   · 薄荷冰淇淋      = 钢铁数量 × 4
    ///   · 薄荷冰淇淋砖块  = 钢铁数量 × 2
    ///   · 巧克力棒        = 钢铁数量 × 0.5
    ///   · 稻米            = 钢铁数量 × 2
    ///   · 蘑菇布          = 等价值（参考超织物数量）
    ///   · 钢铁            = 基准数量
    ///   · 玻璃钢          = 等价值（参考玻璃钢数量）
    ///   · β型纳米塑形液   = 1
    ///   · 超凡胶囊        = 1
    ///
    /// 任务生成时（QuestNode.GiveAllianceReward.RunInt）就已决定抽到哪 4 种、各自多少，
    /// 并存进本 QuestPart；任务结束时（Cleanup）才真正发到地图上。
    /// </summary>
    public class QuestPart_GiveAllianceReward : QuestPart
    {
        public const string EndgameProgressFlag = "MCW_EndgameProgress";

        /// <summary>好感度变化的派系（沁心糖所在的珉巧同盟）。</summary>
        public FactionDef factionDef;

        /// <summary>好感度增减量。</summary>
        public int goodwillChange;

        /// <summary>终局进度增量。</summary>
        public int endgameProgress = 1;

        /// <summary>可选：任务结束时停用该派系（终局 questFF1 用）。</summary>
        public FactionDef disableFactionDef;

        /// <summary>抽中的奖励物品（任务生成时确定）。</summary>
        public List<ThingDef> rewardDefs = new List<ThingDef>();

        /// <summary>与 rewardDefs 一一对应的数量。</summary>
        public List<int> rewardCounts = new List<int>();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref factionDef, "factionDef");
            Scribe_Values.Look(ref goodwillChange, "goodwillChange");
            Scribe_Values.Look(ref endgameProgress, "endgameProgress", 1);
            Scribe_Defs.Look(ref disableFactionDef, "disableFactionDef");
            Scribe_Collections.Look(ref rewardDefs, "rewardDefs", LookMode.Def);
            Scribe_Collections.Look(ref rewardCounts, "rewardCounts", LookMode.Value);
        }

        public override void Cleanup()
        {
            base.Cleanup();
            ApplyReward();
        }

        private void ApplyReward()
        {
            // ── 1. 好感度 ──
            if (goodwillChange != 0 && factionDef != null)
            {
                Faction faction = Find.FactionManager.FirstFactionOfDef(factionDef);
                if (faction != null && faction != Faction.OfPlayer && faction.HasGoodwill)
                {
                    faction.TryAffectGoodwillWith(Faction.OfPlayer, goodwillChange);
                }
            }

            // ── 2. 终局进度 ──
            if (endgameProgress != 0)
            {
                StellarisEventFlag flag = StellarisEventFlagManager.GetFlag(EndgameProgressFlag);
                float current = flag?.floatValue ?? 0f;
                StellarisEventFlagManager.SetFlag(EndgameProgressFlag, current + endgameProgress);
            }

            // ── 3. 停用派系（终局 questFF1） ──
            if (disableFactionDef != null)
            {
                Faction faction = Find.FactionManager.FirstFactionOfDef(disableFactionDef);
                if (faction != null)
                {
                    faction.defeated = true;
                    foreach (Map map in Find.Maps)
                    {
                        foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned.ToList())
                        {
                            if (pawn.Faction == faction && !pawn.Dead)
                            {
                                pawn.Destroy(DestroyMode.Vanish);
                            }
                        }
                    }
                }
            }

            // ── 4. 物品奖励 ──
            GiveItemRewards();
        }

        private void GiveItemRewards()
        {
            Map map = Find.AnyPlayerHomeMap;
            if (map == null)
            {
                return;
            }

            IntVec3 dropCell = DropCellFinder.RandomDropSpot(map);

            for (int i = 0; i < rewardDefs.Count && i < rewardCounts.Count; i++)
            {
                ThingDef def = rewardDefs[i];
                int count = rewardCounts[i];
                if (def == null || count <= 0)
                {
                    continue;
                }

                // 分批放置，避免单格堆叠超出上限
                int remaining = count;
                while (remaining > 0)
                {
                    int stack = System.Math.Min(remaining, def.stackLimit);
                    Thing thing = ThingMaker.MakeThing(def);
                    thing.stackCount = stack;
                    GenPlace.TryPlaceThing(thing, dropCell, map, ThingPlaceMode.Near);
                    remaining -= stack;
                }
            }
        }
    }
}
