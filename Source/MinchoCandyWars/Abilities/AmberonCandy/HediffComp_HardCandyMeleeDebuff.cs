using RimWorld;
using Verse;

namespace MinchoCandyWars.Abilities.AmberonCandy
{
    public class HediffCompProperties_HardCandyMeleeDebuff : HediffCompProperties
    {
        public HediffDef? debuffHediffDef;

        public HediffCompProperties_HardCandyMeleeDebuff()
        {
            compClass = typeof(HediffComp_HardCandyMeleeDebuff);
        }
    }

    /// <summary>
    /// 硬糖 —— 持有者近战攻击敌人时给敌人附加 MCW_SplashedIceCream。
    /// 基于 Hediff.Notify_PawnUsedVerb 触发，无需 Harmony。
    /// </summary>
    public class HediffComp_HardCandyMeleeDebuff : HediffComp
    {
        public HediffCompProperties_HardCandyMeleeDebuff Props => (HediffCompProperties_HardCandyMeleeDebuff)props;

        public override void Notify_PawnUsedVerb(Verb verb, LocalTargetInfo target)
        {

            if (Props.debuffHediffDef == null) return;
            if (!(verb is Verb_MeleeAttack)) return;

            Pawn victim = target.Pawn;
            if (victim == null || victim.Dead || victim.health == null) return;

            if (victim.health.hediffSet.GetFirstHediffOfDef(Props.debuffHediffDef) == null)
            {
                victim.health.AddHediff(Props.debuffHediffDef);
            }
        }
    }
}
