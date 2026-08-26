using RimWorld;
using System.Collections.Generic;
using Verse;

namespace MinchoCandyWars
{
    /// <summary>
    /// 糖饰解锁手术配方的配置：指定解锁哪个糖饰。
    /// </summary>
    public class MinchoCandyUnlockRecipeModExtension : DefModExtension
    {
        public CandyTypeDef candyType = null!;
    }

    /// <summary>
    /// 糖饰解锁手术：成功后把对应 CandyTypeDef 注册到目标珉巧的 candyTypeDefsAccessible。
    /// </summary>
    public class Recipe_MinchoCandyUnlock : Recipe_Surgery
    {
        public MinchoCandyUnlockRecipeModExtension Ext => recipe.GetModExtension<MinchoCandyUnlockRecipeModExtension>();

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
            if (core == null || !core.Active)
            {
                return false;
            }
            if (Ext.candyType == null)
            {
                return false;
            }
            return !core.candyTypeDefsAccessible.Contains(Ext.candyType);
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

            if (Ext.candyType != null)
            {
                core.AddCandyTypeDefAccessible(Ext.candyType);
            }

            OnSurgerySuccess(pawn, part, billDoer, ingredients, bill);
        }
    }
}
