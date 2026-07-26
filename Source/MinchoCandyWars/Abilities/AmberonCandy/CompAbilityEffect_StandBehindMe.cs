using RimWorld;
using Verse;

namespace MinchoCandyWars.Abilities.AmberonCandy
{
    public class CompAbilityEffect_StandBehindMe : CompAbilityEffect
    {
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!target.IsValid || !target.Cell.IsValid)
            {
                if (throwMessages)
                {
                    Messages.Message("MinchoCandyWars.Abilities.StandBehindMeInvalidCell".Translate(), parent.pawn, MessageTypeDefOf.RejectInput, historical: false);
                }
                return false;
            }
            Map map = parent.pawn.Map;
            if (map == null) return false;
            if (!GenConstruct.CanPlaceBlueprintAt(MCW_DefOf.MCW_AmberonPositionShield, target.Cell, Rot4.North, map, godMode: false).Accepted)
            {
                if (throwMessages)
                {
                    Messages.Message("MinchoCandyWars.Abilities.StandBehindMeBlocked".Translate(), target.ToTargetInfo(map), MessageTypeDefOf.RejectInput, historical: false);
                }
                return false;
            }
            return base.Valid(target, throwMessages);
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Map map = parent.pawn.Map;
            if (map == null) return;
            GenSpawn.Spawn(MCW_DefOf.MCW_AmberonPositionShield, target.Cell, map, Rot4.North);
        }
    }
}
