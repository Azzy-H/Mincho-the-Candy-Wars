#nullable disable
using HarmonyLib;
using MinchoCandyWars;
using MinchoCandyWars.Render;
using Verse;

namespace MinchoCandyWars.QuestStory;

// Only remove the duplicate picture. The active candy, bonuses and abilities
// remain untouched, for every Mincho wearing the matching headgear.
[HarmonyPatch(typeof(PawnRenderNodeWorker_CandyDecoration), nameof(PawnRenderNodeWorker_CandyDecoration.CanDrawNow))]
public static class CandyHeadgearFusion
{
    public static bool Matches(Pawn pawn)
    {
        if (pawn?.def != MCW_ThingDefOf.Mincho_ThingDef) return false;
        var candy = pawn.GetComp<CompMinchoCore>()?.CurrentCandyType?.defName;
        var worn = pawn.apparel?.WornApparel;
        if (worn == null) return false;
        for (int i = 0; i < worn.Count; i++)
            if (HeadgearPolicy.Merge(candy, worn[i].def.defName)) return true;
        return false;
    }
    static void Postfix(PawnDrawParms parms, ref bool __result)
    {
        if (!__result || parms.pawn == null) return;
        // Qinxin never shows a separate forehead ornament, regardless of her active candy.
        if (Matches(parms.pawn) || (Current.Game != null && Story.Current?.Chiefs.ElementAtOrDefault(0) == parms.pawn))
            __result = false;
    }
}
