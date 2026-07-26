using HarmonyLib;
using RimWorld;
using Verse;

namespace MinchoCandyWars.Patch
{
    /// <summary>
    /// 琥珀糖心无敌：当 pawn 拥有 MCW_AmberonHeart 时，免疫所有 incoming damage。
    /// 在 Pawn_HealthTracker.PreApplyDamage 中标记 absorbed。
    /// </summary>
    [StaticConstructorOnStartup]
    [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.PreApplyDamage))]
    public class Patch_AmberonHeartInvincibility
    {
        static Patch_AmberonHeartInvincibility()
        {
            new Harmony("MinchoCandyWars.Patch.AmberonHeartInvincibility").PatchAll();
        }

        public static bool Prefix(Pawn_HealthTracker __instance, ref DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;
            Pawn? target = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (target?.health?.hediffSet?.GetFirstHediffOfDef(MCW_DefOf.MCW_AmberonHeart) != null)
            {
                if (dinfo.Def != null && dinfo.Def.ExternalViolenceFor(target) && dinfo.Amount > 0f)
                {
                    absorbed = true;
                    return false;
                }
            }
            return true;
        }
    }
}
