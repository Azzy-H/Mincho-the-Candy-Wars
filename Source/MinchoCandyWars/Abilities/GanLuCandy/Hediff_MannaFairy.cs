using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace MinchoCandyWars.Abilities.GanLuCandy
{
    /// <summary>
    /// 甘露妖精 —— CandyTypeDef body5 的被动 hediff。
    /// LevelMet 仅用于决定 Buff 是否生成；Buff 生成后不做检测，由 Buff 自己查询 IsValid。
    /// 未派遣：自身享受 Buff (再生 + 闪避 + 护盾)
    /// 已派遣：目标爱人享受 Buff，自身失去
    /// </summary>
    [StaticConstructorOnStartup]
    public class Hediff_MannaFairy : HediffWithComps
    {
        public Pawn? dispatchedTo;

        private Hediff? buffHediff;

        private const int RequiredCore = 3;
        private const int RequiredBody = 5;

        private static readonly Texture2D RecallCommand = ContentFinder<Texture2D>.Get("UI/Commands/SplitCaravan");

        private CompMinchoCore? _cachedCore;

        private CompMinchoCore Core
        {
            get
            {
                if (_cachedCore == null)
                    _cachedCore = pawn.GetComp<CompMinchoCore>();
                return _cachedCore;
            }
        }

        public bool LevelMet
        {
            get
            {
                if (Core == null) return false;
                return Core.MinchoCoreGrade >= RequiredCore && Core.MinchoBodyGrade >= RequiredBody;
            }
        }

        /// <summary>
        /// Buff 查询此属性决定是否 ShouldRemove。
        /// 已派遣时始终有效（无条件通过）；未派遣时需等级达标。
        /// </summary>
        internal bool IsValid
        {
            get
            {
                if (pawn.Dead) return false;
                if (dispatchedTo != null) return true;
                return LevelMet;
            }
        }

        public override bool ShouldRemove
        {
            get
            {
                if (pawn.Dead) return true;
                if (!IsValid) return true;
                return false;
            }
        }

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            if (LevelMet)
                ApplySelfBuff();
        }

        public override void Tick()
        {
            if (!IsValid) return;

            base.Tick();

            if (!pawn.IsHashIntervalTick(60))
                return;

            if (dispatchedTo != null)
            {
                if (dispatchedTo.Dead || dispatchedTo.Destroyed || dispatchedTo.Map != pawn.Map)
                {
                    Recall();
                }
            }
        }

        public void DispatchTo(Pawn target)
        {
            if (!LevelMet) return;
            RemoveSelfBuff();
            dispatchedTo = target;
            ApplyTargetBuff();
        }

        public void Recall()
        {
            RemoveTargetBuff();
            dispatchedTo = null;
            ApplySelfBuff();
        }

        private void ApplySelfBuff()
        {
            Pawn host = pawn;
            if (host == null || host.Dead || !LevelMet) return;

            if (host.health.hediffSet.GetFirstHediffOfDef(MCW_DefOf.MCW_MannaFairyBuff) == null)
                buffHediff = host.health.AddHediff(MCW_DefOf.MCW_MannaFairyBuff);
        }

        private void RemoveSelfBuff()
        {
            Pawn host = pawn;
            if (host == null) return;

            if (buffHediff != null)
            {
                host.health.RemoveHediff(buffHediff);
                buffHediff = null;
            }
            else
            {
                Hediff? existing = host.health.hediffSet.GetFirstHediffOfDef(MCW_DefOf.MCW_MannaFairyBuff);
                if (existing != null) host.health.RemoveHediff(existing);
            }
        }

        private void ApplyTargetBuff()
        {
            if (dispatchedTo == null || dispatchedTo.Dead) return;

            if (dispatchedTo.health.hediffSet.GetFirstHediffOfDef(MCW_DefOf.MCW_MannaFairyBuff) == null)
                dispatchedTo.health.AddHediff(MCW_DefOf.MCW_MannaFairyBuff);
        }

        private void RemoveTargetBuff()
        {
            if (dispatchedTo == null) return;

            Hediff? existing = dispatchedTo.health.hediffSet.GetFirstHediffOfDef(MCW_DefOf.MCW_MannaFairyBuff);
            if (existing != null) dispatchedTo.health.RemoveHediff(existing);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            if (!IsValid) yield break;

            if (dispatchedTo != null)
            {
                yield return new Command_Action
                {
                    defaultLabel = "MannaFairyRecall".Translate(),
                    defaultDesc = "MannaFairyRecallDesc".Translate(dispatchedTo.LabelShort),
                    icon = RecallCommand,
                    action = Recall
                };
            }
        }

        public override void PostRemoved()
        {
            base.PostRemoved();
            RemoveSelfBuff();
            RemoveTargetBuff();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref dispatchedTo, "dispatchedTo");
        }
    }
}