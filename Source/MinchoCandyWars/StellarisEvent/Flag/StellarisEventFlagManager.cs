using System.Collections.Generic;
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

        public static StellarisEventFlagManager? Instance { get; private set; }

        // ──────────────────── 内部存储 ────────────────────

        /// <summary>运行时字典，key = flag.name</summary>
        private Dictionary<string, StellarisEventFlag> flags = new Dictionary<string, StellarisEventFlag>();

        /// <summary>延时触发登记表，key = flag name（与定时 flag 同名）</summary>
        private Dictionary<string, DelayedFireEntry> delayedFires = new Dictionary<string, DelayedFireEntry>();

        /// <summary>待移除队列，避免在遍历中直接移除</summary>
        private List<string> pendingRemoval = new List<string>();

        /// <summary>Thing 状态检查间隔（tick 数）</summary>
        private const int ThingCheckInterval = 250;

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
                    // 打印调试日志
                    if (Prefs.DevMode)
                        Verse.Log.Message($"[StellarisEvent] 延时触发到期: '{entry.eventDefName}' (flag={key})");

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

        /// <summary>
        /// 执行延时触发：点火目标事件。
        /// forceFire 时跳过 trigger 判定直接开窗。
        /// </summary>
        private static void FireDelayedEvent(DelayedFireEntry entry)
        {
            var targetDef = StellarisEventManager.GetEvent(entry.eventDefName);
            if (targetDef == null)
            {
                Verse.Log.Warning($"[StellarisEvent] 延时触发失败: 未找到事件 '{entry.eventDefName}'");
                return;
            }

            if (entry.forceFire)
            {
                targetDef.immediate?.Execute();
                targetDef.SelectRandomPicture();
                targetDef.LoadBackgroundTexture();
                Verse.Find.WindowStack.Add(new StellarisEventWindow(targetDef));
            }
            else
            {
                StellarisEventManager.FireEvent(targetDef);
            }
        }

        // ──────────────────── 存档 ────────────────────

        public override void ExposeData()
        {
            base.ExposeData();

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                // 保存 flags
                var flagList = new List<StellarisEventFlag>();
                foreach (var kvp in flags)
                    flagList.Add(kvp.Value);

                Scribe_Collections.Look(ref flagList, "flagList", LookMode.Deep);

                // 保存 delayed fires：转为 list of string "flagName|eventDefName|forceFire"
                var delayedFireList = new List<string>();
                foreach (var kvp in delayedFires)
                    delayedFireList.Add($"{kvp.Key}|{kvp.Value.eventDefName}|{kvp.Value.forceFire}");
                Scribe_Collections.Look(ref delayedFireList, "delayedFireList", LookMode.Value);
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
                            flags[f.name!] = f;
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
            if (Instance == null || name.NullOrEmpty()) return null!;
            Instance.flags.TryGetValue(name, out var flag);
            return flag;
        }

        /// <summary>
        /// 获取指定名字的 flag 所追踪的 Thing（需为 Thing 类型 flag）。
        /// </summary>
        public static Thing GetTrackedThing(string name)
        {
            var flag = GetFlag(name);
            if (flag == null || flag.valueType != FlagValueType.Thing) return null!;
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
        /// 注册一个延时触发：创建定时 flag 并在到期后自动点火目标事件。
        /// 内部创建一个唯一命名的 float flag（值 = delayTicks）作为计时器，
        /// 同时登记 delayed fire 条目。到期时 GameComponentTick 自动点火。
        /// </summary>
        /// <param name="eventDefName">目标事件 defName</param>
        /// <param name="delayTicks">延迟 tick 数</param>
        /// <param name="forceFire">是否跳过 trigger 判定</param>
        public static void ScheduleDelayedFire(string eventDefName, int delayTicks, bool forceFire)
        {
            if (Instance == null || eventDefName.NullOrEmpty() || delayTicks <= 0) return;

            // 生成唯一 flag 名，确保多次 Schedule 不冲突
            string flagName = $"__delayed_fire_{eventDefName}_{Verse.Find.TickManager.TicksGame}_{Verse.Rand.Int}";

            // 创建定时 flag（float 值仅占位，实际使用 remainingTicks 计时）
            SetFlag(flagName, (float)delayTicks, delayTicks);

            // 登记 delayed fire
            Instance.delayedFires[flagName] = new DelayedFireEntry(eventDefName, forceFire);

            if (Verse.Prefs.DevMode)
                Verse.Log.Message($"[StellarisEvent] 注册延时触发: '{eventDefName}' 将在 {delayTicks} tick 后触发 (flag={flagName})");
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
