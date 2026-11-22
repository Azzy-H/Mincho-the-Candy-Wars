#nullable disable
using RimWorld;
using Verse;

namespace MinchoCandyWars.QuestStory;

public static class Relations
{
    private static Faction Alliance
    {
        get
        {
            var def = DefDatabase<FactionDef>.GetNamedSilentFail("MCW_Alliance_Faction");
            return def == null ? null : Find.FactionManager.FirstFactionOfDef(def);
        }
    }

    public static int Number => Alliance?.PlayerGoodwill ?? -100;
    public static string Value => Alliance?.PlayerGoodwill.ToString()
        ?? "MinchoCandyWars.AllianceContact.Unavailable".Translate().RawText;

    public static string Summary => "MinchoCandyWars.AllianceContact.GoodwillSummary".Translate(Value);
}
