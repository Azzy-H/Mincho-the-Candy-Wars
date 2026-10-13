#nullable disable
using RimWorld;
using Verse;

namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// 为殖民地的主文化（PrimaryIdeo）增加指定的文化改革点数。
    /// 若文化已达当前改革所需点数上限，则不会继续累加（但会触发改革提示信）。
    /// 非流动性文化（Fluid=false）无效。
    ///
    /// XML 示例:
    ///   &lt;li Class="MinchoCandyWars.StellarisEvent.Effect_AddIdeoDevelopmentPoints"&gt;
    ///     &lt;points&gt;3&lt;/points&gt;
    ///   &lt;/li&gt;
    ///
    ///   从 flag 读取点数（优先级高于直接值）:
    ///   &lt;li Class="MinchoCandyWars.StellarisEvent.Effect_AddIdeoDevelopmentPoints"&gt;
    ///     &lt;pointsFlag&gt;myPoints&lt;/pointsFlag&gt;
    ///     &lt;points&gt;1&lt;/points&gt;  &lt;!-- 兜底值，flag 不存在或非 float 时使用 --&gt;
    ///   &lt;/li&gt;
    /// </summary>
    public class Effect_AddIdeoDevelopmentPoints : StellarisEventEffect
    {
        /// <summary>要增加的文化点数（直接指定）</summary>
        public int points = 1;

        /// <summary>可选：从指定 flag 读取点数。若 flag 存在且为 Float 类型，优先使用 flag 的值。</summary>
        public string pointsFlag;

        public override void Execute()
        {
            Ideo primaryIdeo = Faction.OfPlayer?.ideos?.PrimaryIdeo;
            if (primaryIdeo == null)
            {
                Log.Warning("[StellarisEvent] Effect_AddIdeoDevelopmentPoints: 未找到殖民地主文化。");
                return;
            }

            if (!primaryIdeo.Fluid)
            {
                Log.Warning($"[StellarisEvent] Effect_AddIdeoDevelopmentPoints: 主文化 '{primaryIdeo.name}' 不是流动性文化，无法增加改革点数。");
                return;
            }

            int pointsToAdd = ResolvePoints();
            if (pointsToAdd <= 0) return;

            IdeoDevelopmentTracker dev = primaryIdeo.development;
            if (dev == null)
            {
                Log.Warning("[StellarisEvent] Effect_AddIdeoDevelopmentPoints: 文化开发追踪器为空。");
                return;
            }

            dev.TryAddDevelopmentPoints(pointsToAdd);
        }

        private int ResolvePoints()
        {
            // 优先从 flag 读取
            if (!pointsFlag.NullOrEmpty())
            {
                var flag = StellarisEventFlagManager.GetFlag(pointsFlag);
                if (flag != null && flag.valueType == FlagValueType.Float)
                {
                    return (int)flag.floatValue;
                }
            }

            return points;
        }
    }
}
