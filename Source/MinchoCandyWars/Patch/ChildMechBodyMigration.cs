using System.Linq;
using HarmonyLib;
using Verse;

namespace MinchoCandyWars.Patch;

// Old saves retain the original BodyDef in each injury's body-part reference.
[HarmonyPatch(typeof(Pawn), nameof(Pawn.ExposeData))]
static class ChildMechBodyMigration
{
    static void Postfix(Pawn __instance)
    {
        if (Scribe.mode != LoadSaveMode.PostLoadInit ||
            __instance.def.GetModExtension<Review.ChildMechExtension>() == null ||
            __instance.RaceProps.body != MCW_DefOf.Mincho_BodyDef)
            return;

        var parts = __instance.RaceProps.body.AllParts;
        bool changed = false;
        foreach (Hediff injury in __instance.health.hediffSet.hediffs)
        {
            if (injury.Part?.body != MCW_DefOf.Mech_Light)
                continue;

            // Feet share their corresponding leg. Hearing and smell belong to the core.
            int index = injury.Part.Index switch
            {
                0 => 0,
                1 => 1,
                2 => 2,
                3 => 4,
                4 => 6,
                5 => 3,
                6 => 3,
                7 or 8 => 9,
                9 or 10 => 10,
                11 => 3,
                12 => 0,
                _ => -1
            };
            if (index < 0)
                continue;

            injury.Part = parts[index];
            changed = true;
        }

        if (changed)
            __instance.health.hediffSet.DirtyCache();
    }
}
