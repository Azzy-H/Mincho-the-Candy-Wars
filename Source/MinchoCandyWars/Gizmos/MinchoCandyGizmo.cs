using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace MinchoCandyWars.Gizmos
{
    [StaticConstructorOnStartup]
    public class MinchoCandyGizmo : Gizmo
    {
        public Pawn pawn;

        public CompMinchoCore compMinchoCore;
        
        public int minchoCoreGrade => compMinchoCore.MinchoCoreGrade;
        public int minchoBodyGrade => compMinchoCore.MinchoBodyGrade;
        public CandyTypeDef? candyType => compMinchoCore.CurrentCandyType;
        public float minchoCandyValue => compMinchoCore.MinchoCandyValue;
        public float currentMaxCandyValue => compMinchoCore.CurrentMaxCandyValue;

        public static readonly Texture2D BGText = ContentFinder<Texture2D>.Get("UI/Gizmos/MinchoCandyGizmo_bg");
        public MinchoCandyGizmo(Pawn pawn ,CompMinchoCore compMinchoCore)
        {
            this.pawn = pawn;
            this.compMinchoCore = compMinchoCore;
        }

        public override float GetWidth(float maxWidth)
        {
            return 150f;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            GUI.DrawTexture(rect, BGText);
            Widgets.DrawBox(rect, 1);
            Rect rectCore = new Rect(topLeft.x + 0.2f * GetWidth(maxWidth), topLeft.y + 5, GetWidth(maxWidth) / 2.5f, 30f);
            Widgets.DrawBox(rectCore, 1);
            Widgets.Label(rectCore, "MinchoCandyWars.Gizmos.CoreLabel".Translate());
            return new GizmoResult(GizmoState.Clear);

        }
    }
}
