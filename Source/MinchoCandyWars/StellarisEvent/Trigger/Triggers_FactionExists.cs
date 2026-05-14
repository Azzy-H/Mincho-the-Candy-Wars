using Verse;
using RimWorld;

namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// 判定指定派系 Def 对应的派系是否已在世界中生成（注册于 FactionManager）。
    /// XML 示例:
    ///   &lt;li Class="MinchoCandyWars.StellarisEvent.Triggers_FactionExists"&gt;
    ///     &lt;factionDef&gt;ARA_MinchoCandyWars&lt;/factionDef&gt;
    ///     &lt;idGroup&gt;checkFactions&lt;/idGroup&gt;
    ///     &lt;logic&gt;OR&lt;/logic&gt;
    ///   &lt;/li&gt;
    /// </summary>
    public class Triggers_FactionExists : StellarisEventTrigger
    {
        public FactionDef? factionDef;

        public override bool CanTrigger()
        {
            if (factionDef == null)
                return false;

            return Find.FactionManager.FirstFactionOfDef(factionDef) != null;
        }
    }
}
