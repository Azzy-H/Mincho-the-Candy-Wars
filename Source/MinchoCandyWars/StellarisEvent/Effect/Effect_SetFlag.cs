namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// 设置一个 flag 到指定值。
    /// 支持 Create / Override / Add / Subtract / Multiply / Divide 模式。
    ///
    /// XML 示例:
    ///   &lt;li Class="MinchoCandyWars.StellarisEvent.Effect_SetFlag"&gt;
    ///     &lt;flagName&gt;score&lt;/flagName&gt;
    ///     &lt;valueType&gt;Float&lt;/valueType&gt;
    ///     &lt;floatValue&gt;42&lt;/floatValue&gt;
    ///     &lt;setMode&gt;Add&lt;/setMode&gt;
    ///     &lt;ticks&gt;600&lt;/ticks&gt;  &lt;!-- 可选，null = 永久 --&gt;
    ///     &lt;local&gt;false&lt;/local&gt;  &lt;!-- 可选，默认 true；false = 全局持久 flag --&gt;
    ///   &lt;/li&gt;
    /// </summary>
    public class Effect_SetFlag : StellarisEventEffect
    {
        /// <summary>目标 flag 名</summary>
        public string? flagName;

        /// <summary>值类型</summary>
        public FlagValueType valueType = FlagValueType.Bool;

        /// <summary>float 值（valueType == Float 时使用）</summary>
        public float floatValue;

        /// <summary>string 值（valueType == String 时使用）</summary>
        public string? stringValue;

        /// <summary>bool 值（valueType == Bool 时使用）</summary>
        public bool boolValue;

        /// <summary>设置模式，默认 Override（始终覆盖）</summary>
        public FlagSetMode setMode = FlagSetMode.Override;

        /// <summary>剩余 tick。null = 永久；有值 = 到期自动移除</summary>
        public int? ticks;

        /// <summary>
        /// 是否标记为 local flag（事件结束后自动清理）。默认 true。
        /// 设为 false 则创建全局持久 flag，跨事件存在并随存档保存。
        /// </summary>
        public bool local = true;

        public override void Execute()
        {
            if (flagName ==  null) return;

            var existing = StellarisEventFlagManager.GetFlag(flagName);

            switch (setMode)
            {
                case FlagSetMode.Create:
                    SetCreate(existing);
                    break;
                case FlagSetMode.Override:
                    SetOverride(existing);
                    break;
                case FlagSetMode.Add:
                case FlagSetMode.Subtract:
                case FlagSetMode.Multiply:
                case FlagSetMode.Divide:
                    SetOperation(existing);
                    break;
            }

            // 标记 local
            if (local)
            {
                var flag = StellarisEventFlagManager.GetFlag(flagName);
                if (flag != null)
                    flag.isLocal = true;
            }
        }

        // ──────────────────── Create ────────────────────

        private void SetCreate(StellarisEventFlag existing)
        {
            if (existing != null)
                return; // 已存在，不操作

            WriteFlag();
        }

        // ──────────────────── Override ────────────────────

        private void SetOverride(StellarisEventFlag existing)
        {
            WriteFlag();
        }

        // ──────────────────── 运算 ────────────────────

        private void SetOperation(StellarisEventFlag existing)
        {
            // 运算仅对 float 生效
            if (valueType != FlagValueType.Float) return;

            if (existing == null)
            {
                // 不存在时：Add/Subtract 以 0 为初值创建；Multiply/Divide 跳过
                if (setMode == FlagSetMode.Add || setMode == FlagSetMode.Subtract)
                {
                    float init = setMode == FlagSetMode.Add ? floatValue : -floatValue;
                    StellarisEventFlagManager.SetFlag(flagName!, init, ticks);
                }
                // Multiply/Divide: 不存在则跳过
                return;
            }

            // 已有 flag 但不是 float → 跳过
            if (existing.valueType != FlagValueType.Float) return;

            float result = existing.floatValue;
            switch (setMode)
            {
                case FlagSetMode.Add:      result += floatValue; break;
                case FlagSetMode.Subtract: result -= floatValue; break;
                case FlagSetMode.Multiply: result *= floatValue; break;
                case FlagSetMode.Divide:
                    if (floatValue == 0f) return; // 除零保护
                    result /= floatValue;
                    break;
            }

            StellarisEventFlagManager.SetFlag(flagName!, result, ticks);
        }

        // ──────────────────── 辅助 ────────────────────

        private void WriteFlag()
        {
            switch (valueType)
            {
                case FlagValueType.Float:
                    StellarisEventFlagManager.SetFlag(flagName!, floatValue, ticks);
                    break;
                case FlagValueType.String:
                    StellarisEventFlagManager.SetFlag(flagName!, stringValue!, ticks);
                    break;
                case FlagValueType.Bool:
                    StellarisEventFlagManager.SetFlag(flagName!, boolValue, ticks);
                    break;
            }
        }
    }
}
