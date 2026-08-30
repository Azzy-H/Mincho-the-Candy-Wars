using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace MinchoCandyWars
{
    public class HediffGiver_Mincho : HediffGiver
    {
        public override void OnIntervalPassed(Pawn pawn, Hediff cause)
        {
            if (pawn.Spawned)
            {
                if(pawn.Map.snowGrid.GetDepth(pawn.Position) > 0f || pawn.Position.GetTerrain(pawn.Map) == TerrainDefOf.Ice)
                {
                    Hediff firstHediffOfDef = pawn.health.hediffSet.GetFirstHediffOfDef(hediff);
                    if (firstHediffOfDef == null)
                    {
                        Hediff hediff = HediffMaker.MakeHediff(this.hediff, pawn, null);
                        pawn.health.AddHediff(hediff, null, null, null);
                    }
                }
                else
                {
                    Hediff firstHediffOfDef = pawn.health.hediffSet.GetFirstHediffOfDef(hediff);
                    if (firstHediffOfDef != null)
                    {
                        pawn.health.RemoveHediff(firstHediffOfDef);
                    }
                }
            }
        }
    }
}
