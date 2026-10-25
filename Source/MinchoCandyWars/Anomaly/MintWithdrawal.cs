using MinchoCandyWars;
using RimWorld;
using Verse;

namespace MinchoCandyWars.Anomaly;

/// <summary>薄荷冰淇淋戒断心情：需求越低，stage 越高。</summary>
public class MintWithdrawal : ThoughtWorker
{
    /// <summary>需求低于此值算"重度戒断"（thought stage 1）。</summary>
    private const float SevereThreshold = 0.01f;

    /// <summary>需求低于此值算"轻度戒断"（thought stage 0）。</summary>
    private const float MildThreshold = 0.5f;

    private static int Stage(float need) =>
        need <= SevereThreshold ? 1 : need <= MildThreshold ? 0 : -1;

    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        if (MintAddiction.IsMincho(p))
        {
            return false;
        }

        if (p.health.hediffSet.GetFirstHediffOfDef(MCW_DefOf.MCW_MintAddiction) is Hediff_Addiction { Need: { } need })
        {
            int stage = Stage(need.CurLevel);
            return stage < 0 ? ThoughtState.Inactive : ThoughtState.ActiveAtStage(stage);
        }

        return ThoughtState.Inactive;
    }
}
