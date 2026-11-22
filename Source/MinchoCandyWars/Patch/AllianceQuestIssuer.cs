using HarmonyLib;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace MinchoCandyWars.Patch;

[HarmonyPatch(typeof(QuestNode_GetFaction), "IsGoodFaction")]
internal static class AllianceQuestIssuer
{
    private static bool Prefix(Faction faction, ref bool __result)
    {
        if (faction.def != MCW_DefOf.MCW_Alliance_Faction)
            return true;
        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), typeof(PawnGenerationRequest))]
internal static class AlliancePawnGeneration
{
    private static void Prefix(ref PawnGenerationRequest request)
    {
        if (request.Faction?.def != MCW_DefOf.MCW_Alliance_Faction)
            return;
        var smart = MCW_DefOf.MCW_SmartMincho;
        var member = MCW_DefOf.Mincho_Mercenary;
        if (!request.KindDef.race.race.Humanlike || request.KindDef.race == member.race)
            return;
        request.PawnKindDefGetter = null;
        request.KindDef = Rand.Chance(0.15f) ? smart : member;
    }

    private static void Postfix(Pawn __result)
    {
        if (__result?.kindDef == MCW_DefOf.MCW_SmartMincho &&
            __result.GetComp<CompMinchoCore>() is { Active: false } core)
            core.Active = true;
    }}
