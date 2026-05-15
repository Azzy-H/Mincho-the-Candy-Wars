using RimWorld.QuestGen;
using Verse;

namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// 使用固定值或 Slate 变量创建 / 覆写一个 StellarisEventFlag。
    ///
    /// 支持四种数据类型：Float、String、Bool、Thing，由 flagType 字段决定。
    /// 可选设置 ticks 让 flag 在指定 tick 后自动过期（null = 永久）。
    /// 可选通过 storeAs 将 flag 名存入 Slate 供后续节点引用。
    ///
    /// XML 用法示例：
    /// <code>
    /// &lt;!-- 固定布尔值 --&gt;
    /// &lt;li Class="MinchoCandyWars.StellarisEvent.QuestNode_SetFlag"&gt;
    ///   &lt;flagName&gt;mission_completed&lt;/flagName&gt;
    ///   &lt;flagType&gt;Bool&lt;/flagType&gt;
    ///   &lt;boolValue&gt;true&lt;/boolValue&gt;
    /// &lt;/li&gt;
    ///
    /// &lt;!-- 使用 Slate 变量 --&gt;
    /// &lt;li Class="MinchoCandyWars.StellarisEvent.QuestNode_SetFlag"&gt;
    ///   &lt;flagName&gt;$flagNameVar&lt;/flagName&gt;
    ///   &lt;flagType&gt;Float&lt;/flagType&gt;
    ///   &lt;floatValue&gt;$pointsVar&lt;/floatValue&gt;
    ///   &lt;ticks&gt;600&lt;/ticks&gt;
    ///   &lt;storeAs&gt;createdFlagName&lt;/storeAs&gt;
    /// &lt;/li&gt;
    /// </code>
    /// </summary>
    public class QuestNode_SetFlag : QuestNode
    {
        /// <summary>Flag 名（固定字符串或 $varName Slate 变量引用）</summary>
        [NoTranslate]
        public SlateRef<string> flagName;

        /// <summary>数据类型，决定使用 floatValue / stringValue / boolValue 中的哪一个</summary>
        public SlateRef<FlagValueType> flagType;

        /// <summary>float 值（flagType == Float 时有效）</summary>
        public SlateRef<float> floatValue;

        /// <summary>string 值（flagType == String 时有效）</summary>
        public SlateRef<string> stringValue;

        /// <summary>bool 值（flagType == Bool 时有效）</summary>
        public SlateRef<bool> boolValue;

        /// <summary>Thing 引用（flagType == Thing 时有效）</summary>
        public SlateRef<Thing> thingValue;

        /// <summary>
        /// 剩余 tick 数。留空(null)表示永久 flag，不会自动过期。
        /// 设置后由 StellarisEventFlagManager 每 tick 递减，归零后自动移除。
        /// </summary>
        public SlateRef<int?> ticks;

        /// <summary>可选：将创建的 flag 名存入 Slate 中供后续节点使用</summary>
        [NoTranslate]
        public SlateRef<string> storeAs;

        protected override bool TestRunInt(Slate slate)
        {
            return !flagName.GetValue(slate).NullOrEmpty();
        }

        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            string name = flagName.GetValue(slate);

            if (name.NullOrEmpty())
                return;

            int? ticksVal = ticks.GetValue(slate);

            switch (flagType.GetValue(slate))
            {
                case FlagValueType.Float:
                    StellarisEventFlagManager.SetFlag(name, floatValue.GetValue(slate), ticksVal);
                    break;
                case FlagValueType.String:
                    StellarisEventFlagManager.SetFlag(name, stringValue.GetValue(slate), ticksVal);
                    break;
                case FlagValueType.Bool:
                    StellarisEventFlagManager.SetFlag(name, boolValue.GetValue(slate), ticksVal);
                    break;
                case FlagValueType.Thing:
                    StellarisEventFlagManager.SetFlag(name, thingValue.GetValue(slate), ticksVal);
                    break;
            }

            // 将 flag 名存入 Slate 供后续节点引用
            string storeAsStr = storeAs.GetValue(slate);
            if (!storeAsStr.NullOrEmpty())
                slate.Set(storeAsStr, name);
        }
    }
}
