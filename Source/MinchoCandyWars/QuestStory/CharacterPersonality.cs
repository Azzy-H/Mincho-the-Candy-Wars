#nullable disable
using RimWorld;
using Verse;
namespace MinchoCandyWars.QuestStory;
static class CharacterPersonality
{
    static readonly (string def, int degree)[][] Traits = [
        [("NaturalMood",1),("GreatMemory",0),("Beauty",2)],
        [("Tough",0),("Kind",0),("Industriousness",1)],
        [("Kind",0),("GreatMemory",0),("NaturalMood",1)],
        [("Tough",0),("GreatMemory",0),("Industriousness",2)],
        [("Tough",0),("Brawler",0),("FastLearner",0)],
        [("Beauty",2),("NaturalMood",1),("PsychicSensitivity",1)]];
    public static void Apply(Pawn pawn, int role)
    {
        pawn.story.traits.allTraits.Clear();
        foreach (var (id, degree) in Traits[role])
            pawn.story.traits.allTraits.Add(new Trait(DefDatabase<TraitDef>.GetNamed(id), degree) { pawn = pawn });
    }
}
static class AllianceBelief
{
    public static void Apply(Pawn pawn)
    {
        if (!ModsConfig.IdeologyActive || pawn.ideo == null) return;
        var alliance = Find.FactionManager.FirstFactionOfDef(MCW_DefOf.MCW_Alliance_Faction);
        if (alliance?.ideos?.PrimaryIdeo is { } ideology) pawn.ideo.SetIdeo(ideology);
    }
}
