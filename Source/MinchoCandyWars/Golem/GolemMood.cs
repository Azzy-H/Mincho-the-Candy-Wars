using System.Collections.Generic;
using MinchoCandyWars;
using RimWorld;
using Verse;

namespace MinchoCandyWars.Golem
{
    /// <summary>
    /// 「稳定的巨大冰淇淋」心情：殖民地里存在一座活着、没被击倒、没有狂暴的珉巧魔像时激活。
    ///
    /// 联动种族 MinchoWitch 用更高一档（+8，普通殖民者是 +3），见 MCW_GolemMood.xml 的两个阶段。
    /// </summary>
    public sealed class ThoughtWorker_StableGolem : ThoughtWorker
    {
        /// <summary>ThoughtDef 里更高一档的下标。</summary>
        private const int WitchStage = 1;
        private const int DefaultStage = 0;

        /// <summary>
        /// 地图级结果缓存时长。心情重算很频繁（需求面板打开、每段需要 tick），
        /// 而魔像进出地图/倒地是低频事件，60 tick 的过期完全够用。
        /// </summary>
        private const int CacheTicks = 60;

        private static Map? cachedMap;
        private static int cachedTick = -1;
        private static bool cachedStable;

        protected override ThoughtState CurrentStateInternal(Pawn pawn)
        {
            if (!pawn.Spawned || !pawn.IsColonist || pawn.IsPrisoner)
            {
                return ThoughtState.Inactive;
            }

            if (!HasStableGolem(pawn.Map))
            {
                return ThoughtState.Inactive;
            }

            return ThoughtState.ActiveAtStage(pawn.def == MCW_DefOf.MinchoWitch ? WitchStage : DefaultStage);
        }

        private static bool HasStableGolem(Map map)
        {
            int tick = Find.TickManager.TicksGame;
            if (map == cachedMap && tick - cachedTick < CacheTicks)
            {
                return cachedStable;
            }

            cachedMap = map;
            cachedTick = tick;
            cachedStable = Scan(map);
            return cachedStable;
        }

        /// <summary>只看本派系的已生成单位，而不是整张地图的所有生物。</summary>
        private static bool Scan(Map map)
        {
            List<Pawn> pawns = map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer);
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn golem = pawns[i];
                if (golem.def == MCW_DefOf.MCW_Mech_Golem
                    && !golem.Dead
                    && !golem.Downed
                    && !golem.InMentalState)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
