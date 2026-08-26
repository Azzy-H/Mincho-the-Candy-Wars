using RimWorld;
using System.Collections.Generic;
using Verse;

namespace MinchoCandyWars
{
    public enum MinchoBoosterType
    {
        Intel,
        CoreBasic,
        CoreAdvanced,
        Body
    }

    /// <summary>
    /// 改造包手术配方的配置：指定改造类型。
    /// </summary>
    public class MinchoBoosterRecipeModExtension : DefModExtension
    {
        public MinchoBoosterType boosterType;
    }

    /// <summary>
    /// 智慧珉巧改造包 / 核心改造包 / 躯体改造包的手术逻辑。
    /// 100% 成功率由 surgerySuccessChanceFactor 配置（99999 = 不会失败）。
    /// </summary>
    public class Recipe_MinchoBooster : Recipe_Surgery
    {
        public MinchoBoosterRecipeModExtension Ext => recipe.GetModExtension<MinchoBoosterRecipeModExtension>();

        public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
        {
            if (!base.AvailableOnNow(thing, part))
            {
                return false;
            }
            if (!(thing is Pawn pawn))
            {
                return false;
            }

            CompMinchoCore core = pawn.GetComp<CompMinchoCore>();
            if (core == null)
            {
                return false;
            }

            switch (Ext.boosterType)
            {
                case MinchoBoosterType.Intel:
                    // 普通珉巧 → 智慧珉巧
                    return !core.Active;
                case MinchoBoosterType.CoreBasic:
                    // 核心 1→2、2→3
                    return core.Active && core.MinchoCoreGrade >= 1 && core.MinchoCoreGrade <= 2;
                case MinchoBoosterType.CoreAdvanced:
                    // 核心 3→4、4→5
                    return core.Active && core.MinchoCoreGrade >= 3 && core.MinchoCoreGrade <= 4;
                case MinchoBoosterType.Body:
                    // 躯体 1→5
                    return core.Active && core.MinchoBodyGrade >= 1 && core.MinchoBodyGrade <= 4;
            }
            return false;
        }

        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            if (billDoer != null && CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
            {
                return;
            }

            CompMinchoCore core = pawn.GetComp<CompMinchoCore>();
            if (core == null)
            {
                return;
            }

            switch (Ext.boosterType)
            {
                case MinchoBoosterType.Intel:
                    core.Active = true;
                    break;
                case MinchoBoosterType.CoreBasic:
                case MinchoBoosterType.CoreAdvanced:
                    core.MinchoCoreGradeSet(core.MinchoCoreGrade + 1);
                    break;
                case MinchoBoosterType.Body:
                    core.MinchoCoreBodySet(core.MinchoBodyGrade + 1);
                    break;
            }

            OnSurgerySuccess(pawn, part, billDoer, ingredients, bill);
        }
    }
}
