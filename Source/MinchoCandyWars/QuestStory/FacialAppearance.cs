#nullable disable
using HarmonyLib;
using Verse;

namespace MinchoCandyWars.QuestStory;

// Generated parts use the existing FA shapes and eyelid masks; keep its animation controller intact.
// Only named-character creation sets these identities; ordinary Mincho may draw the same parts from the FA random pool.
public static class FacialAppearance
{
    static readonly string[] Lids = ["08", "02", "05", "04", "08", "Pink"];

    public static void Apply(Pawn pawn, int role)
    {
        if (!ModsConfig.IsActive("nals.facialanimation") || !ModsConfig.IsActive("talos.mincho.fa")) return;
        foreach (var comp in pawn.AllComps)
        {
            string face = comp.GetType().FullName switch
            {
                "FacialAnimation.HeadControllerComp" => "Mincho_Head_" + (role == 4 ? "Blue" : "00"),
                "FacialAnimation.EyeballControllerComp" => $"MCW_Face124_{role}_Eye",
                "FacialAnimation.LidControllerComp" => role == 1 ? "MCW_Lid_Amber132" : "Mincho_Lid_" + Lids[role],
                "FacialAnimation.BrowControllerComp" => $"MCW_Face124_{role}_Brow",
                "FacialAnimation.MouthControllerComp" => $"MCW_Face124_{role}_Mouth",
                _ => null
            };
            if (face == null) continue;
            AccessTools.Property(comp.GetType(), "FaceTypeDefName").SetValue(comp, face);
            AccessTools.Method(comp.GetType(), "SetDirty").Invoke(comp, null);
        }
    }
}


