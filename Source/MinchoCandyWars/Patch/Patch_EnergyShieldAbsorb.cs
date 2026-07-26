using HarmonyLib;
using MinchoCandyWars.Abilities;
using Verse;

namespace MinchoCandyWars.Patch
{
    /// <summary>
    /// 护盾腰带式能量护盾 —— 在 Pawn.PreApplyDamage 中拦截伤害。
    /// 检查 HediffComp_EnergyShield 子类，调用 TryAbsorbDamage。
    /// </summary>
    [StaticConstructorOnStartup]
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.PreApplyDamage))]
    public class Patch_EnergyShieldAbsorb
    {
        static Patch_EnergyShieldAbsorb()
        {
            new Harmony("MinchoCandyWars.Patch.EnergyShieldAbsorb").PatchAll();
        }

        public static bool Prefix(Pawn __instance, ref DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;
            if (__instance?.health?.hediffSet == null) return true;

            var hediffs = __instance.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (hediffs[i] is HediffWithComps hediffWithComps)
                {
                    foreach (var comp in hediffWithComps.comps)
                    {
                        if (comp is HediffComp_EnergyShield shield && shield.TryAbsorbDamage(ref dinfo))
                        {
                            absorbed = true;
                            return false;
                        }
                    }
                }
            }

            return true;
        }
    }
}