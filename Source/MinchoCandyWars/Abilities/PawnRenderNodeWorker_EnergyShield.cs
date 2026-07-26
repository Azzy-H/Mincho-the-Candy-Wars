using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace MinchoCandyWars.Abilities
{
    /// <summary>
    /// 护盾泡泡渲染 Worker —— 在 pawn 身上绘制护盾腰带式泡泡。
    /// 通过 HediffDef.renderNodeProperties 挂载。
    /// 受击时产生抖动效果，与 CompShield 表现一致。
    /// </summary>
    [StaticConstructorOnStartup]
    public class PawnRenderNodeWorker_EnergyShield : PawnRenderNodeWorker
    {
        private static readonly Material BubbleMat = MaterialPool.MatFrom("Other/ShieldBubble", ShaderDatabase.Transparent);

        private const float MaxDamagedJitterDist = 0.05f;
        private const int JitterDurationTicks = 8;

        private HediffComp_EnergyShield? FindShield(PawnRenderNode node)
        {
            if (node.hediff is HediffWithComps hediffWithComps)
            {
                for (int i = 0; i < hediffWithComps.comps.Count; i++)
                {
                    if (hediffWithComps.comps[i] is HediffComp_EnergyShield shield)
                        return shield;
                }
            }
            return null;
        }

        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            if (!base.CanDrawNow(node, parms)) return false;
            var shield = FindShield(node);
            return shield != null && shield.ShieldActive && shield.ShouldDraw;
        }

        public override void AppendDrawRequests(PawnRenderNode node, PawnDrawParms parms, List<PawnGraphicDrawRequest> requests)
        {
            if (CanDrawNow(node, parms))
            {
                requests.Add(new PawnGraphicDrawRequest(node, MeshPool.plane10, BubbleMat));
            }
        }

        public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
        {
            var shield = FindShield(node);
            pivot = Vector3.zero;
            if (shield != null)
            {
                int ticksSinceAbsorb = Find.TickManager.TicksGame - shield.LastAbsorbDamageTick;
                if (ticksSinceAbsorb < JitterDurationTicks)
                {
                    float jitter = (float)(JitterDurationTicks - ticksSinceAbsorb) / JitterDurationTicks * MaxDamagedJitterDist;
                    return shield.ImpactAngleVect * jitter;
                }
            }
            return base.OffsetFor(node, parms, out pivot);
        }

        public override Vector3 ScaleFor(PawnRenderNode node, PawnDrawParms parms)
        {
            var shield = FindShield(node);
            float s = 1f;
            if (shield != null)
            {
                s = Mathf.Lerp(0.5f, 1.5f, shield.Energy / Mathf.Max(shield.EnergyMax, 1f));
                int ticksSinceAbsorb = Find.TickManager.TicksGame - shield.LastAbsorbDamageTick;
                if (ticksSinceAbsorb < JitterDurationTicks)
                {
                    float jitter = (float)(JitterDurationTicks - ticksSinceAbsorb) / JitterDurationTicks * MaxDamagedJitterDist;
                    s -= jitter;
                }
            }
            return new Vector3(s, 1f, s);
        }

        public override Quaternion RotationFor(PawnRenderNode node, PawnDrawParms parms)
        {
            return Quaternion.AngleAxis(Rand.Range(0, 360), Vector3.up);
        }

        public override MaterialPropertyBlock GetMaterialPropertyBlock(PawnRenderNode node, Material material, PawnDrawParms parms)
        {
            var mpb = base.GetMaterialPropertyBlock(node, material, parms) ?? new MaterialPropertyBlock();
            var shield = FindShield(node);
            if (shield != null)
            {
                float alpha = Mathf.Lerp(0.3f, 0.6f, shield.Energy / Mathf.Max(shield.EnergyMax, 1f));
                mpb.SetColor(ShaderPropertyIDs.Color, new Color(1f, 1f, 1f, alpha));
            }
            return mpb;
        }

        public override float LayerFor(PawnRenderNode node, PawnDrawParms parms)
        {
            return PawnRenderUtility.AltitudeForLayer(AltitudeLayer.MoteOverhead.AltitudeFor());
        }
    }
}