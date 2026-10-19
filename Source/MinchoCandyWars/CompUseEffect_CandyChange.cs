using RimWorld;
using Verse;

namespace MinchoCandyWars
{
    /// <summary>
    /// 「糖饰解锁物品」自身携带糖种信息。
    ///
    /// 之前 CompUseEffect_CandyChange.DoEffect 从 <c>parent.def</c>（ThingDef）上
    /// 取 <see cref="MinchoCandyChangeRecipeModExtension"/>，但那个扩展只写在
    /// RecipeDef 上，ThingDef 上并不存在 → GetModExtension 返回 null，
    /// 直接使用物品时必然空引用（表现为「无法植入」）。
    ///
    /// 改为专门的 CompProperties 承载 candyType：
    /// 类型安全、XML 一目了然，也不再和手术配方的扩展混用。
    /// </summary>
    public class CompProperties_UseEffect_CandyChange : CompProperties_UseEffect
    {
        /// <summary>使用该物品后要切换到的糖饰。</summary>
        public CandyTypeDef candyType = null!;

        public CompProperties_UseEffect_CandyChange()
        {
            compClass = typeof(CompUseEffect_CandyChange);
        }

        public override System.Collections.Generic.IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef))
            {
                yield return error;
            }
            if (candyType == null)
            {
                yield return parentDef.defName + ": CompProperties_UseEffect_CandyChange 缺少 candyType。";
            }
        }
    }

    /// <summary>
    /// 糖饰解锁物品被直接使用时的效果：把珉巧的当前糖饰切换为目标糖饰，并附加虚弱。
    /// </summary>
    public class CompUseEffect_CandyChange : CompUseEffect
    {
        private CompProperties_UseEffect_CandyChange CandyProps => (CompProperties_UseEffect_CandyChange)props;

        public override AcceptanceReport CanBeUsedBy(Pawn p)
        {
            CompMinchoCore? core = p.GetComp<CompMinchoCore>();
            if (core == null)
            {
                return new AcceptanceReport("MCW_NotMincho".Translate());
            }
            if (!core.Active)
            {
                return new AcceptanceReport("MCW_MinchoCoreNotActive".Translate());
            }
            if (p.health.hediffSet.HasHediff(MCW_DefOf.MCW_CandyChangeExhaustion))
            {
                return new AcceptanceReport("MCW_CandyChangeExhaustion".Translate());
            }
            return base.CanBeUsedBy(p);
        }

        public override void DoEffect(Pawn usedBy)
        {
            base.DoEffect(usedBy);

            CompMinchoCore? core = usedBy.GetComp<CompMinchoCore>();
            if (core == null)
            {
                return;
            }

            CandyTypeDef? target = CandyProps?.candyType;
            if (target == null)
            {
                Log.ErrorOnce($"[MCW] {parent.def.defName} 的 CompProperties_UseEffect_CandyChange 没有配置 candyType，无法切换糖饰。", parent.def.GetHashCode());
                return;
            }

            core.CurrentCandyType = target;
            usedBy.health.AddHediff(MCW_DefOf.MCW_CandyChangeExhaustion);
        }
    }
}
