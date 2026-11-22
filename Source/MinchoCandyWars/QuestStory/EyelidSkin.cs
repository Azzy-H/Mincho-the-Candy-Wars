using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace MinchoCandyWars.QuestStory;

// Match the cover palette when FA loads graphics, not on every animation tick.
[HarmonyPatch]
static class EyelidSkin
{
    static bool Prepare() => ModsConfig.IsActive("nals.facialanimation")
        && ModsConfig.IsActive("talos.mincho.fa")
        && AccessTools.TypeByName("FacialAnimation.LidControllerComp") != null;

    static IEnumerable<MethodBase> TargetMethods()
    {
        if (!ModsConfig.IsActive("nals.facialanimation") || !ModsConfig.IsActive("talos.mincho.fa")) yield break;
        var type = AccessTools.TypeByName("FacialAnimation.LidControllerComp");
        if (type != null) yield return AccessTools.Method(type, "LoadTextures");
    }

    static void Prefix(ThingComp __instance)
    {
        if (__instance.parent is not Pawn pawn || pawn.def != MCW_ThingDefOf.Mincho_ThingDef) return;
        var name = AccessTools.Property(__instance.GetType(), "FaceTypeDefName");
        var current = name.GetValue(__instance) as string;
        if (current == null) return;
        // Keep the bespoke eye paired with a side mask whose opening fits the native iris.
        foreach (var eye in pawn.AllComps)
            if (eye.GetType().FullName == "FacialAnimation.EyeballControllerComp" &&
                AccessTools.Property(eye.GetType(), "FaceTypeDefName").GetValue(eye) as string == "MCW_Face124_2_Eye")
            {
                name.SetValue(__instance, "MCW_Lid_Nectar31");
                return;
            }
        foreach (var comp in pawn.AllComps)
        {
            if (comp.GetType().FullName != "FacialAnimation.HeadControllerComp") continue;
            var head = AccessTools.Property(comp.GetType(), "FaceTypeDefName").GetValue(comp) as string;
            const string variant = "MCW_LidBlue_";
            const string original = "Mincho_Lid_";
            if (head == "Mincho_Head_Blue" && current.StartsWith(original) && current != "Mincho_Lid_Blue")
                name.SetValue(__instance, variant + current.Substring(original.Length));
            else if (head == "Mincho_Head_00" && current.StartsWith(variant))
                name.SetValue(__instance, original + current.Substring(variant.Length));
            else if (head == "Mincho_Head_00" && current == "Mincho_Lid_Blue")
                name.SetValue(__instance, "Mincho_Lid_08");
            return;
        }
    }
}

