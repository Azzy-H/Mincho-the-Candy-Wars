using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MinchoCandyWars.Buildings;

/// <summary>冰淇淋柜：通电（或加了燃料）时把柜内物品的温度压到目标值以下。</summary>
public class Building_IceCreamCabinet : Building_Storage
{
    /// <summary>柜内目标温度，与译文 MinchoCandyWars.UI.ColdStorage.Cooling 里的数值保持一致。</summary>
    public const float TargetTemperature = -5f;

    public bool CoolingActive
    {
        get
        {
            if (GetComp<CompFlickable>() is { SwitchIsOn: false }) return false;
            var power = GetComp<CompPowerTrader>();
            return power != null ? power.PowerOn : GetComp<CompRefuelable>()?.HasFuel == true;
        }
    }

    public float StorageTemperature(float ambient) => CoolingActive ? Math.Min(ambient, TargetTemperature) : ambient;

    public override string GetInspectString()
    {
        var text = base.GetInspectString();
        return text + (text.Length > 0 ? "\n" : "") + (CoolingActive
            ? "MinchoCandyWars.UI.ColdStorage.Cooling".Translate()
            : "MinchoCandyWars.UI.ColdStorage.NotCooling".Translate());
    }
}

/// <summary>
/// 柜内物品的环境温度改由柜子决定。
/// <c>Thing.AmbientTemperature</c> 是原版的非虚属性、没有可替换的入口，只能打补丁。
/// </summary>
[HarmonyPatch(typeof(Thing), nameof(Thing.AmbientTemperature), MethodType.Getter)]
public static class CabinetTemperature
{
    public static void Postfix(Thing __instance, ref float __result)
    {
        if (__instance.def.category != ThingCategory.Item || !__instance.Spawned) return;
        if (__instance.Position.GetSlotGroup(__instance.Map)?.parent is Building_IceCreamCabinet cabinet)
        {
            __result = cabinet.StorageTemperature(__result);
        }
    }
}

/// <summary>
/// 柜内物品不单独绘制：柜子自己会画出内容物，否则物品会叠在柜子上。
/// 三个绘制入口各挡一次（Thing 与 ThingWithComps 各自声明了重写）。
/// </summary>
[HarmonyPatch]
public static class CabinetContents
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        // DrawAt 是 protected，不能用 nameof，只能写字面量。
        string[] methods = { "Print", "DrawAt", "DrawGUIOverlay" };
        foreach (Type type in new[] { typeof(Thing), typeof(ThingWithComps) })
        {
            foreach (string method in methods)
            {
                MethodBase target = AccessTools.DeclaredMethod(type, method);
                if (target != null) yield return target;
            }
        }
    }

    [HarmonyPrefix]
    public static bool SkipDraw(Thing __instance)
    {
        return __instance.def.category != ThingCategory.Item || !__instance.Spawned ||
            __instance.Position.GetSlotGroup(__instance.Map)?.parent is not Building_IceCreamCabinet;
    }
}
