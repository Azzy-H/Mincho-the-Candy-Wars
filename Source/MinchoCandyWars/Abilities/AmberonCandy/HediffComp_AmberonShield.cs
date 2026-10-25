using System.Linq;
using RimWorld;
using Verse;
using MinchoCandyWars.Abilities;

namespace MinchoCandyWars.Abilities.AmberonCandy
{
    public class HediffCompProperties_AmberonShield : HediffCompProperties
    {
        public int hitpoints = 200;
        public int hitpointsOnReset = 40;
        public int startingTicksToReset = 7200;

        public HediffCompProperties_AmberonShield()
        {
            compClass = typeof(HediffComp_AmberonShield);
        }
    }

    /// <summary>
    /// 琥珀糖护盾 —— 护盾腰带式个人护盾，仅吸收伤害，不干扰投射物。
    /// </summary>
    public class HediffComp_AmberonShield : HediffComp_EnergyShield
    {
        public HediffCompProperties_AmberonShield Props => (HediffCompProperties_AmberonShield)props;

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

    /// <summary>护盾说明：把自定义护盾的「伤害点数」换算成原版风格的显示值。</summary>
    public class ShieldInfoProperties : HediffCompProperties
    {
        public ShieldInfoProperties() => compClass = typeof(ShieldInfo);
    }

    public class ShieldInfo : HediffComp
    {
        public override string CompTipStringExtra => null!;

        public override string CompDescriptionExtra
        {
            get
            {
                var shield = parent.comps?.OfType<HediffComp_EnergyShield>().FirstOrDefault();
                if (shield == null) return null!;

                return "MinchoCandyWars.UI.HealthTips.Shield".Translate(
                    (shield.Energy * ShieldDisplay.DisplayPerDamage).ToString("0").Named("SHIELD"),
                    (shield.EnergyMax * ShieldDisplay.DisplayPerDamage).ToString("0").Named("SHIELDMAX"),
                    shield.Energy.ToString("0.#").Named("CAPACITY"),
                    shield.EnergyMax.ToString("0.#").Named("CAPACITYMAX"));
            }
        }
    }
}
