using UnityEngine;
using Verse;

namespace MinchoCandyWars.Render
{
    /// <summary>
    /// 琥珀糖心②：琥珀糖糖饰的发光。
    ///
    /// 这是与①护体光环相互独立的第二个特效——它挂在头部糖饰的位置（parentTagDef=Head），
    /// baseLayer 76 叠在糖饰（75）之上，只在琥珀糖心生效期间出现，
    /// 与糖果种类无关（糖饰本身由 PawnRenderNode_CandyDecoration 按种类绘制）。
    ///
    /// 周期与①的呼吸一致（90 tick），让糖饰跟着身上的光环一起明暗，视觉上同源。
    /// </summary>
    [StaticConstructorOnStartup]
    public class PawnRenderNodeWorker_AmberonDecorationGlow : PawnRenderNodeWorker
    {
        /// <summary>与①护体光环的呼吸周期保持一致，避免两者相位漂移。</summary>
        private const float GlowPeriodTicks = 90f;
        private const float BaseAmplitude = 0.25f;
        private static float GlowPhase => Find.TickManager.TicksGame % GlowPeriodTicks / GlowPeriodTicks;

        public override MaterialPropertyBlock GetMaterialPropertyBlock(PawnRenderNode node, Material material, PawnDrawParms parms)
        {
            MaterialPropertyBlock mpb = base.GetMaterialPropertyBlock(node, material, parms) ?? new MaterialPropertyBlock();

            // MoteGlow 是加法混合：颜色越亮越「发光」，因此亮度用近似白热的暖金，
            // 不做 alpha 衰减（加法混合下降低 alpha 会让辉光整体消失，而不是变柔）。
            float glow = BaseAmplitude + 0.25f * Mathf.Sin(GlowPhase * Mathf.PI * 2f);
            mpb.SetColor(ShaderPropertyIDs.Color, new Color(glow, 0.9f * glow, 0.6f * glow, 1f));

            return mpb;
        }
    }
}
