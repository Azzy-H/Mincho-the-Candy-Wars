using System.Text;
using Verse;
using RimWorld;

namespace MinchoCandyWars.Abilities.GanLuCandy
{
    /// <summary>
    /// 冰淇淋重塑 —— 复刻 UnnaturalHealing 逻辑，失败时生成 Mincho 触手而非血肉触手。
    /// </summary>
    public class CompAbilityEffect_IceCreamReshape : CompAbilityEffect
    {
        private const float TentacleChance = 0.25f;

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn pawn = target.Pawn;
            if (pawn == null)
                return false;

            bool flag = HealthUtility.TryGetWorstHealthCondition(pawn, out _, out _);
            if (!flag && throwMessages)
            {
                Messages.Message(string.Format("{0}: {1}", "CannotUseAbility".Translate(parent.def.label),
                    "AbilityCannotCastNoHealableInjury".Translate(pawn.Named("PAWN")).Resolve().StripTags()),
                    pawn, MessageTypeDefOf.RejectInput, historical: false);
            }
            return flag;
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn pawn = target.Pawn;
            if (pawn == null)
                return;

            // Tend all bleeding wounds
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
            {
                if (hediff.Bleeding)
                {
                    hediff.Tended(1f, 1f, 1);
                }
            }

            string text = HealthUtility.FixWorstHealthCondition(pawn);
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("MinchoCandyWars.Abilities.IceCreamReshapeLetter".Translate(parent.pawn.Named("CASTER"), pawn.Named("PAWN")));
            if (!string.IsNullOrEmpty(text))
            {
                stringBuilder.AppendLine("\n" + text);
            }

            bool tentacleGrown = false;
            if (pawn.ageTracker.Adult && Rand.Chance(TentacleChance))
            {
                // Use Mincho tentacle instead of flesh tentacle
                if (FleshbeastUtility.TryGiveMutation(pawn, MCW_DefOf.MCW_MinchoTentacle))
                {
                    stringBuilder.Append("\n" + "IceCreamReshapeTentacle".Translate(pawn.Named("PAWN")));
                    tentacleGrown = true;
                }
            }

            if (!tentacleGrown)
            {
                TaleRecorder.RecordTale(TaleDefOf.HealedMe, parent.pawn, pawn);
            }

            TaggedString label = "IceCreamReshapeLabel".Translate();
            LetterDef textLetterDef = tentacleGrown ? LetterDefOf.NegativeEvent : LetterDefOf.NeutralEvent;
            Find.LetterStack.ReceiveLetter(label, stringBuilder.ToString().TrimEndNewlines(), textLetterDef, pawn);
        }

        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            return false;
        }
    }
}