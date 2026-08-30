using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace MinchoCandyWars.Patch
{
    [HarmonyPatch(typeof(Pawn),nameof(Pawn.GetDisabledWorkTypes))]
    internal class Patch_Pawn
    {
        [HarmonyPostfix]
        public static void Postfix_GetDisabledWorkTypes(Pawn __instance)
        {
            __instance.GetComp<CompMinchoCore>()?.TryRemoveDisabledWorkTypes();
        }
    }
}
