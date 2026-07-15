using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace MinchoCandyWars.Abilities.GanLuCandy
{
    /// <summary>
    /// 珉巧敷药 —— 直接复刻 Coagulate 逻辑，包扎目标所有伤口。
    /// </summary>
    public class CompAbilityEffect_MinchoBandage : CompAbilityEffect
    {
        private new CompProperties_AbilityMinchoBandage Props => (CompProperties_AbilityMinchoBandage)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn pawn = target.Pawn;
            if (pawn == null)
                return;

            int num = 0;
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            for (int i = hediffs.Count - 1; i >= 0; i--)
            {
                if ((hediffs[i] is Hediff_Injury || hediffs[i] is Hediff_MissingPart) && hediffs[i].TendableNow())
                {
                    hediffs[i].Tended(Props.tendQualityRange.RandomInRange, Props.tendQualityRange.TrueMax, 1);
                    num++;
                }
            }

            if (num > 0)
            {
                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "NumWoundsTended".Translate(num), 3.65f);
            }
            FleckMaker.AttachedOverlay(pawn, FleckDefOf.FlashHollow, Vector3.zero, 1.5f);
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn pawn = target.Pawn;
            if (pawn == null)
                return false;
            if (!AbilityUtility.ValidateHasTendableWound(pawn, throwMessages, parent))
                return false;
            return true;
        }
    }
}