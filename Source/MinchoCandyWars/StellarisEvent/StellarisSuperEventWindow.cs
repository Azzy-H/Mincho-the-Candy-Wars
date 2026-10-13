#nullable disable
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// 超事件窗口 —— 立绘在上、内容在下，无重叠的垂直布局。
    /// 通过 StellarisEventDef.isSuperEvent = true 启用。
    /// 布局：立绘顶部全宽（仅下方截断）→ 标题 → 描述 → 选项，依次向下排列。
    /// </summary>
    public class StellarisSuperEventWindow : Window
    {
        // ──────────────────── 布局常量 ────────────────────
        private const float WindowWidth = 900f;
        private const float WindowHeight = 700f;
        private const float PortraitHeight = 350f;          // 立绘固定区域高度
        private const float PortraitEndGap = 10f;           // 立绘与内容之间的间距
        private const float ContentWidthRatio = 0.9f;       // 内容占窗口宽度比例
        private const float ContentPadding = 15f;
        private const float TitleHeight = 50f;
        private const float DescHeight = 150f;
        private const float OptionHeight = 35f;
        private const float OptionSpacing = 5f;

        private float ContentStartY => PortraitHeight + PortraitEndGap;

        private static float CalcContentWidth(Rect inRect) => inRect.width * ContentWidthRatio;
        private static float CalcContentX(Rect inRect) => inRect.x + (inRect.width - CalcContentWidth(inRect)) / 2f;

        // ──────────────────── 数据 ────────────────────
        private StellarisEventDef eventDef;
        private List<OptionDrawInfo> optionDrawInfos = new List<OptionDrawInfo>();
        private Vector2 descScrollPos = Vector2.zero;
        private string resolvedTitleText;
        private string resolvedDescText;

        // ──────────────────── 样式 ────────────────────
        private static readonly Color OptionColor = new Color(0.3f, 0.3f, 0.3f, 0.6f);
        private static readonly Color OptionColorHover = new Color(0.5f, 0.5f, 0.5f, 0.75f);
        private static readonly Color OptionColorDisabled = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        private static readonly Color DividerColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        private static readonly Color FallbackBgColor = new Color(0.05f, 0.05f, 0.08f);

        // ──────────────────── 内部类 ────────────────────
        private class OptionDrawInfo
        {
            public StellarisEventOption option;
            public bool available;
            public Rect rect;
            public string labelText;
            public string tooltipText;
        }

        // ──────────────────── 构造 ────────────────────

        public StellarisSuperEventWindow(StellarisEventDef def)
        {
            eventDef = def;
            doCloseX = !def.forceChoose;
            closeOnCancel = !def.forceChoose;
            closeOnClickedOutside = false;
            absorbInputAroundWindow = false;
            forcePause = true;
            draggable = true;
            resizeable = false;
            preventCameraMotion = false;
            doWindowBackground = false;
            drawShadow = false;

            resolvedTitleText = eventDef.InjectParams((eventDef.title ?? eventDef.label).Translate().ToString());
            resolvedDescText = eventDef.ResolveLocalizedDescText(eventDef.desc);
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
                    available = available,
                    labelText = eventDef.InjectParams((opt.label ?? "???").Translate().ToString()),
                    tooltipText = BuildOptionTooltipText(opt, available)
                });
            }
        }

        private string BuildOptionTooltipText(StellarisEventOption option, bool available)
        {
            if (!available && !string.IsNullOrEmpty(option.disabledReason))
                return eventDef.InjectParams(option.disabledReason.Translate().ToString());

            return eventDef.ResolveLocalizedDescText(option.desc ?? option.label);
        }

        // ──────────────────── 主绘制 ────────────────────

        public override void DoWindowContents(Rect inRect)
        {
            // ── 背景（全窗口） ──
            DrawBackground(inRect);

            // ── 立绘（顶部全宽，仅下方截断） ──
            DrawSuperPortrait(inRect);

            // ── 内容区（立绘下方，依次排列） ──
            DrawContentStack(inRect);
        }

        // ──────────────────── 背景 ────────────────────

        private void DrawBackground(Rect rect)
        {
            Texture2D bgTex = eventDef.backgroundTexture;
            if (bgTex != null)
            {
                GUI.DrawTexture(rect, bgTex, ScaleMode.ScaleAndCrop);
            }
            else
            {
                Widgets.DrawBoxSolid(rect, FallbackBgColor);
            }
        }

        // ──────────────────── 立绘（顶部全宽 + 下方截断） ────────────────────

        /// <summary>
        /// 绘制超事件立绘。
        /// 缩放至窗口全宽，通过 GUI.BeginGroup 在立绘区域底部截断。
        /// 上/左/右不截断，仅下方在超出 PortraitHeight 时截断。
        /// </summary>
        private void DrawSuperPortrait(Rect inRect)
        {
            if (eventDef.selectedPicture == null)
                return;

            Texture2D tex = eventDef.selectedPicture;
            Rect portraitArea = new Rect(inRect.x, inRect.y, inRect.width, PortraitHeight);

            GUI.BeginGroup(portraitArea);

            float scale = portraitArea.width / tex.width;
            float scaledHeight = tex.height * scale;
            float offsetX = eventDef.selectedPictureOffsetX;
            float offsetY = eventDef.selectedPictureOffsetY;

            Rect imageRect = new Rect(offsetX, offsetY, portraitArea.width, scaledHeight);
            GUI.DrawTexture(imageRect, tex, ScaleMode.StretchToFill);

            GUI.EndGroup();
        }

        // ──────────────────── 内容栈（立绘下方垂直排列） ────────────────────

        /// <summary>
        /// 在立绘下方依次绘制：标题 → 分割线 → 描述 → 选项列表。
        /// 各元素居中宽度（ContentWidthRatio），无重叠覆盖。
        /// </summary>
        private void DrawContentStack(Rect inRect)
        {
            float contentW = CalcContentWidth(inRect);
            float contentX = CalcContentX(inRect);
            float currentY = ContentStartY;

            // ── 标题（居中） ──
            Rect titleRect = new Rect(contentX, currentY, contentW, TitleHeight);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = Color.white;
            Widgets.Label(titleRect, resolvedTitleText);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            currentY = titleRect.yMax + 4f;

            // ── 分割线 ──
            Rect dividerRect = new Rect(contentX, currentY, contentW, 1f);
            Widgets.DrawBoxSolid(dividerRect, DividerColor);
            currentY = dividerRect.yMax + 6f;

            // ── 描述（居中文本，可滚动） ──
            Rect descAreaRect = new Rect(contentX, currentY, contentW, DescHeight);
            float descTextHeight = Text.CalcHeight(resolvedDescText, descAreaRect.width - 16f);
            Rect descViewRect = new Rect(0f, 0f, descAreaRect.width - 16f, descTextHeight);

            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.BeginScrollView(descAreaRect, ref descScrollPos, descViewRect);
            GUI.color = new Color(0.9f, 0.9f, 0.9f);
            Widgets.Label(descViewRect, resolvedDescText);
            GUI.color = Color.white;
            Widgets.EndScrollView();
            Text.Anchor = TextAnchor.UpperLeft;
            currentY = descAreaRect.yMax + 10f;

            // ── 选项列表 ──
            float optionsBottom = inRect.y + inRect.height - ContentPadding;
            Rect optionsAreaRect = new Rect(contentX, currentY, contentW, optionsBottom - currentY);
            DrawOptionSection(optionsAreaRect);
        }

        // ──────────────────── 选项绘制 ────────────────────

        private void DrawOptionSection(Rect optionRect)
        {
            int optionCount = optionDrawInfos.Count;
            if (optionCount == 0)
                return;

            float totalOptionHeight = optionCount * OptionHeight + (optionCount - 1) * OptionSpacing;
            float startY = optionRect.y;

            if (totalOptionHeight > optionRect.height)
            {
                float availablePerOption = (optionRect.height - (optionCount - 1) * OptionSpacing) / optionCount;
                float clampedH = Mathf.Min(OptionHeight, availablePerOption);

                for (int i = 0; i < optionCount; i++)
                {
                    float y = startY + i * (clampedH + OptionSpacing);
                    Rect optRect = new Rect(optionRect.x, y, optionRect.width, clampedH);
                    optionDrawInfos[i].rect = optRect;
                    DrawOption(optRect, optionDrawInfos[i]);
                }
            }
            else
            {
                float offsetY = (optionRect.height - totalOptionHeight) / 2f;
                for (int i = 0; i < optionCount; i++)
                {
                    float y = startY + offsetY + i * (OptionHeight + OptionSpacing);
                    Rect optRect = new Rect(optionRect.x, y, optionRect.width, OptionHeight);
                    optionDrawInfos[i].rect = optRect;
                    DrawOption(optRect, optionDrawInfos[i]);
                }
            }
        }

        private void DrawOption(Rect optRect, OptionDrawInfo info)
        {
            Color bgColor = info.available
                ? (Mouse.IsOver(optRect) ? OptionColorHover : OptionColor)
                : OptionColorDisabled;
            Widgets.DrawBoxSolid(optRect, bgColor);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = info.available ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            Widgets.Label(optRect, info.labelText);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;

            if (info.available && Widgets.ButtonInvisible(optRect))
            {
                OnOptionSelected(info.option);
            }

            if (!string.IsNullOrEmpty(info.tooltipText))
            {
                TooltipHandler.TipRegion(optRect, info.tooltipText);
            }
        }

        // ──────────────────── 窗口关闭 ────────────────────

        public override void Close(bool doCloseSound = true)
        {
            StellarisEventManager.PendingWindowEventDefName = null;
            base.Close(doCloseSound);
        }

        // ──────────────────── 选项处理 ────────────────────

        private void OnOptionSelected(StellarisEventOption option)
        {
            option.action?.Execute();
            Close();
            eventDef.after?.Execute();
            StellarisEventFlagManager.ClearLocalFlags();
        }
    }
}
