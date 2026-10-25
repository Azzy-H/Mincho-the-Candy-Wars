using System.Collections.Generic;
using MinchoCandyWars.StellarisEvent;
using RimWorld;
using UnityEngine;
using Verse;

namespace MinchoCandyWars
{
    /// <summary>
    /// questB1：「收留珉巧」。
    ///
    /// 由事件 A1（收留珉巧请求）的「是」选项通过 Effect_FireIncident 触发。
    /// 内容：
    ///   1. 生成 3~7 名珉巧太空难民，直接加入玩家殖民地；
    ///   2. 10% 概率额外生成 1 名智慧珉巧（CompMinchoCore.Active = true）；
    ///   3. 奖励：智慧珉巧改造包或随机糖饰（50% / 50%），落在附近；
    ///   4. 沁心糖（珉巧同盟）好感度 +40；
    ///   5. 终局进度 flag +1（MCW_EndgameProgress）。
    ///
    /// TODO（后续）：
    ///   · 庇护期 5~25 天（期间提供冰淇淋的持续收益）
    ///   · 任务结束后 3~5 天触发随机 questA1~A5
    ///   · 终局进度到达 10 后 10 天触发 questF1
    /// </summary>
    public class IncidentWorker_RecruitMincho : IncidentWorker
    {
        private const string AllianceFactionDefName = "MCW_Alliance_Faction";
        private const string RefugeKindDefName = "Mincho_SpaceRefugee";
        private const string SmartKindDefName = "MCW_SmartMincho";
        private const string EndgameProgressFlag = "MCW_EndgameProgress";

        /// <summary>防止事件被重复打开时反复发放难民/奖励。</summary>
        private const string TriggeredFlag = "MCW_QuestB1_Triggered";

        /// <summary>可作为奖励的糖饰解锁物品。</summary>
        private static readonly string[] CandyItemDefNames =
        {
            "MCW_MechaCandyChangeItem",
            "MCW_NectarCandyChangeItem",
            "MCW_AmberonCandyChangeItem",
            "MCW_PrismCandyChangeItem",
            "MCW_LinglonCandyChangeItem",
        };

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return parms.target is Map map && map.mapPawns.FreeColonistsCount > 0;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (!(parms.target is Map map))
            {
                return false;
            }

            // 防止重复：事件可以被多次打开（"待定"后可重试），但任务只能触发一次
            if (StellarisEventFlagManager.HasFlag(TriggeredFlag))
            {
                return false;
            }
            StellarisEventFlagManager.SetFlag(TriggeredFlag, true);

            PawnKindDef refugeKind = DefDatabase<PawnKindDef>.GetNamed(RefugeKindDefName, errorOnFail: false);
            if (refugeKind == null)
            {
                Log.Warning("[MCW] IncidentWorker_RecruitMincho: 找不到 PawnKindDef " + RefugeKindDefName);
                return false;
            }

            // ── 1. 生成 3~7 名珉巧难民 ──
            int count = Rand.RangeInclusive(3, 7);
            List<Pawn> arrived = new List<Pawn>();
            for (int i = 0; i < count; i++)
            {
                Pawn pawn = SpawnRefugee(refugeKind, map);
                if (pawn != null)
                {
                    arrived.Add(pawn);
                }
            }

            // ── 2. 10% 概率额外一名智慧珉巧 ──
            bool smartArrived = false;
            if (Rand.Chance(0.1f))
            {
                PawnKindDef smartKind = DefDatabase<PawnKindDef>.GetNamed(SmartKindDefName, errorOnFail: false);
                if (smartKind != null)
                {
                    Pawn smart = SpawnRefugee(smartKind, map);
                    if (smart != null)
                    {
                        CompMinchoCore core = smart.GetComp<CompMinchoCore>();
                        if (core != null && !core.Active)
                        {
                            core.Active = true;
                        }
                        arrived.Add(smart);
                        smartArrived = true;
                    }
                }
            }

            if (arrived.Count == 0)
            {
                return false;
            }

            // ── 3. 奖励：智慧珉巧改造包或随机糖饰 ──
            GiveReward(map);

            // ── 4. 沁心糖好感度 +40 ──
            FactionDef allianceDef = DefDatabase<FactionDef>.GetNamed(AllianceFactionDefName, errorOnFail: false);
            Faction alliance = allianceDef == null ? null : Find.FactionManager.FirstFactionOfDef(allianceDef);
            if (alliance != null && alliance.HasGoodwill)
            {
                alliance.TryAffectGoodwillWith(Faction.OfPlayer, 40);
            }

            // ── 5. 终局进度 +1 ──
            IncrementEndgameProgress();

            // ── 6. 信件：「珉巧到达」 ──
            string letterText = smartArrived
                ? "MinchoCandyWars.UI.Letter.RecruitMincho.TextSmart".Translate()
                : "MinchoCandyWars.UI.Letter.RecruitMincho.Text".Translate();
            Find.LetterStack.ReceiveLetter("MinchoCandyWars.UI.Letter.RecruitMincho.Label".Translate(), letterText, LetterDefOf.PositiveEvent, arrived[0]);

            // ── 7. 解锁薄荷冰淇淋通讯台科技（初始任务完成后给予） ──
            ResearchProjectDef commsResearch = DefDatabase<ResearchProjectDef>.GetNamed("MCW_Research_CommsConsole", errorOnFail: false);
            if (commsResearch != null && !commsResearch.IsFinished)
            {
                Find.ResearchManager.FinishProject(commsResearch, doCompletionDialog: false, doCompletionLetter: true);
            }

            // ── 8. 提示玩家如何继续剧情：使用冰淇淋通讯台联络沁心糖 ──
            Find.LetterStack.ReceiveLetter(
                "MinchoCandyWars.UI.Letter.RecruitMincho.CommsLabel".Translate(),
                "MinchoCandyWars.UI.Letter.RecruitMincho.CommsText".Translate(),
                LetterDefOf.NeutralEvent);

            // ── 9. 任务完成：随机决定下一个可接的珉巧同盟任务 ──
            QuestParts.QuestPart_SetRandomNextQuest.SetRandomNextQuest();

            return true;
        }

        /// <summary>生成一名难民并放到地图边缘，直接归属玩家殖民地。</summary>
        private static Pawn SpawnRefugee(PawnKindDef kind, Map map)
        {
            Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, Faction.OfPlayer));
            if (pawn == null)
            {
                return null;
            }

            IntVec3 cell = CellFinder.RandomEdgeCell(map);
            if (!cell.IsValid)
            {
                cell = CellFinder.RandomCell(map);
            }
            GenSpawn.Spawn(pawn, cell, map);
            return pawn;
        }

        /// <summary>
        /// 发放任务奖励：从奖励表（9 种）里随机抽 4 种，数量按玩家财富换算。
        /// 与 QuestNode_GiveAllianceReward（B2~B5）使用同一套奖励表。
        /// </summary>
        private static void GiveReward(Map map)
        {
            float wealth = map.wealthWatcher?.WealthTotal ?? 0f;
            float baseValue = Mathf.Clamp(wealth * 0.015f, 300f, 3000f);
            float steelPrice = ThingDefOf.Steel.BaseMarketValue;
            int steelCount = steelPrice > 0f ? Mathf.RoundToInt(baseValue / steelPrice) : 0;

            // 奖励表（defName, 数量）
            List<(string defName, int count)> table = new List<(string, int)>
            {
                ("Mincho_Mintchoco", steelCount * 4),           // 冰淇淋 = 钢铁 × 4
                ("Mincho_BlocksMintchoco", steelCount * 2),     // 冰淇淋砖块 = 钢铁 × 2
                ("Mincho_ChocoStick", Mathf.RoundToInt(steelCount * 0.5f)), // 巧克力棒 = 钢铁 × 0.5
                ("RawRice", steelCount * 2),                    // 稻米 = 钢铁 × 2
                ("Hyperweave", ValueToCount(baseValue, "Hyperweave")),     // 蘑菇布 = 超织物数量
                ("Steel", steelCount),                          // 钢铁 = 基准
                ("Plasteel", ValueToCount(baseValue, "Plasteel")),         // 玻璃钢 = 玻璃钢数量
                ("MCW_Beta_NanoShapingFluid", 1),               // β型纳米塑形液
                ("ArchiteCapsule", 1),                          // 超凡胶囊
            };

            IntVec3 dropCell = DropCellFinder.RandomDropSpot(map);

            for (int i = 0; i < 4 && table.Count > 0; i++)
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

        /// <summary>终局进度 flag +1。</summary>
        private static void IncrementEndgameProgress()
        {
            StellarisEventFlag flag = StellarisEventFlagManager.GetFlag(EndgameProgressFlag);
            float current = flag?.floatValue ?? 0f;
            StellarisEventFlagManager.SetFlag(EndgameProgressFlag, current + 1f);
        }
    }
}
