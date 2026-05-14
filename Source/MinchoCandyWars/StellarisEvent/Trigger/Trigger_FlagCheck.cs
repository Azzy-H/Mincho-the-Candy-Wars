namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// Float 比较模式（仅 checkType == Float 时生效）。
    /// </summary>
    public enum FloatCompareMode
    {
        Equal,
        GreaterThan,
        LessThan,
        GreaterOrEqual,
        LessOrEqual
    }

    /// <summary>
    /// 判定指定 flag 是否存在且值与类型均匹配。
    /// Float 类型支持大小比较：Equal / GreaterThan / LessThan / GreaterOrEqual / LessOrEqual。
    /// Thing 类型检查追踪状态：Normal / Despawn / DiedOrDisappeared。
    /// 未查找到、数据类型不同均返回 false。
    ///
    /// XML 示例:
    ///   &lt;li Class="MinchoCandyWars.StellarisEvent.Trigger_FlagCheck"&gt;
    ///     &lt;flagName&gt;score&lt;/flagName&gt;
    ///     &lt;checkType&gt;Float&lt;/checkType&gt;
    ///     &lt;expectedFloat&gt;100&lt;/expectedFloat&gt;
    ///     &lt;floatCompare&gt;GreaterOrEqual&lt;/floatCompare&gt;
    ///   &lt;/li&gt;
    ///
    ///   &lt;!-- 检测 Thing flag 状态 --&gt;
    ///   &lt;li Class="MinchoCandyWars.StellarisEvent.Trigger_FlagCheck"&gt;
    ///     &lt;flagName&gt;elite_warrior&lt;/flagName&gt;
    ///     &lt;checkType&gt;Thing&lt;/checkType&gt;
    ///     &lt;expectedThingStatus&gt;DiedOrDisappeared&lt;/expectedThingStatus&gt;
    ///   &lt;/li&gt;
    /// </summary>
    public class Trigger_FlagCheck : StellarisEventTrigger
    {
        /// <summary>要检查的 flag 名</summary>
        public string? flagName;

        /// <summary>期望的 flag 数据类型</summary>
        public FlagValueType checkType = FlagValueType.Bool;

        /// <summary>Float 比较模式，默认 ==（仅 checkType == Float 时生效）</summary>
        public FloatCompareMode floatCompare = FloatCompareMode.Equal;

        /// <summary>期望的 float 值（checkType == Float 时使用）</summary>
        public float expectedFloat;

        /// <summary>期望的 string 值（checkType == String 时使用）</summary>
        public string? expectedString;

        /// <summary>期望的 bool 值（checkType == Bool 时使用）</summary>
        public bool expectedBool;

        /// <summary>期望的 Thing 追踪状态（checkType == Thing 时使用）</summary>
        public ThingFlagStatus expectedThingStatus = ThingFlagStatus.Normal;

        public override bool CanTrigger()
        {
            var flag = StellarisEventFlagManager.GetFlag(flagName!);
            if (flag == null)
                return false;

            if (flag.valueType != checkType)
                return false;

            switch (checkType)
            {
                case FlagValueType.Float:
                    return floatCompare switch
                    {
                        FloatCompareMode.Equal          => flag.floatValue == expectedFloat,
                        FloatCompareMode.GreaterThan    => flag.floatValue > expectedFloat,
                        FloatCompareMode.LessThan       => flag.floatValue < expectedFloat,
                        FloatCompareMode.GreaterOrEqual => flag.floatValue >= expectedFloat,
                        FloatCompareMode.LessOrEqual    => flag.floatValue <= expectedFloat,
                        _                               => false
                    };
                case FlagValueType.String:
                    return flag.stringValue == expectedString;
                case FlagValueType.Bool:
                    return flag.boolValue == expectedBool;
                case FlagValueType.Thing:
                    return flag.GetThingStatus() == expectedThingStatus;
                default:
                    return false;
            }
        }
    }
}
