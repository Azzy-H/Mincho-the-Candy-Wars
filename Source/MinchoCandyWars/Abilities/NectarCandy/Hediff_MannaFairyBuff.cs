using RimWorld;
using Verse;

namespace MinchoCandyWars.Abilities.NectarCandy
{
    /// <summary>
    /// 甘露妖精增益 —— 再生 + 闪避 + 护盾。
    /// 由 Hediff_MannaFairy 创建，Buff 自身检查父 Hediff 是否仍然合法。
    /// </summary>
    public class Hediff_MannaFairyBuff : HediffWithComps
    {
        public override bool ShouldRemove
        {
            get
            {
                if (pawn.Dead) return true;

                // Find the parent fairy hediff on this pawn
                Hediff_MannaFairy? fairy = pawn.health.hediffSet.GetFirstHediffOfDef(MCW_DefOf.MCW_MannaFairy) as Hediff_MannaFairy;
                if (fairy == null)
                {
                    // Maybe dispatched from someone else — check all pawns on the map
                    if (pawn.Map != null)
                    {
                        foreach (Pawn p in pawn.Map.mapPawns.AllPawns)
                        {
                            fairy = p.health.hediffSet.GetFirstHediffOfDef(MCW_DefOf.MCW_MannaFairy) as Hediff_MannaFairy;
                            if (fairy != null && fairy.dispatchedTo == pawn)
                                break;
                            fairy = null;
                        }
                    }
                }

                if (fairy == null) return true;
                if (!fairy.IsValid) return true;

                return false;
            }
        }
    }
}