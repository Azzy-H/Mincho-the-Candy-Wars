#nullable disable
using Verse;
using RimWorld;

namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// 判定玩家是否已完成指定科技研究。
    ///
    /// XML 示例:
    ///   &lt;li Class="MinchoCandyWars.StellarisEvent.Trigger_ResearchComplete"&gt;
    ///     &lt;researchProject&gt;GunSmithing&lt;/researchProject&gt;
    ///   &lt;/li&gt;
    ///
    ///   带分组:
    ///   &lt;li Class="MinchoCandyWars.StellarisEvent.Trigger_ResearchComplete"&gt;
    ///     &lt;researchProject&gt;GunSmithing&lt;/researchProject&gt;
    ///     &lt;idGroup&gt;checkTech&lt;/idGroup&gt;
    ///     &lt;logic&gt;OR&lt;/logic&gt;
    ///   &lt;/li&gt;
    /// </summary>
    public class Trigger_ResearchComplete : StellarisEventTrigger
    {
        /// <summary>要检查的科技项目 Def</summary>
        public ResearchProjectDef researchProject;

        public override bool CanTrigger()
        {
            if (researchProject == null)
                return false;

            return researchProject.IsFinished;
        }
    }
}
