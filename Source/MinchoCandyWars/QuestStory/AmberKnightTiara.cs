#nullable disable
using System.Collections.Generic;
using HarmonyLib;
using MinchoCandyWars;
using Verse;

namespace MinchoCandyWars.QuestStory;

// Keep the same apparel and material. Only its worn picture changes for Amberon.
[HarmonyPatch(typeof(PawnRenderNode_Apparel), "GraphicsFor")]
public static class AmberKnightTiara
{
    public const string TexturePath = "MCWQ/Apparel/AmberKnightTiara/Texture";

    public static bool Matches(Pawn pawn, string apparelDef) =>
        pawn?.def == MCW_ThingDefOf.Mincho_ThingDef && apparelDef == "Mincho_KnightlyTiara" &&
        pawn.GetComp<CompMinchoCore>()?.CurrentCandyType == MCW_DefOf.AmberonCandy;

    static void Postfix(PawnRenderNode_Apparel __instance, Pawn pawn, ref IEnumerable<Graphic> __result)
    {
        if (Matches(pawn, __instance.apparel?.def.defName)) __result = Replace(__result);
    }

    static IEnumerable<Graphic> Replace(IEnumerable<Graphic> originals)
    {
        foreach (var graphic in originals)
            yield return GraphicDatabase.Get<Graphic_Multi>(TexturePath, graphic.Shader,
                graphic.drawSize, graphic.color, graphic.colorTwo);
    }
}
