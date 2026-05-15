using System.Collections.Generic;
using UnityEngine;
using Verse;
using LudeonTK;

namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// Flag 调试控制台 —— 检视、添加、删除 StellarisEvent Flag
    /// </summary>
    public class StellarisEventFlagConsole : Window
    {
        private const float WindowWidth = 700f;
        private const float WindowHeight = 600f;
        private const float RowHeight = 28f;
        private const float Padding = 10f;
        private const float ColName = 160f;
        private const float ColType = 70f;
        private const float ColValue = 200f;
        private const float ColTicks = 100f;
        private const float ColDel = 60f;
        private const float WarningHeight = 26f;

        private Vector2 scrollPos = Vector2.zero;
        private string filterText = "";
        private List<StellarisEventFlag> filteredFlags = new List<StellarisEventFlag>();

        // 添加模式
        private bool addingMode = false;
        private string newFlagName = "";
        private FlagValueType newFlagType = FlagValueType.Bool;
        private string newFlagStringVal = "";
        private string newFlagFloatVal = "0";
        private bool newFlagBoolVal = false;
        private string newFlagTicks = "";
        private string[] typeNames = { "Bool", "Float", "String", "Thing" };

        // 警告
        private string? warningMessage = null;
        private int warningExpireTick = 0;

        // 颜色
        private static readonly Color WarnBgColor = new Color(0.5f, 0.4f, 0f, 0.85f);
        private static readonly Color RowEvenColor = new Color(0.15f, 0.15f, 0.18f);
        private static readonly Color RowOddColor = new Color(0.12f, 0.12f, 0.15f);
        private static readonly Color AddBtnColor = new Color(0.25f, 0.55f, 0.25f);
        private static readonly Color DelBtnColor = new Color(0.65f, 0.25f, 0.2f);
        private static readonly Color PermTicksColor = new Color(0.4f, 0.7f, 0.9f);

        public StellarisEventFlagConsole()
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

        private void RefreshList()
        {
            filteredFlags.Clear();
            foreach (var flag in StellarisEventFlagManager.AllFlags())
            {
                if (flag == null) continue;
                if (string.IsNullOrEmpty(filterText) ||
                    flag.name!.ToLowerInvariant().Contains(filterText.ToLowerInvariant()))
                {
                    filteredFlags.Add(flag);
                }
            }
            filteredFlags.Sort((a, b) => a.name!.CompareTo(b.name));
        }

        // ──────────────────── 主绘制 ────────────────────

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;

            // ── 标题栏 ──
            Rect titleRect = new Rect(inRect.x, inRect.y, inRect.width, 30f);
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(titleRect.x + Padding, titleRect.y, 260f, titleRect.height),
                "Flag Console");
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(titleRect.x + titleRect.width - 160f, titleRect.y + 8f, 160f, 20f),
                $"Total: {filteredFlags.Count} flags");

            // ── 过滤 & 操作按钮栏 ──
            float barY = titleRect.yMax + 4f;
            Rect filterRect = new Rect(inRect.x + Padding, barY, 160f, 24f);
            filterText = Widgets.TextField(filterRect, filterText);
            if (Widgets.ButtonText(new Rect(filterRect.xMax + 6f, barY, 50f, 24f), "Filter"))
                RefreshList();
            if (Widgets.ButtonText(new Rect(filterRect.xMax + 60f, barY, 50f, 24f), "Clear"))
            {
                filterText = "";
                RefreshList();
            }

            // 添加按钮
            Rect addBtnRect = new Rect(filterRect.xMax + 120f, barY, 60f, 24f);
            DrawColoredButton(addBtnRect, addingMode ? "Cancel" : "Add", addingMode ? DelBtnColor : AddBtnColor);
            if (Widgets.ButtonInvisible(addBtnRect))
            {
                addingMode = !addingMode;
                ClearWarning();
            }

            // 清空全部按钮
            Rect clearAllRect = new Rect(inRect.xMax - Padding - 80f, barY, 80f, 24f);
            DrawColoredButton(clearAllRect, "Clear All", DelBtnColor);
            if (Widgets.ButtonInvisible(clearAllRect))
            {
                StellarisEventFlagManager.ClearAll();
                RefreshList();
                ShowWarning("All flags cleared.");
            }

            // ── 添加表单 ──
            float formY = barY + 28f;
            if (addingMode)
            {
                DrawAddForm(inRect, formY);
                formY += 90f;
            }

            // ── 列标题 ──
            Rect headerRect = new Rect(inRect.x + Padding, formY, inRect.width - Padding * 2f, 20f);
            DrawHeaderRow(headerRect);

            // ── Flag 列表 ──
            float listTop = headerRect.yMax + 2f;
            Rect listRect = new Rect(inRect.x + Padding, listTop,
                inRect.width - Padding * 2f, inRect.height - listTop - WarningHeight - Padding);
            DrawFlagList(listRect);

            // ── 底部警告 ──
            Rect warnRect = new Rect(inRect.x + Padding, inRect.yMax - WarningHeight - Padding,
                inRect.width - Padding * 2f, WarningHeight);
            DrawWarningArea(warnRect);
        }

        // ──────────────────── 添加表单 ────────────────────

        private void DrawAddForm(Rect inRect, float y)
        {
            Rect formBg = new Rect(inRect.x + Padding, y, inRect.width - Padding * 2f, 82f);
            Widgets.DrawBoxSolid(formBg, new Color(0.18f, 0.18f, 0.22f));

            float fy = y + 6f;
            float fx = inRect.x + Padding + 10f;

            // Name
            Widgets.Label(new Rect(fx, fy + 4f, 40f, 22f), "Name:");
            Rect nameRect = new Rect(fx + 42f, fy + 2f, 140f, 22f);
            newFlagName = Widgets.TextField(nameRect, newFlagName);

            // Type
            Widgets.Label(new Rect(fx + 190f, fy + 4f, 36f, 22f), "Type:");
            Rect typeRect = new Rect(fx + 228f, fy + 2f, 70f, 22f);
            if (Widgets.ButtonText(typeRect, newFlagType.ToString()))
            {
                // 循环切换类型
                newFlagType = newFlagType switch
                {
                    FlagValueType.Bool   => FlagValueType.Float,
                    FlagValueType.Float  => FlagValueType.String,
                    FlagValueType.String => FlagValueType.Thing,
                    FlagValueType.Thing  => FlagValueType.Bool,
                    _ => FlagValueType.Bool
                };
            }

            // Value（根据类型切换）
            Widgets.Label(new Rect(fx + 304f, fy + 4f, 36f, 22f), "Val:");
            Rect valRect = new Rect(fx + 340f, fy + 2f, 130f, 22f);
            switch (newFlagType)
            {
                case FlagValueType.Bool:
                    if (Widgets.ButtonText(valRect, newFlagBoolVal ? "true" : "false"))
                        newFlagBoolVal = !newFlagBoolVal;
                    break;
                case FlagValueType.Float:
                    newFlagFloatVal = Widgets.TextField(valRect, newFlagFloatVal);
                    break;
                case FlagValueType.String:
                    newFlagStringVal = Widgets.TextField(valRect, newFlagStringVal);
                    break;
                case FlagValueType.Thing:
                    GUI.color = Color.gray;
                    Widgets.Label(valRect, "(use Effect)");
                    GUI.color = Color.white;
                    break;
            }

            // Ticks (optional)
            Widgets.Label(new Rect(fx, fy + 28f, 40f, 22f), "Ticks:");
            Rect ticksRect = new Rect(fx + 42f, fy + 26f, 80f, 22f);
            newFlagTicks = Widgets.TextField(ticksRect, newFlagTicks);
            Widgets.Label(new Rect(fx + 128f, fy + 30f, 100f, 20f), "(empty = permanent)");

            // Confirm 按钮
            Rect confirmRect = new Rect(fx + 240f, fy + 26f, 70f, 22f);
            DrawColoredButton(confirmRect, "Confirm", AddBtnColor);
            if (Widgets.ButtonInvisible(confirmRect))
            {
                TryAddFlag();
            }
        }

        private void TryAddFlag()
        {
            if (newFlagName.NullOrEmpty())
            {
                ShowWarning("Flag name cannot be empty.");
                return;
            }

            int? ticks = null;
            if (!newFlagTicks.NullOrEmpty())
            {
                if (int.TryParse(newFlagTicks, out int t) && t > 0)
                    ticks = t;
                else
                {
                    ShowWarning("Ticks must be a positive integer.");
                    return;
                }
            }

            switch (newFlagType)
            {
                case FlagValueType.Bool:
                    StellarisEventFlagManager.SetFlag(newFlagName, newFlagBoolVal, ticks);
                    break;
                case FlagValueType.Float:
                    if (!float.TryParse(newFlagFloatVal, out float fv))
                    {
                        ShowWarning("Invalid float value.");
                        return;
                    }
                    StellarisEventFlagManager.SetFlag(newFlagName, fv, ticks);
                    break;
                case FlagValueType.String:
                    StellarisEventFlagManager.SetFlag(newFlagName, newFlagStringVal, ticks);
                    break;
                case FlagValueType.Thing:
                    ShowWarning("Thing flags must be created via Effect_SpawnPawnFromFlag or SetFlag(Thing).");
                    return;
            }

            // 重置表单
            newFlagName = "";
            newFlagFloatVal = "0";
            newFlagStringVal = "";
            newFlagBoolVal = false;
            newFlagTicks = "";

            RefreshList();
            ShowWarning($"Flag '{newFlagName}' added/updated.");
        }

        // ──────────────────── 列标题 ────────────────────

        private void DrawHeaderRow(Rect rect)
        {
            Text.Font = GameFont.Tiny;
            float x = rect.x;
            Widgets.Label(new Rect(x, rect.y, ColName, rect.height), "Name"); x += ColName + 6f;
            Widgets.Label(new Rect(x, rect.y, ColType, rect.height), "Type"); x += ColType + 6f;
            Widgets.Label(new Rect(x, rect.y, ColValue, rect.height), "Value"); x += ColValue + 6f;
            Widgets.Label(new Rect(x, rect.y, ColTicks, rect.height), "Ticks"); x += ColTicks + 6f;
            Widgets.Label(new Rect(x, rect.y, ColDel, rect.height), "Del");
            Text.Font = GameFont.Small;
        }

        // ──────────────────── Flag 列表 ────────────────────

        private void DrawFlagList(Rect listRect)
        {
            float contentHeight = filteredFlags.Count * RowHeight;
            Rect viewRect = new Rect(0f, 0f, listRect.width - 16f, contentHeight);

            Widgets.BeginScrollView(listRect, ref scrollPos, viewRect);

            for (int i = 0; i < filteredFlags.Count; i++)
            {
                Rect rowRect = new Rect(0f, i * RowHeight, viewRect.width, RowHeight);
                DrawFlagRow(rowRect, filteredFlags[i], i);
            }

            Widgets.EndScrollView();

            if (filteredFlags.Count == 0)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = Color.gray;
                Widgets.Label(listRect, "No flags. Use 'Add' to create one, or clear the filter.");
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
            }
        }

        private void DrawFlagRow(Rect rect, StellarisEventFlag flag, int index)
        {
            Color bgColor = index % 2 == 0 ? RowEvenColor : RowOddColor;
            if (Mouse.IsOver(rect))
                bgColor = new Color(bgColor.r + 0.08f, bgColor.g + 0.08f, bgColor.b + 0.08f);
            Widgets.DrawBoxSolid(rect, bgColor);

            float x = rect.x + 4f;
            float y = rect.y + (rect.height - 20f) / 2f;

            // Name
            Widgets.Label(new Rect(x, y, ColName, 20f), flag.name.Truncate(ColName - 4f));
            x += ColName + 6f;

            // Type
            GUI.color = GetTypeColor(flag.valueType);
            Widgets.Label(new Rect(x, y, ColType, 20f), flag.valueType.ToString());
            GUI.color = Color.white;
            x += ColType + 6f;

            // Value
            string valStr = FormatFlagValue(flag);
            Widgets.Label(new Rect(x, y, ColValue, 20f), valStr.Truncate(ColValue - 4f));
            x += ColValue + 6f;

            // Ticks
            if (flag.remainingTicks.HasValue)
            {
                float remainRatio = Mathf.Clamp01(flag.remainingTicks.Value / 60000f); // 假设 max 60000 ticks 满条
                Rect tickBarRect = new Rect(x, y + 8f, 80f, 10f);
                Widgets.DrawBoxSolid(tickBarRect, new Color(0.1f, 0.1f, 0.15f));
                Rect fillRect = new Rect(tickBarRect.x + 1f, tickBarRect.y + 1f,
                    (tickBarRect.width - 2f) * remainRatio, tickBarRect.height - 2f);
                Widgets.DrawBoxSolid(fillRect, new Color(0.3f, 0.65f, 0.35f));
                Widgets.Label(new Rect(x + 84f, y, 40f, 20f), flag.remainingTicks.Value.ToString());
            }
            else
            {
                GUI.color = PermTicksColor;
                Widgets.Label(new Rect(x, y, ColTicks, 20f), "∞ (permanent)");
                GUI.color = Color.white;
            }
            x += ColTicks + 6f;

            // Delete 按钮
            Rect delRect = new Rect(x, rect.y + 3f, ColDel - 4f, rect.height - 6f);
            DrawColoredButton(delRect, "X", DelBtnColor);
            if (Widgets.ButtonInvisible(delRect))
            {
                StellarisEventFlagManager.RemoveFlag(flag.name!);
                RefreshList();
                ShowWarning($"Flag '{flag.name}' removed.");
            }
        }

        // ──────────────────── 辅助 ────────────────────

        private static string FormatFlagValue(StellarisEventFlag flag)
        {
            switch (flag.valueType)
            {
                case FlagValueType.Bool:   return flag.boolValue ? "true" : "false";
                case FlagValueType.Float:  return flag.floatValue.ToString("F1");
                case FlagValueType.String: return flag.stringValue ?? "(null)";
                case FlagValueType.Thing:
                {
                    var status = flag.GetThingStatus();
                    Thing t = flag.ResolveThing();
                    string label = t != null ? t.LabelShortCap : $"ID:{flag.thingID}";
                    return $"[{status}] {label}";
                }
                default: return "?";
            }
        }

        private static Color GetTypeColor(FlagValueType t)
        {
            return t switch
            {
                FlagValueType.Bool   => new Color(0.9f, 0.7f, 0.3f),
                FlagValueType.Float  => new Color(0.4f, 0.8f, 0.9f),
                FlagValueType.String => new Color(0.7f, 0.5f, 0.9f),
                FlagValueType.Thing  => new Color(0.4f, 0.9f, 0.5f),
                _                    => Color.white
            };
        }

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

        // ──────────────────── 警告区域 ────────────────────

        private void DrawWarningArea(Rect rect)
        {
            if (string.IsNullOrEmpty(warningMessage)) return;
            if (Find.TickManager.TicksGame > warningExpireTick)
            {
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

        private void ClearWarning()
        {
            warningExpireTick = 0;
        }
    }

    /// <summary>
    /// Flag 控制台 Debug 菜单入口
    /// </summary>
    [StaticConstructorOnStartup]
    public static class StellarisEventFlagConsoleMenu
    {
        [DebugAction("Stellaris Events", "Open Flag Console", false, false, false, false, false, 0, false)]
        public static void OpenConsole()
        {
            Find.WindowStack.Add(new StellarisEventFlagConsole());
        }
    }
}
