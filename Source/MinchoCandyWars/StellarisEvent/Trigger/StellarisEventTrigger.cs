namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// 事件触发器基类 —— 支持 idGroup 分组逻辑判定。
    /// 覆写 CanTrigger() 添加具体判定条件。
    ///
    /// 分组判定规则 (idGroup 非空时生效):
    ///   1. NOT 翻转该 trigger 的原始结果
    ///   2. 任一 OR 为 true → 该 idGroup 内所有 OR 视为 true
    ///   3. 最终所有 trigger 的结果必须全部为 true，idGroup 才返回 true
    /// </summary>
    public class StellarisEventTrigger
    {
        /// <summary>
        /// 分组标识。为空则作为独立 trigger 判定（默认 AND 行为）。
        /// 非空时收集同一 StellarisEventDef 内所有同名 idGroup 的 trigger 进行统一逻辑判定。
        /// </summary>
        public string? idGroup;

        /// <summary>
        /// 逻辑运算符，仅在 idGroup 非空时生效。
        /// </summary>
        public TriggerLogic logic = TriggerLogic.AND;

        /// <summary>
        /// 覆写此方法实现具体判定逻辑。默认始终返回 true。
        /// </summary>
        public virtual bool CanTrigger()
        {
            return true;
        }
    }

    /// <summary>
    /// Trigger 逻辑运算符 —— 仅在 idGroup 有效时生效
    /// </summary>
    public enum TriggerLogic
    {
        /// <summary>默认：该 trigger 原始结果必须为 true（idGroup 下与其他 AND 一起判定）</summary>
        AND,

        /// <summary>同一 idGroup 内任一 OR 为 true 则所有 OR 视为 true</summary>
        OR,

        /// <summary>同一 idGroup 内翻转该 trigger 的原始输出</summary>
        NOT
    }
}
