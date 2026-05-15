using RimWorld.QuestGen;
using Verse;

namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// 从 StellarisEvent 的 Thing 类型 flag 中读取被追踪的 Pawn 对象，
    /// 并将其存入 Quest Slate 供后续 QuestNode 使用。
    ///
    /// 仅处理 FlagValueType == Thing 且 ResolveThing() 返回 Pawn 的 flag。
    /// 非 Thing 类型、Thing 不存在、Thing 不是 Pawn 等情况会输出 Warning 并跳过。
    ///
    /// 读取是只读操作 —— flag 不会被移除，后续仍可再次读取。
    ///
    /// XML 用法示例：
    /// <code>
    /// &lt;!-- 读取 flag 中的 Pawn，存入 Slate 变量 --&gt;
    /// &lt;li Class="MinchoCandyWars.StellarisEvent.QuestNode_GetFlagPawn"&gt;
    ///   &lt;flagName&gt;elite_warrior&lt;/flagName&gt;
    ///   &lt;storeAs&gt;warrior&lt;/storeAs&gt;
    /// &lt;/li&gt;
    ///
    /// &lt;!-- 从 Slate 变量中获取 flag 名 --&gt;
    /// &lt;li Class="MinchoCandyWars.StellarisEvent.QuestNode_GetFlagPawn"&gt;
    ///   &lt;flagName&gt;$flagNameVar&lt;/flagName&gt;
    ///   &lt;storeAs&gt;targetPawn&lt;/storeAs&gt;
    /// &lt;/li&gt;
    ///
    /// &lt;!-- 追加到 Slate 列表 --&gt;
    /// &lt;li Class="MinchoCandyWars.StellarisEvent.QuestNode_GetFlagPawn"&gt;
    ///   &lt;flagName&gt;captured_prisoner&lt;/flagName&gt;
    ///   &lt;addToList&gt;prisoners&lt;/addToList&gt;
    /// &lt;/li&gt;
    /// </code>
    /// </summary>
    public class QuestNode_GetFlagPawn : QuestNode
    {
        /// <summary>
        /// 要读取的 flag 名。支持 $varName 形式的 Slate 变量引用。
        /// flag 必须为 Thing 类型且追踪的 Thing 为 Pawn。
        /// </summary>
        [NoTranslate]
        public SlateRef<string> flagName;

        /// <summary>
        /// 将解析出的 Pawn 存入 Slate 的变量名。
        /// 后续 QuestNode 可通过 $varName 引用该 Pawn。
        /// 可与 addToList 同时使用。
        /// </summary>
        [NoTranslate]
        public SlateRef<string> storeAs;

        /// <summary>
        /// 将解析出的 Pawn 追加到 Slate 中的列表。
        /// 若列表不存在则自动创建。
        /// 可与 storeAs 同时使用。
        /// </summary>
        [NoTranslate]
        public SlateRef<string> addToList;

        protected override bool TestRunInt(Slate slate)
        {
            string name = flagName.GetValue(slate);
            if (name.NullOrEmpty())
                return false;

            // 至少需要一个输出目标
            if (storeAs.GetValue(slate).NullOrEmpty() && addToList.GetValue(slate).NullOrEmpty())
                return false;

            // 检查 flag 是否存在且为 Thing 类型且可解析为 Pawn
            var flag = StellarisEventFlagManager.GetFlag(name);
            if (flag == null || flag.valueType != FlagValueType.Thing)
                return false;

            Thing thing = flag.ResolveThing();
            return thing is Pawn;
        }

        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            string name = flagName.GetValue(slate);

            if (name.NullOrEmpty())
                return;

            // ── 读取 flag ──
            var flag = StellarisEventFlagManager.GetFlag(name);
            if (flag == null)
            {
                Log.Warning($"[QuestNode_GetFlagPawn] Flag '{name}' 不存在");
                return;
            }

            if (flag.valueType != FlagValueType.Thing)
            {
                Log.Warning($"[QuestNode_GetFlagPawn] Flag '{name}' 类型为 {flag.valueType}，期望 Thing");
                return;
            }

            Thing thing = flag.ResolveThing();
            if (thing == null)
            {
                Log.Warning($"[QuestNode_GetFlagPawn] Flag '{name}' 追踪的 Thing 无法解析（thingID={flag.thingID}）");
                return;
            }

            if (!(thing is Pawn pawn))
            {
                Log.Warning($"[QuestNode_GetFlagPawn] Flag '{name}' 追踪的 Thing 不是 Pawn（类型={thing.GetType().Name}）");
                return;
            }

            // ── 存入 Slate ──
            string storeAsStr = storeAs.GetValue(slate);
            if (!storeAsStr.NullOrEmpty())
                slate.Set(storeAsStr, pawn);

            string addToListStr = addToList.GetValue(slate);
            if (!addToListStr.NullOrEmpty())
                QuestGenUtility.AddToOrMakeList(slate, addToListStr, pawn);
        }
    }
}
