using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace MinchoCandyWars.Buff
{
    public class Hediff_Buff : HediffWithComps
    {
        private CompMinchoCandyBuffApply? cachedBuffComp;
        public override bool ShouldRemove => base.ShouldRemove || cachedBuffComp == null || !cachedBuffComp.HediffDefsNeedToApplyForReading.Contains(def);

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            cachedBuffComp = pawn.GetComp<CompMinchoCandyBuffApply>();
        }
    }
}
