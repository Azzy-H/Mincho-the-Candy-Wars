using System.Collections.Generic;
using Verse;

namespace MinchoCandyWars
{
    /// <summary>
    /// 给 Thing 添加多个「点击触发 StellarisEvent」的 Gizmo 按钮（Thing 版本）。
    ///
    /// 移植自 ProdrakeRace 的 PDR.CompProperties_StellarisEventGizmo。
    ///
    /// XML 用法：
    /// <code>
    /// &lt;comps&gt;
    ///   &lt;li Class="MinchoCandyWars.CompProperties_StellarisEventGizmo"&gt;
    ///     &lt;gizmos&gt;
    ///       &lt;li&gt;
    ///         &lt;eventDef&gt;MCW_Event_QuestA1_AllianceContact&lt;/eventDef&gt;
    ///         &lt;label&gt;联络珉巧同盟&lt;/label&gt;
    ///         &lt;desc&gt;呼叫珉巧同盟……&lt;/desc&gt;
    ///       &lt;/li&gt;
    ///     &lt;/gizmos&gt;
    ///   &lt;/li&gt;
    /// &lt;/comps&gt;
    /// </code>
    /// </summary>
    public class CompProperties_StellarisEventGizmo : CompProperties
    {
        /// <summary>Gizmo 按钮条目列表。</summary>
        public List<StellarisEventGizmoEntry> gizmos = new List<StellarisEventGizmoEntry>();

        public CompProperties_StellarisEventGizmo()
        {
            compClass = typeof(Comp_StellarisEventGizmo);
        }
    }
}
