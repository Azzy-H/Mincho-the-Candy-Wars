using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace MinchoCandyWars.Patch;

[HarmonyPatch(typeof(StorytellerUI), nameof(StorytellerUI.DrawStorytellerSelectionInterface))]
internal static class QiaomingPortraitSize
{
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var portrait = AccessTools.Field(typeof(StorytellerDef), nameof(StorytellerDef.portraitLargeTex));
        var draw = AccessTools.Method(typeof(GUI), nameof(GUI.DrawTexture), [typeof(Rect), typeof(Texture)]);
        var replacement = AccessTools.Method(typeof(QiaomingPortraitSize), nameof(DrawLargePortrait));
        var button = AccessTools.Method(typeof(Widgets), nameof(Widgets.ButtonImage),
            [typeof(Rect), typeof(Texture2D), typeof(Color), typeof(Color), typeof(bool), typeof(string)]);
        var tinyReplacement = AccessTools.Method(typeof(QiaomingPortraitSize), nameof(DrawTinyPortrait));
        bool followsPortraitLoad = false;
        foreach (CodeInstruction instruction in instructions)
        {
            bool isPortraitLoad = instruction.LoadsField(portrait);
            if (followsPortraitLoad && instruction.Calls(draw))
                instruction.operand = replacement;
            else if (instruction.Calls(button))
                instruction.operand = tinyReplacement;
            yield return instruction;
            followsPortraitLoad = isPortraitLoad;
        }
    }

    private static bool DrawTinyPortrait(Rect rect, Texture2D texture, Color color,
        Color mouseoverColor, bool doMouseoverSound, string tooltip)
    {
        if (texture != DefDatabase<StorytellerDef>.GetNamedSilentFail("MCW_Qiaoming")?.portraitTinyTex)
            return Widgets.ButtonImage(rect, texture, color, mouseoverColor, doMouseoverSound, tooltip);

        float scale = Math.Min(rect.width / texture.width, rect.height / texture.height);
        var fitted = new Rect(rect.center.x - texture.width * scale / 2f,
            rect.center.y - texture.height * scale / 2f, texture.width * scale, texture.height * scale);
        // Preserve the full original selection area, including the unused margins.
        return Widgets.ButtonImage(fitted, texture, color, mouseoverColor, doMouseoverSound, tooltip)
            || Widgets.ButtonInvisible(rect, false);
    }

    private static void DrawLargePortrait(Rect rect, Texture texture)
    {
        if (texture != DefDatabase<StorytellerDef>.GetNamedSilentFail("MCW_Qiaoming")?.portraitLargeTex)
        {
            GUI.DrawTexture(rect, texture);
            return;
        }

        const float scale = 1.06f;
        float leftInset = rect.width * 0.10f;
        GUI.BeginGroup(new Rect(rect.x + leftInset, rect.y, rect.width - leftInset, rect.height));
        try
        {
            GUI.DrawTexture(new Rect(-leftInset - (scale - 1f) * rect.width / 2f, -(scale - 1f) * rect.height,
                rect.width * scale, rect.height * scale), texture);
        }
        finally
        {
            GUI.EndGroup();
        }
    }
}
