#nullable disable
using HarmonyLib;
using MinchoCandyWars.StellarisEvent;
using RimWorld;
using Verse;

namespace MinchoCandyWars.QuestStory;

public sealed partial class Story
{
    public int CompanyDeadline = -1;
    void CompletionExpose() => Scribe_Values.Look(ref CompanyDeadline, "companyDeadline102", -1);
    public void StartCompanyTimer()
    {
        if (CompanyDeadline < 0 && !StellarisEventFlagManager.HasFlag("MCW_QS1_CorpDecided"))
            CompanyDeadline = Find.TickManager.TicksGame + CompletionPolicy.CompanyDecisionTicks;
    }
    public void CompletionTick(int now)
    {
        if (CompletionPolicy.CompanyDue(now, CompanyDeadline, StellarisEventFlagManager.HasFlag("MCW_QS1_CorpDecided")))
        {
            CompanyDeadline = -1;
            var def = MCW_EventDefOf.MCW_Event_QS1_CorpContact;
            foreach (var window in Find.WindowStack.Windows.OfType<StellarisEventWindow>().Where(w => AccessTools.Field(typeof(StellarisEventWindow), "eventDef").GetValue(w) == def).ToList()) window.Close(false);
            // Run the same rejection effects as the visible choice: faction change,
            // tracked delegation attack, flags and next contact. No second custom branch.
            def.options[1].action.Execute();
            Letter("MinchoCandyWars.StoryLetter.CorpDeadlineTitle".Translate().RawText, "MinchoCandyWars.StoryLetter.CorpDeadlineBody".Translate().RawText);
        }
        var leader = Chiefs.ElementAtOrDefault(0);
        if (leader is { Dead: false }) EnsureLeaderRegeneration(leader);
    }
    public static void EnsureLeaderRegeneration(Pawn pawn)
    {
        if (!ModsConfig.AnomalyActive) return;
        var def = MCW_HediffDefOf.Regeneration;
        if (!pawn.health.hediffSet.HasHediff(def)) pawn.health.AddHediff(def).Severity = 0.1f;
    }
}
