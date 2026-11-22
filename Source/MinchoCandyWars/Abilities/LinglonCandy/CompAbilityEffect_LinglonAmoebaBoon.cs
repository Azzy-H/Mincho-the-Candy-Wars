using RimWorld;
using Verse;

namespace MinchoCandyWars.Abilities.LinglonCandy
{
    /// <summary>
    /// 纳米变形虫增益（开关）——再按一次即切换。
    /// </summary>
    public class CompAbilityEffect_LinglonAmoebaBoon : CompAbilityEffect
    {
        public override string ExtraTooltipPart()
        {
            var cost = MCW_HediffDefOf.MCW_LinglonAmoebaBoon.comps
                .OfType<HediffCompProperties_LinglonAmoebaBoon>().FirstOrDefault()?.costPerHour;
            return cost.HasValue ? "MinchoCandyWars.Abilities.AmoebaBoonHourlyCost".Translate(cost.Value.ToString("0.#")) : "";
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn caster = parent.pawn;
            Hediff? hediff = caster.health.hediffSet.GetFirstHediffOfDef(MCW_HediffDefOf.MCW_LinglonAmoebaBoon);
            if (hediff == null)
            {
                caster.health.AddHediff(HediffMaker.MakeHediff(MCW_HediffDefOf.MCW_LinglonAmoebaBoon, caster));
            }
            else
            {
                caster.health.RemoveHediff(hediff);
            }
        }
    }
}
