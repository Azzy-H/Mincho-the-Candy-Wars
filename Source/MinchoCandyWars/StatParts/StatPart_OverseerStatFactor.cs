using RimWorld;
using Verse;

namespace MinchoCandyWars.StatParts
{
    public class StatPart_OverseerStatFactor : StatPart
    {
        private StatDef stat = null!;

        [MustTranslate]
        private string label = null!;

        public override void TransformValue(StatRequest req, ref float val)
        {
            if (TryGetFactor(req, out float factor))
            {
                val *= factor;
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (TryGetFactor(req, out float factor) && factor != 1f)
            {
                return label + ": x" + factor.ToStringPercent();
            }

            return null!;
        }

        private bool TryGetFactor(StatRequest req, out float factor)
        {
            if (ModsConfig.BiotechActive && req.HasThing && req.Thing is Pawn pawn)
            {
                Pawn overseer = pawn.GetOverseer();
                if (overseer != null)
                {
                    factor = overseer.GetStatValue(stat);
                    return true;
                }
            }

            factor = 1f;
            return false;
        }
    }
}