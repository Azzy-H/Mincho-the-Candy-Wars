using RimWorld;
using Verse;
using System.Linq;

namespace MinchoCandyWars.Abilities.AmberonCandy
{
    /// <summary>
    /// 琥珀糖心情 Thought。
    /// 心情加成 = 当前殖民地 Mincho 队友数量 × baseMoodEffect。
    /// 仅对自身生效（通过 ThoughtWorker 控制）。
    /// </summary>
    public class Thought_AmberonHeartMood : Thought_Situational
    {
        protected override float BaseMoodOffset
        {
            get
            {
                int count = CountMinchoMates(pawn);
                return base.CurStage.baseMoodEffect * count;
            }
        }

        private static int CountMinchoMates(Pawn p)
        {
            if (p.Map == null || p.Faction == null) return 0;
            return p.Map.mapPawns.SpawnedPawnsInFaction(p.Faction)
                .Count(other => other != p && IsMinchoPawn(other) && !other.Dead);
        }

        private static bool IsMinchoPawn(Pawn p)
        {
            return p.def == MCW_DefOf.Mincho_ThingDef || p.kindDef == MCW_DefOf.MCW_MinchoSlime;
        }
    }
}
