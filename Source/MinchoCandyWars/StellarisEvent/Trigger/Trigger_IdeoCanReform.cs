#nullable disable
using RimWorld;
using Verse;

namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// 判定殖民地的主文化是否已达到可改革的条件（文化点数 ≥ 改革所需点数）。
    /// 非流动性文化（Fluid=false）始终返回 false。
    ///
    /// XML 示例:
    ///   &lt;li Class="MinchoCandyWars.StellarisEvent.Trigger_IdeoCanReform"&gt;
    ///     &lt;invert&gt;false&lt;/invert&gt;  &lt;!-- true = 不可改革时返回 true --&gt;
    ///   &lt;/li&gt;
    ///
    ///   带分组:
    ///   &lt;li Class="MinchoCandyWars.StellarisEvent.Trigger_IdeoCanReform"&gt;
    ///     &lt;idGroup&gt;reformCheck&lt;/idGroup&gt;
    ///     &lt;logic&gt;AND&lt;/logic&gt;
    ///   &lt;/li&gt;
    /// </summary>
    public class Trigger_IdeoCanReform : StellarisEventTrigger
    {
        /// <summary>
        /// 是否反转结果。true = 不可改革时返回 true。
        /// </summary>
        public bool invert = false;

        public override bool CanTrigger()
        {
            Ideo primaryIdeo = Faction.OfPlayer?.ideos?.PrimaryIdeo;
            if (primaryIdeo == null)
                return invert; // 无主文化：反转模式下视为不可改革

            bool canReform = primaryIdeo.Fluid && primaryIdeo.development?.CanReformNow == true;
            return invert ? !canReform : canReform;
        }
    }
}
