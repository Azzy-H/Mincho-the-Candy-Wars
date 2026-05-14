namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// Effect 基类 —— 覆写 Execute() 实现具体效果。
    /// 取代旧 StellarisEventAction，供 immediate / after / option.action 使用。
    /// </summary>
    public class StellarisEventEffect
    {
        public virtual void Execute()
        {
            // 留空，供子类覆写
        }
    }
}
