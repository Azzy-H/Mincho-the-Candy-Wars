using RimWorld;
using Verse;

namespace MinchoCandyWars.Abilities.NectarCandy
{
    /// <summary>
    /// 甘露妖精派遣 —— 将妖精派到爱人身边提供护盾/回复/闪避。
    /// Hediff_MannaFairy 是 CandyTypeDef body5 的被动，能力仅触发派遣。
    /// </summary>
    public class CompAbilityEffect_MannaFairy : CompAbilityEffect
    {
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!target.TryGetPawn(out Pawn pawn))
                return false;

            if (!LovePartnerRelationUtility.LovePartnerRelationExists(parent.pawn, pawn))
            {
                if (throwMessages)
                {
                    Messages.Message("MinchoCandyWars.Abilities.MannaFairyNotLover".Translate(), pawn, MessageTypeDefOf.RejectInput, historical: false);
                }
                return false;
            }

            if (pawn.Map != parent.pawn.Map)
                return false;

            // Cannot dispatch to a target that already has fairy buffs from someone else
            if (pawn.health.hediffSet.GetFirstHediffOfDef(MCW_DefOf.MCW_MannaFairyBuff) != null)
            {
                if (throwMessages)
                {
                    Messages.Message("MinchoCandyWars.Abilities.MannaFairyAlreadyProtected".Translate(), pawn, MessageTypeDefOf.RejectInput, historical: false);
                }
                return false;
            }

            return true;
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            return Valid(target, false);
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn lover = target.Pawn;
            Pawn self = parent.pawn;

            Hediff_MannaFairy? fairy = (Hediff_MannaFairy?)self.health.hediffSet.GetFirstHediffOfDef(MCW_DefOf.MCW_MannaFairy);
            if (fairy == null || !fairy.LevelMet) return;

            fairy.DispatchTo(lover);
        }
    }
}