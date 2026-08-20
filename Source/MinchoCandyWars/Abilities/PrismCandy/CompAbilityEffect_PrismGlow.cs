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
