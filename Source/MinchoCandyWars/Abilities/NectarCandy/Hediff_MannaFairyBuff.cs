using Verse;

namespace MinchoCandyWars.Abilities.NectarCandy
{
    public class Hediff_MannaFairyBuff : HediffWithComps
    {
        internal static bool HasIncomingProtection(Pawn target)
        {
            if (target.Map == null) return false;

            foreach (Pawn source in target.Map.mapPawns.AllPawns)
            {
                var fairy = source.health.hediffSet.GetFirstHediffOfDef(MCW_HediffDefOf.MCW_MannaFairy) as Hediff_MannaFairy;
                if (source != target && fairy?.dispatchedTo == target && fairy.IsValid)
                    return true;
            }
            return false;
        }

        public override bool ShouldRemove
        {
            get
            {
                if (pawn.Dead) return true;
                var ownFairy = pawn.health.hediffSet.GetFirstHediffOfDef(MCW_HediffDefOf.MCW_MannaFairy) as Hediff_MannaFairy;
                if (ownFairy is { IsValid: true, dispatchedTo: null }) return false;
                return !HasIncomingProtection(pawn);
            }
        }
    }
}
