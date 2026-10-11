using UnityEngine;
using Verse;

namespace MinchoCandyWars.Render
{
    [StaticConstructorOnStartup]
    public class PawnRenderNodeWorker_CandyDecoration : PawnRenderNodeWorker
    {
        public override Vector3 ScaleFor(PawnRenderNode node, PawnDrawParms parms)
        {
            // 基类结果已含 Props.drawSize（XML 可调），再按体型微调。
            Vector3 baseScale = base.ScaleFor(node, parms);
            float bodyFactor = Mathf.Clamp(parms.pawn.BodySize, 0.5f, 2f);
            return new Vector3(baseScale.x * bodyFactor, 1f, baseScale.z * bodyFactor);
        }
    }
}
