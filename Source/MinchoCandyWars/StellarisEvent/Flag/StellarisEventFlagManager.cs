#nullable disable
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// StellarisEvent Flag 管理器 —— GameComponent，随存档生命周期。
    /// 每 tick 自动递减所有有时限 flag 的 remainingTicks，到期 flag 自动移除。
    /// 同时提供静态方法供其他系统创建、查询、覆写和删除 flag。
    /// </summary>
    public class StellarisEventFlagManager : GameComponent
    {
        // ──────────────────── 单例 ────────────────────

        public static StellarisEventFlagManager Instance { get; private set; }

        // ──────────────────── 内部存储 ────────────────────

        /// <summary>运行时字典，key = flag.name</summary>
        private Dictionary<string, StellarisEventFlag> flags = new Dictionary<string, StellarisEventFlag>();

        /// <summary>延时触发登记表，key = flag name</summary>
        private Dictionary<string, DelayedFireEntry> delayedFires = new Dictionary<string, DelayedFireEntry>();

        /// <summary>待移除队列，避免在遍历中直接移除</summary>
        private List<string> pendingRemoval = new List<string>();

        /// <summary>Thing 状态检查间隔（tick 数）</summary>
        private const int ThingCheckInterval = 250;

        /// <summary>是否已尝试从存档恢复过事件窗口</summary>
        private bool windowRestoreAttempted;

        /// <summary>下次 Thing 状态检查的 tick</summary>
        private int nextThingCheckTick;

        // ──────────────────── 延时触发内部类型 ────────────────────

        private struct DelayedFireEntry
        {
            public string eventDefName;
            public bool forceFire;

            public DelayedFireEntry(string eventDefName, bool forceFire)
            {
                this.eventDefName = eventDefName;
                this.forceFire = forceFire;
            }
        }

        // ──────────────────── 构造 ────────────────────

        public StellarisEventFlagManager(Game game)
        {
            Instance = this;
        }

        // ──────────────────── Tick ────────────────────

        public override void GameComponentTick()
        {
            // ── 新游戏开始后首个 tick：派发一次性 onAction 信号（On_Game_Start） ──
            StellarisEventManager.TickGameStartAction();

            // ── 存档加载后首个 tick：恢复未关闭的事件窗口 ──
            if (!windowRestoreAttempted)
            {
                windowRestoreAttempted = true;
                TryRestorePendingWindow();
                StellarisEventManager.TryRestorePendingToast();
            }

            // ── 吐司窗口 Tick ──
            StellarisEventManager.TickToasts();

            // ── 周期触发器 Tick（每日/每月/每年/Anytime） ──
            StellarisEventManager.TickPeriodicTriggers();

            pendingRemoval.Clear();

            foreach (var kvp in flags)
            {
                if (kvp.Value.remainingTicks.HasValue)
                {
                    kvp.Value.remainingTicks--;
                    if (kvp.Value.remainingTicks.Value <= 0)
                        pendingRemoval.Add(kvp.Key);
                }
            }

            // 延时触发：检查已过期 flag 是否关联了 delayed fire
            foreach (var key in pendingRemoval)
            {
                if (delayedFires.TryGetValue(key, out var entry))
                {
                    FireDelayedEvent(entry);
                    delayedFires.Remove(key);
                }
            }

            // 定期检查 Thing 类型 flag 的状态，清理已失效的
            if (Find.TickManager.TicksGame >= nextThingCheckTick)
            {
                nextThingCheckTick = Find.TickManager.TicksGame + ThingCheckInterval;
                foreach (var kvp in flags)
                {
                    if (kvp.Value.valueType == FlagValueType.Thing)
                    {
                        if (kvp.Value.GetThingStatus() == ThingFlagStatus.DiedOrDisappeared)
                            pendingRemoval.Add(kvp.Key);
                    }
                }
            }

            foreach (var key in pendingRemoval)
                flags.Remove(key);
        }

        // ──────────────────── 延时触发 ────────────────────

        private static void FireDelayedEvent(DelayedFireEntry entry)
        {
            var targetDef = StellarisEventManager.GetEvent(entry.eventDefName);
            if (targetDef == null) return;

            if (entry.forceFire)
            {
                targetDef.immediate?.Execute();
                targetDef.SelectRandomPicture();
                targetDef.LoadBackgroundTexture();
                if (targetDef.isToastEvent)
                    StellarisEventManager.EnqueueToastEvent(targetDef);
                else
                {
                    StellarisEventManager.PendingWindowEventDefName = targetDef.defName;
                    if (targetDef.isSuperEvent)
                        Verse.Find.WindowStack.Add(new StellarisSuperEventWindow(targetDef));
                    else
                        Verse.Find.WindowStack.Add(new StellarisEventWindow(targetDef));
                }
            }
            else
            {
                StellarisEventManager.FireEvent(targetDef);
            }
        }

        public static void ScheduleDelayedFire(string eventDefName, int delayTicks, bool forceFire)
        {
            if (Instance == null || eventDefName.NullOrEmpty() || delayTicks <= 0) return;

            string flagName = $"__delayed_fire_{eventDefName}_{Verse.Find.TickManager.TicksGame}_{Verse.Rand.Int}";
            SetFlag(flagName, (float)delayTicks, delayTicks);
            Instance.delayedFires[flagName] = new DelayedFireEntry(eventDefName, forceFire);
        }

        // ──────────────────── 存档 ────────────────────

        public override void ExposeData()
        {
            base.ExposeData();

            // ── 待恢复窗口状态（在 flag 之前处理，独立于模式分支） ──
            string pendingEvent = StellarisEventManager.PendingWindowEventDefName;
            Scribe_Values.Look(ref pendingEvent, "pendingStellarisEventWindow");
            StellarisEventManager.PendingWindowEventDefName = pendingEvent;

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                // 保存 flags
                var flagList = new List<StellarisEventFlag>();
                foreach (var kvp in flags)
                    flagList.Add(kvp.Value);

                Scribe_Collections.Look(ref flagList, "flagList", LookMode.Deep);

                // 保存 delayed fires
                var delayedFireList = new List<string>();
                foreach (var kvp in delayedFires)
                    delayedFireList.Add($"{kvp.Key}|{kvp.Value.eventDefName}|{kvp.Value.forceFire}");
                Scribe_Collections.Look(ref delayedFireList, "delayedFireList", LookMode.Value);

                // ── 保存吐司队列状态 ──
                string activeToastName = StellarisEventManager.ActiveToastDefName;
                Scribe_Values.Look(ref activeToastName, "activeToastDefName");

                var toastQueueList = new List<string>(StellarisEventManager.ToastQueueDefNames);
                Scribe_Collections.Look(ref toastQueueList, "toastQueueList", LookMode.Value);

                bool hasToastPos = StellarisEventManager.HasSavedToastPosition;
                Scribe_Values.Look(ref hasToastPos, "hasToastPos");
                if (hasToastPos)
                {
                    Vector2 pos = StellarisEventManager.SavedToastPosition;
                    Scribe_Values.Look(ref pos.x, "toastPosX");
                    Scribe_Values.Look(ref pos.y, "toastPosY");
                }

                // ── 保存周期触发器状态 ──
                int dayTick = StellarisEventManager.LastDayTriggerTick;
                Scribe_Values.Look(ref dayTick, "lastDayTriggerTick");

                int monthTick = StellarisEventManager.LastMonthTriggerTick;
                Scribe_Values.Look(ref monthTick, "lastMonthTriggerTick");

                int yearTick = StellarisEventManager.LastYearTriggerTick;
                Scribe_Values.Look(ref yearTick, "lastYearTriggerTick");

                int anytimeDay = StellarisEventManager.LastAnytimeTriggerDay;
                Scribe_Values.Look(ref anytimeDay, "lastAnytimeTriggerDay");

                int anytimeTick = StellarisEventManager.AnytimeTargetTick;
                Scribe_Values.Look(ref anytimeTick, "anytimeTargetTick");
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                // 加载 flags
                var flagList = new List<StellarisEventFlag>();
                Scribe_Collections.Look(ref flagList, "flagList", LookMode.Deep);
                flags.Clear();
                if (flagList != null)
                {
                    foreach (var f in flagList)
                    {
                        if (f != null && !f.name.NullOrEmpty())
                            flags[f.name] = f;
                    }
                }

                // 加载 delayed fires
                var delayedFireList = new List<string>();
                Scribe_Collections.Look(ref delayedFireList, "delayedFireList", LookMode.Value);
                delayedFires.Clear();
                if (delayedFireList != null)
                {
                    foreach (var entry in delayedFireList)
                    {
                        if (entry == null) continue;
                        var parts = entry.Split('|');
                        if (parts.Length >= 3 &&
                            bool.TryParse(parts[2], out bool force) &&
                            !parts[0].NullOrEmpty() && !parts[1].NullOrEmpty())
                        {
                            delayedFires[parts[0]] = new DelayedFireEntry(parts[1], force);
                        }
                    }
                }

                // ── 加载吐司队列状态（延迟到首个 GameComponentTick 恢复） ──
                string activeToastName = null;
                Scribe_Values.Look(ref activeToastName, "activeToastDefName");

                var toastQueueList = new List<string>();
                Scribe_Collections.Look(ref toastQueueList, "toastQueueList", LookMode.Value);

                bool hasToastPos = false;
                Scribe_Values.Look(ref hasToastPos, "hasToastPos");
                Vector2 savedPos = Vector2.zero;
                if (hasToastPos)
                {
                    Scribe_Values.Look(ref savedPos.x, "toastPosX");
                    Scribe_Values.Look(ref savedPos.y, "toastPosY");
                }

                // 暂存数据，由 GameComponentTick 首个 tick 调用 TryRestorePendingToast
                if (!activeToastName.NullOrEmpty() || (toastQueueList != null && toastQueueList.Count > 0))
                {
                    StellarisEventManager.StorePendingToastRestoreData(
                        activeToastName, StellarisEventManager.ToastDurationSeconds,
                        toastQueueList, savedPos, hasToastPos);
                }

                // ── 加载周期触发器状态 ──
                int dayTick = -1;
                Scribe_Values.Look(ref dayTick, "lastDayTriggerTick");
                StellarisEventManager.LastDayTriggerTick = dayTick;

                int monthTick = -1;
                Scribe_Values.Look(ref monthTick, "lastMonthTriggerTick");
                StellarisEventManager.LastMonthTriggerTick = monthTick;

                int yearTick = -1;
                Scribe_Values.Look(ref yearTick, "lastYearTriggerTick");
                StellarisEventManager.LastYearTriggerTick = yearTick;

                int anytimeDay = -1;
                Scribe_Values.Look(ref anytimeDay, "lastAnytimeTriggerDay");
                StellarisEventManager.LastAnytimeTriggerDay = anytimeDay;

                int anytimeTick = -1;
                Scribe_Values.Look(ref anytimeTick, "anytimeTargetTick");
                StellarisEventManager.AnytimeTargetTick = anytimeTick;
            }
            else if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                // 读档后重置"窗口恢复尝试"标志：
                // GameComponent 实例在存档→读档时被复用（不重建），若此处不重置，
                // windowRestoreAttempted 保持 true，同会话内直接读档时 TryRestorePendingWindow
                // 不会再次执行，导致悬而未决的事件窗口无法恢复。
                windowRestoreAttempted = false;
            }
        }

        public override void StartedNewGame()
        {
            base.StartedNewGame();
            windowRestoreAttempted = false;
            StellarisEventManager.ResetToastState();
            StellarisEventManager.ResetPeriodicTriggers();
            StellarisEventManager.ResetOnActionCounts();
            StellarisEventManager.NotifyNewGameStarted();
        }

        // ── 窗口恢复 ──

        /// <summary>
        /// 从存档中恢复未关闭的事件窗口（跳过 trigger 和 immediate）。
        /// 仅在 GameComponentTick 首个 tick 调用。
        /// </summary>
        private void TryRestorePendingWindow()
        {
            string defName = StellarisEventManager.PendingWindowEventDefName;
            if (defName.NullOrEmpty()) return;

            var def = DefDatabase<StellarisEventDef>.GetNamedSilentFail(defName);
            if (def == null)
            {
                Log.Warning($"[StellarisEvent] 无法恢复事件窗口: Def '{defName}' 不存在（可能 mod 已移除）");
                StellarisEventManager.PendingWindowEventDefName = null;
                return;
            }

            try
            {
                def.RestoreWindow();
            }
            catch (Exception ex)
            {
                Log.Error($"[StellarisEvent] 恢复事件窗口 '{defName}' 时发生异常: {ex}");
                StellarisEventManager.PendingWindowEventDefName = null;
            }
        }

        // ──────────────────── 公开静态 API ────────────────────

        /// <summary>
        /// 设置 / 覆写一个 flag。同名 flag 会被覆盖。
        /// </summary>
        /// <param name="name">flag 名</param>
        /// <param name="value">float 值</param>
        /// <param name="ticks">剩余 tick，null = 永久</param>
        public static void SetFlag(string name, float value, int? ticks = null)
        {
            if (Instance == null || name.NullOrEmpty()) return;
            Instance.flags[name] = new StellarisEventFlag(name, value, ticks);
        }

        /// <summary>
        /// 设置 / 覆写一个 flag。
        /// </summary>
        public static void SetFlag(string name, string value, int? ticks = null)
        {
            if (Instance == null || name.NullOrEmpty()) return;
            Instance.flags[name] = new StellarisEventFlag(name, value, ticks);
        }

        /// <summary>
        /// 设置 / 覆写一个 flag。
        /// </summary>
        public static void SetFlag(string name, bool value, int? ticks = null)
        {
            if (Instance == null || name.NullOrEmpty()) return;
            Instance.flags[name] = new StellarisEventFlag(name, value, ticks);
        }

        /// <summary>
        /// 设置 / 覆写一个 Thing 类型 flag，用于追踪特定 Thing。
        /// </summary>
        public static void SetFlag(string name, Thing thing, int? ticks = null)
        {
            if (Instance == null || name.NullOrEmpty() || thing == null) return;
            Instance.flags[name] = new StellarisEventFlag(name, thing, ticks);
        }

        /// <summary>
        /// 获取指定名字的 flag，不存在则返回 null。
        /// </summary>
        public static StellarisEventFlag GetFlag(string name)
        {
            if (Instance == null || name.NullOrEmpty()) return null;
            Instance.flags.TryGetValue(name, out var flag);
            return flag;
        }

        /// <summary>
        /// 获取指定名字的 flag 所追踪的 Thing（需为 Thing 类型 flag）。
        /// </summary>
        public static Thing GetTrackedThing(string name)
        {
            var flag = GetFlag(name);
            if (flag == null || flag.valueType != FlagValueType.Thing) return null;
            return flag.ResolveThing();
        }

        /// <summary>
        /// 获取指定名字的 Thing flag 的追踪状态。
        /// 若 flag 不存在或非 Thing 类型，返回 null。
        /// </summary>
        public static ThingFlagStatus? GetThingFlagStatus(string name)
        {
            var flag = GetFlag(name);
            if (flag == null || flag.valueType != FlagValueType.Thing) return null;
            return flag.GetThingStatus();
        }

        /// <summary>
        /// 判断指定名字的 flag 是否存在。
        /// </summary>
        public static bool HasFlag(string name)
        {
            return GetFlag(name) != null;
        }

        /// <summary>
        /// 删除指定名字的 flag。删除不存在的 flag 也是安全的。
        /// </summary>
        public static void RemoveFlag(string name)
        {
            if (Instance == null || name.NullOrEmpty()) return;
            Instance.flags.Remove(name);
        }

        /// <summary>
        /// 清除所有 local flag。应在事件完整生命周期结束后调用。
        /// local flag 参与存档——事件窗口打开期间存盘可正常恢复。
        /// </summary>
        public static void ClearLocalFlags()
        {
            if (Instance == null) return;
            Instance.pendingRemoval.Clear();
            foreach (var kvp in Instance.flags)
            {
                if (kvp.Value.isLocal)
                    Instance.pendingRemoval.Add(kvp.Key);
            }
            foreach (var key in Instance.pendingRemoval)
                Instance.flags.Remove(key);
        }

        /// <summary>
        /// 清除所有 flag（含 local 与全局）。调试用。
        /// </summary>
        public static void ClearAll()
        {
            if (Instance == null) return;
            Instance.flags.Clear();
        }

        /// <summary>
        /// 延长指定 flag 的剩余 tick。若 flag 不存在或无剩余 tick 则无操作。
        /// </summary>
        public static void ExtendTicks(string name, int additionalTicks)
        {
            var flag = GetFlag(name);
            if (flag != null && flag.remainingTicks.HasValue)
                flag.remainingTicks += additionalTicks;
        }

        /// <summary>
        /// 将指定 flag 设为永久（清除其剩余 tick）。
        /// </summary>
        public static void MakePermanent(string name)
        {
            var flag = GetFlag(name);
            if (flag != null)
                flag.remainingTicks = null;
        }

        /// <summary>
        /// 判定 flag 是否存在且值与类型均匹配。
        /// 类型不同、值不同或 flag 不存在均返回 false。
        /// </summary>
        public static bool CheckFlag(string name, FlagValueType expectedType, object expectedValue)
        {
            var flag = GetFlag(name);
            if (flag == null) return false;
            if (flag.valueType != expectedType) return false;

            switch (expectedType)
            {
                case FlagValueType.Float:
                    if (expectedValue is float f)
                        return flag.floatValue == f;
                    return false;
                case FlagValueType.String:
                    if (expectedValue is string s)
                        return flag.stringValue == s;
                    return false;
                case FlagValueType.Bool:
                    if (expectedValue is bool b)
                        return flag.boolValue == b;
                    return false;
                case FlagValueType.Thing:
                    if (expectedValue is Thing t)
                        return flag.thingID == t.thingIDNumber;
                    if (expectedValue is int id)
                        return flag.thingID == id;
                    return false;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 获取当前所有 flag 的快照（调试用）。
        /// </summary>
        public static IEnumerable<StellarisEventFlag> AllFlags()
        {
            if (Instance == null) yield break;
            foreach (var kvp in Instance.flags)
                yield return kvp.Value;
        }
    }
}
