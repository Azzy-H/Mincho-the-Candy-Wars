using System.Text;
using MinchoCandyWars.Abilities;
using RimWorld;
using Verse;

namespace MinchoCandyWars.Buff;

// Display the actual current stages, never all upgrade tiers.
static class IntelSummary
{
    public static string Describe(Pawn pawn, CompMinchoCore core)
    {
        var text = new StringBuilder();
        text.AppendLine("MinchoCandyWars.IntelSummary.Ornament".Translate(OrnamentLabel(core)).RawText);
        text.AppendLine("MinchoCandyWars.IntelSummary.Grades".Translate(core.MinchoCoreGrade, core.MinchoBodyGrade).RawText);
        text.AppendLine("MinchoCandyWars.IntelSummary.Chill".Translate(core.CurrentMaxCandyValue.ToString("0")).RawText);


        var abilities = DefDatabase<AbilityDef>.AllDefsListForReading
            .Where(a => a.GetModExtension<MinchoAbilityDefModExtension>() is { } ext &&
                ext.candyType == core.CurrentCandyType && ext.IsAvailableFor(pawn));
        text.Append("MinchoCandyWars.IntelSummary.Abilities".Translate(string.Join(", ", abilities.Select(a => a.LabelCap.ToString()))).RawText);
        return text.ToString();
    }
    public static string OrnamentLabel(CompMinchoCore core) =>
        core.CurrentCandyType?.LabelCap.ToString() ?? "MinchoCandyWars.IntelSummary.NoOrnament".Translate().RawText;

    public static string Effects(CompMinchoCore core)
    {
        var text = new StringBuilder();
        var stages = new[] { core.CoreStage, core.BodyStage }.OfType<CandyTypeStage>();
        foreach (var group in stages.SelectMany(s => s.statOffsets ?? new List<StatModifier>()).GroupBy(s => s.stat))
            text.AppendLine($"  - {group.Key.LabelCap}: {group.Key.Worker.ValueToString(group.Sum(s => s.value), false, ToStringNumberSense.Offset)}");
        foreach (var group in stages.SelectMany(s => s.statFactors ?? new List<StatModifier>()).GroupBy(s => s.stat))
            text.AppendLine($"  - {group.Key.LabelCap}: {group.Key.Worker.ValueToString(group.Aggregate(1f, (value, s) => value * s.value), false, ToStringNumberSense.Factor)}");
        foreach (var effect in stages.SelectMany(s => s.gainHediffs ?? new List<HediffDef>()).Distinct())
            text.AppendLine($"  - {effect.LabelCap}");
        return text.ToString().TrimEnd();
    }
}

/// <summary>
/// 智慧核心汇总。只挂在 MCW_Intel 这一个 HediffDef 上，靠继承覆盖基类的
/// LabelBase / Description / TipStringExtra，不再对 Hediff 基类打 Harmony 补丁。
/// </summary>
public class Hediff_IntelSummary : Hediff_Buff
{
    private CompMinchoCore? ActiveCore =>
        pawn?.GetComp<CompMinchoCore>() is { Active: true } core ? core : null;

    public override string LabelBase
    {
        get
        {
            CompMinchoCore? core = ActiveCore;
            if (core == null) return base.LabelBase;
            return base.LabelBase + "MinchoCandyWars.IntelSummary.Suffix".Translate(
                IntelSummary.OrnamentLabel(core), core.MinchoCoreGrade, core.MinchoBodyGrade).RawText;
        }
    }

    public override string Description
    {
        get
        {
            CompMinchoCore? core = ActiveCore;
            return core == null ? base.Description : IntelSummary.Describe(pawn, core);
        }
    }

    // Keep current bonuses in the native tooltip effects block beside the intelligence bonus.
    public override string TipStringExtra
    {
        get
        {
            CompMinchoCore? core = ActiveCore;
            if (core == null) return base.TipStringExtra;
            string effects = IntelSummary.Effects(core);
            return string.IsNullOrEmpty(effects) ? base.TipStringExtra : effects + "\n" + base.TipStringExtra;
        }
    }
}
