using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace MinchoCandyWars.Nuzzle
{
    /// <summary>
    /// 「撒娇」的工作派发器。
    ///
    /// 设计要点（按需求）：
    ///   · 写成 WorkGiver，挂在**已有**的 WorkType 下面（Cooking / Doctor / Childcare /
    ///     Mining / PlantCutting / Growing / Hauling / Cleaning 各注册一个 WorkGiverDef），
    ///     这样完全不用改机械体的思考树 —— 它们本来就是靠 JobGiver_Work 干活的。
    ///   · 只给**机械体**派活：HasJobOnThing 里用 RaceProps.IsMechanoid + 必须有 Comp_Nuzzle 双重判定，
    ///     人类/动物即使开着对应工作类型也永远接不到。
    ///   · 冷却由机械体身上的 Comp_Nuzzle 承载（每台机器各自计时）。
    ///   · priorityInType 设成负值，排在同类工作后面，不会抢真正的活。
    /// </summary>
    public class WorkGiver_MCWNuzzle : WorkGiver_Scanner
    {
        /// <summary>JobDef 只在第一次用到时解析一次。</summary>
        private static JobDef? cachedJobDef;

        private static JobDef? NuzzleJobDef
        {
            get
            {
                if (cachedJobDef == null)
                {
                    cachedJobDef = DefDatabase<JobDef>.GetNamedSilentFail("MCW_Nuzzle");
                }
                return cachedJobDef;
            }
        }

        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Pawn);

        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override Danger MaxPathDanger(Pawn pawn) => Danger.Deadly;

        /// <summary>候选目标：同阵营已生成的人形。比全图扫描便宜得多。</summary>
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            if (pawn.Map == null)
            {
                yield break;
            }

            List<Pawn> candidates = pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction);
            for (int i = 0; i < candidates.Count; i++)
            {
                yield return candidates[i];
            }
        }

        /// <summary>不是机械体、没有 comp、或者还在冷却，就整类跳过。</summary>
        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            if (pawn == null || pawn.Dead || !pawn.RaceProps.IsMechanoid)
            {
                return true;
            }

            Comp_Nuzzle? comp = pawn.TryGetComp<Comp_Nuzzle>();
            return comp == null || !comp.Ready;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (NuzzleJobDef == null)
            {
                return false;
            }
            if (pawn == null || pawn.Dead || pawn.Map == null || !pawn.RaceProps.IsMechanoid)
            {
                return false;
            }

            Comp_Nuzzle? comp = pawn.TryGetComp<Comp_Nuzzle>();
            if (comp == null || !comp.Ready)
            {
                return false;
            }

            if (t is not Pawn target || target == pawn)
            {
                return false;
            }

            if (target.Dead || target.Downed || target.Map != pawn.Map)
            {
                return false;
            }

            // 只对**自由殖民者**撒娇：奴隶、囚犯、客人、亚人都不算
            // （IsFreeNonSlaveColonist = Faction 是玩家 + 自由人 + 不是奴隶）
            if (!target.IsFreeNonSlaveColonist)
            {
                return false;
            }

            if (target.needs?.mood == null)
            {
                return false;
            }

            if (!target.Position.InHorDistOf(pawn.Position, comp.Props.maxDistance))
            {
                return false;
            }

            if (!target.CanCasuallyInteractNow())
            {
                return false;
            }

            return pawn.CanReach(target, PathEndMode.Touch, Danger.Deadly, false, false);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            JobDef? jobDef = NuzzleJobDef;
            if (jobDef == null)
            {
                return null;
            }

            Job job = JobMaker.MakeJob(jobDef, t);
            job.locomotionUrgency = LocomotionUrgency.Walk;
            job.expiryInterval = 3000;
            job.count = 1;
            return job;
        }
    }
}
