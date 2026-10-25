using HarmonyLib;
using RimWorld;
using Verse;

namespace MinchoCandyWars.Patch
{
    /// <summary>
    /// 琥珀位置护盾：拦截的子弹有 10% 概率转化为冰淇淋（三种随机一种）。
    /// 通过 patch CompProjectileInterceptor.CheckIntercept 在拦截成功后处理。
    /// </summary>
    [StaticConstructorOnStartup]
    [HarmonyPatch(typeof(CompProjectileInterceptor), nameof(CompProjectileInterceptor.CheckIntercept))]
    public class Patch_AmberonPositionShieldConvert
    {
        private const float ConvertChance = 0.1f;

        public static void Postfix(CompProjectileInterceptor __instance, Projectile projectile, ref bool __result)
        {
            if (!__result) return;
            if (__instance.parent?.def != MCW_DefOf.MCW_AmberonPositionShield) return;
            if (projectile?.Launcher == null) return;
            if (!Rand.Chance(ConvertChance)) return;

            Map map = __instance.parent.Map;
            if (map == null) return;
            IntVec3 cell = __instance.parent.Position;

            ThingDef iceCream = RandomIceCream();
            if (iceCream == null) return;

            Thing mint = ThingMaker.MakeThing(iceCream);
            if (mint != null)
            {
                GenPlace.TryPlaceThing(mint, cell, map, ThingPlaceMode.Near);
            }
        }

        /// <summary>三种薄荷冰淇淋里随机一种；某个 Def 缺失时退回薄荷巧克力。</summary>
        private static ThingDef RandomIceCream()
        {
            ThingDef pick = Rand.Range(0, 3) switch
            {
                1 => MCW_DefOf.Mincho_BluePolarBear,
                2 => MCW_DefOf.Mincho_PinkFallInLove,
                _ => MCW_DefOf.Mincho_Mintchoco,
            };
            return pick ?? MCW_DefOf.Mincho_Mintchoco;
        }
    }
}
