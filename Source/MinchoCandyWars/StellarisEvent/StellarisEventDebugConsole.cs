using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using LudeonTK;

namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// Stellaris 事件调试控制台 —— 浏览所有已注册事件，检查 trigger 状态，快速测试触发
    /// </summary>
    public class StellarisEventDebugConsole : Window
    {
        private const float WindowWidth = 800f;
        private const float WindowHeight = 650f;
        private const float RowHeight = 32f;
        private const float Padding = 12f;
        private const float WarningHeight = 26f;

        private Vector2 scrollPos = Vector2.zero;
        private List<EventRowInfo> rows = new List<EventRowInfo>();
        private string filterText = "";

        // 警告提示
        private string? warningMessage = null;
        private int warningExpireTick = 0;

        // 颜色
        private static readonly Color TriggerPassColor = Color.green;
        private static readonly Color TriggerFailColor = Color.yellow;
        private static readonly Color WarnBgColor = new Color(0.5f, 0.4f, 0f, 0.85f);
        private static readonly Color RowEvenColor = new Color(0.15f, 0.15f, 0.18f);
        private static readonly Color RowOddColor = new Color(0.12f, 0.12f, 0.15f);
        private static readonly Color BtnNormalColor = new Color(0.25f, 0.55f, 0.25f);
        private static readonly Color BtnForceColor = new Color(0.65f, 0.35f, 0.1f);
        private static readonly Color BtnRefreshColor = new Color(0.2f, 0.4f, 0.65f);

        private class EventRowInfo
        {
            public StellarisEventDef? def;
            public bool canTrigger;
        }

        public StellarisEventDebugConsole()
        {
            doCloseX = true;
            closeOnClickedOutside = false;
            absorbInputAroundWindow = true;
            forcePause = false;
            draggable = true;
            resizeable = false;
            preventCameraMotion = false;

            RefreshList();
        }

        public override Vector2 InitialSize => new Vector2(WindowWidth, WindowHeight);

        // ──────────────────── 数据刷新 ────────────────────

        private void RefreshList()
        {
            // 明确使用全限定类型以避免与其他 assembly 中的同名类型冲突
            // （例如 MinchoCandyWars.dll 中可能残留旧的 MinchoCandyWars.StellarisEventDef）
            System.Type exactType = typeof(MinchoCandyWars.StellarisEvent.StellarisEventDef);

            var dbProperty = typeof(DefDatabase<>).MakeGenericType(exactType)
                .GetProperty("AllDefsListForReading",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            System.Collections.IList? allDefsRaw = null;
            if (dbProperty != null)
            {
                allDefsRaw = dbProperty.GetValue(null) as System.Collections.IList;
            }

            rows.Clear();

            if (allDefsRaw == null || allDefsRaw.Count == 0)
            {
                // 回退：通过 DefDatabase<T> 泛型直接访问
                var allDefs = DefDatabase<StellarisEventDef>.AllDefsListForReading;
                if (allDefs != null)
                {
                    foreach (var def in allDefs)
                    {
                        AddRowIfMatch(def);
                    }
                }

                if (rows.Count == 0)
                {
                    Log.Warning($"[StellarisEvent Debug] 未找到任何 StellarisEventDef。"
                        + $"已加载类型: {exactType.FullName}, Assembly: {exactType.Assembly.GetName().Name}");
                }
            }
            else
            {
                foreach (StellarisEventDef def in allDefsRaw)
                {
                    AddRowIfMatch(def);
                }
            }

            rows.Sort((a, b) => a.def!.defName.CompareTo(b.def!.defName));
        }

        private void AddRowIfMatch(StellarisEventDef def)
        {
            if (def == null) return;
            if (string.IsNullOrEmpty(filterText) ||
                def.defName.ToLowerInvariant().Contains(filterText.ToLowerInvariant()) ||
                (def.title?.ToLowerInvariant().Contains(filterText.ToLowerInvariant()) ?? false) ||
                (def.label?.ToLowerInvariant().Contains(filterText.ToLowerInvariant()) ?? false))
            {
                rows.Add(new EventRowInfo
                {
                    def = def,
                    canTrigger = def.CanTrigger()
                });
            }
        }

        // ──────────────────── 主绘制 ────────────────────

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;

            // ── 标题栏 ──
            Rect titleRect = new Rect(inRect.x, inRect.y, inRect.width, 36f);
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(titleRect.x + Padding, titleRect.y, 300f, titleRect.height),
                "Stellaris Event Debug Console");
            Text.Font = GameFont.Small;

            Widgets.Label(new Rect(titleRect.x + titleRect.width - 200f, titleRect.y + 10f, 200f, 20f),
                $"Total: {rows.Count} events");

            // ── 过滤输入 ──
            Rect filterRect = new Rect(inRect.x + Padding, titleRect.yMax + 6f, 220f, 26f);
            string newFilter = Widgets.TextField(filterRect, filterText);
            if (newFilter != filterText)
            {
                filterText = newFilter;
                RefreshList();
            }
            if (filterText == "" && GUI.GetNameOfFocusedControl() != "StellarisEventFilter")
            {
                GUI.SetNextControlName("StellarisEventFilter");
                GUI.FocusControl("StellarisEventFilter");
            }

            // 清除过滤按钮
            if (Widgets.ButtonText(new Rect(filterRect.xMax + 8f, filterRect.y, 60f, 26f), "Clear"))
            {
                filterText = "";
                RefreshList();
            }

            // 刷新按钮
            if (Widgets.ButtonText(new Rect(filterRect.xMax + 76f, filterRect.y, 80f, 26f), "Refresh"))
            {
                RefreshList();
            }

            // 全部强行触发按钮
            Rect forceAllRect = new Rect(inRect.xMax - Padding - 130f, filterRect.y, 130f, 26f);
            if (Widgets.ButtonText(forceAllRect, "Force Fire All"))
            {
                foreach (var row in rows)
                {
                    ForceFireEvent(row.def!);
                }
            }
            TooltipHandler.TipRegion(forceAllRect, "Skip all triggers and fire every event in sequence");

            // ── 列标题 ──
            float listTop = filterRect.yMax + 10f;
            Rect headerRect = new Rect(inRect.x + Padding, listTop, inRect.width - Padding * 2f, 24f);
            DrawHeaderRow(headerRect);

            // ── 事件列表 ──
            Rect listRect = new Rect(inRect.x + Padding, listTop + 26f, inRect.width - Padding * 2f, inRect.height - listTop - 30f - WarningHeight);
            DrawEventList(listRect);

            // ── 底部警告区域 ──
            Rect warnRect = new Rect(inRect.x + Padding, inRect.yMax - WarningHeight - Padding, inRect.width - Padding * 2f, WarningHeight);
            DrawWarningArea(warnRect);
        }

        // ──────────────────── 列标题 ────────────────────

        private void DrawHeaderRow(Rect rect)
        {
            Text.Font = GameFont.Tiny;
            float colDefName = 180f;
            float colTitle = 234f;
            float colStatus = 80f;
            float colOptions = 60f;
            float colPictures = 60f;
            float colAction1 = 80f;
            float colAction2 = 80f;

            float x = rect.x;
            Widgets.Label(new Rect(x, rect.y, colDefName, rect.height), "defName");
            x += colDefName;
            Widgets.Label(new Rect(x, rect.y, colTitle, rect.height), "Title");
            x += colTitle;
            Widgets.Label(new Rect(x, rect.y, colStatus, rect.height), "Trigger");
            x += colStatus;
            Widgets.Label(new Rect(x, rect.y, colOptions, rect.height), "Options");
            x += colOptions;
            Widgets.Label(new Rect(x, rect.y, colPictures, rect.height), "Pics");
            x += colPictures;
            Widgets.Label(new Rect(x, rect.y, colAction1, rect.height), "Fire");
            x += colAction1;
            Widgets.Label(new Rect(x, rect.y, colAction2, rect.height), "Force");

            Text.Font = GameFont.Small;
        }

        // ──────────────────── 事件列表 ────────────────────

        private void DrawEventList(Rect listRect)
        {
            float contentHeight = rows.Count * RowHeight;
            Rect viewRect = new Rect(0f, 0f, listRect.width - 16f, contentHeight);

            Widgets.BeginScrollView(listRect, ref scrollPos, viewRect);

            for (int i = 0; i < rows.Count; i++)
            {
                Rect rowRect = new Rect(0f, i * RowHeight, viewRect.width, RowHeight);
                DrawEventRow(rowRect, rows[i], i);
            }

            Widgets.EndScrollView();

            if (rows.Count == 0)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(listRect, "No events found. Add <StellarisEventDef> entries to your XML defs.");
                Text.Anchor = TextAnchor.UpperLeft;
            }
        }

        private void DrawEventRow(Rect rect, EventRowInfo row, int index)
        {
            // 交替行背景
            Color bgColor = index % 2 == 0 ? RowEvenColor : RowOddColor;
            if (Mouse.IsOver(rect))
                bgColor = new Color(bgColor.r + 0.08f, bgColor.g + 0.08f, bgColor.b + 0.08f);
            Widgets.DrawBoxSolid(rect, bgColor);

            float colDefName = 180f;
            float colTitle = 230f;
            float colStatus = 80f;
            float colOptions = 50f;
            float colPictures = 50f;
            float colAction1 = 75f;
            float colAction2 = 75f;

            float x = rect.x + 4f;
            float y = rect.y + (rect.height - 20f) / 2f;

            // defName
            Widgets.Label(new Rect(x, y, colDefName, 20f), row.def!.defName);
            x += colDefName;

            // Title
            string displayTitle = row.def.title ?? row.def.label ?? "(no title)";
            Widgets.Label(new Rect(x, y, colTitle, 20f), displayTitle.Truncate(colTitle - 10f));
            x += colTitle;

            // Trigger 状态
            Color prevColor = GUI.color;
            GUI.color = row.canTrigger ? TriggerPassColor : TriggerFailColor;
            string statusText = row.canTrigger ? "✓ Pass" : "⚠ Fail";
            Widgets.Label(new Rect(x, y, colStatus, 20f), statusText);
            GUI.color = prevColor;
            x += colStatus;

            // Options 计数
            Widgets.Label(new Rect(x, y, colOptions, 20f), $"{row.def.options.Count}");
            x += colOptions;

            // Pictures 计数
            Widgets.Label(new Rect(x, y, colPictures, 20f), $"{row.def.pictures.Count}");
            x += colPictures;

            // Fire 按钮（正常触发）
            Rect fireBtnRect = new Rect(x, rect.y + 3f, colAction1 - 4f, rect.height - 6f);
            DrawColoredButton(fireBtnRect, "Fire", BtnNormalColor);
            if (Widgets.ButtonInvisible(fireBtnRect))
            {
                HandleNormalFire(row);
            }
            x += colAction1;

            // Force Fire 按钮（强制触发）
            Rect forceBtnRect = new Rect(x, rect.y + 3f, colAction2 - 4f, rect.height - 6f);
            DrawColoredButton(forceBtnRect, "Force", BtnForceColor);
            if (Widgets.ButtonInvisible(forceBtnRect))
            {
                HandleForceFire(row);
            }
        }

        private void DrawColoredButton(Rect rect, string label, Color color)
        {
            Color prevColor = GUI.color;
            GUI.color = color;
            Widgets.DrawBoxSolid(rect, color);
            GUI.color = Color.white;

            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, label);
            Text.Anchor = TextAnchor.UpperLeft;

            GUI.color = prevColor;
        }

        // ──────────────────── 事件触发逻辑 ────────────────────

        private void HandleNormalFire(EventRowInfo row)
        {
            if (row.canTrigger)
            {
                ClearWarning();
                row.def!.TryFire();
                Log.Message($"Fired event: {row.def.defName}");
            }
            else
            {
                // Trigger 不通过 → 输出黄色警告
                ShowWarning($"Trigger FAILED for '{row.def!.defName}'. Use 'Force' to bypass the trigger check and fire anyway.");
            }
        }

        private void HandleForceFire(EventRowInfo row)
        {
            if (!row.canTrigger)
            {
                ShowWarning($"Force-firing '{row.def!.defName}' — trigger check BYPASSED.");
            }

            ForceFireEvent(row.def!);
            RefreshList(); // 刷新以反映可能的状态变更
        }

        private void ForceFireEvent(StellarisEventDef def)
        {
            try
            {
                // 跳过 trigger 判定，直接执行 immediate → 选立绘 → 开窗口
                def.immediate?.Execute();
                def.SelectRandomPicture();
                Find.WindowStack.Add(new StellarisEventWindow(def));
            }
            catch (System.Exception ex)
            {
                Log.Error($"[StellarisEvent Debug] Error force-firing '{def.defName}': {ex}");
                ShowWarning($"Error force-firing '{def.defName}': {ex.Message}");
            }
        }

        // ──────────────────── 警告区域 ────────────────────

        private void DrawWarningArea(Rect rect)
        {
            if (string.IsNullOrEmpty(warningMessage))
                return;

            // 警告自动过期（5 秒 = 300 ticks）
            if (Find.TickManager.TicksGame > warningExpireTick)
            {
                warningMessage = null;
                return;
            }

            Widgets.DrawBoxSolid(rect, WarnBgColor);

            Text.Anchor = TextAnchor.MiddleLeft;
            Color prevColor = GUI.color;
            GUI.color = Color.yellow;
            Widgets.Label(new Rect(rect.x + 8f, rect.y, rect.width - 16f, rect.height), $"⚠ {warningMessage}");
            GUI.color = prevColor;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void ShowWarning(string msg)
        {
            warningMessage = msg;
            warningExpireTick = Find.TickManager.TicksGame + 300; // 5 秒后消失
        }

        private void ClearWarning()
        {
            warningMessage = null;
            warningExpireTick = 0;
        }
    }

    /// <summary>
    /// 调试菜单入口 —— 在 Debug 操作菜单中注册入口，也可通过 dev 控制台调用
    /// </summary>
    [StaticConstructorOnStartup]
    public static class StellarisEventDebugMenu
    {
        /// <summary>快捷打开调试控制台（可通过 dev 控制台输入此方法名直接调用）</summary>
        [DebugAction("Stellaris Events", "Open Event Console", false, false, false, false, false, 0, false)]
        public static void OpenConsole()
        {
            Find.WindowStack.Add(new StellarisEventDebugConsole());
        }
    }
}
