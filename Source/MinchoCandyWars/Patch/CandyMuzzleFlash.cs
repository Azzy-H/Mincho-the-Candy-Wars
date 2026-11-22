using HarmonyLib;
using RimWorld;
using MinchoCandyWars.Review;
using UnityEngine;
using Verse;

namespace MinchoCandyWars.Patch;

[HarmonyPatch(typeof(Verb_LaunchProjectile), "TryCastShot")]
internal static class CandyMuzzleFlash
{
    private static void Postfix(Verb_LaunchProjectile __instance, bool __result)
    {
        if (!__result || __instance.Projectile != MCW_ThingDefOf.MCW_VrocanGatling_Bullet ||
            __instance.CasterPawn is not { Map: { } map } pawn ||
            __instance.EquipmentSource is not { } gun)
            return;

        var target = __instance.CurrentTarget;
        var targetPos = target.HasThing ? target.Thing.DrawPos : target.Cell.ToVector3Shifted();
        float aim = __instance.AimAngleOverride ?? (targetPos - pawn.DrawPos).AngleFlat();
        bool flipped = aim > 200f && aim < 340f;
        float angle = aim - 90f + (flipped ? -180f - gun.def.equippedAngleOffset : gun.def.equippedAngleOffset);
        float holdDistance = (0.4f + gun.def.equippedDistanceOffset) * pawn.ageTracker.CurLifeStage.equipmentDrawDistanceFactor;
        var gunCenter = pawn.DrawPos + new Vector3(0f, 0f, holdDistance).RotatedBy(aim);
        ChildMechWeaponPosition.Prefix(gun, ref gunCenter);
        if (gun.TryGetComp<CompEquippable>() is { } equippable)
        {
            EquipmentUtility.Recoil(gun.def, EquipmentUtility.GetRecoilVerb(equippable.AllVerbs), out var shift, out var twist, aim);
            gunCenter += shift;
            angle += twist;
        }

        // Barrel center at (974, 546) on the right-facing 1024px sprite.
        var size = gun.Graphic.drawSize;
        var muzzle = new Vector3((flipped ? -0.451f : 0.451f) * size.x, 0f, -0.033f * size.y);
        FleckMaker.Static(gunCenter + muzzle.RotatedBy(angle), map, MCW_DefOf.SparkFlash, 1.1f);
    }
}
