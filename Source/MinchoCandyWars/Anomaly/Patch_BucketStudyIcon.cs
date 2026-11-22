using HarmonyLib;
using RimWorld;
using UnityEngine;
using System.Reflection.Emit;

namespace MinchoCandyWars.Anomaly;

// The native study tab draws every source texture into a square without preserving aspect.
[HarmonyPatch(typeof(ITab_StudyNotes), "DrawTitle")]
static class Patch_BucketStudyIcon
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var original = AccessTools.Method(typeof(GUI), nameof(GUI.DrawTexture), new[] { typeof(Rect), typeof(Texture) });
        var replacement = AccessTools.Method(typeof(Patch_BucketStudyIcon), nameof(DrawIcon));
        foreach (var instruction in instructions)
        {
            if (instruction.Calls(original))
                yield return new CodeInstruction(OpCodes.Call, replacement);
            else
                yield return instruction;
        }
    }

    static void DrawIcon(Rect rect, Texture texture)
    {
        if (texture != null && texture == MCW_ThingDefOf.MCW_EndlessIceCream?.uiIcon)
            GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit);
        else
            GUI.DrawTexture(rect, texture);
    }
}
