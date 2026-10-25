using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace MinchoCandyWars.Abilities
{
    /// <summary>
    /// 把「护盾 Hediff 吸收伤害」接进原版伤害管线的适配器。
    ///
    /// 为什么需要它：<c>ThingWithComps.PreApplyDamage</c> 只遍历 pawn 自己的 ThingComp
    /// （<c>comp.PostPreApplyDamage</c>），不会去看 HediffComp，
    /// 所以以前只能靠 Harmony 拦截 <c>Pawn.PreApplyDamage</c>。
    ///
    /// 本类**不保存任何数据**：护盾能量、重置计时都在 HediffComp 上，
    /// 这里只是把受伤事件转发过去，因此没有 ExposeData 覆写。
    /// </summary>
    public class ThingComp_EnergyShieldAbsorb : ThingComp
    {
        public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;
            if (parent is not Pawn pawn || pawn.health?.hediffSet == null) return;

            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (hediffs[i] is not HediffWithComps hediffWithComps) continue;

                List<HediffComp> comps = hediffWithComps.comps;
                for (int j = 0; j < comps.Count; j++)
                {
                    if (comps[j] is HediffComp_EnergyShield shield && shield.TryAbsorbDamage(ref dinfo))
                    {
                        absorbed = true;
                        return;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 动态挂载 / 摘除 <see cref="ThingComp_EnergyShieldAbsorb"/>。
    ///
    /// 为什么要碰私有字段：<c>ThingWithComps.AllComps</c> 在内部 comps 为 null 时会返回一个
    /// **共享的静态空列表**，往它 Add 会污染所有 Thing；所以这里用 FieldRef 拿真实字段，
    /// 必要时自己初始化一个列表。
    ///
    /// 注意 <c>compsByType</c> 缓存不会包含动态添加的组件，
    /// 因此只能用 <c>AllComps</c> / 直接遍历来查找它，不要用 <c>GetComp&lt;T&gt;()</c>。
    /// </summary>
    public static class EnergyShieldAbsorbComp
    {
        private static readonly AccessTools.FieldRef<ThingWithComps, List<ThingComp>> CompsRef =
            AccessTools.FieldRefAccess<ThingWithComps, List<ThingComp>>("comps");

        /// <summary>有护盾 Hediff 时确保 pawn 身上挂着吸收组件（幂等）。</summary>
        public static void Attach(Pawn pawn)
        {
            if (pawn == null) return;

            List<ThingComp> comps = CompsRef(pawn) ?? new List<ThingComp>();
            CompsRef(pawn) = comps;

            for (int i = 0; i < comps.Count; i++)
            {
                if (comps[i] is ThingComp_EnergyShieldAbsorb) return;
            }

            comps.Add(new ThingComp_EnergyShieldAbsorb { parent = pawn });
        }

        /// <summary>护盾 Hediff 被移除后调用：身上还有别的护盾就保留组件。</summary>
        public static void DetachIfUnused(Pawn pawn)
        {
            if (pawn == null) return;

            List<ThingComp>? comps = CompsRef(pawn);
            if (comps == null || comps.Count == 0) return;

            List<Hediff>? hediffs = pawn.health?.hediffSet?.hediffs;
            if (hediffs != null)
            {
                for (int i = 0; i < hediffs.Count; i++)
                {
                    if (hediffs[i] is not HediffWithComps hediffWithComps) continue;
                    for (int j = 0; j < hediffWithComps.comps.Count; j++)
                    {
                        if (hediffWithComps.comps[j] is HediffComp_EnergyShield) return;
                    }
                }
            }

            comps.RemoveAll(comp => comp is ThingComp_EnergyShieldAbsorb);
        }
    }
}
