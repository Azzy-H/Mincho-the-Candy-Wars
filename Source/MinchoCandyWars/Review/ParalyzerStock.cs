using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace MinchoCandyWars.Review;

/// <summary>瘫痪枪的商人库存：太空时代以上的派系商人有 5% 概率备一把。</summary>
public class ParalyzerStock : StockGenerator
{
    private const float StockChance = 0.05f;

    public override bool HandlesThingDef(ThingDef def) => def == MCW_DefOf.MCW_Paralyzer;

    public override IEnumerable<Thing> GenerateThings(PlanetTile tile, Faction? faction = null)
    {
        if (faction != null
            && faction.def.techLevel >= TechLevel.Spacer
            && Rand.Value < StockChance)
        {
            yield return ThingMaker.MakeThing(MCW_DefOf.MCW_Paralyzer);
        }
    }
}

/// <summary>
/// 启动时把所有太空时代以上派系的商人都挂上瘫痪枪库存。
///
/// 这一段没法用 XML patch 表达：xpath 不能跨 Def 关联，
/// 而"哪些 TraderKindDef 属于太空时代派系"必须遍历 FactionDef.baseTraderKinds 才知道，
/// 用 XML 只能把 TraderKindDef 的 defName 一个个写死。
/// </summary>
[StaticConstructorOnStartup]
public static class ParalyzerStockSetup
{
    static ParalyzerStockSetup()
    {
        foreach (FactionDef faction in DefDatabase<FactionDef>.AllDefsListForReading)
        {
            if (faction.techLevel < TechLevel.Spacer) continue;
            if (faction.baseTraderKinds == null) continue;

            foreach (TraderKindDef trader in faction.baseTraderKinds)
            {
                if (trader?.stockGenerators == null) continue;

                // 已经有生成器认领瘫痪枪就不再重复添加（XML、其它 mod 或重复初始化都可能加过）。
                if (trader.stockGenerators.Any(generator => generator.HandlesThingDef(MCW_DefOf.MCW_Paralyzer)))
                {
                    continue;
                }

                var stock = new ParalyzerStock();
                stock.ResolveReferences(trader);
                trader.stockGenerators.Add(stock);
            }
        }
    }
}