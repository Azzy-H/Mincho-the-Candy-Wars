using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace MinchoCandyWars.QuestNodes
{
    /// <summary>
    /// 在「站点地图」上生成援军（questB3 用）。
    ///
    /// 背景：
    ///   vanilla 的 QuestNode_PawnsArrive 只能作用于玩家主地图（slate["map"]），
    ///   而本 mod 需要把援军放到玩家正在进攻的站点地图上。
    ///   这里直接构造 QuestPart_PawnsArrive，并把 mapParent 指向 Site，
    ///   即可让援军出现在站点地图（Site 是 MapParent 的子类，'HasMap' 时有效）。
    ///
    /// 生成内容（按 N = count 计算）：
    ///   · 2N 名珉巧雇佣兵（Mincho_Mercenary）
    ///   · 1N 名珉巧精英骑士（Mincho_Knight_Elite）
    ///   · 1 名智慧珉巧（MCW_SmartMincho，CompMinchoCore.Active = true）
    /// 全部加入玩家阵营（友军）。
    ///
    /// XML 用法（放在 QuestNode_Signal(site.MapGenerated) → QuestNode_Delay 之后）：
    ///   &lt;li Class="MinchoCandyWars.QuestNode_SpawnSiteReinforcements"&gt;
    ///     &lt;site&gt;$site&lt;/site&gt;
    ///     &lt;count&gt;3&lt;/count&gt;
    ///   &lt;/li&gt;
    /// </summary>
    public class QuestNode_SpawnSiteReinforcements : QuestNode
    {
        /// <summary>目标站点（必须已有地图）。</summary>
        public SlateRef<Site> site;

        /// <summary>
        /// 规模 N：2N 雇佣兵、1N 精英骑士、1 智慧珉巧。
        /// 显式填 &gt; 0 时使用固定值；留空（默认 -1）时按玩家财富自动计算。
        /// </summary>
        public SlateRef<int> count = -1;

        /// <summary>自动计算规模时：玩家每积累多少财富 +1 规模。</summary>
        public SlateRef<float> wealthPerUnit = 12000f;

        /// <summary>自动计算规模时的下限。</summary>
        public SlateRef<int> minCount = 2;

        /// <summary>自动计算规模时的上限。</summary>
        public SlateRef<int> maxCount = 6;

        /// <summary>可选前缀（信件标题/正文）。</summary>
        public SlateRef<string> letterLabel = "珉巧援军到达";
        public SlateRef<string> letterText = "珉巧同盟的援军赶到了战场，她们决定帮你一把。";

        private const string MercenaryKindDefName = "Mincho_Mercenary";
        private const string KnightKindDefName = "Mincho_Knight_Elite";
        private const string SmartKindDefName = "MCW_SmartMincho";

        protected override bool TestRunInt(Slate slate)
        {
            return site.GetValue(slate) != null;
        }

        /// <summary>
        /// 解析本次援军规模 N：
        ///   · XML 显式指定 count &gt; 0 → 用固定值
        ///   · 否则按玩家主地图财富计算：floor(财富 / wealthPerUnit) + 1，再夹在 [minCount, maxCount]
        /// </summary>
        private int ResolveCount(Slate slate)
        {
            int fixedCount = count.GetValue(slate);
            if (fixedCount > 0)
            {
                return fixedCount;
            }

            Map playerMap = Find.AnyPlayerHomeMap;
            float wealth = playerMap?.wealthWatcher?.WealthTotal ?? 0f;
            int n = Mathf.FloorToInt(wealth / wealthPerUnit.GetValue(slate)) + 1;
            return Mathf.Clamp(n, minCount.GetValue(slate), maxCount.GetValue(slate));
        }

        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            Site siteValue = site.GetValue(slate);
            if (siteValue == null)
            {
                Log.Warning("[MCW] QuestNode_SpawnSiteReinforcements: site 为空，跳过");
                return;
            }

            int n = ResolveCount(slate);

            List<Pawn> reinforcements = new List<Pawn>();

            PawnKindDef mercenaryKind = DefDatabase<PawnKindDef>.GetNamed(MercenaryKindDefName, errorOnFail: false);
            PawnKindDef knightKind = DefDatabase<PawnKindDef>.GetNamed(KnightKindDefName, errorOnFail: false);
            PawnKindDef smartKind = DefDatabase<PawnKindDef>.GetNamed(SmartKindDefName, errorOnFail: false);

            // 2N 珉巧雇佣兵
            if (mercenaryKind != null)
            {
                for (int i = 0; i < 2 * n; i++)
                {
                    Pawn p = SpawnPawn(mercenaryKind);
                    if (p != null) reinforcements.Add(p);
                }
            }

            // 1N 珉巧精英骑士
            if (knightKind != null)
            {
                for (int i = 0; i < n; i++)
                {
                    Pawn p = SpawnPawn(knightKind);
                    if (p != null) reinforcements.Add(p);
                }
            }

            // 1 名智慧珉巧
            if (smartKind != null)
            {
                Pawn smart = SpawnPawn(smartKind);
                if (smart != null)
                {
                    CompMinchoCore core = smart.GetComp<CompMinchoCore>();
                    if (core != null && !core.Active)
                    {
                        core.Active = true;
                    }
                    reinforcements.Add(smart);
                }
            }

            if (reinforcements.Count == 0)
            {
                Log.Warning("[MCW] QuestNode_SpawnSiteReinforcements: 没有生成任何援军（kindDef 缺失？）");
                return;
            }

            // 用 vanilla 的到达部件，但把 mapParent 指向站点 —— 这样援军会出现在站点地图上
            string signal = QuestGen.slate.Get<string>("inSignal");
            QuestPart_PawnsArrive part = new QuestPart_PawnsArrive();
            part.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(signal) ?? signal;
            part.mapParent = siteValue;
            part.pawns.AddRange(reinforcements);
            part.joinPlayer = true;
            part.sendStandardLetter = true;
            part.customLetterLabel = letterLabel.GetValue(slate);
            part.customLetterText = letterText.GetValue(slate);
            part.addPawnsToLookTargets = true;

            QuestGen.quest.AddPart(part);
        }

        private static Pawn SpawnPawn(PawnKindDef kind)
        {
            return PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, Faction.OfPlayer));
        }
    }
}
