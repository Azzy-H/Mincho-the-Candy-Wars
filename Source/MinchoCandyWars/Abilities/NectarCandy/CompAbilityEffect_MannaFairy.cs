using RimWorld;
using Verse;

namespace MinchoCandyWars.Abilities.NectarCandy;

// Dispatch to the existing single Manna connection, not a romantic relationship.
public class CompAbilityEffect_MannaFairy : CompAbilityEffect
{
    Pawn? Partner => (parent.pawn.health.hediffSet.GetFirstHediffOfDef(MCW_HediffDefOf.MCW_MannaConnection) as Hediff_MannaConnection)?.pairedPawn;
    Hediff_MannaFairy? Fairy => parent.pawn.health.hediffSet.GetFirstHediffOfDef(MCW_HediffDefOf.MCW_MannaFairy) as Hediff_MannaFairy;

    bool Ready(out string reason)
    {
        var partner = Partner;
        reason = partner == null ? "MinchoCandyWars.Reason.FairyNoLink".Translate().RawText :
            partner.Dead || !partner.Spawned || partner.Map != parent.pawn.Map ? "MinchoCandyWars.Reason.FairyPartnerAbsent".Translate().RawText :
            Fairy?.LevelMet != true ? "MinchoCandyWars.Reason.FairyUpgradeRequired".Translate().RawText :
            partner.health.hediffSet.HasHediff(MCW_HediffDefOf.MCW_MannaFairyBuff) ? "MinchoCandyWars.Reason.FairyAlreadyProtected".Translate().RawText : "";
        return reason.Length == 0;
    }
    public override bool GizmoDisabled(out string reason) => !Ready(out reason);
    public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
    {
        bool ready = Ready(out string reason);
        if (!ready && throwMessages) Messages.Message(reason, parent.pawn, MessageTypeDefOf.RejectInput, false);
        return ready;
    }
    public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest) => Ready(out _);
    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        if (!Ready(out _)) return;
        base.Apply(target, dest);
        Fairy!.DispatchTo(Partner!);
    }
}
