using RimWorld;
using Verse;

namespace MinchoCandyWars.Abilities.GanLuCandy
{
    /// <summary>
    /// 甘露连接 —— 两个 Mincho 之间的灵魂绑定。
    /// stage 0 = 远离（无效果），stage 1 = 同图（+20% 意识 + 心情）。
    /// </summary>
    public class Hediff_MannaConnection : HediffWithComps
    {
        public Pawn? pairedPawn;

        public override void Tick()
        {
            base.Tick();

            if (pairedPawn == null || pairedPawn.Dead || pairedPawn.Destroyed)
            {
                RemoveConnection();
                return;
            }

            if (!pawn.IsHashIntervalTick(60))
                return;

            bool sameMap = pawn.Map == pairedPawn.Map && pawn.Map != null;
            int targetStage = sameMap ? 1 : 0;

            if (curStageIndex != targetStage)
            {
                curStageIndex = targetStage;
            }
        }

        public void RemoveConnection()
        {
            if (pairedPawn != null)
            {
                Hediff_MannaConnection? otherConn = (Hediff_MannaConnection?)pairedPawn.health.hediffSet.GetFirstHediffOfDef(MCW_DefOf.MCW_MannaConnection);
                if (otherConn != null)
                {
                    pairedPawn.health.RemoveHediff(otherConn);
                }
            }

            pawn.health.RemoveHediff(this);
        }

        public override bool ShouldRemove
        {
            get
            {
                if (pairedPawn == null || pairedPawn.Dead || pairedPawn.Destroyed)
                    return true;
                return base.ShouldRemove;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref pairedPawn, "pairedPawn");
        }
    }
}