using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// Stellaris 风格事件对话窗口
    /// 布局：左侧 320px 立绘 + 右侧标题/描述/选项
    /// </summary>
    public class StellarisEventWindow : Window
    {
        // ──────────────────── 布局常量 ────────────────────
        private const float WindowWidth = 900f;
        private const float WindowHeight = 600f;
        private const float PortraitWidth = 320f;
        private const float PortraitHeight = 600f;
        private const float RightPanelWidth = WindowWidth - PortraitWidth; // 580
        private const float TopSectionHeight = 280f;
        private const float TitleHeight = 50f;
        private const float OptionHeight = 35f;
        private const float OptionSpacing = 8f;
        private const float SectionPadding = 15f;

        // ──────────────────── 数据 ────────────────────
        private StellarisEventDef eventDef;
        private List<OptionDrawInfo> optionDrawInfos = new List<OptionDrawInfo>();
        private Vector2 descScrollPos = Vector2.zero;

        // ──────────────────── 样式 ────────────────────
        // 右侧面板半透明覆盖层：对应 rightDiv `rgba(0.3, 0.3, 0.3, 0.15)`
        private static readonly Color OverlayColor = new Color(0.3f, 0.3f, 0.3f, 0f);
        // 选项按钮半透明：对应 option `rgba(0.3, 0.3, 0.3, 0.15)`
        private static readonly Color OptionColor = new Color(0.3f, 0.3f, 0.3f, 0.15f);
        private static readonly Color OptionColorHover = new Color(0.5f, 0.5f, 0.5f, 0.35f);
        private static readonly Color OptionColorDisabled = new Color(0.2f, 0.2f, 0.2f, 0.25f);
        // 分割线
        private static readonly Color DividerColor = new Color(0f, 0f, 0f, 0.4f);
        // 默认背景色（无自定义背景图时使用）
        private static readonly Color FallbackBgColor = new Color(0.08f, 0.08f, 0.12f);

        // ──────────────────── 内部类 ────────────────────
        private class OptionDrawInfo
        {
            public StellarisEventOption? option;
            public bool available;
            public Rect rect;
        }

        // ──────────────────── 构造 ────────────────────

        public StellarisEventWindow(StellarisEventDef def)
        {
            eventDef = def;
            doCloseX = true;
            closeOnClickedOutside = false;
            absorbInputAroundWindow = false;
            forcePause = true;
            draggable = true;
            resizeable = false;
            preventCameraMotion = false;
            doWindowBackground = false; // 由 DrawBackground 处理背景绘制
            drawShadow = false;

            BuildOptionList();
        }

        public override Vector2 InitialSize => new Vector2(WindowWidth, WindowHeight);

        // ──────────────────── 选项列表构建 ────────────────────

        private void BuildOptionList()
        {
            optionDrawInfos.Clear();
            foreach (var opt in eventDef.options)
            {
                bool available = opt.IsAvailable();
                if (!available && opt.hideWhenDisabled)
                    continue;

                optionDrawInfos.Add(new OptionDrawInfo
                {
                    option = opt,
                    available = available
                });
            }
        }

        // ──────────────────── 主绘制 ────────────────────

        public override void DoWindowContents(Rect inRect)
        {
            // ── 背景：优先使用自定义纹理，回退到纯色 ──
            DrawBackground(inRect);

            // ── 左侧：立绘 ──
            Rect portraitRect = new Rect(inRect.x, inRect.y, PortraitWidth, PortraitHeight);
            DrawPortrait(portraitRect);

            // ── 右侧面板：半透明覆盖层（对应 .rightDiv） ──
            Rect rightPanelRect = new Rect(
                inRect.x + PortraitWidth,
                inRect.y,
                RightPanelWidth,
                WindowHeight
            );
            DrawRightPanelOverlay(rightPanelRect);
        }

        // ──────────────────── 背景绘制 ────────────────────

        private void DrawBackground(Rect rect)
        {
            Texture2D? bgTex = eventDef.backgroundTexture;
            if (bgTex != null)
            {
                GUI.DrawTexture(rect, bgTex, ScaleMode.ScaleAndCrop);
            }
            else
            {
                Widgets.DrawBoxSolid(rect, FallbackBgColor);
            }
        }

        // ──────────────────── 立绘绘制（含非对称裁剪） ────────────────────

        private void DrawPortrait(Rect portraitRect)
        {
            if (eventDef.selectedPicture == null)
                return;
            Texture2D tex = eventDef.selectedPicture;
            float scale = portraitRect.height / tex.height;          // 以高度为基准等比缩放
            float scaledWidth = tex.width * scale;                   // 缩放后的宽度
                                                                     // 应用 XML 中每张立绘的 XY 偏移
            float offsetX = eventDef.selectedPictureOffsetX;
            float offsetY = eventDef.selectedPictureOffsetY;
            Rect imageRect = new Rect(offsetX, offsetY, scaledWidth, portraitRect.height);
            GUI.BeginGroup(portraitRect);                            // 所有绘制被限制在 portraitRect 内
            GUI.DrawTexture(imageRect, tex, ScaleMode.StretchToFill); // 由于 imageRect 宽高比与纹理一致，不会拉伸变形
            GUI.EndGroup();
        }

        // ──────────────────── 右侧面板 ────────────────────

        /// <summary>
        /// 绘制右侧面板半透明覆盖层（对应 HTML .rightDiv）
        /// 包含上部的标题+描述以及下部的选项列表
        /// </summary>
        private void DrawRightPanelOverlay(Rect rightRect)
        {
            // 半透明覆盖层背景（对应 .rightDiv 的 rgba(0.3,0.3,0.3,0.15) + border-radius）
            Widgets.DrawBoxSolid(rightRect, OverlayColor);

            // ── 上部：标题 + 描述（对应 .topDiv > .nameAndDesc） ──
            Rect topRect = new Rect(rightRect.x, rightRect.y, rightRect.width, TopSectionHeight);
            DrawTopSection(topRect);

            // ── 下部：选项列表（对应 .optionList） ──
            Rect bottomRect = new Rect(
                rightRect.x,
                rightRect.y + TopSectionHeight,
                rightRect.width,
                WindowHeight - TopSectionHeight
            );
            DrawOptionSection(bottomRect);
        }

        private void DrawTopSection(Rect topRect)
        {
            // ── 标题（对应 .name：left-aligned） ──
            Rect titleRect = new Rect(
                topRect.x + SectionPadding,
                topRect.y + 10f,
                topRect.width - SectionPadding * 2f,
                TitleHeight
            );
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = Color.white;
            Widgets.Label(titleRect, eventDef.InjectParams(eventDef.title ?? eventDef.label));
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            // ── 描述（对应 .desc：带顶部边框，padding，margin auto） ──
            Rect descRect = new Rect(
                topRect.x + SectionPadding,
                titleRect.yMax + 10f,
                topRect.width - SectionPadding * 2f,
                topRect.yMax - titleRect.yMax - 20f
            );

            // 描述顶部分割线（border-top: 1px solid black）
            Rect dividerRect = new Rect(descRect.x, descRect.y, descRect.width, 1f);
            Widgets.DrawBoxSolid(dividerRect, DividerColor);

            descRect.y += 6f;
            descRect.height -= 6f;

            // 描述文本区域（padding: 10px）
            Rect descTextRect = new Rect(descRect.x, descRect.y + 10f,
                descRect.width - 20f, descRect.height - 20f);

            string descText = eventDef.InjectParams(eventDef.desc ?? "");
            float descHeight = Text.CalcHeight(descText, descTextRect.width);
            Rect descViewRect = new Rect(0f, 0f, descTextRect.width - 16f, descHeight);

            Widgets.BeginScrollView(descTextRect, ref descScrollPos, descViewRect);
            GUI.color = new Color(0.9f, 0.9f, 0.9f);
            Widgets.Label(descViewRect, descText);
            GUI.color = Color.white;
            Widgets.EndScrollView();
        }

        private void DrawOptionSection(Rect optionRect)
        {
            int optionCount = optionDrawInfos.Count;
            if (optionCount == 0)
                return;

            float usableHeight = optionRect.height - SectionPadding * 2f;
            float totalOptionHeight = optionCount * OptionHeight + (optionCount - 1) * OptionSpacing;
            float startY = optionRect.y + SectionPadding;

            // 计算选项布局
            if (totalOptionHeight > usableHeight)
            {
                float availablePerOption = usableHeight / optionCount;
                float clampedOptionH = Mathf.Min(OptionHeight, availablePerOption - 2f);

                for (int i = 0; i < optionCount; i++)
                {
                    float y = startY + i * (usableHeight / optionCount);
                    Rect optRect = new Rect(
                        optionRect.x + SectionPadding, y,
                        optionRect.width - SectionPadding * 4f, clampedOptionH
                    );
                    optionDrawInfos[i].rect = optRect;
                    DrawOption(optRect, optionDrawInfos[i]);
                }
            }
            else
            {
                float offsetY = (usableHeight - totalOptionHeight) / 2f;
                for (int i = 0; i < optionCount; i++)
                {
                    float y = startY + offsetY + i * (OptionHeight + OptionSpacing);
                    Rect optRect = new Rect(
                        optionRect.x + SectionPadding, y,
                        optionRect.width - SectionPadding * 4f, OptionHeight
                    );
                    optionDrawInfos[i].rect = optRect;
                    DrawOption(optRect, optionDrawInfos[i]);
                }
            }
        }

        /// <summary>
        /// 绘制半透明圆角选项按钮（对应 .option: text-align center, rgba bg, border-radius 7px）
        /// </summary>
        private void DrawOption(Rect optRect, OptionDrawInfo info)
        {
            // 按钮背景：悬停时高亮
            Color bgColor = info.available
                ? (Mouse.IsOver(optRect) ? OptionColorHover : OptionColor)
                : OptionColorDisabled;
            Widgets.DrawBoxSolid(optRect, bgColor);

            // 按钮文本：居中
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;

            GUI.color = info.available ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            Widgets.Label(optRect, eventDef.InjectParams(info.option!.label ?? "???"));
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;

            // 点击处理
            if (info.available && Widgets.ButtonInvisible(optRect))
            {
                OnOptionSelected(info.option);
            }

            // 悬停提示
            string? tooltipText;
            if (!info.available && !string.IsNullOrEmpty(info.option.disabledReason))
                tooltipText = eventDef.InjectParams(info.option.disabledReason ?? "");
            else
                tooltipText = eventDef.InjectParams(info.option.desc ?? info.option.label ?? "");

            if (!string.IsNullOrEmpty(tooltipText))
            {
                TooltipHandler.TipRegion(optRect, tooltipText);
            }
        }

        // ──────────────────── 选项处理 ────────────────────

        private void OnOptionSelected(StellarisEventOption option)
        {
            // 执行 option 的 action
            option.action?.Execute();

            // 关闭窗口
            Close();

            // 执行 after
            eventDef.after?.Execute();

            // 清理本事件内创建的所有 local flag
            StellarisEventFlagManager.ClearLocalFlags();
        }
    }
}
