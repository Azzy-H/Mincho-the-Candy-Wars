using System.Globalization;

namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// 从指定 flag 抓取值，存入事件的 localParams 供本地化文本注入。
    /// 专为 immediate 设计（此时窗口尚未构建，参数可在渲染前就绪）。
    ///
    /// 同一 event 中多个此效果可按添加顺序依次填充参数，
    /// 后续如通过 InjectParams 即可一次性全部替换。
    ///
    /// XML 示例:
    ///   &lt;li Class="MinchoCandyWars.StellarisEvent.Effect_FetchFlagAsParam"&gt;
    ///     &lt;flagName&gt;enemyName&lt;/flagName&gt;
    ///     &lt;paramKey&gt;enemy&lt;/paramKey&gt;
    ///   &lt;/li&gt;
    /// </summary>
    public class Effect_FetchFlagAsParam : StellarisEventEffect
    {
        /// <summary>要抓取值的 flag 名</summary>
        public string? flagName;

        /// <summary>
        /// 存入的参数 key（本地化中用 {key} 引用）。
        /// 为空时默认使用 flagName 作为 key。
        /// </summary>
        public string? paramKey;

        public override void Execute()
        {
            var def = StellarisEventDef.ExecutingInstance;
            if (def == null || flagName == null) return;

            var flag = StellarisEventFlagManager.GetFlag(flagName);
            if (flag == null) return;

            string key = paramKey == null ? flagName : paramKey;
            string value = FlagValueToString(flag);
            def.SetParam(key, value);
        }

        /// <summary>
        /// 将 flag 值转为字符串。Float 保留 2 位小数。
        /// </summary>
        private static string FlagValueToString(StellarisEventFlag flag)
        {
            switch (flag.valueType)
            {
                case FlagValueType.Bool:
                    return flag.boolValue ? "true" : "false";
                case FlagValueType.Float:
                    return flag.floatValue.ToString("F2", CultureInfo.InvariantCulture);
                case FlagValueType.String:
                    return flag.stringValue ?? "";
                default:
                    return "";
            }
        }
    }
}
