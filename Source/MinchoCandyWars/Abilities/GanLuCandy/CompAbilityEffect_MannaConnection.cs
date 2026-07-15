using RimWorld;
using Verse;

namespace MinchoCandyWars.Abilities.GanLuCandy
{
    public class CompAbilityEffect_MannaConnection : CompAbilityEffect
    {
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!target.TryGetPawn(out Pawn pawn))
                return false;

            // Must be a Mincho
            if (pawn.def != MCW_DefOf.Mincho_ThingDef)
                return false;

            // Cannot be self
            if (pawn == parent.pawn)
                return false;

            return base.Valid(target, throwMessages);
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            return Valid(target, false);
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn other = target.Pawn;
            Pawn self = parent.pawn;

            // Remove existing connection on either side
            Hediff_MannaConnection? existingSelf = (Hediff_MannaConnection)self.health.hediffSet.GetFirstHediffOfDef(MCW_DefOf.MCW_MannaConnection);
            Hediff_MannaConnection? existingOther = (Hediff_MannaConnection)other.health.hediffSet.GetFirstHediffOfDef(MCW_DefOf.MCW_MannaConnection);

            existingSelf?.RemoveConnection();
            existingOther?.RemoveConnection();

            // Create new connection
            Hediff_MannaConnection connSelf = (Hediff_MannaConnection)self.health.AddHediff(MCW_DefOf.MCW_MannaConnection);
            Hediff_MannaConnection connOther = (Hediff_MannaConnection)other.health.AddHediff(MCW_DefOf.MCW_MannaConnection);

            connSelf.pairedPawn = other;
            connOther.pairedPawn = self;

            MoteMaker.ThrowText(self.DrawPos, self.Map, "MinchoCandyWars.Abilities.MannaConnectionFormed".Translate(), 3.65f);
        }
    }
}