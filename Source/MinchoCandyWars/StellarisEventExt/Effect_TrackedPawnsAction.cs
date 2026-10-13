using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// 对由 Thing 类型 flag 追踪的 Pawn 执行一个动作。
    ///
    /// 这是 Mincho 侧对 StellarisEvent 框架的扩展。用途：
    /// Effect_SpawnPawnFromFlag 的 trackFlagName 会为生成的 Pawn 建一个 Thing flag，
    /// 之后就能用本 Effect 一次性对这批 Pawn 下令（例如"三名回收公司成员立即发起进攻"），
    /// 而不需要再写专门的生成逻辑。
    ///
    /// XML 示例:
    ///   &lt;li Class="MinchoCandyWars.StellarisEvent.Effect_TrackedPawnsAction"&gt;
    ///     &lt;flagNames&gt;
    ///       &lt;li&gt;MCW_CorpDelegation_0&lt;/li&gt;
    ///       &lt;li&gt;MCW_CorpDelegation_1&lt;/li&gt;
    ///       &lt;li&gt;MCW_CorpDelegation_2&lt;/li&gt;
    ///     &lt;/flagNames&gt;
    ///     &lt;action&gt;Assault&lt;/action&gt;
    ///   &lt;/li&gt;
    /// </summary>
    public class Effect_TrackedPawnsAction : StellarisEventEffect
    {
        /// <summary>追踪 Pawn 的 Thing flag 名列表</summary>
        public List<string> flagNames = new List<string>();

        /// <summary>动作：Assault（进攻玩家殖民地）/ Despawn（离场）</summary>
        public TrackedPawnAction action = TrackedPawnAction.Assault;

        /// <summary>Assault 时是否可以因时间过长而撤退（false = 死战不退）</summary>
        public bool canTimeoutOrFlee = true;

        /// <summary>
        /// 仅 Despawn：若追踪到的 Pawn 属于玩家派系，先把它转交给该派系，
        /// 然后让它们自行走到地图边缘离开（与原生 QuestPart_Leave 的表现一致）。
        /// 留空则不改派系，直接离场。
        /// </summary>
        public FactionDef? toFactionDef;

        public override void Execute()
        {
            List<Pawn> pawns = ResolvePawns();
            if (pawns.Count == 0)
            {
                Log.Warning("[StellarisEvent] Effect_TrackedPawnsAction: 没有解析到任何 Pawn");
                return;
            }

            switch (action)
            {
                case TrackedPawnAction.Assault:
                    Assault(pawns);
                    break;
                case TrackedPawnAction.Despawn:
                    Despawn(pawns);
                    break;
            }
        }

        private List<Pawn> ResolvePawns()
        {
            var pawns = new List<Pawn>();
            if (flagNames == null)
            {
                return pawns;
            }

            foreach (string name in flagNames)
            {
                if (name.NullOrEmpty())
                {
                    continue;
                }

                if (StellarisEventFlagManager.GetTrackedThing(name) is Pawn pawn
                    && pawn.Spawned && !pawn.Dead && !pawn.Destroyed)
                {
                    pawns.Add(pawn);
                }
            }

            return pawns;
        }

        private void Assault(List<Pawn> pawns)
        {
            Pawn first = pawns[0];
            Map? map = first.Map ?? Find.AnyPlayerHomeMap;
            if (map == null)
            {
                Log.Warning("[StellarisEvent] Effect_TrackedPawnsAction: 找不到地图，无法发起进攻");
                return;
            }

            Faction faction = first.Faction ?? Faction.OfPlayer;

            LordJob_AssaultColony lordJob = new LordJob_AssaultColony(
                faction,
                canKidnap: false,
                canTimeoutOrFlee: canTimeoutOrFlee,
                sappers: false,
                useAvoidGridSmart: false,
                canSteal: false,
                breachers: false,
                canPickUpOpportunisticWeapons: false);

            LordMaker.MakeNewLord(faction, lordJob, map, pawns);
        }

        private void Despawn(List<Pawn> pawns)
        {
            Faction? toFaction = toFactionDef != null
                ? Find.FactionManager.FirstFactionOfDef(toFactionDef)
                : null;

            foreach (Pawn pawn in pawns)
            {
                // 属于玩家派系的人先"转交"出去，否则会一直算作殖民地成员
                if (toFaction != null && toFaction != Faction.OfPlayer && pawn.Faction == Faction.OfPlayer)
                {
                    pawn.SetFaction(toFaction);
                }

                // 脱离原有 Lord，改由"走到地图边缘离开"的 Lord 接管
                pawn.GetLord()?.Notify_PawnLost(pawn, PawnLostCondition.ForcedByQuest);
            }

            List<Pawn> leavers = pawns.Where(p => p.Spawned && !p.Downed).ToList();
            if (leavers.Count == 0)
            {
                return;
            }

            Pawn first = leavers[0];
            Map? map = first.Map;
            if (map == null)
            {
                return;
            }

            // 与原生 QuestPart_Leave 一样：自己走到地图边缘后离开
            LordJob_ExitMapBest lordJob = new LordJob_ExitMapBest(
                LocomotionUrgency.Walk,
                canDig: true,
                canDefendSelf: true);

            LordMaker.MakeNewLord(first.Faction, lordJob, map, leavers);
        }
    }

    /// <summary>Effect_TrackedPawnsAction 的动作类型</summary>
    public enum TrackedPawnAction
    {
        /// <summary>向玩家殖民地发起进攻</summary>
        Assault,

        /// <summary>离开地图（退回世界）</summary>
        Despawn
    }
}
