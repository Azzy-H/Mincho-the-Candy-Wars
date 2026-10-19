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

        private const int toolTipHash1 = 19458323;
        private const int toolTipHash2 = 19458324;
        private const int toolTipHash3 = 19458325;
        private const int toolTipHash4 = 19458326;

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

        /// <summary>糖种 defName → 饼干按钮图标。按需加载并缓存，避免每帧查文件。</summary>
        private static readonly Dictionary<string, Texture2D?> candyIconCache = new Dictionary<string, Texture2D?>();

        /// <summary>
        /// 取当前糖饰对应的饼干按钮图标。
        ///
        /// 图放在 UI/Gizmos/MinchoCandyGizmo/Candy/&lt;糖种 defName&gt;.png，
        /// 内容是「糖饰物品贴图」的副本——之所以另存一份而不是直接引用物品贴图，
        /// 是为了让 Gizmo 图标可以独立替换而不影响物品图标。
        /// 没有专属图（或还没铺图）时退回通用饼干图，不会报错。
        /// </summary>
        private static Texture2D GetCandyIcon(CandyTypeDef? candy)
        {
            if (candy == null)
            {
                return cookieTexture;
            }
            if (candyIconCache.TryGetValue(candy.defName, out Texture2D? cached) && cached != null)
            {
                return cached;
            }
            Texture2D? tex = ContentFinder<Texture2D>.Get("UI/Gizmos/MinchoCandyGizmo/Candy/" + candy.defName, reportFailure: false);
            candyIconCache[candy.defName] = tex;
            return tex ?? cookieTexture;
        }

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
            //Text.Anchor = TextAnchor.MiddleCenter;
            //Widgets.Label(pinkRect, compMinchoCore.MinchoCoreGrade.ToString());
            //Text.Anchor = TextAnchor.UpperLeft;
            TooltipHandler.TipRegion(pinkRect, new TipSignal(() => "MinchoCandyWars.Abilities.MinchoCoreGradeTips".Translate(compMinchoCore.MinchoCoreGrade), toolTipHash1));

            Rect blueRect = new Rect(root.x + 97f, root.y + 7f, 58f, 22f);
            Widgets.DrawTextureFitted(blueRect, blueCandyTexture, 1f);
            //Text.Anchor = TextAnchor.MiddleCenter;
            //Widgets.Label(blueRect, compMinchoCore.MinchoBodyGrade.ToString());
            //Text.Anchor = TextAnchor.UpperLeft;
            TooltipHandler.TipRegion(blueRect, new TipSignal(() => "MinchoCandyWars.Abilities.MinchoBodyGradeTips".Translate(compMinchoCore.MinchoBodyGrade), toolTipHash2));

            // 饼干本身就是按钮：图标跟随当前糖饰，使用「糖饰物品贴图」的副本
            // （Textures/UI/Gizmos/MinchoCandyGizmo/Candy/<糖种 defName>.png，方便单独替换）；
            // 找不到专属图时退回通用饼干图。
            // 物品图是 43x49 的竖图，直接铺进方形按钮会变形，所以用 ButtonImageFitted 保持比例。
            Rect cookieRect = new Rect(root.x + 8f, root.y + 35f, 32f, 32f);
            if (Widgets.ButtonImageFitted(cookieRect, GetCandyIcon(compMinchoCore.CurrentCandyType)))
            {
                compMinchoCore.ShowAbilityGizmos = !compMinchoCore.ShowAbilityGizmos;
                //List<FloatMenuOption> options = new List<FloatMenuOption>();

                //foreach (CandyTypeDef candyTypeDef in compMinchoCore.candyTypeDefsAccessible)
                //{
                //    CandyTypeDef localCandyTypeDef = candyTypeDef;
                //    string label;
                //    if (candyTypeDef == compMinchoCore.CurrentCandyType)
                //    {
                //        label = "MinchoCandyWars.Abilities.CurrentCandyType".Translate(candyTypeDef.label);
                //    }
                //    else
                //    {
                //        label = candyTypeDef.label;
                //    }
                //    FloatMenuOption option = new FloatMenuOption(
                //        label,
                //        delegate
                //        {
                //            compMinchoCore.CurrentCandyType = localCandyTypeDef;
                //        }
                //    );
                //    if (candyTypeDef == compMinchoCore.CurrentCandyType)
                //    {
                //        option.Disabled = true;
                //    }
                //    options.Add(option);
                //}

                //Find.WindowStack.Add(new FloatMenu(options));
            }

            // 饼干是按钮，提示只说明「点击的作用」；恢复速度改到能量棒上显示，不再做 tooltip。
            TooltipHandler.TipRegion(cookieRect, new TipSignal(() => "MinchoCandyWars.Abilities.CookieToggleTips".Translate(), toolTipHash4));

            float pct = Mathf.Clamp01(FillPercent());
            Rect barRect = new Rect(root.x + 48f, root.y + 39f, 118f, 25f);
            FillableBarWithUVCut(barRect, pct, progressBarFillTexture, progressBarBGTexture, true);

            // 恢复速度叠印在右下角能量棒上，黑色字。
            // 取 RecoveryPerDay，已含温度系数；能量棒底色偏浅，黑字对比足够。
            string recoveryText = "MinchoCandyWars.Abilities.CandyValueRecovery".Translate(compMinchoCore.RecoveryPerDay.ToString("F0"));
            GameFont prevFont = Text.Font;
            Color prevColor = GUI.color;
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = Color.black;
            Widgets.Label(barRect, recoveryText);
            GUI.color = prevColor;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = prevFont;

            TooltipHandler.TipRegion(barRect, new TipSignal(() => "MinchoCandyWars.Abilities.MinchoCandyValueTips".Translate(minchoCandyValue.Named("VALUE"), currentMaxCandyValue.Named("MAXVALUE")), toolTipHash3));
            //Rect textRect = new Rect(barRect.x + (barRect.width - 45f) / 2f, barRect.y, 45f, barRect.height);

            //Text.Anchor = TextAnchor.MiddleCenter;
            //Widgets.DrawBoxSolid(textRect, Color.gray);
            //Widgets.Label(textRect, $"{pct:P0}");
            //Text.Anchor = TextAnchor.UpperLeft;

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
