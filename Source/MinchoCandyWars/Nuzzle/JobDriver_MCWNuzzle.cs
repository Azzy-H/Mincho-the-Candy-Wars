using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace MinchoCandyWars.Nuzzle
{
    /// <summary>
    /// 亲昵（撒娇）的 JobDriver：
    ///   寻路到目标 → 贴上去接触 contactTicks（默认 30 tick = 0.5 秒）→ 给目标加心情想法 → 触发冷却。
    ///
    /// 驱动的 JobDef 是 MCW_Nuzzle；由 WorkGiver_MCWNuzzle 派发。
    /// </summary>
    public class JobDriver_MCWNuzzle : JobDriver
    {
        /// <summary>接触时长（tick）。取 comp 配置，拿不到时退回 30（0.5 秒）。</summary>
        private int ContactTicks
        {
            get
            {
                Comp_Nuzzle? comp = pawn.TryGetComp<Comp_Nuzzle>();
                int ticks = comp?.Props.contactTicks ?? 30;
                return ticks > 0 ? ticks : 30;
            }
        }

        private Pawn? Target => job.targetA.Thing as Pawn;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            // 不占用目标（人不是"工位"），避免和别人抢预约
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(delegate
            {
                Pawn? target = Target;
                return target == null || target.Dead || target.Downed;
            });

            // 1) 走过去
            Toil go = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return go;

            // 2) 接触 0.5 秒（面朝目标）
            //    FailOn 用 driver 级的（MakeNewToils 开头那两条），对所有 toil 生效，
            //    这里再写会撞上 FailOn 的重载二义性。
            Toil contact = Toils_General.Wait(ContactTicks, TargetIndex.A);
            yield return contact;

            // 3) 生效：加心情 + 开始冷却
            yield return Toils_General.Do(delegate
            {
                Pawn? target = Target;
                Comp_Nuzzle? comp = pawn.TryGetComp<Comp_Nuzzle>();
                if (target == null)
                {
                    return;
                }

                ThoughtDef? thought = comp?.Props.thoughtDef
                    ?? DefDatabase<ThoughtDef>.GetNamedSilentFail("MCW_NuzzledByMech");

                if (thought != null && target.needs?.mood != null)
                {
                    Thought_Memory memory = (Thought_Memory)ThoughtMaker.MakeThought(thought);
                    target.needs.mood.thoughts.memories.TryGainMemory(memory, pawn);
                }

                comp?.Notify_Nuzzled();
            });
        }
    }
}
