using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace MinchoCandyWars.Abilities.MechaCandy
{
    public class CompAbilityEffect_MechHack : CompAbilityEffect
    {
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (target.TryGetPawn(out Pawn pawn) && pawn.RaceProps.IsMechanoid && pawn.Faction != parent.pawn.Faction)
            {
                return true;
            }
            return false;
        }
        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (target.TryGetPawn(out Pawn pawn) && pawn.RaceProps.IsMechanoid && pawn.Faction != parent.pawn.Faction)
            {
                return true;
            }
            return false;
        }
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            target.Pawn.SetFaction(parent.pawn.Faction, parent.pawn);
        }
    }
}
