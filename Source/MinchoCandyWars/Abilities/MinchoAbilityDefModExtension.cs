using System.Collections.Generic;
using System.Linq;
using Verse;

namespace MinchoCandyWars.Abilities
{
    //minchoAbility数据拓展
    public class MinchoAbilityDefModExtension : DefModExtension
    {
        public CandyTypeDef? candyType = null;

        public int requiredMinchoCoreGrade = 0;

        public int requiredMinchoBodyGrade = 0;

        public float requiredMinchoCandyValue = 0f;

        public List<HediffDef> requiredHediffDefs = new List<HediffDef>();

        /// <summary>
        /// 检查该技能对指定 pawn 是否可用（核心等级、躯体等级、糖饰种类、所需 Hediff）。
        /// </summary>
        public bool IsAvailableFor(Pawn pawn)
        {
            if (pawn == null) return false;
            CompMinchoCore comp = pawn.GetComp<CompMinchoCore>();
            if (comp == null) return false;

            if (comp.MinchoCoreGrade < requiredMinchoCoreGrade) return false;
            if (comp.MinchoBodyGrade < requiredMinchoBodyGrade) return false;
            if (candyType != null && comp.CurrentCandyType != candyType) return false;
            if (!requiredHediffDefs.NullOrEmpty())
            {
                foreach (HediffDef hediffDef in requiredHediffDefs)
                {
                    if (!pawn.health.hediffSet.HasHediff(hediffDef)) return false;
                }
            }
            return true;
        }
    }
}
