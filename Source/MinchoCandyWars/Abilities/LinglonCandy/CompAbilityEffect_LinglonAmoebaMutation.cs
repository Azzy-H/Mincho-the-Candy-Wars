using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace MinchoCandyWars.Abilities.LinglonCandy
{
    /// <summary>
    /// 纳米变形虫变异——随机学习一个当前 psylink 等级可用的心灵能力；若无可用则禁止使用。
    /// 参考 Hediff_Psylink.TryGiveAbilityOfLevel。
    /// </summary>
    public class CompAbilityEffect_LinglonAmoebaMutation : CompAbilityEffect
    {
        public override bool GizmoDisabled(out string reason)
        {
            if (!TryFindAvailablePsycast(parent.pawn, out _))
            {
                reason = "MinchoCandyWars.Abilities.AmoebaMutationNoAvailable".Translate();
                return true;
            }

            return base.GizmoDisabled(out reason);
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            if (TryFindAvailablePsycast(parent.pawn, out AbilityDef psycastDef))
            {
                parent.pawn.abilities.GainAbility(psycastDef);
            }
        }

        private bool TryFindAvailablePsycast(Pawn pawn, out AbilityDef psycastDef)
        {
            int psylinkLevel = pawn.GetPsylinkLevel();
            List<AbilityDef> candidates = DefDatabase<AbilityDef>.AllDefsListForReading
                .Where(a => a.IsPsycast && a.level <= psylinkLevel && !pawn.abilities.abilities.Any(x => x.def == a))
                .ToList();

            if (candidates.Count == 0)
            {
                psycastDef = null!;
                return false;
            }

            psycastDef = candidates.RandomElement();
            return true;
        }
    }
}
