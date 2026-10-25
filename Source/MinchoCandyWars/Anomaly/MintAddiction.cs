using MinchoCandyWars;
using System;
using System.Linq;
using RimWorld;
using Verse;

namespace MinchoCandyWars.Anomaly;

/// <summary>
/// 薄荷冰淇淋依赖。
///
/// 除了继承化学需求，还负责"吃薄荷食品 = 满足、吃别的 = 难受"的记忆判定；
/// 同时提供几项被桶本体与戒断心情共用的珉巧判定。
/// </summary>
public class MintAddiction : Hediff_Addiction
{
    /// <summary>造成本次依赖的冰淇淋桶（被摧毁/读档丢失时为 null）。</summary>
    public Thing? source;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref source, "mintSource");
    }

    public override void Notify_IngestedThing(Thing food, int numTaken)
    {
        base.Notify_IngestedThing(food, numTaken);

        // 无尽桶的 numTaken 恒为 0（见 EndlessBucket.IngestedCalculateAmounts），
        // 这里实际不会被触发；保留是为了普通薄荷食品也能正确记账。
        if (IsMincho(pawn) || numTaken <= 0 || food.def.ingestible == null)
        {
            return;
        }

        if (IsMintFood(food))
        {
            Feed(pawn);
        }
        else if (food.def.IsNutritionGivingIngestible)
        {
            GiveMemory(pawn, MCW_DefOf.MCW_MintDisgust);
        }
    }

    /// <summary>把化学需求拉满，并给一份"薄荷冰淇淋的滋味"记忆。</summary>
    public static void Feed(Pawn p)
    {
        Need? need = p.needs?.TryGetNeed(MCW_DefOf.Chemical_MCW_Mint);
        if (need != null)
        {
            need.CurLevel = 1f;
        }

        GiveMemory(p, MCW_DefOf.MCW_MintDelight);
    }

    public static void GiveMemory(Pawn p, ThoughtDef memory) =>
        p.needs?.mood?.thoughts.memories.TryGainMemory(memory);

    /// <summary>珉巧本体，以及珉巧血肉类型的种族，都不受冰淇淋影响。</summary>
    public static bool IsMincho(Pawn p) =>
        p.def == MCW_DefOf.Mincho_ThingDef || p.RaceProps?.FleshType == MCW_DefOf.Mincho_FleshTypeDef;

    /// <summary>能解薄荷瘾的食品：无尽桶、名称带薄荷的 Mincho 食品、任何 Mincho 分类食品，或原料含上述任一项。</summary>
    public static bool IsMintFood(Thing food) =>
        IsMintDef(food.def) || food.TryGetComp<CompIngredients>()?.ingredients.Any(IsMintDef) == true;

    private static bool IsMintDef(ThingDef? d) =>
        d != null &&
        (d == MCW_DefOf.MCW_EndlessIceCream ||
         d == MCW_DefOf.MCW_EndlessIceCream_Released ||
         IsMinchoFood(d) ||
         (d.ingestible != null &&
          d.defName.StartsWith("Mincho_", StringComparison.Ordinal) &&
          d.defName.Contains("Mintchoco", StringComparison.OrdinalIgnoreCase)));

    /// <summary>thingCategories 里含有 Mincho_Food（或其子分类）的可食用食物。</summary>
    public static bool IsMinchoFood(ThingDef? def)
    {
        if (def?.ingestible == null || !def.IsNutritionGivingIngestible || def.IsDrug)
        {
            return false;
        }

        if (def.thingCategories == null)
        {
            return false;
        }

        foreach (ThingCategoryDef category in def.thingCategories)
        {
            for (ThingCategoryDef parent = category; parent != null; parent = parent.parent)
            {
                if (parent == MCW_DefOf.Mincho_Food)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
