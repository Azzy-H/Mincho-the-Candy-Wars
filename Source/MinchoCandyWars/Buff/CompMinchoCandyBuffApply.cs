using MinchoCandyWars.Data;
using RimWorld;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Verse;
using UnityEngine;

namespace MinchoCandyWars.Buff
{
    public class CompMinchoCandyBuffApply : ThingComp
    {
        //Pawn_NeedsTracker.SetInitialLevels会读取stat，此时PostSpawnSetup未执行
        private CompMinchoCore? cachedCoreComp;
        private bool effectsDirty = true;
        public Pawn pawn => (Pawn)parent;
        public CandyTypeStage? BodyStage => cachedCoreComp?.BodyStage;
        public CandyTypeStage? CoreStage => cachedCoreComp?.CoreStage;
        private HashSet<HediffDef> hediffDefsNeedToApply = new HashSet<HediffDef>();
        public HashSet<HediffDef> HediffDefsNeedToApplyForReading => hediffDefsNeedToApply;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            cachedCoreComp = pawn.GetComp<CompMinchoCore>();
            
            effectsDirty = true;
        }

        
        public override void ReceiveCompSignal(string signal)
        {
            if (signal == CompSignals.MinchoCoreDataChange)
            {
                effectsDirty = true;
            }
        }
        public override void CompTick()
        {
            base.CompTick();
            if (effectsDirty)
            {
                RefreshEffectsCache();
                effectsDirty = false;
            }
        }

        // 重新建立当前生效的缓存。
        private void RefreshEffectsCache()
        {
            hediffDefsNeedToApply.Clear();
            if (BodyStage?.gainHediffs != null)
            {
                hediffDefsNeedToApply.AddRange(BodyStage.gainHediffs);
            }
            if (CoreStage?.gainHediffs != null)
            {
                hediffDefsNeedToApply.AddRange(CoreStage.gainHediffs);
            }
            foreach (HediffDef hediffDef in hediffDefsNeedToApply)
            {
                if (!pawn.health.hediffSet.HasHediff(hediffDef))
                {
                    Hediff hediff = HediffMaker.MakeHediff(hediffDef, pawn);
                    pawn.health.AddHediff(hediff);
                }
            }
            effectsDirty = false;
        }

        // 应用属性乘数
        public override float GetStatFactor(StatDef stat)
        {
            return BodyStage?.statFactors.GetStatFactorFromList(stat) ?? 1f *
                CoreStage?.statFactors.GetStatFactorFromList(stat) ?? 1f;
        }

        // 应用属性偏移
        public override float GetStatOffset(StatDef stat)
        {
            return BodyStage?.statOffsets.GetStatOffsetFromList(stat) ?? 0f +
                CoreStage?.statOffsets.GetStatOffsetFromList(stat) ?? 0f;
        }

        // 输出 stat 面板中的来源说明。
        public override void GetStatsExplanation(StatDef stat, StringBuilder sb, string whitespace = "")
        {
            if (cachedCoreComp == null) return;
            StringBuilder stringBuilder = new StringBuilder();
            float bodyStatOffset = cachedCoreComp.BodyStage?.statOffsets.GetStatOffsetFromList(stat) ?? 0f;
            if (!Mathf.Approximately(bodyStatOffset, 0f))
            {
                stringBuilder.AppendLine(whitespace + "    " + "MinchoCandyWars.Buff.BodyOffset".Translate() + ": " + stat.Worker.ValueToString(bodyStatOffset, finalized: false, ToStringNumberSense.Offset));
            }
            float coreStatOffset = cachedCoreComp.CoreStage?.statOffsets.GetStatOffsetFromList(stat) ?? 0f;
            if (!Mathf.Approximately(coreStatOffset, 0f))
            {
                stringBuilder.AppendLine(whitespace + "    " + "MinchoCandyWars.Buff.CoreOffset".Translate() + ": " + stat.Worker.ValueToString(coreStatOffset, finalized: false, ToStringNumberSense.Offset));
            }
            float bodyStatFactor = cachedCoreComp.BodyStage?.statFactors.GetStatFactorFromList(stat) ?? 1f;
            if (!Mathf.Approximately(bodyStatFactor, 1f))
            {
                stringBuilder.AppendLine(whitespace + "    " + "MinchoCandyWars.Buff.BodyFactor".Translate() + ": " + stat.Worker.ValueToString(bodyStatFactor, finalized: false, ToStringNumberSense.Factor));
            }
            float coreStatFactor = cachedCoreComp.CoreStage?.statFactors.GetStatFactorFromList(stat) ?? 1f;
            if (!Mathf.Approximately(coreStatFactor, 1f))
            {
                stringBuilder.AppendLine(whitespace + "    " + "MinchoCandyWars.Buff.CoreFactor".Translate() + ": " + stat.Worker.ValueToString(coreStatFactor, finalized: false, ToStringNumberSense.Factor));
            }
            if (stringBuilder.Length != 0)
            {
                sb.AppendLine(whitespace + "MinchoCandyWars.Buff".Translate() + ":");
                sb.Append(stringBuilder.ToString());
            }
        }

    }

    public class CompProperties_MinchoCandyBuffApply : CompProperties
    {
        public CompProperties_MinchoCandyBuffApply()
        {
            this.compClass = typeof(CompMinchoCandyBuffApply);
        }
    }
}
