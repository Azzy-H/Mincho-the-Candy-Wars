using UnityEngine;
using Verse;

namespace MinchoCandyWars.Render
{
    /// <summary>
    /// 琥珀糖心①：身上护体光环。
    ///
    /// 贴图与着色器由 HediffDef.renderNodeProperties 声明
    /// （Effects/Amberon/MCW_AmberonHeart + MoteGlowCircularScrolling）。
    ///
    /// 缩放与旋转必须在这里做：PawnRenderNode 的矩阵只读取 worker 的
    /// ScaleFor/RotationFor，Def 上的 drawSize 不会进入 TRS 矩阵。
    ///
    /// 贴图是完美径向对称的环，绕 Y 轴自转本身看不出流动，
    /// 因此旋转交给 A1（MoteGlowCircularScrolling 的环绕 UV 滚动），
    /// 本类只负责 A2（径向脉冲扩散）与呼吸。
    /// </summary>
    [StaticConstructorOnStartup]
    public class PawnRenderNodeWorker_AmberonHeart : PawnRenderNodeWorker
    {
        /// <summary>呼吸周期（tick）。</summary>
        private const float PulsePeriodTicks = 90f;

        private const float PulseAmplitude = 0.06f;

        /// <summary>A2 扩散：光环尺寸的连续起伏幅度（配合 FlowPeriodTicks 形成流动感）。</summary>
        private const float FlowPeriodTicks = 150f;

        private const float FlowAmplitude = 0.12f;

        /// <summary>最后 12% 时长用于淡出，让状态结束前有预告。</summary>
        private const float FadeFrom = 0.88f;
        /// <summary>呼吸与 A2 扩散的亮度起伏幅度。</summary>
        private const float IntensityAmplitude = 0.5f;

        /// <summary>呼吸相位 0→1，供缩放与亮度共用。</summary>
        private static float PulsePhase => Find.TickManager.TicksGame % PulsePeriodTicks / PulsePeriodTicks;

        /// <summary>A2 扩散相位 0→1。</summary>
        private static float FlowPhase => Find.TickManager.TicksGame % FlowPeriodTicks / FlowPeriodTicks;

        public override Vector3 ScaleFor(PawnRenderNode node, PawnDrawParms parms)
        {
            // 呼吸：轻微胀缩，让持续状态有生命感。
            // 用 Sin 而非锯齿相位：相位经 % 回绕时 Sin 本身连续，不会跳变。
            float pulse = 1f + PulseAmplitude * Mathf.Sin(PulsePhase * Mathf.PI * 2f);

            // A2 扩散：再叠加一个连续的呼吸式缩放，表达「流动」。
            float flow = 1f + FlowAmplitude * Mathf.Sin(FlowPhase * Mathf.PI * 2f);

            // 不按贴图画布/可见包围盒做任何换算：光环的实际大小由 Def 的 drawSize 直接决定，
            // 这里只叠加脉动。贴图的长宽比由美术按目标尺寸出图。
            return base.ScaleFor(node, parms) * pulse * flow;
        }

        public override Quaternion RotationFor(PawnRenderNode node, PawnDrawParms parms)
        {
            // 贴图是完美径向对称的环，自转本身不可见；
            // 保留旋转是为了将来 A1（带角向结构的 _MaskTex）启用时能直接生效。
            return Quaternion.AngleAxis(Find.TickManager.TicksGame * 0.6f % 360f, Vector3.up);
        }

        public override MaterialPropertyBlock GetMaterialPropertyBlock(PawnRenderNode node, Material material, PawnDrawParms parms)
        {
            MaterialPropertyBlock mpb = base.GetMaterialPropertyBlock(node, material, parms) ?? new MaterialPropertyBlock();

            // 着色器是 MoteGlow（加法混合），颜色亮度即发光强度，
            // 因此强度起伏同样要用连续函数表达，避免相位回绕处出现亮暗跳变。
            float intensity = 1f - IntensityAmplitude * (1f - Mathf.Cos(FlowPhase * Mathf.PI * 2f)) * 0.5f;

            HediffComp_Disappears? disappears = (node.hediff as HediffWithComps)?.TryGetComp<HediffComp_Disappears>();
            if (disappears != null && disappears.Progress > FadeFrom)
            {
                intensity *= Mathf.Clamp01((1f - disappears.Progress) / (1f - FadeFrom));
            }

            // 与呼吸同相位的亮度起伏，强化「琥珀光泽」观感。
            float glow = (0.85f + 0.15f * Mathf.Sin(PulsePhase * Mathf.PI * 2f)) * intensity;
            mpb.SetColor(ShaderPropertyIDs.Color, new Color(glow, 0.92f * glow, 0.72f * glow, 1f));

            return mpb;
        }
    }
}
