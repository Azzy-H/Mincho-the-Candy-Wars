using RimWorld;
using Verse;

namespace MinchoCandyWars.Abilities.AmberonCandy
{
    public class CompAbilityEffect_AmberonHeart : CompAbilityEffect
    {
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            return base.Valid(target, throwMessages);
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            return true;
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn self = parent.pawn;
            if (self.health.hediffSet.GetFirstHediffOfDef(MCW_DefOf.MCW_AmberonHeart) == null)
            {
                self.health.AddHediff(MCW_DefOf.MCW_AmberonHeart);
            }
        }
    }
}
