using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Ciao.MCW.LocalReview.Apparel030;

// Native UI source-rectangle selection only. No PNG pixels, worn graphics or world scale are edited.
public static class IconFraming
{
    public static bool TryFrame(string name, float scale, out Rect uv, out Vector2 proportions, out float framedScale)
    {
        (int width, int height, int x, int y, int right, int bottom) bounds;
        switch (name)
        {
            case "MCW_NurseOutfit": bounds = (128, 128, 22, 6, 110, 126); break;
            case "MCW_NurseCap": bounds = (128, 128, 24, 38, 103, 81); break;
            case "MCW_Bikini": bounds = (128, 128, 20, 6, 123, 128); break;
            case "MCW_BikiniBW": bounds = (128, 128, 12, 0, 128, 128); break;
            case "MCW_GorgeousDress": bounds = (256, 256, 77, 81, 224, 247); break;
            case "MCW_Qipao": bounds = (1024, 1024, 397, 408, 627, 823); break;
            case "MCW_MechaArmor": bounds = (1024, 1024, 389, 398, 676, 879); break;
            case "MCW_HoloVisor": bounds = (1024, 1024, 375, 463, 649, 659); break;
            default: uv = default; proportions = default; framedScale = scale; return false;
        }
        proportions = new Vector2(bounds.right - bounds.x, bounds.bottom - bounds.y);
        uv = new Rect((float)bounds.x / bounds.width, 1f - (float)bounds.bottom / bounds.height,
            proportions.x / bounds.width, proportions.y / bounds.height);
        framedScale = scale * 0.84f;
        return true;
    }
}

[HarmonyPatch(typeof(Widgets), "ThingIconWorker")]
public static class ApparelIconDisplay
{
    public static bool Prefix(Rect rect, ThingDef thingDef, Texture resolvedIcon, float resolvedIconAngle,
        float scale, Material mat, float alpha)
    {
        // Respect styles or other mods supplying an alternate icon.
        if (!ReferenceEquals(resolvedIcon, thingDef.uiIcon) ||
            !IconFraming.TryFrame(thingDef.defName, scale, out Rect uv, out Vector2 proportions, out float framedScale))
            return true;
        Color previous = GUI.color;
        // Match the user-approved 025 palette rule, including newly stuffable mechanitor gear.
        if (thingDef.defName != "MCW_Bikini" && thingDef.defName != "MCW_BikiniBW")
            GUI.color = new Color(1f, 1f, 1f, previous.a);
        Widgets.DrawTextureFitted(rect, resolvedIcon, framedScale, proportions, uv, resolvedIconAngle, mat, alpha);
        GUI.color = previous;
        return false;
    }
}

// The vanilla multiplier stat is shared by all armor types. Only these two documented
// outfits use a separate 20% heat-material multiplier; explanation and value share this getter.
[HarmonyPatch(typeof(StatPart_Stuff), "GetMultiplier")]
public static class MechanitorHeatMaterial
{
    public static void Postfix(StatPart_Stuff __instance, StatRequest req, ref float __result)
    {
        if (__instance.multiplierStat != StatDefOf.StuffEffectMultiplierArmor ||
            __instance.parentStat != StatDefOf.ArmorRating_Heat) return;
        string name = req.BuildableDef?.defName;
        if (name == "MCW_MechaArmor" || name == "MCW_HoloVisor") __result = 0.2f;
    }
}
