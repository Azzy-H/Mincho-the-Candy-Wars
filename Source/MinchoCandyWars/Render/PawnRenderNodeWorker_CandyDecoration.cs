using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace MinchoCandyWars.Render
{
    public class PawnRenderNodeWorker_CandyDecoration : PawnRenderNodeWorker
    {
        public override Vector3 ScaleFor(PawnRenderNode node, PawnDrawParms parms)
        {
            // 基类结果已含 Props.drawSize（XML 可调），再按体型微调。
            Vector3 baseScale = base.ScaleFor(node, parms);
            float bodyFactor = Mathf.Clamp(parms.pawn.BodySize, 0.5f, 2f);
            return new Vector3(baseScale.x * bodyFactor, 1f, baseScale.z * bodyFactor);
        }

        /// <summary>佩戴全息面罩时隐藏糖饰，避免与面罩穿模。</summary>
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
            => base.CanDrawNow(node, parms) && !WearsHoloVisor(parms.pawn);

        /// <summary>这条路径每帧、每个装饰节点都会走，用 for 循环避免 LINQ 分配。</summary>
        private static bool WearsHoloVisor(Pawn pawn)
        {
            List<Apparel>? worn = pawn?.apparel?.WornApparel;
            if (worn == null) return false;

            for (int i = 0; i < worn.Count; i++)
            {
                if (worn[i].def == MCW_DefOf.MCW_HoloVisor) return true;
            }

            return false;
        }
    }
}