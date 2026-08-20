using RimWorld;
using Verse;

namespace MinchoCandyWars.Abilities.PrismCandy
{
    public class CompProperties_PrismSwordSlowZone : CompProperties
    {
        public int durationTicks = 900;

        public CompProperties_PrismSwordSlowZone()
        {
            compClass = typeof(Comp_PrismSwordSlowZone);
        }
    }

    /// <summary>
    /// 巧克力冰淇淋巨剑的减速区域：透明物体，每 tick 遍历地图内所有 pawn，
    /// 若 pawn 所在格在 start-end 线段周围范围内，则施加/刷新减速 Hediff。
    /// </summary>
    public class Comp_PrismSwordSlowZone : ThingComp
    {
        public CompProperties_PrismSwordSlowZone Props => (CompProperties_PrismSwordSlowZone)props;

        private IntVec3 start;
        private IntVec3 end;
        private float radius;
        private int ticksRemaining;

        public void Setup(IntVec3 start, IntVec3 end, float radius)
        {
            this.start = start;
            this.end = end;
            this.radius = radius;
            this.ticksRemaining = Props.durationTicks;
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                ticksRemaining = Props.durationTicks;
            }
        }

        public override void CompTick()
        {
            base.CompTick();

            ticksRemaining--;
            if (ticksRemaining <= 0)
            {
                parent.Destroy();
                return;
            }

            Map map = parent.Map;
            if (map == null) return;

            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn.Dead || !PrismCandyAbilityUtility.IsPointInRange(pawn.Position, start, end, radius))
                {
                    continue;
                }

                Hediff? hediff = pawn.health.hediffSet.GetFirstHediffOfDef(MCW_DefOf.MCW_PrismSwordSlowHediff);
                if (hediff == null)
                {
                    pawn.health.AddHediff(HediffMaker.MakeHediff(MCW_DefOf.MCW_PrismSwordSlowHediff, pawn));
                }
                else
                {
                    hediff.TryGetComp<HediffComp_Disappears>()?.ResetElapsedTicks();
                }
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref start, "start");
            Scribe_Values.Look(ref end, "end");
            Scribe_Values.Look(ref radius, "radius", 0f);
            Scribe_Values.Look(ref ticksRemaining, "ticksRemaining", 0);
        }
    }
}
