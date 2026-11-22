using HarmonyLib;
using Verse;
using RimWorld;

namespace MinchoCandyWars.Patch;

// Earlier saves attached the helmet decoration to the torso armor.
[HarmonyPatch(typeof(Pawn), nameof(Pawn.ExposeData))]
static class MechaHeadAttachment
{
    static void Postfix(Pawn __instance)
    {
        if (Scribe.mode != LoadSaveMode.PostLoadInit || __instance.apparel == null)
            return;
        var decoration = __instance.health.hediffSet.GetFirstHediffOfDef(
            MCW_HediffDefOf.MCW_MechaArmor_AddonHediff);
        bool visor = __instance.apparel.WornApparel.Any(a => MCW_ApparelTags.HasTag(a, MCW_ApparelTags.FaceCover));
        if (!visor)
        {
            if (decoration != null) __instance.health.RemoveHediff(decoration);
            return;
        }
        var head = __instance.RaceProps.body.AllParts.FirstOrDefault(p => p.def == BodyPartDefOf.Head);
        if (head == null) return;
        if (decoration != null)
            decoration.Part = head;
        else
            __instance.health.AddHediff(MCW_HediffDefOf.MCW_MechaArmor_AddonHediff, head);
    }
}

