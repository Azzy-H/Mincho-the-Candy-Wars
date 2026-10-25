using HarmonyLib;
using RimWorld;
using Verse;

namespace MinchoCandyWars.Anomaly;

/// <summary>
/// 评分阶段就抬高薄荷类食物，让成瘾者在"吃什么"的比较里优先选它们。
///
/// <c>FoodUtility.FoodOptimality</c> 是原版静态方法，没有虚函数、也没有 Def 能表达
/// "某个 pawn 更偏好某类食物"，所以只能打补丁。
/// </summary>
[HarmonyPatch(typeof(FoodUtility), nameof(FoodUtility.FoodOptimality))]
public static class MintFoodOptimality
{
    /// <summary>只需压过原版评分的量级即可；原版对不可食用目标返回 -9999999。</summary>
    private const float PriorityBonus = 100000f;

    public static void Postfix(Pawn eater, Thing foodSource, bool takingToInventory, ref float __result)
    {
        // 往背包里装食物时不改偏好，只在"当场吃什么"的评分上加成。
        if (takingToInventory) return;
        if (!MintFoodPriority.IsAffected(eater)) return;

        __result += MintFoodPriority.Rank(foodSource) * PriorityBonus;
    }
}
