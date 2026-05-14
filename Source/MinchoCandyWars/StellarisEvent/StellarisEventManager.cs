using System;
using System.Collections.Generic;
using Verse;

namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// Stellaris 事件管理器 —— 静态工具类，提供事件的触发与注册入口
    /// </summary>
    [StaticConstructorOnStartup]
    public static class StellarisEventManager
    {
        /// <summary>
        /// 所有已注册的 StellarisEventDef（由 Def 数据库自动填充）
        /// </summary>
        public static IEnumerable<StellarisEventDef> AllEvents => DefDatabase<StellarisEventDef>.AllDefs;

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
    }
}
