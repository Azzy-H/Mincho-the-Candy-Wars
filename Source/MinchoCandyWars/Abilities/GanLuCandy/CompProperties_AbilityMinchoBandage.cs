using Verse;
using RimWorld;

namespace MinchoCandyWars.Abilities.GanLuCandy
{
    public class CompProperties_AbilityMinchoBandage : CompProperties_AbilityEffect
    {
        public FloatRange tendQualityRange;

        public CompProperties_AbilityMinchoBandage()
        {
            compClass = typeof(CompAbilityEffect_MinchoBandage);
        }
    }
}