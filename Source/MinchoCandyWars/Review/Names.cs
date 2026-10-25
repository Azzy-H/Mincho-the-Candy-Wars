using HarmonyLib;
using RimWorld;
using Verse;

namespace MinchoCandyWars.Review;

/// <summary>
/// 联盟派系的珉巧使用专属名字池（糖果名）。
///
/// 为什么必须打补丁：原版给 pawn 起名只有 5 个入口
/// （xenotype → PawnKindDef.nameMaker → 背景故事 → 种族 nameGenerator → 文化），
/// 没有"按派系"这一层。FactionDef 上确实有三个 RulePackDef 字段
/// （factionNameMaker / settlementNameMaker / playerInitialSettlementNameMaker），
/// 但分别用于派系名和定居点名，管不到 pawn。
///
/// TODO(PawnKind)：更干净的做法是做一个联盟专属的 PawnKindDef，
/// ParentName 继承对应的原版 kind，只覆盖 nameMaker 为 MCW_AllianceCandyNames，
/// 让原版命名链自己选中这个池子；届时本补丁即可删除。
/// 这一块属于 PawnKind / XML 的职责，不在本文件范围内。
///
/// 目前没这么做的原因：联盟据点与投奔事件用的 kind（Mincho_Colonist、
/// Mincho_SpaceRefugee、Mincho_Mercenary 等）同时也被玩家阵营生成
/// （例如 IncidentWorker_RecruitMincho 用 Faction.OfPlayer 生成难民），
/// 直接在共用 kind 上写 nameMaker 会连带影响玩家招进来的珉巧。
/// </summary>
[HarmonyPatch(typeof(PawnBioAndNameGenerator), nameof(PawnBioAndNameGenerator.GeneratePawnName))]
public static class AllianceCandyNames
{
    private static bool ShouldUsePool(Pawn pawn, NameStyle style, string forcedLastName)
        => pawn != null
           && pawn.Name == null
           && pawn.def == MCW_DefOf.Mincho_ThingDef
           && pawn.Faction?.def == MCW_DefOf.MCW_Alliance_Faction
           && style == NameStyle.Full
           && forcedLastName == null
           && pawn.kindDef != null
           && pawn.kindDef.GetNameMaker(pawn.gender) == null;

    // Priority.First：必须抢在 HAR 等其它命名补丁之前决定用哪个池子。
    [HarmonyPrefix, HarmonyPriority(Priority.First)]
    public static bool Prefix(Pawn pawn, NameStyle style, string forcedLastName, ref Name __result)
    {
        if (!ShouldUsePool(pawn, style, forcedLastName)) return true;

        RulePackDef? pool = MCW_DefOf.MCW_AllianceCandyNames;
        if (pool == null) return true;

        // 与原版种族命名器（PawnBioAndNameGenerator.GenerateFullPawnName 里的 nameGenner 分支）
        // 完全一致：撞满池子时原版会记录错误并返回重名，这里不额外兜底。
        __result = new NameSingle(NameGenerator.GenerateName(pool, name => !new NameSingle(name).UsedThisGame));
        return false;
    }
}