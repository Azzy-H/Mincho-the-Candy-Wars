#nullable disable
using System.Collections.Generic;
using Verse;

namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// onAction 随机触发条目。
    /// eventDefName 为空时表示"空项"——抽到则不在本次 onAction 中触发任何事件。
    /// </summary>
    public class OnActionRandomEntry
    {
        /// <summary>事件 defName。null 或空字符串 = 空项（不触发任何事件）</summary>
        public string eventDefName;

        /// <summary>权重，默认 1</summary>
        public float weight = 1f;
    }

    /// <summary>
    /// onAction 信号定义 —— 接收外部信号，批量触发 StellarisEvent。
    ///
    /// XML 配置示例:
    ///   &lt;OnActionDef&gt;
    ///     &lt;defName&gt;MyOnAction&lt;/defName&gt;
    ///     &lt;action&gt;signal_name&lt;/action&gt;
    ///     &lt;triggerList&gt;
    ///       &lt;li&gt;EventA&lt;/li&gt;
    ///       &lt;li&gt;EventB&lt;/li&gt;
    ///     &lt;/triggerList&gt;
    ///     &lt;randomTriggerList&gt;
    ///       &lt;li&gt;
    ///         &lt;eventDefName&gt;EventX&lt;/eventDefName&gt;
    ///         &lt;weight&gt;3&lt;/weight&gt;
    ///       &lt;/li&gt;
    ///       &lt;li&gt;
    ///         &lt;!-- 空项：不设 eventDefName --&gt;
    ///         &lt;weight&gt;1&lt;/weight&gt;
    ///       &lt;/li&gt;
    ///     &lt;/randomTriggerList&gt;
    ///   &lt;/OnActionDef&gt;
    ///
    /// 触发逻辑：
    /// 1. StellarisEventManager.DispatchAction("signal_name")
    /// 2. 收集 triggerList 中所有事件 + randomTriggerList 中按权重随机抽一个
    /// 3. 按顺序依次触发，每个事件在触发前检查自身 trigger
    /// 4. 对有 option 的事件，唤起窗口后不等待，立刻进入下一个事件
    /// </summary>
    public class OnActionDef : Def
    {
        /// <summary>触发信号字符串。与 DispatchAction 的参数匹配时触发。</summary>
        public string action;

        /// <summary>
        /// 顺序触发的 StellarisEventDef defName 列表。
        /// 每个事件触发前会检查自身 trigger，不通过则跳过。
        /// </summary>
        public List<string> triggerList = new List<string>();

        /// <summary>
        /// 加权随机触发列表。从列表项中按 weight 随机抽取一个触发，
        /// 若抽到空项（eventDefName 为空）则不触发。
        /// </summary>
        public List<OnActionRandomEntry> randomTriggerList = new List<OnActionRandomEntry>();
    }
}
