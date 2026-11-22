using RimWorld;
using System.Collections.Generic;
using Verse;

namespace MinchoCandyWars
{

    public class MinchoCandyChangeRecipeModExtension : DefModExtension
    {
        public CandyTypeDef candyType = null!;
    }

    /// <summary>
    /// 糖饰切换
    /// </summary>
    public class Recipe_MinchoCandyChange : Recipe_Surgery
    {
        public MinchoCandyChangeRecipeModExtension Ext => recipe.GetModExtension<MinchoCandyChangeRecipeModExtension>();

        public override bool AvailableOnNow(Thing thing, BodyPartRecord? part = null)
        {
            if (!base.AvailableOnNow(thing, part))
            {
                return false;
            }
            if (!(thing is Pawn pawn))
            {
                return false;
            }

            CompMinchoCore core = pawn.GetComp<CompMinchoCore>();
            if (core == null || !core.Active)
            {
                return false;
            }
            return !pawn.health.hediffSet.HasHediff(MCW_HediffDefOf.MCW_CandyChangeExhaustion);
        }

        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            CompMinchoCore core = pawn.GetComp<CompMinchoCore>();
            core.CurrentCandyType = Ext.candyType;
            pawn.health.AddHediff(MCW_HediffDefOf.MCW_CandyChangeExhaustion);
        }
    }
}
