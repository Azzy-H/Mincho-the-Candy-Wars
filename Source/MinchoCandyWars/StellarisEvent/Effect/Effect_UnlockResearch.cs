#nullable disable
using System.Collections.Generic;
using Verse;

namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// 解锁指定的科技项目。
    ///
    /// XML 示例:
    ///   &lt;li Class="MinchoCandyWars.StellarisEvent.Effect_UnlockResearch"&gt;
    ///     &lt;defName&gt;GunSmithing&lt;/defName&gt;
    ///     &lt;completeResearch&gt;false&lt;/completeResearch&gt;  &lt;!-- 仅解锁蓝图（默认） --&gt;
    ///   &lt;/li&gt;
    ///
    ///   &lt;li Class="MinchoCandyWars.StellarisEvent.Effect_UnlockResearch"&gt;
    ///     &lt;defName&gt;MicroelectronicsBasics&lt;/defName&gt;
    ///     &lt;completeResearch&gt;true&lt;/completeResearch&gt;   &lt;!-- 直接完成研发 --&gt;
    ///   &lt;/li&gt;
    /// </summary>
    public class Effect_UnlockResearch : StellarisEventEffect
    {
        /// <summary>目标科技项目的 defName</summary>
        public string defName;

        /// <summary>是否直接完成研发。false = 仅解锁蓝图（默认），true = 直接完成</summary>
        public bool completeResearch = false;

        public override void Execute()
        {
            if (defName.NullOrEmpty()) return;

            ResearchProjectDef project = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(defName);
            if (project == null)
            {
                Log.Warning($"[StellarisEvent] Effect_UnlockResearch: 未找到 ResearchProjectDef '{defName}'");
                return;
            }

            // 若目标科技已经完成，无需操作
            if (project.IsFinished)
                return;

            // 先递归完成所有前置科技（包括 hiddenPrerequisites）
            CompleteAllPrerequisites(project);

            // completeResearch ? 直接完成 : 仅解锁蓝图
            if (completeResearch)
            {
                Find.ResearchManager.FinishProject(project);
            }
        }

        /// <summary>
        /// 递归完成指定科技项目的所有前置科技（prerequisites + hiddenPrerequisites）。
        /// 已完成的会跳过。
        /// </summary>
        private void CompleteAllPrerequisites(ResearchProjectDef project)
        {
            var allPrereqs = new List<ResearchProjectDef>();
            if (project.prerequisites != null)
                allPrereqs.AddRange(project.prerequisites);
            if (project.hiddenPrerequisites != null)
                allPrereqs.AddRange(project.hiddenPrerequisites);

            foreach (var prereq in allPrereqs)
            {
                if (prereq == null) continue;
                if (prereq.IsFinished) continue;

                // 递归：先完成前置的前置
                CompleteAllPrerequisites(prereq);

                // 完成此前置科技
                Find.ResearchManager.FinishProject(prereq);
            }
        }
    }
}
