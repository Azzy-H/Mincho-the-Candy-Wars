using HarmonyLib;
using Verse;

namespace MinchoCandyWars.Render
{
    // This animal supplies a west-facing side sprite. Pawn meshes otherwise assume an east-facing source.
    [HarmonyPatch(typeof(PawnRenderNode_AnimalPart), nameof(PawnRenderNode_AnimalPart.MeshSetFor))]
    public static class SlimeSideFacing
    {
        public static void Postfix(PawnRenderNode_AnimalPart __instance, Pawn pawn, ref GraphicMeshSet __result)
        {
            if (pawn.def != MCW_ThingDefOf.MCW_MinchoSlime || __result == null) return;
            Graphic graphic = __instance.GraphicFor(pawn);
            if (graphic == null || !graphic.EastFlipped || graphic.WestFlipped) return;
            __result = new GraphicMeshSet(__result.MeshAt(Rot4.North), __result.MeshAt(Rot4.West),
                __result.MeshAt(Rot4.South), __result.MeshAt(Rot4.East));
        }
    }
}
