namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// Flag 设置模式。
    /// Create  : 仅在 flag 不存在时新建（已有则跳过，无论值是否相同）
    /// Override: 始终覆盖写入
    /// Add     : float 加法（不存在时以 0 为初始值）
    /// Subtract: float 减法（不存在时以 0 为初始值）
    /// Multiply: float 乘法（不存在则跳过）
    /// Divide  : float 除法（不存在则跳过）
    /// </summary>
    public enum FlagSetMode
    {
        Create,
        Override,
        Add,
        Subtract,
        Multiply,
        Divide
    }
}
