using RimWorld;
using Verse;

namespace MinchoCandyWars.Abilities.AmberonCandy
{
    public class CompAbilityEffect_AmberonHeart : CompAbilityEffect
    {
        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            return true;
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn self = parent.pawn;
            if (self.health.hediffSet.GetFirstHediffOfDef(MCW_HediffDefOf.MCW_AmberonHeart) == null)
            {
                self.health.AddHediff(MCW_HediffDefOf.MCW_AmberonHeart);
            }

            if (self.Map is not Map map)
                return;

            foreach (Pawn colonist in map.mapPawns.FreeColonistsSpawned)
                colonist.needs?.mood?.thoughts.memories.TryGainMemory(MCW_DefOf.MCW_AmberonHeartInspired);
        }
    }
}
