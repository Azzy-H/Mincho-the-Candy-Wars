using Verse;
using RimWorld;

namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// 触发指定的 IncidentDef（如袭击、商队到达等 RimWorld 原生事件）。
    ///
    /// XML 示例:
    ///   &lt;li Class="MinchoCandyWars.StellarisEvent.Effect_FireIncident"&gt;
    ///     &lt;defName&gt;RaidEnemy&lt;/defName&gt;
    ///   &lt;/li&gt;
    /// </summary>
    public class Effect_FireIncident : StellarisEventEffect
    {
        /// <summary>要触发的 IncidentDef 的 defName</summary>
        public string? defName;

        public override void Execute()
        {
            if (defName.NullOrEmpty()) return;

            IncidentDef incident = DefDatabase<IncidentDef>.GetNamedSilentFail(defName);
            if (incident == null)
            {
                Log.Warning($"[StellarisEvent] Effect_FireIncident: 未找到 IncidentDef '{defName}'");
                return;
            }

            Map map = Find.AnyPlayerHomeMap ?? Find.CurrentMap;
            if (map == null)
            {
                Log.Warning($"[StellarisEvent] Effect_FireIncident: 无可用的地图触发 '{defName}'");
                return;
            }

            IncidentParms parms = StorytellerUtility.DefaultParmsNow(incident.category, map);
            incident.Worker.TryExecute(parms);
        }
    }
}
