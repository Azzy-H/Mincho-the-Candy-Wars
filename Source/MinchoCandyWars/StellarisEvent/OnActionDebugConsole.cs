#nullable disable
using System.Collections.Generic;
using UnityEngine;
using Verse;
using LudeonTK;

namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// OnAction 调试控制台 —— 浏览所有 OnActionDef，查看信号、关联事件、触发次数，支持手动强制触发。
    /// </summary>
    public class OnActionDebugConsole : Window
    {
        private const float WindowWidth = 750f;
        private const float WindowHeight = 550f;
        private const float RowHeight = 32f;
        private const float Padding = 12f;
        private const float WarningHeight = 26f;

        private Vector2 scrollPos = Vector2.zero;
        private List<OnActionRowInfo> rows = new List<OnActionRowInfo>();
        private string filterText = "";

        // 警告提示
        private string warningMessage = null;
        private int warningExpireTick = 0;

        // 颜色
        private static readonly Color WarnBgColor = new Color(0.5f, 0.4f, 0f, 0.85f);
        private static readonly Color RowEvenColor = new Color(0.15f, 0.15f, 0.18f);
        private static readonly Color RowOddColor = new Color(0.12f, 0.12f, 0.15f);
        private static readonly Color BtnFireColor = new Color(0.25f, 0.55f, 0.25f);
        private static readonly Color BtnRefreshColor = new Color(0.2f, 0.4f, 0.65f);

        private class OnActionRowInfo
        {
            public OnActionDef def;
            public int triggerCount;
        }

        public OnActionDebugConsole()
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
            rows.Clear();
            var allDefs = DefDatabase<OnActionDef>.AllDefsListForReading;
            if (allDefs != null)
            {
                foreach (var def in allDefs)
                {
                    if (def == null) continue;
                    if (string.IsNullOrEmpty(filterText) ||
                        def.defName.ToLowerInvariant().Contains(filterText.ToLowerInvariant()) ||
                        (def.action?.ToLowerInvariant().Contains(filterText.ToLowerInvariant()) ?? false))
                    {
                        rows.Add(new OnActionRowInfo
                        {
                            def = def,
                            triggerCount = StellarisEventManager.GetOnActionTriggerCount(def.defName)
                        });
                    }
                }
            }
            rows.Sort((a, b) => a.def.defName.CompareTo(b.def.defName));
        }

        // ──────────────────── 主绘制 ────────────────────

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;

            // ── 标题栏 ──
            Rect titleRect = new Rect(inRect.x, inRect.y, inRect.width, 30f);
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(titleRect.x + Padding, titleRect.y, 300f, titleRect.height),
                "OnAction Debug Console");
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(titleRect.x + titleRect.width - 200f, titleRect.y + 8f, 200f, 20f),
                $"Total: {rows.Count} onActions");

            // ── 过滤输入 ──
            float barY = titleRect.yMax + 4f;
            Rect filterRect = new Rect(inRect.x + Padding, barY, 200f, 24f);
            string newFilter = Widgets.TextField(filterRect, filterText);
            if (newFilter != filterText)
            {
                filterText = newFilter;
                RefreshList();
            }

            if (Widgets.ButtonText(new Rect(filterRect.xMax + 6f, barY, 50f, 24f), "Filter"))
                RefreshList();
            if (Widgets.ButtonText(new Rect(filterRect.xMax + 60f, barY, 50f, 24f), "Clear"))
            {
                filterText = "";
                RefreshList();
            }

            // 刷新按钮
            Rect refreshBtnRect = new Rect(filterRect.xMax + 120f, barY, 70f, 24f);
            DrawColoredButton(refreshBtnRect, "Refresh", BtnRefreshColor);
            if (Widgets.ButtonInvisible(refreshBtnRect))
                RefreshList();

            // ── 列标题 ──
            float listTop = barY + 30f;
            Rect headerRect = new Rect(inRect.x + Padding, listTop, inRect.width - Padding * 2f, 22f);
            DrawHeaderRow(headerRect);

            // ── 列表 ──
            Rect listRect = new Rect(inRect.x + Padding, listTop + 24f,
                inRect.width - Padding * 2f, inRect.height - listTop - 30f - WarningHeight);
            DrawList(listRect);

            // ── 底部警告区域 ──
            Rect warnRect = new Rect(inRect.x + Padding, inRect.yMax - WarningHeight - Padding,
                inRect.width - Padding * 2f, WarningHeight);
            DrawWarningArea(warnRect);
        }

        // ──────────────────── 列标题 ────────────────────

        private void DrawHeaderRow(Rect rect)
        {
            Text.Font = GameFont.Tiny;
            float colDefName = 150f;
            float colAction = 160f;
            float colTrigList = 70f;
            float colRandList = 70f;
            float colCount = 60f;
            float colFire = 80f;

            float x = rect.x;
            Widgets.Label(new Rect(x, rect.y, colDefName, rect.height), "defName"); x += colDefName;
            Widgets.Label(new Rect(x, rect.y, colAction, rect.height), "Action (Signal)"); x += colAction;
            Widgets.Label(new Rect(x, rect.y, colTrigList, rect.height), "Triggers"); x += colTrigList;
            Widgets.Label(new Rect(x, rect.y, colRandList, rect.height), "Random"); x += colRandList;
            Widgets.Label(new Rect(x, rect.y, colCount, rect.height), "Fired"); x += colCount;
            Widgets.Label(new Rect(x, rect.y, colFire, rect.height), "Force");

            Text.Font = GameFont.Small;
        }

        // ──────────────────── 列表 ────────────────────

        private void DrawList(Rect listRect)
        {
            float contentHeight = rows.Count * RowHeight;
            Rect viewRect = new Rect(0f, 0f, listRect.width - 16f, contentHeight);

            Widgets.BeginScrollView(listRect, ref scrollPos, viewRect);

            for (int i = 0; i < rows.Count; i++)
            {
                Rect rowRect = new Rect(0f, i * RowHeight, viewRect.width, RowHeight);
                DrawRow(rowRect, rows[i], i);
            }

            Widgets.EndScrollView();

            if (rows.Count == 0)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(listRect, "No OnActionDefs found. Add <OnActionDef> entries to your XML defs.");
                Text.Anchor = TextAnchor.UpperLeft;
            }
        }

        private void DrawRow(Rect rect, OnActionRowInfo row, int index)
        {
            Color bgColor = index % 2 == 0 ? RowEvenColor : RowOddColor;
            if (Mouse.IsOver(rect))
                bgColor = new Color(bgColor.r + 0.08f, bgColor.g + 0.08f, bgColor.b + 0.08f);
            Widgets.DrawBoxSolid(rect, bgColor);

            float colDefName = 150f;
            float colAction = 160f;
            float colTrigList = 70f;
            float colRandList = 70f;
            float colCount = 60f;
            float colFire = 80f;

            float x = rect.x + 4f;
            float y = rect.y + (rect.height - 20f) / 2f;

            // defName
            Widgets.Label(new Rect(x, y, colDefName, 20f), row.def.defName);
            x += colDefName;

            // Action (signal)
            GUI.color = new Color(0.5f, 0.8f, 0.9f);
            Widgets.Label(new Rect(x, y, colAction, 20f),
                (row.def.action ?? "(none)").Truncate(colAction - 10f));
            GUI.color = Color.white;
            x += colAction;

            // triggerList 计数
            int trigCount = row.def.triggerList?.Count ?? 0;
            string trigTooltip = trigCount > 0
                ? string.Join("\n", row.def.triggerList)
                : "No triggerList entries";
            Widgets.Label(new Rect(x, y, colTrigList, 20f), trigCount.ToString());
            TooltipHandler.TipRegion(new Rect(x, y, colTrigList, 20f), trigTooltip);
            x += colTrigList;

            // randomTriggerList 计数
            int randCount = row.def.randomTriggerList?.Count ?? 0;
            var randTooltipParts = new List<string>();
            if (row.def.randomTriggerList != null)
            {
                foreach (var entry in row.def.randomTriggerList)
                {
                    string label = entry.eventDefName.NullOrEmpty() ? "(empty)" : entry.eventDefName;
                    randTooltipParts.Add($"[w={entry.weight}] {label}");
                }
            }
            string randTooltip = randTooltipParts.Count > 0
                ? string.Join("\n", randTooltipParts)
                : "No randomTriggerList entries";
            Widgets.Label(new Rect(x, y, colRandList, 20f), randCount.ToString());
            TooltipHandler.TipRegion(new Rect(x, y, colRandList, 20f), randTooltip);
            x += colRandList;

            // 触发次数
            Widgets.Label(new Rect(x, y, colCount, 20f), row.triggerCount.ToString());
            x += colCount;

            // Force Fire 按钮
            Rect fireBtnRect = new Rect(x, rect.y + 3f, colFire - 8f, rect.height - 6f);
            DrawColoredButton(fireBtnRect, "Fire", BtnFireColor);
            if (Widgets.ButtonInvisible(fireBtnRect))
            {
                HandleForceFire(row);
            }
        }

        // ──────────────────── 按钮辅助 ────────────────────

        private static void DrawColoredButton(Rect rect, string label, Color color)
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

        // ──────────────────── 强制触发 ────────────────────

        private void HandleForceFire(OnActionRowInfo row)
        {
            if (row.def.action.NullOrEmpty())
            {
                ShowWarning($"OnAction '{row.def.defName}' has no action signal defined.");
                return;
            }

            StellarisEventManager.DispatchAction(row.def.action);
            ShowWarning($"Force-fired OnAction '{row.def.defName}' (signal: '{row.def.action}')");
            RefreshList(); // 刷新触发计数
        }

        // ──────────────────── 警告区域 ────────────────────

        private void DrawWarningArea(Rect rect)
        {
            if (string.IsNullOrEmpty(warningMessage))
                return;

            if (Find.TickManager.TicksGame > warningExpireTick)
            {
                warningMessage = null;
                return;
            }

            Widgets.DrawBoxSolid(rect, WarnBgColor);
            Text.Anchor = TextAnchor.MiddleLeft;
            Color prevColor = GUI.color;
            GUI.color = Color.yellow;
            Widgets.Label(new Rect(rect.x + 8f, rect.y, rect.width - 16f, rect.height), $"! {warningMessage}");
            GUI.color = prevColor;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void ShowWarning(string msg)
        {
            warningMessage = msg;
            warningExpireTick = Find.TickManager.TicksGame + 300;
        }
    }

    /// <summary>
    /// OnAction 调试控制台 Debug 菜单入口
    /// </summary>
    [StaticConstructorOnStartup]
    public static class OnActionDebugMenu
    {
        [DebugAction("Stellaris Events", "Open OnAction Console", false, false, false, false, false, 0, false)]
        public static void OpenConsole()
        {
            Find.WindowStack.Add(new OnActionDebugConsole());
        }
    }
}
