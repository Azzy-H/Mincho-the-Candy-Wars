using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
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

        public float FillPercent()
        {
            if (currentMaxCandyValue <= 0f) return 0f;
            return minchoCandyValue / currentMaxCandyValue;
        }

        public static readonly Texture2D BGText = ContentFinder<Texture2D>.Get("UI/Gizmos/MinchoCandyGizmo/MinchoCandyGizmo_bg");
        public static readonly Texture2D pinkCandyTexture = ContentFinder<Texture2D>.Get("UI/Gizmos/MinchoCandyGizmo/MinchoCandyGizmo_pink");
        public static readonly Texture2D blueCandyTexture = ContentFinder<Texture2D>.Get("UI/Gizmos/MinchoCandyGizmo/MinchoCandyGizmo_blue");
        public static readonly Texture2D cookieTexture = ContentFinder<Texture2D>.Get("UI/Gizmos/MinchoCandyGizmo/MinchoCandyGizmo_cookie");
        public static readonly Texture2D progressBarBGTexture = ContentFinder<Texture2D>.Get("UI/Gizmos/MinchoCandyGizmo/MinchoCandyGizmo_progressBarBG");
        public static readonly Texture2D progressBarFillTexture = ContentFinder<Texture2D>.Get("UI/Gizmos/MinchoCandyGizmo/MinchoCandyGizmo_progressBarFill");
        public MinchoCandyGizmo(Pawn pawn ,CompMinchoCore compMinchoCore)
        {
            this.pawn = pawn;
            this.compMinchoCore = compMinchoCore;
        }

        public override float GetWidth(float maxWidth)
        {
            return 180f;
        }
        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect root = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);

            Widgets.DrawTextureFitted(root, BGText, 1f);

            Rect pinkRect = new Rect(root.x + 18f, root.y + 7f, 58f, 22f);
            Widgets.DrawTextureFitted(pinkRect, pinkCandyTexture, 1f);

            Rect blueRect = new Rect(root.x + 97f, root.y + 7f, 58f, 22f);
            Widgets.DrawTextureFitted(blueRect, blueCandyTexture, 1f);

            Rect cookieRect = new Rect(root.x + 8f, root.y + 35f, 32f, 32f);
            if (Widgets.ButtonImage(cookieRect, cookieTexture))
            {
                Log.Message("[MinchoCandy] 点击了饼干！");
            }

            float pct = Mathf.Clamp01(FillPercent());
            Rect barRect = new Rect(root.x + 48f, root.y + 39f, 118f, 25f);
            FillableBarWithUVCut(barRect, pct, progressBarFillTexture, progressBarBGTexture, true);

            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(barRect, $"{pct:P0}");
            Text.Anchor = TextAnchor.UpperLeft;

            return new GizmoResult(GizmoState.Clear);
        }
        public static Rect FillableBarWithUVCut(Rect rect, float fillPercent, Texture2D fillTex, Texture2D bgTex, bool doBorder)
        {
            if (doBorder)
            {
                GUI.DrawTexture(rect, BaseContent.BlackTex);
                rect = rect.ContractedBy(3f);
            }
            if (bgTex != null)
            {
                GUI.DrawTexture(rect, bgTex);
            }
            Rect result = rect;
            rect.width *= fillPercent;
            Rect uvRect = new Rect(0f, 0f, fillPercent, 1f);
            GUI.DrawTextureWithTexCoords(rect, fillTex, uvRect);
            return result;
        }
    }
}
