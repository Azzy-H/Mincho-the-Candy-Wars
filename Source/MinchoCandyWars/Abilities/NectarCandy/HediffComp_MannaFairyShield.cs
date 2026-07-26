using RimWorld;
using Verse;
using MinchoCandyWars.Abilities;

namespace MinchoCandyWars.Abilities.NectarCandy
{
    public class HediffCompProperties_MannaFairyShield : HediffCompProperties
    {
        public int hitpoints = 200;
        public int hitpointsOnReset = 40;
        public int startingTicksToReset = 7200;

        public HediffCompProperties_MannaFairyShield()
        {
            compClass = typeof(HediffComp_MannaFairyShield);
        }
    }

    /// <summary>
    /// 甘露妖精护盾 —— 护盾腰带式个人护盾，仅吸收伤害，不干扰投射物。
    /// </summary>
    /// 
    public class HediffComp_MannaFairyShield : HediffComp_EnergyShield
    {
        public HediffCompProperties_MannaFairyShield Props => (HediffCompProperties_MannaFairyShield)props;

        public override float EnergyMax
        {
            get
            {
                CompMinchoCore? core = Pawn?.GetComp<CompMinchoCore>();
                if (core != null)
                    return (float)core.CurrentMaxCandyValue;
                return Props.hitpoints;
            }
        }

        protected override float EnergyGainPerTick => EnergyMax / Props.startingTicksToReset;
        public override int StartingTicksToReset => Props.startingTicksToReset;
        protected override float EnergyOnReset => Props.hitpointsOnReset > 0 ? Props.hitpointsOnReset : EnergyMax * 0.2f;
    }
}