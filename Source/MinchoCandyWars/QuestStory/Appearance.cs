#nullable disable
using RimWorld;
using Verse;
using UnityEngine;
using HarmonyLib;

namespace MinchoCandyWars.QuestStory;

// Native race hairstyles keep their matching rear-hair addons and facing rules.
// Applied at named-character creation; never rerolled on tick or overwritten on load.
public static class Appearance
{
    public static readonly int[] Hair = [7, 3, 1, 12, 5, 9];
    // 立绘匹配：沁心/玲珑橙金瞳；琥珀青蓝瞳与精神眉眼；甘露柔和大眼；千机冷静；琉璃北极熊蓝眼。
    public static readonly int[] Head = [5, 2, 1, 4, 0, 5];
    public static string HairName(int role) => role switch
    {
        1 => "MCW_Hair_Amber109", 2 => "MCW_Hair_Nectar109", 4 => "MCW_Hair_Prism109",
        5 => "MCW_Hair_Linglon174", _ => $"Mincho_Hair_Large_Mintchoco_{Hair[role]:00}"
    };
    public static void Apply(Pawn pawn, int role)
    {
        if (role == 4)
        {
            foreach (var old in pawn.health.hediffSet.hediffs.Where(h => (h.def == MCW_HediffDefOf.Mincho_Mintchoco_Hediff || h.def == MCW_HediffDefOf.Mincho_PinkFallInLove_Hediff)).ToList()) pawn.health.RemoveHediff(old);
            var flavor = MCW_HediffDefOf.Mincho_BluePolarBear_Hediff;
            if (!pawn.health.hediffSet.HasHediff(flavor)) pawn.health.AddHediff(flavor);
        }
        pawn.story.hairDef = DefDatabase<HairDef>.GetNamed(HairName(role));
        pawn.story.headType = DefDatabase<HeadTypeDef>.GetNamed($"Mincho_Head_Large_{(role == 4 ? "BluePolarBear" : "Mintchoco")}_{Head[role]:00}");
        pawn.story.HairColor = Color.white;
        var legs = MCW_HediffDefOf.Mincho_Humanlike_Body_Hediff;
        if (!pawn.health.hediffSet.HasHediff(legs)) pawn.health.AddHediff(legs);
        pawn.story.bodyType = MCW_DefOf.Mincho_Body_Large_Mintchoco_Humanlike;
        FacialAppearance.Apply(pawn, role);
        pawn.Drawer.renderer.SetAllGraphicsDirty();
    }
}

// Small Mincho diadems preserve hair for every wearer. Keep all unrelated skip flags.
[HarmonyPatch(typeof(PawnRenderTree), "AdjustParms")]
static class MinchoDiademHair
{
    static void Postfix(PawnRenderTree __instance, ref PawnDrawParms parms)
    {
        if (Current.Game == null) return;
        var pawn = __instance.pawn;
        if (pawn.def != MCW_ThingDefOf.Mincho_ThingDef) return;
        var hats = pawn.apparel.WornApparel.Where(a => a.def.apparel.layers.Contains(ApparelLayerDefOf.Overhead)).ToList();
        if (hats.Count == 1 && (hats[0].def == MCW_ThingDefOf.Mincho_MintchocoDonutTiara || hats[0].def == MCW_ThingDefOf.Mincho_KnightlyTiara || hats[0].def == MCW_ThingDefOf.MCW_LinglonTiara || hats[0].def == MCW_ThingDefOf.MCW_QinxinCrown))
            parms.skipFlags &= ~(ulong)RenderSkipFlagDefOf.Hair;
    }
}
