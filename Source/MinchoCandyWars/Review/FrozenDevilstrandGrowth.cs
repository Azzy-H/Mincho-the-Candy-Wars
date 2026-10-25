using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MinchoCandyWars.Patch;

/// <summary>
/// 超低温恶魔菇：越冷长得越快，-5℃ 及以下开始加成，-100℃ 封顶 ×2。
///
/// 为什么必须打补丁：原版 <c>PlantUtility.GrowthRateFactorFor_Temperature</c> 用的是
/// <c>InverseLerp</c>，上限就是 1.0，纯 Def 给不出大于 1 的加成。
///
/// 为什么改温度因子而不是 <c>GrowthRate</c>：inspect 面板显示的正是各因子，
/// 改 <c>GrowthRate</c> 会让面板显示 ×1、实际却是 ×2。
/// </summary>
[HarmonyPatch(typeof(Plant), nameof(Plant.GrowthRateFactor_Temperature), MethodType.Getter)]
public static class FrozenDevilstrandGrowth
{
    /// <summary>低于此温度开始获得生长加成。</summary>
    private const float BoostStartTemperature = -5f;

    /// <summary>从起始温度再降多少度达到满加成（-5℃ → -100℃）。</summary>
    private const float BoostFullSpan = 95f;

    /// <summary>温度生长倍率：-5℃ 及以上 ×1.0，-100℃ 及以下 ×2.0，中间线性、两端截断。</summary>
    private static float ColdBoost(float temperature) =>
        1f + Math.Clamp((BoostStartTemperature - temperature) / BoostFullSpan, 0f, 1f);

    public static void Postfix(Plant __instance, ref float __result)
    {
        if (__instance.def == MCW_DefOf.MCW_Frozen_Devilstrand_Plant && __instance.Spawned)
        {
            __result *= ColdBoost(__instance.AmbientTemperature);
        }
    }
}
