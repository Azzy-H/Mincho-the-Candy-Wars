using HarmonyLib;
using UnityEngine;
using Verse;
using Ciao.MCW.LocalReview.Apparel030;

namespace Ciao.MCW.LocalReview.Geometry032;

// New item PNGs already have approved padding. Do not crop them a second time.
// Retain the installed 030 assembly and its independent heat-material patch.
[HarmonyPatch(typeof(IconFraming), nameof(IconFraming.TryFrame))]
public static class NormalizedItemFraming
{
    public static bool Prefix(string name, float scale, ref Rect uv,
        ref Vector2 proportions, ref float framedScale, ref bool __result)
    {
        if (DefDatabase<ThingDef>.GetNamedSilentFail(name)?.uiIconPath?.StartsWith("Things/Apparel/Icons/", System.StringComparison.Ordinal) != true)
            return true;
        // Keep 030's original palette handling, but use the entire approved canvas.
        uv = new Rect(0f, 0f, 1f, 1f);
        proportions = Vector2.one;
        framedScale = scale;
        __result = true;
        return false;
    }
}
