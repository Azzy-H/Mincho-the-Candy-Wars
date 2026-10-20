using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MinchoCandyWars.Buildings
{
    /// <summary>
    /// 格子温度覆盖：把建筑影响的格子温度恒定覆盖为指定值。
    ///
    /// 两种作用范围（由 affectWorkTables 决定）：
    ///   · affectWorkTables = false（默认）：只覆盖建筑**自身占据的格子** ——
    ///     用于珉巧床：躺在这张床上的人（正好位于床格）才会被恒温保护。
    ///   · affectWorkTables = true：覆盖建筑所在**房间内所有工作台**的
    ///     工作台格子 + 互动格（pawn 工作时站的位置）——
    ///     用于饼干操作臂：让工作台前的 pawn 读取到正常温度，
    ///     而**不影响**房间其它格子（不会把冷库整体变暖）。
    ///
    /// affectWorkTables=true 时会定期重新注册，以跟随工作台的增减与房间结构变化。
    /// </summary>
    public class CompProperties_CellTemperatureOverride : CompProperties
    {
        public CompProperties_CellTemperatureOverride()
        {
            this.compClass = typeof(CompCellTemperatureOverride);
        }

        /// <summary>温度覆盖值（摄氏度）。</summary>
        public float temperature = 0f;

        /// <summary>
        /// 是否覆盖房间内所有工作台（工作台格子 + 互动格）。
        /// false（默认）= 只覆盖建筑自身占据的格子。
        /// </summary>
        public bool affectWorkTables = false;
    }

    public class CompCellTemperatureOverride : ThingComp
    {
        public CompProperties_CellTemperatureOverride Props => (CompProperties_CellTemperatureOverride)this.props;

        private readonly List<IntVec3> registeredCells = new List<IntVec3>();

        /// <summary>重新注册的检测间隔（tick）。</summary>
        private const int RefreshInterval = 250;

        private int ticksToRefresh;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            RegisterToMap(parent.Map);
            ticksToRefresh = RefreshInterval;
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            DeregisterFromMap(map);
            base.PostDeSpawn(map, mode);
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            DeregisterFromMap(previousMap);
            base.PostDestroy(mode, previousMap);
        }

        public override void CompTick()
        {
            base.CompTick();

            // 只覆盖自身格子的建筑（如珉巧床）不需要刷新
            if (!Props.affectWorkTables)
            {
                return;
            }

            if (--ticksToRefresh > 0)
            {
                return;
            }
            ticksToRefresh = RefreshInterval;

            if (!parent.Spawned || parent.Map == null)
            {
                return;
            }

            // 工作台可能被建造/拆除/移动，房间也可能变化 —— 定期重新注册
            RegisterToMap(parent.Map);
        }

        private void RegisterToMap(Map map)
        {
            if (map == null || !parent.Spawned)
            {
                return;
            }

            DeregisterFromMap(map);

            foreach (IntVec3 cell in AffectedCells())
            {
                map.GetComponent<MapComponent_CellTemperatureOverrideRegistry>().Register(cell, this);
                registeredCells.Add(cell);
            }
        }

        /// <summary>
        /// 本建筑温度覆盖影响的格子：
        ///   affectWorkTables=true → 房间内所有工作台的工作台格子 + 互动格；
        ///   否则 → 仅建筑自身占据的格子。
        /// </summary>
        private IEnumerable<IntVec3> AffectedCells()
        {
            if (!Props.affectWorkTables)
            {
                foreach (IntVec3 cell in parent.OccupiedRect())
                {
                    yield return cell;
                }
                yield break;
            }

            Room room = parent.GetRoom();
            if (room != null)
            {
                foreach (Thing thing in room.ContainedAndAdjacentThings)
                {
                    if (thing is Building_WorkTable table && !table.Destroyed && table.Spawned)
                    {
                        yield return table.Position;

                        IntVec3 interaction = table.InteractionCell;
                        if (interaction.IsValid)
                        {
                            yield return interaction;
                        }
                    }
                }
                yield break;
            }

            // 操作臂不在房间里（户外/无屋顶）时退化为自身格子
            foreach (IntVec3 cell in parent.OccupiedRect())
            {
                yield return cell;
            }
        }

        private void DeregisterFromMap(Map map)
        {
            if (map == null)
            {
                registeredCells.Clear();
                return;
            }

            MapComponent_CellTemperatureOverrideRegistry registry = map.GetComponent<MapComponent_CellTemperatureOverrideRegistry>();
            for (int i = 0; i < registeredCells.Count; i++)
            {
                registry.Deregister(registeredCells[i], this);
            }
            registeredCells.Clear();
        }
    }
}
