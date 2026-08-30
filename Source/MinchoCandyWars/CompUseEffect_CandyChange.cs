using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace MinchoCandyWars
{
    internal class CompUseEffect_CandyChange : CompUseEffect
    {
        public override AcceptanceReport CanBeUsedBy(Pawn p)
        {
            CompMinchoCore core = p.GetComp<CompMinchoCore>();
            if (core == null)
            {
                return new AcceptanceReport("MCW_NotMincho".Translate());
            }
            if(!core.Active)
            {
                return new AcceptanceReport("MCW_MinchoCoreNotActive".Translate());
            }
            if(p.health.hediffSet.HasHediff(MCW_DefOf.MCW_CandyChangeExhaustion))
            {
                return new AcceptanceReport("MCW_CandyChangeExhaustion".Translate());
            }
            return base.CanBeUsedBy(p);
        }
        public override void DoEffect(Pawn usedBy)
        {
            CompMinchoCore core = usedBy.GetComp<CompMinchoCore>()!;
            core.CurrentCandyType = this.parent.def.GetModExtension<MinchoCandyChangeRecipeModExtension>()!.candyType;
            usedBy.health.AddHediff(MCW_DefOf.MCW_CandyChangeExhaustion);
        }
    }
}
