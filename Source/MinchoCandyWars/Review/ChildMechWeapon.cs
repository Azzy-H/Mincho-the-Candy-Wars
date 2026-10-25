using HarmonyLib;
using UnityEngine;
using Verse;

namespace MinchoCandyWars.Review
{
    /// <summary>
    /// 子个体机械的种族标记：所有手持武器的整体偏移。
    /// 挂在 MCW_Mech_Base 上，6 只子个体机械都继承到。
    /// </summary>
    public class ChildMechExtension : DefModExtension
    {
        /// <summary>整体偏移（x 横向, y 纵向），Vector3 的 z 由 y 提供。</summary>
        public Vector2 weaponAimOffset;
    }

    /// <summary>
    /// 子个体机械专用武器的额外偏移，按朝向分别记录。
    /// 挂在 MCW_CandyCane_ForMech / MCW_MintchocoPunch_ForMech / MCW_TowerShield_ForMech 上。
    /// </summary>
    public class ChildMechWeaponExtension : DefModExtension
    {
        public Vector2 offsetNorth;
        public Vector2 offsetEast;
        public Vector2 offsetSouth;
        public Vector2 offsetWest;

        public Vector2 OffsetFor(Rot4 facing) => facing.AsInt switch
        {
            0 => offsetNorth,
            1 => offsetEast,
            2 => offsetSouth,
            _ => offsetWest,
        };
    }

    /// <summary>
    /// 子个体机械的手持武器位置修正（原先的两条补丁合并成一条）。
    ///
    /// 偏移全部来自 DefModExtension，代码里不再出现种族名或武器名：
    /// 种族侧给整体偏移，武器侧给按朝向的额外偏移，两者相加。
    /// </summary>
    [HarmonyPatch(typeof(PawnRenderUtility), nameof(PawnRenderUtility.DrawEquipmentAiming))]
    public static class ChildMechWeaponPosition
    {
        public static void Prefix(Thing eq, ref Vector3 drawLoc)
        {
            if (eq.ParentHolder is not Pawn_EquipmentTracker holder) return;

            Pawn pawn = holder.pawn;
            ChildMechExtension? raceExt = pawn.def.GetModExtension<ChildMechExtension>();
            if (raceExt == null) return;

            Vector2 offset = raceExt.weaponAimOffset;

            ChildMechWeaponExtension? weaponExt = eq.def.GetModExtension<ChildMechWeaponExtension>();
            if (weaponExt != null)
            {
                offset += weaponExt.OffsetFor(pawn.Rotation);
            }

            drawLoc.x += offset.x;
            drawLoc.z += offset.y;
        }
    }
}
