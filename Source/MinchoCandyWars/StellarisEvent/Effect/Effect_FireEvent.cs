namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// 触发另一个 StellarisEventDef。
    /// 正常走 target 事件的 trigger 判定（trigger 不通过则不触发）。
    ///
    /// 可用于事件链：完成当前事件后自动弹出下一个事件。
    ///
    /// XML 示例:
    ///   &lt;li Class="ArachnaeSwarm.Event.Effect_FireEvent"&gt;
    ///     &lt;defName&gt;ARA_WarAftermath&lt;/defName&gt;
    ///     &lt;delayTicks&gt;600&lt;/delayTicks&gt;  &lt;!-- 可选：延迟 600 tick 后触发 --&gt;
    ///   &lt;/li&gt;
    /// </summary>
    public class Effect_FireEvent : StellarisEventEffect
    {
        /// <summary>要触发的目标事件 defName</summary>
        public string? defName;

        /// <summary>
        /// 是否跳过目标事件的 trigger 判定，强制触发。默认 false（尊重 trigger）。
        /// </summary>
        public bool forceFire = false;

        /// <summary>
        /// 延迟 tick 数。0 或不填 = 立即触发。
        /// 延时管理由 StellarisEventFlagManager 负责：
        /// 创建一个定时 flag 并登记 delayed fire，到期后自动触发目标事件。
        /// </summary>
        public int delayTicks = 0;

        public override void Execute()
        {
            if (defName == null) return;

            var targetDef = StellarisEventManager.GetEvent(defName);
            if (targetDef == null)
            {
                Verse.Log.Warning($"[StellarisEvent] Effect_FireEvent: 未找到目标事件 '{defName}'");
                return;
            }

            if (delayTicks > 0)
            {
                // 委托 StellarisEventFlagManager 管理延时触发
                StellarisEventFlagManager.ScheduleDelayedFire(defName, delayTicks, forceFire);
            }
            else if (forceFire)
            {
                // 跳过 trigger，立即强制触发
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
    }
}
