using RimWorld;
using Verse;

namespace MinchoCandyWars.Abilities.PrismCandy
{
    /// <summary>
    /// 琉璃霞的跳跃 Verb：仅指定自定义飞行器。
    /// 目标单位的传递、能力激活（含清凉度消耗）均由原版 Verb_CastAbilityJump.TryCastShot 完成，
    /// 它会通过 JumpUtility.DoJump 把 currentTarget（含 pawn 引用）传给 PawnFlyer。
    /// </summary>
    public class Verb_CastAbilityPrismGlow : Verb_CastAbilityJump
    {
        public override ThingDef JumpFlyerDef => MCW_DefOf.MCW_PrismGlowFlyer;

        /// <summary>
        /// 原版 Verb_CastAbilityJump.OrderForceTarget 会调用 JumpUtility.OrderJump，
        /// 后者会把目标转成"目标附近的一个格子"再塞进 CastJump job，导致 pawn 引用丢失。
        /// 这里改走 ability.QueueCastingJob，让 job.targetA 保留完整 pawn 目标。
        /// </summary>
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            ability.QueueCastingJob(target, null);
        }
    }
}
