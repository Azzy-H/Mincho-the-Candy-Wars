using RimWorld;
using Verse;

namespace MinchoCandyWars
{
    /// <summary>
    /// questB4 的站点地图生成步骤：在远古复合体遗迹里放置 β 型纳米塑形液。
    ///
    /// 通过 GenStepDef 的 linkWithSite 挂到原版 AncientComplex 站点部件上，
    /// 因此不需要复制整份 AncientComplex 定义 —— 遗迹本体仍由原版生成器构建，
    /// 我们只负责额外塞入本 mod 的任务物品。
    ///
    /// XML 里可覆盖 thingDef / count；默认放置 1 个 MCW_Beta_NanoShapingFluid。
    /// </summary>
    public class GenStep_MCW_BetaFluid : GenStep
    {
        private const string BetaFluidDefName = "MCW_Beta_NanoShapingFluid";

        /// <summary>要放置的物品（为空时使用 MCW_Beta_NanoShapingFluid）。</summary>
        public ThingDef thingDef;

        /// <summary>放置数量。</summary>
        public int count = 1;

        public override int SeedPart => 857139201;

        public override void Generate(Map map, GenStepParams parms)
        {
            ThingDef def = thingDef ?? DefDatabase<ThingDef>.GetNamed(BetaFluidDefName, errorOnFail: false);
            if (def == null)
            {
                Log.Warning("[MCW] GenStep_MCW_BetaFluid: 找不到 ThingDef " + BetaFluidDefName);
                return;
            }

            if (count <= 0)
            {
                return;
            }

            // 放在地图中心附近的可行走格子上（遗迹内部）
            IntVec3 cell = CellFinder.RandomClosewalkCellNear(map.Center, map, 15);
            if (!cell.IsValid)
            {
                cell = map.Center;
            }

            Thing thing = ThingMaker.MakeThing(def);
            thing.stackCount = count;
            GenPlace.TryPlaceThing(thing, cell, map, ThingPlaceMode.Near);
        }
    }
}
