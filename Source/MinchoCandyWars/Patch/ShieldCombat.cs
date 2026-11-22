using HarmonyLib;
using RimWorld;
using Verse;

namespace MinchoCandyWars.Patch;

[HarmonyPatch(typeof(Pawn_DraftController), nameof(Pawn_DraftController.Drafted), MethodType.Setter)]
internal static class ShieldDrafting
{
    private static void Prefix(Pawn_DraftController __instance, out bool __state)
        => __state = __instance.Drafted;

    private static void Postfix(Pawn_DraftController __instance, bool __state)
    {
        if (__instance.Drafted == __state || __instance.pawn.apparel is not { } apparel)
            return;

        foreach (var item in apparel.WornApparel)
            item.TryGetComp<Comp_MCWShieldApparel>()?.NotifyDraftChanged(__instance.pawn);
    }
}

[HarmonyPatch(typeof(Stance_Warmup), MethodType.Constructor, typeof(int), typeof(LocalTargetInfo), typeof(Verb))]
internal static class ShieldRangedWarmup
{
    private static void Prefix(ref int ticks, Verb verb)
    {
        var pawn = verb.CasterPawn;
        var weapon = verb.EquipmentSource;
        if (ticks <= 0 || weapon?.def.IsRangedWeapon != true || weapon != pawn?.equipment?.Primary ||
            pawn.apparel is not { } apparel || verb.IsMeleeAttack)
            return;

        foreach (var item in apparel.WornApparel)
        {
            if (item.def == MCW_ThingDefOf.MCW_TowerShield &&
                pawn.health.hediffSet.HasHediff(MCW_HediffDefOf.MCW_TowerShield_DraftedHediff))
            {
                ticks *= 4;
                return;
            }
            if (item.def == MCW_ThingDefOf.MCW_CookiesShield &&
                pawn.health.hediffSet.HasHediff(MCW_HediffDefOf.MCW_CookiesShield_DraftedHediff))
            {
                ticks *= 2;
                return;
            }
        }
    }
}
