using HarmonyLib;
using RimWorld;
using Verse;
namespace MinchoCandyWars.QuestStory;

// Only NPC generation rolls this distribution. Loaded colonists and chosen candy are untouched.
[HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), new[] { typeof(PawnGenerationRequest) })]
public static class GeneratedCandy
{
    static void Postfix(Pawn __result) => TryAssign(__result);
    public static void TryAssign(Pawn pawn)
    {
        if (pawn == null) return;
        var core = pawn.GetComp<CompMinchoCore>();
        if (core == null || core.GeneratedCandyRolled || pawn.Faction == null || pawn.Faction.IsPlayer
            || !pawn.RaceProps.Humanlike || pawn.ageTracker.AgeBiologicalYears < 18) return;
        core.GeneratedCandyRolled = true;
        if (core.CurrentCandyType != null) return;
        string role = pawn.kindDef.defName;
        bool elite = role.Contains("Elite") || role == "MCW_SmartMincho";
        bool fighter = elite || pawn.kindDef.isFighter;
        if (!Rand.Chance(elite ? .9f : fighter ? .75f : .4f)) return;
        string[] types = role.Contains("Knight") || role.Contains("Defender")
            ? new[] { "AmberonCandy", "PrismCandy" }
            : role.Contains("Mercenary") ? new[] { "PrismCandy", "LinglonCandy", "AmberonCandy" }
            : new[] { "NectarCandy", "AmberonCandy", "PrismCandy", "LinglonCandy", "MechaCandy" };
        core.CurrentCandyType = DefDatabase<CandyTypeDef>.GetNamed(types.RandomElement());
        if (!core.Active) core.Active = true;
        int low = elite ? 3 : fighter ? 2 : 1, high = elite ? 4 : fighter ? 3 : 2;
        core.MinchoCoreGrade = Math.Max(core.MinchoCoreGrade, Rand.RangeInclusive(low, high));
        core.MinchoBodyGrade = Math.Max(core.MinchoBodyGrade, Rand.RangeInclusive(low, high));
        core.MinchoCandyValue = core.CurrentMaxCandyValue;
    }
}
