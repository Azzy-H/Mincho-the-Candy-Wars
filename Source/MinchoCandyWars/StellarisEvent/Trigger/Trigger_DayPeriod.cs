#nullable disable
using UnityEngine;
using Verse;

namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// 检查当前时间是否落在一天中的某个时段内。
    /// 使用 RimWorld 的昼夜系统（GenLocalDate.DayPercent），基于当前地图的经纬度。
    ///
    /// XML 示例:
    ///   &lt;!-- 白天 6:00–18:00 --&gt;
    ///   &lt;li Class="MinchoCandyWars.StellarisEvent.Trigger_DayPeriod"&gt;
    ///     &lt;startHour&gt;6&lt;/startHour&gt;
    ///     &lt;endHour&gt;18&lt;/endHour&gt;
    ///   &lt;/li&gt;
    ///
    ///   &lt;!-- 夜晚 22:00–06:00（跨午夜） --&gt;
    ///   &lt;li Class="MinchoCandyWars.StellarisEvent.Trigger_DayPeriod"&gt;
    ///     &lt;startHour&gt;22&lt;/startHour&gt;
    ///     &lt;endHour&gt;6&lt;/endHour&gt;
    ///   &lt;/li&gt;
    /// </summary>
    public class Trigger_DayPeriod : StellarisEventTrigger
    {
        /// <summary>时段起始小时（0–24），如 6 = 06:00</summary>
        public float startHour = 0f;

        /// <summary>时段结束小时（0–24），如 18 = 18:00。若 endHour &lt; startHour 则视为跨午夜时段。</summary>
        public float endHour = 24f;

        public override bool CanTrigger()
        {
            float dayPercent = GetCurrentDayPercent();
            float startPercent = Mathf.Clamp(startHour / 24f, 0f, 1f);
            float endPercent = Mathf.Clamp(endHour / 24f, 0f, 1f);

            if (startPercent <= endPercent)
            {
                // 普通时段，如 6:00–18:00
                return dayPercent >= startPercent && dayPercent <= endPercent;
            }
            else
            {
                // 跨午夜时段，如 22:00–06:00
                return dayPercent >= startPercent || dayPercent <= endPercent;
            }
        }

        /// <summary>
        /// 获取当前的日占比（0.0–1.0），基于游戏 tick 推算。
        /// 每 60000 tick 一个昼夜循环，0 = 午夜，0.25 = 06:00，0.5 = 正午，0.75 = 18:00。
        /// 注意：不包含地图经纬度偏移，不同地图的实际日出日落时间会有差异。
        /// </summary>
        private static float GetCurrentDayPercent()
        {
            int ticksGame = Find.TickManager.TicksGame;
            return (float)((ticksGame % 60000) / 60000.0);
        }
    }
}
