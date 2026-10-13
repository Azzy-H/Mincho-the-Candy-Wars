#nullable disable
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// Stellaris 事件管理器 —— 静态工具类，提供事件的触发、注册入口以及吐司窗口队列管理。
    /// </summary>
    [StaticConstructorOnStartup]
    public static class StellarisEventManager
    {
        /// <summary>
        /// 当前未关闭的事件窗口 defName（供存档/读档恢复用）。
        /// 由 TryFire 设置、窗口 Close 清除。
        /// </summary>
        public static string PendingWindowEventDefName;

        /// <summary>
        /// 所有已注册的 StellarisEventDef（由 Def 数据库自动填充）
        /// </summary>
        public static IEnumerable<StellarisEventDef> AllEvents => DefDatabase<StellarisEventDef>.AllDefs;

        // ──────────────────── 周期触发器 ────────────────────

        /// <summary>一天 = 60000 ticks</summary>
        private const int TicksPerDay = 60000;

        /// <summary>一月 = 15 天（一个 quadrum）</summary>
        private const int DaysPerMonth = 15;

        /// <summary>一年 = 60 天（四个 quadrums）</summary>
        private const int DaysPerYear = 60;

        /// <summary>一月 = 900000 ticks</summary>
        private const int TicksPerMonth = TicksPerDay * DaysPerMonth;

        /// <summary>一年 = 3600000 ticks</summary>
        private const int TicksPerYear = TicksPerDay * DaysPerYear;

        /// <summary>每日触发的信号名</summary>
        public const string Signal_PerDay = "On_Per_Day";

        /// <summary>每 15 天触发的信号名</summary>
        public const string Signal_PerMonth = "On_Per_Month";

        /// <summary>每 60 天触发的信号名</summary>
        public const string Signal_PerYear = "On_Per_Year";

        /// <summary>每天随机时刻触发的信号名（一天内不重复）</summary>
        public const string Signal_PerDay_Anytime = "On_Per_Day_Anytime";

        /// <summary>新游戏开始后首个 tick 触发的信号名（每次新游戏仅触发一次）</summary>
        public const string Signal_GameStart = "On_Game_Start";

        // ──────────────────── 游戏开始信号（一次性） ────────────────────

        /// <summary>是否已登记"游戏开始后 1 tick"派发（新游戏开始时置 true，派发后置 false）</summary>
        private static bool gameStartActionPending;

        /// <summary>
        /// 新游戏开始时调用，登记"游戏开始后 1 tick"派发 onAction 信号。
        /// 由 StellarisEventFlagManager.StartedNewGame 调用。
        /// </summary>
        public static void NotifyNewGameStarted()
        {
            gameStartActionPending = true;
        }

        /// <summary>
        /// 每 tick 由 StellarisEventFlagManager.GameComponentTick 调用。
        /// 新游戏开始后的首个 tick 派发一次 On_Game_Start 信号，随后不再触发：
        /// - 同一局游戏内不重复（派发后立即清除登记）
        /// - 读档不重新触发（登记状态不参与存档，且 StartedNewGame 仅在新游戏时调用）
        /// </summary>
        public static void TickGameStartAction()
        {
            if (!gameStartActionPending)
                return;
            gameStartActionPending = false;
            DispatchAction(Signal_GameStart);
        }

        /// <summary>上次每日触发的 tick</summary>
        private static int lastDayTriggerTick = -1;

        /// <summary>上次每月触发的 tick</summary>
        private static int lastMonthTriggerTick = -1;

        /// <summary>上次每年触发的 tick</summary>
        private static int lastYearTriggerTick = -1;

        /// <summary>上次 Anytime 触发的"天数编号"，确保同一天不重复</summary>
        private static int lastAnytimeTriggerDay = -1;

        /// <summary>On_Per_Day_Anytime 本次目标触发 tick（-1 = 无待触发）</summary>
        private static int anytimeTargetTick = -1;

        // ──────────────────── 吐司窗口队列管理 ────────────────────

        /// <summary>
        /// 吐司窗口总停留时间（现实秒数，不受游戏倍速影响）。
        /// 计时基于 Time.realtimeSinceStartup 的真实流逝时间，见 TickToasts()。
        /// </summary>
        public const float ToastDurationSeconds = 5f;

        /// <summary>上次 TickToasts 调用时的真实时间（用于计算真实流逝，-1 = 未初始化）</summary>
        private static float lastToastTickRealTime = -1f;

        /// <summary>当前正在显示的吐司窗口（null = 无活跃吐司）</summary>
        private static StellarisToastWindow activeToast;

        /// <summary>活跃吐司的事件 defName（供存档恢复）</summary>
        private static string activeToastDefName;

        /// <summary>吐司窗口队列（defName 列表，不含当前活跃吐司）</summary>
        private static Queue<string> toastQueue = new Queue<string>();

        /// <summary>吐司窗口上次拖动位置</summary>
        private static Vector2 savedToastPosition = Vector2.zero;

        /// <summary>是否已有保存的吐司位置</summary>
        private static bool hasToastPosition = false;

        // ──────────────────── 吐司公共属性 ────────────────────

        /// <summary>是否有已保存的吐司窗口位置</summary>
        public static bool HasSavedToastPosition => hasToastPosition;

        /// <summary>吐司窗口上次保存的位置</summary>
        public static Vector2 SavedToastPosition => savedToastPosition;

        /// <summary>当前活跃吐司的 defName（用于存档持久化，null = 无活跃吐司）</summary>
        public static string ActiveToastDefName => activeToastDefName;

        /// <summary>吐司队列中的 defName 列表（用于存档持久化）</summary>
        public static IEnumerable<string> ToastQueueDefNames => toastQueue;

        // ──────────────────── 事件触发 ────────────────────

        /// <summary>
        /// 按 defName 触发事件
        /// </summary>
        /// <returns>是否成功触发（trigger 通过则成功）</returns>
        public static bool FireEvent(string defName)
        {
            StellarisEventDef def = DefDatabase<StellarisEventDef>.GetNamedSilentFail(defName);
            if (def == null)
            {
                Log.Error($"[StellarisEvent] 未找到事件定义: {defName}");
                return false;
            }
            return FireEvent(def);
        }

        /// <summary>
        /// 按 Def 引用触发事件
        /// </summary>
        /// <returns>是否成功触发（trigger 通过则成功）</returns>
        public static bool FireEvent(StellarisEventDef def)
        {
            if (def == null)
            {
                Log.Error("[StellarisEvent] FireEvent 传入 null 事件定义");
                return false;
            }

            try
            {
                return def.TryFire();
            }
            catch (Exception ex)
            {
                Log.Error($"[StellarisEvent] 触发事件 '{def.defName}' 时发生异常: {ex}");
                return false;
            }
        }

        // ──────────────────── 查询 ────────────────────

        /// <summary>
        /// 获取指定 defName 的事件定义
        /// </summary>
        public static StellarisEventDef GetEvent(string defName)
        {
            return DefDatabase<StellarisEventDef>.GetNamedSilentFail(defName);
        }

        /// <summary>
        /// 获取所有当前可触发的事件
        /// </summary>
        public static IEnumerable<StellarisEventDef> GetAvailableEvents()
        {
            foreach (var def in AllEvents)
            {
                if (def.CanTrigger())
                    yield return def;
            }
        }

        // ──────────────────── onAction 信号调度 ────────────────────

        /// <summary>OnActionDef 触发次数统计（defName → 次数）</summary>
        private static Dictionary<string, int> onActionTriggerCounts = new Dictionary<string, int>();

        /// <summary>获取指定 OnActionDef 的触发次数</summary>
        public static int GetOnActionTriggerCount(string defName)
        {
            onActionTriggerCounts.TryGetValue(defName, out int count);
            return count;
        }

        /// <summary>重置 onAction 触发计数（新游戏时调用）</summary>
        public static void ResetOnActionCounts()
        {
            onActionTriggerCounts.Clear();
        }

        /// <summary>
        /// onAction 信号入口。外部传入 action 字符串，匹配所有 OnActionDef，
        /// 按 triggerList + randomTriggerList 收集事件后依次触发。
        ///
        /// 触发逻辑：
        /// - 每个事件触发前检查自身 trigger，不通过则跳过
        /// - 有 option 的事件：唤起窗口后立即进入下一个，不等待玩家选择
        /// - 吐司事件：走正常吐司队列
        /// </summary>
        /// <param name="action">信号字符串，与 OnActionDef.action 匹配</param>
        public static void DispatchAction(string action)
        {
            if (action.NullOrEmpty())
            {
                Log.Warning("[StellarisEvent] DispatchAction 收到空信号，忽略");
                return;
            }

            var onActionDefs = DefDatabase<OnActionDef>.AllDefsListForReading;
            if (onActionDefs == null || onActionDefs.Count == 0)
            {
                return;
            }

            foreach (var onActionDef in onActionDefs)
            {
                if (onActionDef.action != action)
                    continue;

                // ── 收集待触发事件列表 ──
                var eventsToFire = CollectEventsToFire(onActionDef);

                if (eventsToFire.Count == 0)
                {
                    continue;
                }

                // ── 依次触发 ──
                foreach (var eventName in eventsToFire)
                {
                    FireEventSequential(eventName);
                }

                // ── 增加触发计数 ──
                if (!onActionTriggerCounts.ContainsKey(onActionDef.defName))
                    onActionTriggerCounts[onActionDef.defName] = 0;
                onActionTriggerCounts[onActionDef.defName]++;
            }
        }

        /// <summary>
        /// 从 OnActionDef 中收集所有待触发事件的 defName 列表。
        /// triggerList 全量加入，randomTriggerList 按权重随机抽一个。
        /// </summary>
        private static List<string> CollectEventsToFire(OnActionDef onActionDef)
        {
            var list = new List<string>();

            // triggerList：全部加入
            if (onActionDef.triggerList != null)
            {
                foreach (var name in onActionDef.triggerList)
                {
                    if (!name.NullOrEmpty())
                        list.Add(name);
                }
            }

            // randomTriggerList：按权重随机抽一个
            if (onActionDef.randomTriggerList != null && onActionDef.randomTriggerList.Count > 0)
            {
                var entry = onActionDef.randomTriggerList.RandomElementByWeight(e => e.weight);
                if (entry != null && !entry.eventDefName.NullOrEmpty())
                {
                    list.Add(entry.eventDefName);
                }
            }

            return list;
        }

        /// <summary>
        /// 按顺序触发单个事件（用于 onAction 调度链）。
        /// - 检查事件自身 trigger，不通过则跳过
        /// - 执行 immediate 效果
        /// - 唤起窗口（不等待玩家选择，立即返回）
        /// - 吐司事件走正常队列
        /// </summary>
        private static void FireEventSequential(string defName)
        {
            var def = GetEvent(defName);
            if (def == null)
            {
                Log.Warning($"[StellarisEvent] onAction 无法触发 '{defName}'：事件 Def 不存在");
                return;
            }

            // 检查事件自身 trigger
            if (!def.CanTrigger())
            {
                return;
            }

            try
            {
                // 执行 immediate（与 TryFire 一致，设置 ExecutingInstance）
                StellarisEventDef.ExecutingInstance = def;
                try
                {
                    def.immediate?.Execute();
                }
                finally
                {
                    StellarisEventDef.ExecutingInstance = null;
                }

                // 选立绘、加载背景、开窗
                def.SelectRandomPicture();
                def.LoadBackgroundTexture();

                if (def.isToastEvent)
                {
                    StellarisEventManager.EnqueueToastEvent(def);
                }
                else
                {
                    // 标记待恢复窗口
                    PendingWindowEventDefName = def.defName;

                    if (def.isSuperEvent)
                        Find.WindowStack.Add(new StellarisSuperEventWindow(def));
                    else
                        Find.WindowStack.Add(new StellarisEventWindow(def));
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[StellarisEvent] onAction 触发 '{defName}' 时发生异常: {ex}");
            }
        }

        public static int LastAnytimeTriggerDay
        {
            get => lastAnytimeTriggerDay;
            set => lastAnytimeTriggerDay = value;
        }

        public static int AnytimeTargetTick
        {
            get => anytimeTargetTick;
            set => anytimeTargetTick = value;
        }

        // ──────────────────── 吐司窗口入队与队列 Tick ────────────────────

        /// <summary>
        /// 将一个吐司事件加入显示队列。
        /// 若当前无活跃吐司则立即显示；否则加入等待队列。
        /// 调用前调用方需已完成 def.SelectRandomPicture() 和 def.LoadBackgroundTexture()。
        /// </summary>
        public static void EnqueueToastEvent(StellarisEventDef def, float? remainingRealSeconds = null)
        {
            if (def == null || def.defName.NullOrEmpty())
            {
                Log.Error("[StellarisEvent] EnqueueToastEvent 传入 null 或无效 def");
                return;
            }

            if (activeToast == null)
            {
                ShowToast(def, remainingRealSeconds ?? ToastDurationSeconds);
            }
            else
            {
                toastQueue.Enqueue(def.defName);
            }
        }

        /// <summary>
        /// 每 tick 由 StellarisEventFlagManager.GameComponentTick 调用。
        /// 管理活跃吐司的计时和位置记录。
        ///
        /// 计时基于真实时间（Time.realtimeSinceStartup）而非游戏 tick：
        /// 每 tick 调用时用"本次与上次调用间的真实流逝时间"递减剩余秒数，
        /// 因此无论游戏倍速多少，吐司都在现实 5 秒后消失。
        /// 单次流逝时间上限 0.1s：游戏暂停期间本方法不执行，恢复后的首个 tick
        /// 不会把整个暂停时长计入倒计时（暂停即冻结吐司）。
        /// </summary>
        public static void TickToasts()
        {
            if (activeToast == null)
            {
                lastToastTickRealTime = -1f;
                return;
            }

            // 检查吐司是否已被外部关闭（正常情况下不会发生）
            if (!Find.WindowStack.Windows.Contains(activeToast))
            {
                activeToast = null;
                activeToastDefName = null;
                // 注意：不清 PendingWindowEventDefName —— 该字段属于事件窗口（见 ShowToast 注释）
                ShowNextToast();
                return;
            }

            // 记录吐司窗口当前位置（每次 tick 都保存）
            hasToastPosition = true;
            savedToastPosition = activeToast.WindowRect.position;

            // 计算自上次调用以来流逝的真实时间（吸收暂停间隙）
            float now = Time.realtimeSinceStartup;
            if (lastToastTickRealTime < 0f)
                lastToastTickRealTime = now;
            float realDelta = Mathf.Min(now - lastToastTickRealTime, 0.1f);
            lastToastTickRealTime = now;

            // 鼠标悬停时暂停计时
            if (activeToast.IsTimerPaused)
                return;

            activeToast.RemainingRealSeconds -= realDelta;
            if (activeToast.RemainingRealSeconds <= 0f)
            {
                activeToast.Close();
            }
        }

        /// <summary>
        /// 每 tick 由 StellarisEventFlagManager.GameComponentTick 调用。
        /// 检查周期触发器（每日/每月/每年）是否到期，到期则 DispatchAction。
        /// </summary>
        public static void TickPeriodicTriggers()
        {
            int currentTick = Find.TickManager.TicksGame;

            // 首次初始化：以当前 tick 为基准，下个周期触发
            if (lastDayTriggerTick < 0)
                lastDayTriggerTick = currentTick;
            if (lastMonthTriggerTick < 0)
                lastMonthTriggerTick = currentTick;
            if (lastYearTriggerTick < 0)
                lastYearTriggerTick = currentTick;

            if (currentTick - lastDayTriggerTick >= TicksPerDay)
            {
                lastDayTriggerTick = currentTick;
                DispatchAction(Signal_PerDay);
            }

            if (currentTick - lastMonthTriggerTick >= TicksPerMonth)
            {
                lastMonthTriggerTick = currentTick;
                DispatchAction(Signal_PerMonth);
            }

            if (currentTick - lastYearTriggerTick >= TicksPerYear)
            {
                lastYearTriggerTick = currentTick;
                DispatchAction(Signal_PerYear);
            }

            // ── On_Per_Day_Anytime：每天随机时刻触发一次 ──
            int currentDay = currentTick / TicksPerDay;

            if (currentDay != lastAnytimeTriggerDay)
            {
                lastAnytimeTriggerDay = currentDay;

                int tickInDay = currentTick % TicksPerDay;
                int remainingTicks = TicksPerDay - tickInDay;
                anytimeTargetTick = currentTick + Rand.Range(1, remainingTicks + 1);
            }

            if (anytimeTargetTick > 0 && currentTick >= anytimeTargetTick)
            {
                anytimeTargetTick = -1;
                DispatchAction(Signal_PerDay_Anytime);
            }
        }

        // ──────────────────── 周期触发器存档相关 ────────────────────

        /// <summary>上次每日触发 tick（供存档持久化）</summary>
        public static int LastDayTriggerTick
        {
            get => lastDayTriggerTick;
            set => lastDayTriggerTick = value;
        }

        /// <summary>上次每月触发 tick（供存档持久化）</summary>
        public static int LastMonthTriggerTick
        {
            get => lastMonthTriggerTick;
            set => lastMonthTriggerTick = value;
        }

        /// <summary>上次每年触发 tick（供存档持久化）</summary>
        public static int LastYearTriggerTick
        {
            get => lastYearTriggerTick;
            set => lastYearTriggerTick = value;
        }

        /// <summary>
        /// 重置周期触发器（新游戏时调用）。
        /// </summary>
        public static void ResetPeriodicTriggers()
        {
            lastDayTriggerTick = -1;
            lastMonthTriggerTick = -1;
            lastYearTriggerTick = -1;
            lastAnytimeTriggerDay = -1;
            anytimeTargetTick = -1;
        }

        /// <summary>
        /// 显示一个吐司窗口（立即显示，不经过队列）。
        /// </summary>
        private static void ShowToast(StellarisEventDef def, float remainingRealSeconds)
        {
            var window = new StellarisToastWindow(def, remainingRealSeconds);
            activeToast = window;
            activeToastDefName = def.defName;
            // 注意：不写 PendingWindowEventDefName —— 该字段专用于"事件窗口"的存读档恢复，
            // 吐司有自己的独立恢复机制（TryRestorePendingToast）。之前吐司也写/清此字段，
            // 会覆盖或清掉悬而未决的事件窗口挂起状态，导致读档后事件窗口丢失。
            lastToastTickRealTime = -1f;

            Find.WindowStack.Add(window);
        }

        /// <summary>
        /// 由 StellarisToastWindow.Close() 调用。
        /// 清除当前活跃吐司并弹出队列中的下一个。
        /// </summary>
        public static void OnToastClosed(StellarisToastWindow window)
        {
            if (activeToast == window)
            {
                hasToastPosition = true;
                savedToastPosition = window.WindowRect.position;

                activeToast = null;
                activeToastDefName = null;
                // 注意：不清 PendingWindowEventDefName —— 该字段属于事件窗口（见 ShowToast 注释）

                ShowNextToast();
            }
        }

        /// <summary>
        /// 从队列中弹出下一个吐司并显示。
        /// </summary>
        private static void ShowNextToast()
        {
            while (toastQueue.Count > 0)
            {
                string defName = toastQueue.Dequeue();
                var def = GetEvent(defName);
                if (def == null)
                {
                    Log.Warning($"[StellarisEvent] 队列中的吐司事件 '{defName}' 已不存在，跳过");
                    continue;
                }

                def.SelectRandomPicture();
                def.LoadBackgroundTexture();

                ShowToast(def, ToastDurationSeconds);
                return; // 每次只显示一个
            }
        }

        // ──────────────────── 吐司队列存档恢复 ────────────────────

        /// <summary>存档加载时暂存的活跃吐司 defName（延迟到首个 tick 恢复）</summary>
        private static string pendingActiveToastName;
        /// <summary>存档加载时暂存的活跃吐司剩余现实秒数</summary>
        private static float pendingToastSeconds;
        /// <summary>存档加载时暂存的队列</summary>
        private static List<string> pendingToastQueue;
        /// <summary>存档加载时暂存的位置</summary>
        private static Vector2 pendingToastPos;
        /// <summary>存档加载时暂存的是否有位置</summary>
        private static bool pendingHasPos;
        /// <summary>是否有待恢复的吐司数据</summary>
        private static bool hasPendingToastData = false;

        /// <summary>
        /// 暂存从存档加载的吐司队列数据（由 ExposeData 调用）。
        /// 实际恢复在首个 GameComponentTick 中调用 TryRestorePendingToast。
        /// </summary>
        public static void StorePendingToastRestoreData(
            string activeName, float remainingRealSeconds,
            List<string> queueDefNames,
            Vector2 savedPos, bool hasPos)
        {
            pendingActiveToastName = activeName;
            pendingToastSeconds = Mathf.Max(0.1f, remainingRealSeconds);
            pendingToastQueue = queueDefNames ?? new List<string>();
            pendingToastPos = savedPos;
            pendingHasPos = hasPos;
            hasPendingToastData = true;
        }

        /// <summary>
        /// 从存档恢复吐司队列（由 GameComponentTick 首个 tick 调用）。
        /// </summary>
        public static void TryRestorePendingToast()
        {
            if (!hasPendingToastData)
                return;

            hasPendingToastData = false;

            // 清空当前状态
            activeToast = null;
            activeToastDefName = null;
            toastQueue.Clear();

            // 恢复队列
            if (pendingToastQueue != null)
            {
                foreach (var name in pendingToastQueue)
                    toastQueue.Enqueue(name);
            }

            // 恢复位置
            if (pendingHasPos)
            {
                hasToastPosition = true;
                savedToastPosition = pendingToastPos;
            }

            // 恢复活跃吐司
            if (!pendingActiveToastName.NullOrEmpty())
            {
                var def = GetEvent(pendingActiveToastName);
                if (def != null)
                {
                    def.SelectRandomPicture();
                    def.LoadBackgroundTexture();
                    ShowToast(def, pendingToastSeconds);
                }
                else
                {
                    Log.Warning($"[StellarisEvent] 无法恢复吐司窗口: Def '{pendingActiveToastName}' 不存在（可能 mod 已移除）");
                }
            }

            // 清理暂存数据
            pendingActiveToastName = null;
            pendingToastQueue = null;
        }

        /// <summary>
        /// 新游戏开始时清空吐司队列。
        /// 由 StellarisEventFlagManager.StartedNewGame 调用。
        /// </summary>
        public static void ResetToastState()
        {
            activeToast = null;
            activeToastDefName = null;
            toastQueue.Clear();
            hasPendingToastData = false;
            pendingActiveToastName = null;
            pendingToastQueue = null;
            pendingToastSeconds = 0f;
            lastToastTickRealTime = -1f;
        }
    }
}
