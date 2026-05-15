using System.Collections.Generic;

namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// Effect 列表容器。持有一组 effect，Execute() 时从上到下依次执行。
    /// 供 immediate / after / option.action 使用。
    /// </summary>
    public class StellarisEventEffectList
    {
        /// <summary>Effect 列表，按顺序执行</summary>
        public List<StellarisEventEffect> effects = new List<StellarisEventEffect>();

        /// <summary>
        /// 依次执行所有 effect。空列表安全。
        /// </summary>
        public void Execute()
        {
            if (effects == null) return;
            foreach (var e in effects)
                e?.Execute();
        }
    }
}
