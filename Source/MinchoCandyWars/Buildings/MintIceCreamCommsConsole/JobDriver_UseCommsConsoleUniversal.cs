using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;

namespace MinchoCandyWars.Buildings
{
    /// <summary>
    /// 通用通讯台 JobDriver：同时支持 Building_CommsConsole 和 Building_MintIceCreamCommsConsole。
    /// 替代原版 JobDriver_UseCommsConsole，消除硬编码的 (Building_CommsConsole) 类型转换。
    /// </summary>
    public class JobDriver_UseCommsConsoleUniversal : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);

            // 走到通讯台旁边，如果 CanUseCommsNow 失效则终止
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.InteractionCell)
                .FailOn((Toil to) => !CanUseCommsNowAtTarget(to.actor));

            // 打开通讯界面
            Toil openComms = ToilMaker.MakeToil("MakeNewToils");
            openComms.initAction = delegate
            {
                Pawn actor = openComms.actor;
                if (CanUseCommsNowAtTarget(actor) && actor.jobs.curJob.commTarget != null)
                {
                    actor.jobs.curJob.commTarget.TryOpenComms(actor);
                }
            };
            yield return openComms;
        }

        /// <summary>
        /// 安全获取目标建筑的 CanUseCommsNow，不依赖具体类型
        /// </summary>
        private static bool CanUseCommsNowAtTarget(Pawn pawn)
        {
            Thing target = pawn.jobs.curJob.GetTarget(TargetIndex.A).Thing;
            if (target == null)
            {
                return false;
            }
            if (target is Building_CommsConsole bcc)
            {
                return bcc.CanUseCommsNow;
            }
            if (target is Building_MintIceCreamCommsConsole bmicc)
            {
                return bmicc.CanUseCommsNow;
            }
            return false;
        }
    }
}
