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
/// 把所有太空时代以上派系的商人挂上瘫痪枪库存。
///
/// 为什么不能用 [StaticConstructorOnStartup] 直接跑：
/// 判断「是否已有生成器认领瘫痪枪」需要调用 StockGenerator.HandlesThingDef，
/// 而原版 StockGenerator_Tomes.HandlesThingDef 内部会访问
/// Find.Storyteller.difficulty.AnomalyPlaystyleDef。
/// 在 Def 加载阶段 Storyteller / difficulty 还没有创建，
/// 一碰就是 NullReferenceException，并且会中断整个静态构造，导致库存根本没加进去。
///
/// 因此改成 GameComponent：
///   · Game.FillComponents() 会自动实例化所有 GameComponent 子类；
///   · FinalizeInit() 只在游戏（含 Storyteller）就绪后才被调用；
///   · 每次新游戏 / 读档都会再跑一次，靠 HandlesThingDef 判重保持幂等。
/// </summary>
public class GameComponent_ParalyzerStockSetup : GameComponent
{
    public GameComponent_ParalyzerStockSetup(Game game)
    {
    }

    public override void FinalizeInit()
    {
        base.FinalizeInit();
        ParalyzerStockSetup.Apply();
    }
}

/// <summary>
/// 实际执行体。拆出来是为了让 GameComponent 可以在合适的时机调用。
///
/// 这一段没法用 XML patch 表达：xpath 不能跨 Def 关联，
/// 而「哪些 TraderKindDef 属于太空时代派系」必须遍历 FactionDef.baseTraderKinds 才知道，
/// 用 XML 只能把 TraderKindDef 的 defName 一个个写死。
/// </summary>
public static class ParalyzerStockSetup
{
    public static void Apply()
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
