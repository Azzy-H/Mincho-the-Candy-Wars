using MinchoCandyWars.StellarisEvent;
using RimWorld;
using Verse;

namespace MinchoCandyWars
{
    /// <summary>
    /// 终局：停用回收部队派系（类似停用机械体 / 删除异象）。
    ///
    /// 做法：
    ///   · 把 Faction.defeated 置 true —— FactionManager.GetFactions(allowDefeated: false)
    ///     会过滤掉该派系，于是它不会再出现在袭击 / 通讯 / 任务里；
    ///   · 清掉地图上现存的该派系 pawn（避免残留的袭击者继续行动）。
    ///
    /// XML 用法：
    ///   &lt;li Class="MinchoCandyWars.Effect_DisableRecycleFaction"&gt;
    ///     &lt;factionDef&gt;MCW_RecycleCorp_Faction&lt;/factionDef&gt;
    ///   &lt;/li&gt;
    /// </summary>
    public class Effect_DisableRecycleFaction : StellarisEventEffect
    {
        /// <summary>要停用的派系。</summary>
        public FactionDef factionDef;

        public override void Execute()
        {
            if (factionDef == null)
            {
                Log.Warning("[MCW] Effect_DisableRecycleFaction: factionDef 为空，跳过");
                return;
            }

            Faction faction = Find.FactionManager.FirstFactionOfDef(factionDef);
            if (faction == null)
            {
                return;
            }

            faction.defeated = true;

            // 清掉地图上的该派系单位
            foreach (Map map in Find.Maps)
            {
                foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned.ToList())
                {
                    if (pawn.Faction == faction && !pawn.Dead)
                    {
                        pawn.Destroy(DestroyMode.Vanish);
                    }
                }
            }

            Messages.Message(
                "MinchoCandyWars.UI.Message.RecycleDefeated".Translate(),
                MessageTypeDefOf.PositiveEvent, historical: false);
        }
    }
}
