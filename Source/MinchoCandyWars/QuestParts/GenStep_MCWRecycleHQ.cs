using RimWorld;
using RimWorld.Planet;
using Verse;

namespace MinchoCandyWars
{
    /// <summary>
    /// questFF1 决战据点「回收部队总部」的站点部件。
    /// 本身不做额外生成（据点由 GenStep_Outpost 构建），
    /// 传送点与珉巧信标由 GenStep_MCWRecycleHQ 放置。
    /// </summary>
    public class SitePartWorker_MCWRecycleHQ : SitePartWorker
    {
    }

    /// <summary>
    /// 决战据点地图生成步骤：在据点地图上放置
    ///   · 4 个回收部队传送点（四角/四边）
    ///   · 1 个珉巧援军信标（地图中心附近）
    /// </summary>
    public class GenStep_MCWRecycleHQ : GenStep
    {
        private const string PortalDefName = "MCW_RecyclePortal";
        private const string BeaconDefName = "MCW_MinchoBeacon";

        public override int SeedPart => 195731204;

        public override void Generate(Map map, GenStepParams parms)
        {
            ThingDef portal = DefDatabase<ThingDef>.GetNamed(PortalDefName, errorOnFail: false);
            ThingDef beacon = DefDatabase<ThingDef>.GetNamed(BeaconDefName, errorOnFail: false);

            // ── 4 个传送点：从地图中心向四个方向铺开 ──
            if (portal != null)
            {
                IntVec3 center = map.Center;
                int[] dx = { 1, -1, 0, 0 };
                int[] dz = { 0, 0, 1, -1 };

                for (int i = 0; i < 4; i++)
                {
                    IntVec3 cell = new IntVec3(
                        center.x + dx[i] * (map.Size.x / 3),
                        center.y,
                        center.z + dz[i] * (map.Size.z / 3));

                    cell = cell.InBounds(map) ? cell : map.Center;
                    if (!CellFinder.TryFindRandomCellNear(cell, map, 8, c => c.Standable(map), out IntVec3 found))
                    {
                        found = map.Center;
                    }

                    SpawnBuilding(portal, found, map);
                }
            }

            // ── 1 个珉巧信标 ──
            if (beacon != null)
            {
                IntVec3 cell = map.Center;
                if (!CellFinder.TryFindRandomCellNear(cell, map, 10, c => c.Standable(map), out IntVec3 found))
                {
                    found = map.Center;
                }
                SpawnBuilding(beacon, found, map);
            }
        }

        private static void SpawnBuilding(ThingDef def, IntVec3 cell, Map map)
        {
            Thing thing = ThingMaker.MakeThing(def);
            GenSpawn.Spawn(thing, cell, map);
        }
    }
}
