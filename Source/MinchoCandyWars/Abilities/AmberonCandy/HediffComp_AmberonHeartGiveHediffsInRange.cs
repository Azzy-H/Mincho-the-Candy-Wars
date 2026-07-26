using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace MinchoCandyWars.Abilities.AmberonCandy
{
    public class HediffCompProperties_AmberonHeartGiveHediffsInRange : HediffCompProperties_GiveHediffsInRange
    {
        public HediffCompProperties_AmberonHeartGiveHediffsInRange()
        {
            compClass = typeof(HediffComp_AmberonHeartGiveHediffsInRange);
        }
    }

    /// <summary>
    /// 琥珀糖心范围减速 —— 仅对敌对 pawn 施加 MCW_AmberonSlow。
    /// </summary>
    public class HediffComp_AmberonHeartGiveHediffsInRange : HediffComp_GiveHediffsInRange
    {
        private Mote? mote;

        public new HediffCompProperties_AmberonHeartGiveHediffsInRange Props => (HediffCompProperties_AmberonHeartGiveHediffsInRange)props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            if (!parent.pawn.Awake() || parent.pawn.health == null || parent.pawn.health.InPainShock || !parent.pawn.Spawned)
            {
                return;
            }

            if (!Props.hideMoteWhenNotDrafted || parent.pawn.Drafted)
            {
                if (Props.mote != null && (mote == null || mote.Destroyed))
                {
                    mote = MoteMaker.MakeAttachedOverlay(parent.pawn, Props.mote, Vector3.zero);
                }
                if (mote != null)
                {
                    mote.Maintain();
                }
            }

            IReadOnlyList<Pawn> allPawns = parent.pawn.Map.mapPawns.AllPawnsSpawned;
            foreach (Pawn item in allPawns)
            {
                if (item.Dead || item.health == null || item == parent.pawn)
                    continue;
                if (item.Faction == null || !item.Faction.HostileTo(parent.pawn.Faction))
                    continue;
                if (!(item.Position.DistanceTo(parent.pawn.Position) <= Props.range))
                    continue;
                if (!Props.targetingParameters.CanTarget(item))
                    continue;

                Hediff hediff = item.health.hediffSet.GetFirstHediffOfDef(Props.hediff);
                if (hediff == null)
                {
                    hediff = item.health.AddHediff(Props.hediff, item.health.hediffSet.GetBrain());
                    hediff.Severity = Props.initialSeverity;
                }
                HediffComp_Disappears? hediffComp_Disappears = hediff.TryGetComp<HediffComp_Disappears>();
                if (hediffComp_Disappears != null)
                {
                    hediffComp_Disappears.ticksToDisappear = 5;
                }
            }
        }
    }
}
