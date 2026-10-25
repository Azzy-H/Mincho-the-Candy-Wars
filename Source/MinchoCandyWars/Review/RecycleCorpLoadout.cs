using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MinchoCandyWars.Review;

/// <summary>
/// 派系配装：给该派系生成的 pawn 按概率加一件指定装备。
/// 挂在 FactionDef 上（目前只有回收公司用）。
/// </summary>
public class FactionLoadoutExtension : DefModExtension
{
    /// <summary>要配发的装备。</summary>
    public ThingDef apparel = null!;

    /// <summary>配发概率（0~1）。</summary>
    public float chance;

    /// <summary>只给能战斗的人配（跳过非人形与「暴力」工作被禁用者）。</summary>
    public bool onlyFighters = true;
}

/// <summary>按 FactionDef 上的 <see cref="FactionLoadoutExtension"/> 配装。</summary>
[HarmonyPatch(typeof(PawnApparelGenerator), nameof(PawnApparelGenerator.GenerateStartingApparelFor))]
public static class FactionLoadout
{
    public static void Postfix(Pawn pawn)
    {
        FactionLoadoutExtension? ext = pawn.Faction?.def.GetModExtension<FactionLoadoutExtension>();
        if (ext?.apparel == null) return;

        // 与原实现一致：只要派系配了 loadout 就掷一次，保持随机序列的消耗位置不变。
        float roll = Rand.Value;

        if (pawn.apparel == null) return;
        if (ext.onlyFighters
            && (!pawn.RaceProps.Humanlike || pawn.WorkTagIsDisabled(WorkTags.Violent)))
        {
            return;
        }

        if (roll >= ext.chance) return;

        ThingDef apparel = ext.apparel;
        if (pawn.apparel.WornApparel.Any(worn =>
                !ApparelUtility.CanWearTogether(worn.def, apparel, pawn.RaceProps.body)))
        {
            return;
        }

        pawn.apparel.Wear((Apparel)ThingMaker.MakeThing(apparel));
    }
}
