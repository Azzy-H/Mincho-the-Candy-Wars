using HarmonyLib;
using RimWorld;
using Verse;

namespace MinchoCandyWars.Misc;

/// <summary>
/// 复用型灭火器：没燃料不能点引信，引爆时扣掉一次喷射量。
///
/// 为什么必须打补丁：<c>CompExplosive.StartWick</c> 是 public 非虚、<c>Detonate</c> 是
/// protected 非虚，<c>CompProperties_Explosive</c> 也没有"燃料不足"字段，子类化和纯 Def
/// 都表达不了。触发路径有 4 条（使用 / 近炸 / 受击 / 受伤阈值），必须拦在最里面。
///
/// 燃料量全部来自 <c>CompProperties_Refuelable</c>，代码里不出现数字：
/// 改 Def 的 fuelCapacity / minimumFueledThreshold 即可。
/// </summary>
[HarmonyPatch(typeof(CompExplosive))]
public static class FirefoamChargeGate
{
    private static bool IsPopper(CompExplosive comp) =>
        comp.parent.def == MCW_DefOf.MCW_FirefoamPopper;

    /// <summary>HasFuel 的定义就是 fuel ≥ Props.minimumFueledThreshold（Def 里为 50）。</summary>
    private static bool HasCharge(CompExplosive comp) =>
        comp.parent.GetComp<CompRefuelable>()?.HasFuel == true;

    /// <summary>扣掉一次喷射所需燃料；不足则返回 false。所需量同样取 minimumFueledThreshold。</summary>
    private static bool TryConsumeCharge(CompExplosive comp)
    {
        CompRefuelable fuel = comp.parent.GetComp<CompRefuelable>();
        if (fuel == null || !fuel.HasFuel) return false;
        fuel.ConsumeFuel(fuel.Props.minimumFueledThreshold);
        return true;
    }

    [HarmonyPatch(nameof(CompExplosive.StartWick))]
    [HarmonyPrefix]
    public static bool StartWickPrefix(CompExplosive __instance) =>
        !IsPopper(__instance) || HasCharge(__instance);

    /// <summary>Detonate 是 protected，不能用 nameof。</summary>
    [HarmonyPatch("Detonate")]
    [HarmonyPrefix]
    public static bool DetonatePrefix(CompExplosive __instance, Map map)
    {
        if (!IsPopper(__instance)) return true;
        if (map == null || !__instance.parent.SpawnedOrAnyParentSpawned) return false;
        if (TryConsumeCharge(__instance)) return true;

        // 燃料不足：立刻熄引信，避免出现"点了引信却不爆"的状态。
        __instance.StopWick();
        return false;
    }
}
