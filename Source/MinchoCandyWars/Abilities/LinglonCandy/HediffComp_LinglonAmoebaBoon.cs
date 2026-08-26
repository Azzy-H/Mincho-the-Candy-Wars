using RimWorld;
using Verse;

namespace MinchoCandyWars.Abilities.LinglonCandy
{
    public class HediffCompProperties_LinglonAmoebaBoon : HediffCompProperties
    {
        public HediffDef buffHediffDef = null!;
        public float costPerHour = 10f;

        public HediffCompProperties_LinglonAmoebaBoon()
        {
            compClass = typeof(HediffComp_LinglonAmoebaBoon);
        }
    }

    /// <summary>
    /// 纳米变形虫增益开关本体：
    /// - 每小时从 caster 清凉度扣除 costPerHour，不足则自我移除（关闭）。
    /// - 每 tick 维护全图珉巧的 +20% 意识 buff。
    /// </summary>
    public class HediffComp_LinglonAmoebaBoon : HediffComp
    {
        public HediffCompProperties_LinglonAmoebaBoon Props => (HediffCompProperties_LinglonAmoebaBoon)props;

        private const int HourlyIntervalTicks = 2500; // 1 hour
        private int tickCounter;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            Pawn caster = Pawn;
            Map map = caster.Map;
            if (map == null) return;

            tickCounter++;
            if (tickCounter >= HourlyIntervalTicks)
            {
                tickCounter = 0;

                CompMinchoCore? core = caster.GetComp<CompMinchoCore>();
                if (core == null || core.MinchoCandyValue < Props.costPerHour)
                {
                    caster.health.RemoveHediff(parent);
                    return;
                }

                core.MinchoCandyValue -= Props.costPerHour;
            }

            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn.Dead || pawn.GetComp<CompMinchoCore>() == null)
                {
                    continue;
                }

                Hediff? buff = pawn.health.hediffSet.GetFirstHediffOfDef(Props.buffHediffDef);
                if (buff == null)
                {
                    pawn.health.AddHediff(HediffMaker.MakeHediff(Props.buffHediffDef, pawn));
                }
                else
                {
                    buff.TryGetComp<HediffComp_Disappears>()?.ResetElapsedTicks();
                }
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref tickCounter, "tickCounter", 0);
        }
    }
}
