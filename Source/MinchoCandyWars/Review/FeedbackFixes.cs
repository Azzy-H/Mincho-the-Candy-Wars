using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace MinchoCandyWars.Review;

/// <summary>衣物配色：部分服装固定白/素色，或跟随材质颜色。</summary>
// Run before Apparel applies corpse/style colors; old saved garment colors also follow this rule.
[HarmonyPatch(typeof(ThingWithComps), "get_DrawColor")]
public static class ApparelPalette
{
    public static void Postfix(ThingWithComps __instance, ref Color __result)
    {
        if (!(__instance is Apparel)) return;
        switch (__instance.def.defName)
        {
            case "MCW_Bikini":
            case "MCW_BikiniBW":
                if (__instance.Stuff != null) __result = __instance.def.GetColorForStuff(__instance.Stuff);
                break;
            case "MCW_NurseOutfit":
            case "MCW_NurseCap":
            case "MCW_GorgeousDress":
            case "MCW_Qipao":
            case "MCW_MechaArmor":
            case "MCW_HoloVisor":
                __result = Color.white;
                break;
        }
    }
}

/// <summary>珉巧史莱姆朝向：上游只有朝左的 _west 精灵，需要额外翻转一次。</summary>
[HarmonyPatch(typeof(PawnRenderNode), nameof(PawnRenderNode.FlipGraphic))]
public static class SlimeFacing
{
    public static void Postfix(PawnRenderNode __instance, PawnDrawParms parms, ref bool __result)
    {
        if (!(__instance is PawnRenderNode_AnimalPart) ||
            __instance.tree?.pawn?.def?.defName != "MCW_MinchoSlime" || !parms.facing.IsHorizontal) return;
        Graphic graphic = __instance.PrimaryGraphic;
        // Upstream supplies only a left-facing _west sprite. Pawn meshes mirror West,
        // while Graphic_Multi's EastFlipped flag is ignored by the pawn node path.
        if (graphic != null && graphic.EastFlipped && !graphic.WestFlipped) __result = !__result;
    }
}
