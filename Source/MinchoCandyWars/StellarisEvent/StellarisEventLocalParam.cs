namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// 本地化参数记录。用于在事件文本中注入运行时值。
    /// 本地化文本中使用 {key} 占位，框架在渲染时自动替换为 value。
    /// </summary>
    public class StellarisEventLocalParam
    {
        /// <summary>参数键名，本地化文本中用 {key} 引用</summary>
        public string? key;

        /// <summary>参数值（字符串形式）</summary>
        public string? value;

        public StellarisEventLocalParam() { }

        public StellarisEventLocalParam(string key, string value)
        {
            this.key = key;
            this.value = value;
        }
    }
}
