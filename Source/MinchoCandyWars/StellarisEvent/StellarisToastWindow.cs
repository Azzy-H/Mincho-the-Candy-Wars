#nullable disable
using UnityEngine;
using Verse;

namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// 吐司窗口 —— 轻量级通知弹窗，用于快速反馈事件结果。
    /// 布局：左侧立绘 + 右侧标题/描述 + 底部进度条。
    ///
    /// 生命周期：
    /// - immediate 在事件触发时执行（TryFire）
    /// - after 在吐司窗口消失时执行（Close 内）
    ///
    /// 与常规窗口/超事件窗口的区别：
    /// - 不支持选项（所有 option 被忽略）
    /// - 进度条 5 秒倒计时（现实时间，不受游戏倍速影响），仅用于给玩家阅读时间
    /// - 可拖动，位置会保存供下一个吐司使用
    /// - 队列机制：一次只显示一个吐司
    /// - 不强制暂停游戏
    ///
    /// 计时逻辑由 StellarisEventManager.TickToasts() 驱动（非 WindowTick）：
    /// 按 TickToasts 两次调用间的真实流逝时间（Time.realtimeSinceStartup）递减
    /// RemainingRealSeconds，因此无论游戏倍速多少，吐司都在现实 5 秒后消失；
    /// 鼠标悬停检测在此窗口的 DoWindowContents 中进行，悬停时计时暂停。
    ///
    /// 图像截断规则（与常规窗口一致）：
    /// 右部和下部超出截断，左部和上部不截断。
    /// </summary>
    public class StellarisToastWindow : Window
    {
        // ──────────────────── 布局常量 ────────────────────
        private const float WindowWidth = 515f;
        private const float WindowHeight = 192f;
        private const float PortraitSize = 180f;
        private const float ProgressBarHeight = 3f;
        private const float Padding = 12f;

        /// <summary>吐司总停留时间（现实秒数）</summary>
        public const float TotalDurationSeconds = StellarisEventManager.ToastDurationSeconds;

        /// <summary>剩余停留现实秒数。由 StellarisEventManager.TickToasts() 按真实流逝时间递减。</summary>
        public float RemainingRealSeconds;

        // ──────────────────── 数据 ────────────────────
        private StellarisEventDef eventDef;
        private string resolvedTitleText;
        private string resolvedDescText;

        /// <summary>当前帧鼠标是否悬停在窗口内容区上。由 DoWindowContents 更新，TickToasts 读取。</summary>
        private bool isHovered;

        // ──────────────────── 样式 ────────────────────
        private static readonly Color FallbackBgColor = new Color(0.08f, 0.08f, 0.12f);
        private static readonly Color ProgressBgColor = new Color(0.15f, 0.15f, 0.2f, 0.5f);
        private static readonly Color ProgressFillColor = new Color(0.4f, 0.6f, 0.55f, 0.5f);
        private static readonly Color DividerColor = new Color(0.4f, 0.4f, 0.4f, 0.35f);

        // ──────────────────── 公开属性 ────────────────────

        /// <summary>计时器是否因鼠标悬停而暂停（供 StellarisEventManager 读取）</summary>
        public bool IsTimerPaused => isHovered;

        /// <summary>窗口矩形（供 StellarisEventManager 读取位置）</summary>
        public Rect WindowRect => windowRect;

        // ──────────────────── 构造 ────────────────────

        public StellarisToastWindow(StellarisEventDef def, float remainingRealSeconds)
        {
            eventDef = def;
            RemainingRealSeconds = Mathf.Max(0f, remainingRealSeconds);

            doCloseX = false;
            closeOnCancel = false;
            closeOnClickedOutside = false;
            absorbInputAroundWindow = false;
            forcePause = false;
            draggable = true;
            resizeable = false;
            preventCameraMotion = false;
            doWindowBackground = false;
            drawShadow = false;

            resolvedTitleText = eventDef.InjectParams(
                (eventDef.title ?? eventDef.label).Translate().ToString());
            resolvedDescText = eventDef.ResolveLocalizedDescText(eventDef.desc);
        }

        public override Vector2 InitialSize => new Vector2(WindowWidth, WindowHeight);

        // ──────────────────── 初始位置（支持保存位置） ────────────────────

        protected override void SetInitialSizeAndPosition()
        {
            base.SetInitialSizeAndPosition();

            if (StellarisEventManager.HasSavedToastPosition)
            {
                Vector2 pos = StellarisEventManager.SavedToastPosition;
                windowRect.position = pos;
            }
            else
            {
                // 首次出现：默认定位在屏幕右上角（距上边、右边各 150px），而非基类默认的屏幕居中。
                // 之后的吐司沿用拖动/保存的位置（TickToasts 会保存当前位置）。
                windowRect.position = new Vector2(
                    UI.screenWidth - WindowWidth - 150f,
                    150f);
            }
        }

        // ──────────────────── 主绘制 ────────────────────

        public override void DoWindowContents(Rect inRect)
        {
            // 更新鼠标悬停状态 —— 使用 inRect 而非 windowRect，因为 DoWindowContents
            // 内部的鼠标坐标是窗口相对坐标系，与 windowRect（屏幕坐标系）不匹配。
            isHovered = Mouse.IsOver(inRect);

            // ── 背景 ──
            DrawBackground(inRect);

            // ── 上部：立绘 + 文本（进度条上方所有空间） ──
            float contentHeight = inRect.height - ProgressBarHeight;
            Rect topRect = new Rect(inRect.x, inRect.y, inRect.width, contentHeight);

            // 左侧立绘
            Rect portraitRect = new Rect(topRect.x, topRect.y, PortraitSize, PortraitSize);
            DrawPortrait(portraitRect);

            // 右侧消息区域
            Rect msgRect = new Rect(
                portraitRect.xMax + Padding,
                topRect.y,
                topRect.width - PortraitSize - Padding,
                topRect.height);
            DrawMessageArea(msgRect);

            // ── 底部进度条（基于 inRect 底部，确保始终可见） ──
            Rect barRect = new Rect(
                inRect.x, inRect.yMax - ProgressBarHeight,
                inRect.width, ProgressBarHeight);
            DrawProgressBar(barRect);
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

        // ──────────────────── 立绘绘制（与常规窗口一致的截断规则） ────────────────────

        private void DrawPortrait(Rect portraitRect)
        {
            if (eventDef.selectedPicture == null)
                return;

            Texture2D tex = eventDef.selectedPicture;
            float scale = portraitRect.height / tex.height;
            float scaledWidth = tex.width * scale;
            float offsetX = eventDef.selectedPictureOffsetX;
            float offsetY = eventDef.selectedPictureOffsetY;

            GUI.BeginGroup(portraitRect);
            Rect imageRect = new Rect(offsetX, offsetY, scaledWidth, portraitRect.height);
            GUI.DrawTexture(imageRect, tex, ScaleMode.StretchToFill);
            GUI.EndGroup();
        }

        // ──────────────────── 消息区域 ────────────────────

        private void DrawMessageArea(Rect msgRect)
        {
            // ── 标题 ──
            Rect titleRect = new Rect(
                msgRect.x, msgRect.y + Padding,
                msgRect.width - Padding, 30f);

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = Color.white;
            Widgets.Label(titleRect, resolvedTitleText);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            // ── 分割线 ──
            Rect dividerRect = new Rect(
                msgRect.x, titleRect.yMax + 6f,
                msgRect.width - Padding, 1f);
            Widgets.DrawBoxSolid(dividerRect, DividerColor);

            // ── 描述 ──
            Rect descRect = new Rect(
                msgRect.x, dividerRect.yMax + 8f,
                msgRect.width - Padding,
                msgRect.yMax - dividerRect.yMax - 12f);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = new Color(0.9f, 0.9f, 0.9f);
            Widgets.Label(descRect, resolvedDescText);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        // ──────────────────── 进度条 ────────────────────

        private void DrawProgressBar(Rect barRect)
        {
            Widgets.DrawBoxSolid(barRect, ProgressBgColor);

            float progress = Mathf.Clamp01(RemainingRealSeconds / TotalDurationSeconds);
            Rect fillRect = new Rect(barRect.x, barRect.y, barRect.width * progress, barRect.height);
            Widgets.DrawBoxSolid(fillRect, ProgressFillColor);
        }

        // ──────────────────── 窗口关闭（执行 after 效果） ────────────────────

        public override void Close(bool doCloseSound = true)
        {
            // 吐司消失时执行 after 效果
            eventDef.after?.Execute();
            StellarisEventFlagManager.ClearLocalFlags();

            // 通知管理器：清除活跃吐司，弹出队列中下一个
            StellarisEventManager.OnToastClosed(this);

            base.Close(doCloseSound);
        }
    }
}
