using UnityEngine;
using Verse;

namespace MinchoCandyWars.Render;

// Mirror the side artwork around the shield's own centre, preserving its back/hand anchor.
public class PawnRenderNodeWorker_CookieShield : PawnRenderNodeWorker_FlipWhenCrawling
{
    public override Vector3 ScaleFor(PawnRenderNode node, PawnDrawParms parms)
    {
        Vector3 scale = base.ScaleFor(node, parms);
        if (parms.facing.IsHorizontal)
            scale.x *= -1;
        return scale;
    }

    public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
    {
        Vector3 offset = base.OffsetFor(node, parms, out pivot);
        if (parms.facing.IsHorizontal)
        {
            // Centres measured from the 512px source canvases, rendered at 1.75 cells.
            float correction = node.Props.texPath.EndsWith("_behind") ? -0.33496094f : 0.24609375f;
            offset.x += parms.facing == Rot4.East ? correction : -correction;
        }
        return offset;
    }
}
