using RimWorld;
using UnityEngine;
using Verse;

namespace MinchoCandyWars.Abilities.PrismCandy
{
    /// <summary>
    /// 巧克力冰淇淋巨剑的地面剑痕。
    ///
    /// 这个 Thing 同时承担两件事：
    ///   1. 减速判定（Comp_PrismSwordSlowZone 每 tick 遍历 pawn）—— 因此它必须是
    ///      带 comp 的 ThingWithComps，不能用 Mote/Fleck 代替，否则 comp 不会 tick。
    ///   2. 地面贴图绘制 —— 在 DrawAt 里按施法角度旋转绘制。
    ///
    /// 贴图与着色器一律取自 ThingDef 的 graphicData：本类不写贴图路径，
    /// 也不为某一张贴图的画法做角度/尺寸补偿——换图不需要改代码。
    ///
    /// 绘制尺寸只由技能参数决定（长 = 判定线实长，宽 = 判定胶囊直径），
    /// 不做任何画布或可见包围盒换算；贴图铺满 quad，长宽比由美术按目标尺寸出图。
    ///
    /// 角度约定与判定用的 PrismCandyAbilityUtility.AngleToTarget 一致（东=0°、北=+90°），
    /// 与 Quaternion.AngleAxis(θ, Vector3.up) 完全吻合，因此直接使用。
    /// </summary>
    [StaticConstructorOnStartup]
    public class Thing_PrismSwordSlowZone : ThingWithComps
    {

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            // 完全覆盖基类绘制：本 Thing 的地面贴图只在这里画，避免重复。
            Comp_PrismSwordSlowZone? comp = this.TryGetComp<Comp_PrismSwordSlowZone>();
            Vector3 startVec = comp.StartPos.ToVector3Shifted();
            Vector3 endVec = comp.EndPos.ToVector3Shifted();
            Vector3 delta = endVec - startVec;

            // 轴向长度就是端点到端点的距离本身。
            // 不要再加 Radius：Radius 是胶囊的横向半径，与轴向长度无关，
            // 补上去只会让剑痕比判定范围长出 2×Radius。
            float length = delta.MagnitudeHorizontal();
            if (length <= 0.01f)
            {
                return;
            }

            Vector3 center = (startVec + endVec) * 0.5f;
            center.y = AltitudeLayer.FloorEmplacement.AltitudeFor();

            float angle = PrismCandyAbilityUtility.AngleToTarget(comp.StartPos, comp.EndPos);

            // 尺寸完全由能力参数决定：长 = 判定线实长，宽 = 判定胶囊直径。
            Matrix4x4 matrix = Matrix4x4.TRS(
                center,
                Quaternion.AngleAxis(angle, Vector3.up),
                new Vector3(length, 1f, comp.Radius * 2f));

            float alpha = comp.RemainingFraction;
            if (alpha <= 0.01f)
            {
                return;
            }

            Graphics.DrawMesh(MeshPool.plane10, matrix, FadedMaterialPool.FadedVersionOf(def.graphic.MatSingle, alpha), 0);
        }
    }
}
