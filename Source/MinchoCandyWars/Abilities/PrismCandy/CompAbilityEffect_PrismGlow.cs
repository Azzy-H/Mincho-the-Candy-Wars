using RimWorld;
using UnityEngine;
using Verse;

namespace MinchoCandyWars.Abilities.PrismCandy
{
    /// <summary>
    /// 琉璃霞——跃向敌对目标突刺，造成利器伤害（10% * 最大清凉度），随后获得 10 秒移速加成。
    /// </summary>
    public class CompAbilityEffect_PrismGlow : CompAbilityEffect, ICompAbilityEffectOnJumpCompleted
    {
        private const float DamageRatio = 0.1f;

        /// <summary>落地冲击波直径（格）。贴图是居中径向渐变圆，无需旋转。</summary>
        private const float ImpactScale = 3f;

        public void OnJumpCompleted(IntVec3 origin, LocalTargetInfo target)
        {
            Pawn caster = parent.pawn;

            if (target.Pawn is Pawn victim && !victim.Dead && victim.Spawned && victim.Map == caster.Map)
            {
                int damage = Mathf.RoundToInt(DamageRatio * PrismCandyAbilityUtility.GetMaxCandyValue(caster));
                DamageInfo dinfo = new DamageInfo(DamageDefOf.Cut, damage, instigator: caster);
                victim.TakeDamage(dinfo);
            }

            if (!caster.health.hediffSet.HasHediff(MCW_DefOf.MCW_PrismGlowMoveSpeed))
            {
                caster.health.AddHediff(MCW_DefOf.MCW_PrismGlowMoveSpeed);
            }

            // 冲击波打在「被突刺的目标」身上，而不是施法者脚下，视觉上才对应这次命中。
            // 本回调触发时施法者已被安置到落点，因此 caster.Map 是安全的。
            // 注意不要在 PawnFlyer_PrismGlow.RespawnPawn 里再触发一次，那是同一时刻同一位置，只会叠加。
            Map map = caster.Map;
            if (map != null && MCW_DefOf.MCW_Fleck_PrismGlowImpact != null)
            {
                Vector3 impactLoc = target.Pawn is Pawn p && p.Spawned
                    ? p.DrawPos
                    : caster.DrawPos;

                FleckMaker.Static(impactLoc, map, MCW_DefOf.MCW_Fleck_PrismGlowImpact, ImpactScale);
            }
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn victim = target.Pawn;
            if (victim == null || victim.Dead || !victim.HostileTo(parent.pawn.Faction))
            {
                if (throwMessages)
                {
                    Messages.Message("MinchoCandyWars.Abilities.PrismGlowInvalidTarget".Translate(), parent.pawn, MessageTypeDefOf.RejectInput, historical: false);
                }
                return false;
            }
            return base.Valid(target, throwMessages);
        }

        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            if (target.HasThing && target.Thing is Pawn victim)
            {
                GenDraw.DrawTargetHighlight(victim);
            }
        }
    }
}
