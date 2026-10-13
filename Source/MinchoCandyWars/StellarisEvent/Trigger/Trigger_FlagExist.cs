#nullable disable
namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// 判定指定 flag 是否存在（不检查类型与值）。
    /// flag 不存在返回 false；存在则无论其类型与值如何均返回 true。
    /// 通常用于"某事件已发生过 / 某流程已开启"这类仅关心存在性的判断。
    ///
    /// XML 示例:
    ///   &lt;li Class="MinchoCandyWars.StellarisEvent.Trigger_FlagExist"&gt;
    ///     &lt;flagName&gt;firstContactDone&lt;/flagName&gt;
    ///   &lt;/li&gt;
    ///
    ///   &lt;!-- 配合 idGroup 取反：flag 不存在时通过 --&gt;
    ///   &lt;li Class="MinchoCandyWars.StellarisEvent.Trigger_FlagExist"&gt;
    ///     &lt;flagName&gt;questStarted&lt;/flagName&gt;
    ///     &lt;idGroup&gt;questNotStarted&lt;/idGroup&gt;
    ///     &lt;logic&gt;NOT&lt;/logic&gt;
    ///   &lt;/li&gt;
    /// </summary>
    public class Trigger_FlagExist : StellarisEventTrigger
    {
        /// <summary>要检查的 flag 名</summary>
        public string flagName;

        public override bool CanTrigger()
        {
            return StellarisEventFlagManager.HasFlag(flagName);
        }
    }
}
