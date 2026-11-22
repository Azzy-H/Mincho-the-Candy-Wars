using HarmonyLib;
using UnityEngine;
using Verse;

namespace MinchoCandyWars.Patch;

// Keep the side and back alignment. The front already has a v57 upward shift baked into the sprite.
[HarmonyPatch(typeof(PawnRenderNodeWorker), nameof(PawnRenderNodeWorker.OffsetFor))]
static class TwinTailFrontFit
{
    static void Postfix(PawnRenderNode node, PawnDrawParms parms, ref Vector3 __result)
    {
        if (node is PawnRenderNode_Hair && parms.facing != Rot4.South &&
            parms.pawn.story?.hairDef == MCW_DefOf.MCW_Hair_Linglon174)
            __result.z += 0.06f;
    }
}

