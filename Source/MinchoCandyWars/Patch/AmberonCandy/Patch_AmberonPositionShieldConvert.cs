using HarmonyLib;
using RimWorld;
using Verse;

namespace MinchoCandyWars.Patch
{
    /// <summary>
    /// 琥珀位置护盾：拦截的子弹有 10% 概率生成甜冰（Mincho_Mintchoco）。
    /// 通过 patch CompProjectileInterceptor.CheckIntercept 在拦截成功后处理。
    /// </summary>
    [StaticConstructorOnStartup]
    [HarmonyPatch(typeof(CompProjectileInterceptor), nameof(CompProjectileInterceptor.CheckIntercept))]
    public class Patch_AmberonPositionShieldConvert
    {
        static Patch_AmberonPositionShieldConvert()
        {
            new Harmony("MinchoCandyWars.Patch.AmberonPositionShieldConvert").PatchAll();
        }

        public static void Postfix(CompProjectileInterceptor __instance, Projectile projectile, ref bool __result)
        {
            if (!__result) return;
            if (__instance.parent?.def != MCW_DefOf.MCW_AmberonPositionShield) return;
            if (projectile?.Launcher == null) return;
            if (!Rand.Chance(0.1f)) return;

            Map map = __instance.parent.Map;
            if (map == null) return;
            IntVec3 cell = __instance.parent.Position;
            if (MCW_DefOf.Mincho_Mintchoco != null)
            {
                Thing mint = ThingMaker.MakeThing(MCW_DefOf.Mincho_Mintchoco);
                if (mint != null)
                {
                    GenPlace.TryPlaceThing(mint, cell, map, ThingPlaceMode.Near);
                }
            }
        }
    }
}
