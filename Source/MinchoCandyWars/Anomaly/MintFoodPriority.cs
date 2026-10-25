using Verse;

namespace MinchoCandyWars.Anomaly;

/// <summary>
/// 薄荷成瘾者的进食优先级规则。
///
/// 只影响「非珉巧、正在成瘾、且成瘾未被解除」的人形；没有任何成瘾的人完全不受影响。
///
/// 未启用 Anomaly 时，这里的三个 DefOf 都是 null（[MayRequireAnomaly] 会让 DefOfHelper
/// 跳过赋值），两次查找都返回 null → IsAffected 恒为 false，整套优先级自动关闭。
/// </summary>
public static class MintFoodPriority
{
    /// <summary>这个人形是否需要薄荷进食优先级。</summary>
    public static bool IsAffected(Pawn pawn)
    {
        if (pawn?.RaceProps?.Humanlike != true) return false;
        if (MintAddiction.IsMincho(pawn)) return false;
        if (pawn.health == null) return false;

        HediffSet hediffs = pawn.health.hediffSet;
        if (hediffs.GetFirstHediffOfDef(MCW_DefOf.MCW_MintAddiction) == null) return false;

        return hediffs.GetFirstHediffOfDef(MCW_DefOf.MCW_MintFreedom) == null;
    }

    /// <summary>
    /// 食物档位：0 = 普通，1 = 薄荷类，2 = 无尽冰淇淋桶（活跃版）。
    ///
    /// 解除版（<c>MCW_EndlessIceCream_Released</c>）按**普通薄荷类**处理：
    /// 它本就不该再让人成瘾、营养也只有 0.2；即使存档里残留成瘾，也不给它额外档位。
    /// </summary>
    public static int Rank(Thing food)
    {
        if (food?.def?.ingestible == null || !food.def.IsNutritionGivingIngestible || food.def.IsDrug) return 0;

        if (food.def == MCW_DefOf.MCW_EndlessIceCream) return 2;

        return MintAddiction.IsMintFood(food) ? 1 : 0;
    }
}
