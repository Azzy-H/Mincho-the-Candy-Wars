using AlienRace;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace MinchoCandyWars.Recreation;

internal static class CocoaBathRendering
{
    internal static Building_CocoaBath? ActiveBath(Pawn pawn)
    {
        if (pawn.CurJob?.def != MCW_DefOf.MCW_UseCocoaBath) return null;
        return pawn.CurJob.targetA.Thing is Building_CocoaBath bath && bath.Spawned && bath.Phase == 2 && bath.Bather == pawn ? bath : null;
    }
}

[HarmonyPatch(typeof(PawnRenderer), "GetDrawParms")]
internal static class CocoaBathHeadOnly
{
    static void Postfix(PawnRenderer __instance, ref PawnDrawParms __result)
    {
        if (__result.Portrait || __result.pawn == null) return;
        var bath = CocoaBathRendering.ActiveBath(__result.pawn);
        if (bath == null) return;
        // 身体 / 衣物 / 头部装备的隐藏，以及头部的 0.5 下沉，都由原版对
        // Pawn.Swimming 的处理完成（见 PawnRenderer.DrawPawn）。这里只补原版管不到的部分：
        // 躺姿替换、朝向，以及把整个绘制矩阵挪到浴缸顶部。
        __result.posture = PawnPosture.Standing;
        __result.facing = new Rot4((bath.Rotation.AsInt + 2) % 4);
        var headPosition = bath.DrawPos + (bath.Rotation.IsHorizontal ? new Vector3(.55f, 0f, .2f) : new Vector3(0f, 0f, .65f));
        headPosition.y = AltitudeLayer.Pawn.AltitudeFor();
        __result.matrix = Matrix4x4.TRS(headPosition, Quaternion.identity, Vector3.one);
    }
}

// HAR's tail/slime body is an additional render node, not the vanilla body.
// 原版 PawnRenderFlags.NoBody 只被 PawnRenderNodeWorker_Body 消费，管不到附加体节点，
// 所以这里单独过滤：保留头对齐的面部附加件，隐藏沉在水下的身体附加件。
[HarmonyPatch(typeof(AlienPawnRenderNodeWorker_BodyAddon), nameof(AlienPawnRenderNodeWorker_BodyAddon.CanDrawNow))]
internal static class CocoaBathAlienBody
{
    static void Postfix(PawnRenderNode node, PawnDrawParms parms, ref bool __result)
    {
        if (!__result || parms.Portrait || CocoaBathRendering.ActiveBath(parms.pawn) == null) return;
        if (node.Props is AlienPawnRenderNodeProperties_BodyAddon props
            && props.addon != null
            && !props.addon.alignWithHead)
            __result = false;
    }
}
