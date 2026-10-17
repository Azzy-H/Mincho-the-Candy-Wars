using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace MinchoCandyWars.Buildings
{
    public class CompProperties_CellTemperatureOverride : CompProperties
    {
        public CompProperties_CellTemperatureOverride()
        {
            this.compClass = typeof(CompCellTemperatureOverride);
        }

        public float temperature = 0f;

        /// <summary>
        /// 额外覆盖半径（格）。0（默认）= 仅覆盖建筑自身占据的格子（原有行为）。
        /// 大于 0 时以建筑中心为圆心覆盖半径内的所有格子，
        /// 用于「让附近工作台不受低温影响」这类需求（如饼干操作臂）。
        /// </summary>
        public float radius = 0f;
    }

    public class CompCellTemperatureOverride : ThingComp
    {
        public CompProperties_CellTemperatureOverride Props => (CompProperties_CellTemperatureOverride)this.props;

        private readonly List<IntVec3> registeredCells = new List<IntVec3>();

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            RegisterToMap(parent.Map);
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

        private void RegisterToMap(Map map)
        {
            if (map == null || !parent.Spawned)
            {
                return;
            }

            DeregisterFromMap(map);
            foreach (IntVec3 cell in AffectedCells(map))
            {
                map.GetComponent<MapComponent_CellTemperatureOverrideRegistry>().Register(cell, this);
                registeredCells.Add(cell);
            }
        }

        /// <summary>本建筑温度覆盖影响的格子（radius &lt;= 0 时仅自身占据的格子）。</summary>
        private IEnumerable<IntVec3> AffectedCells(Map map)
        {
            if (Props.radius <= 0f)
            {
                foreach (IntVec3 cell in parent.OccupiedRect())
                {
                    yield return cell;
                }
                yield break;
            }

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(parent.Position, Props.radius, true))
            {
                if (cell.InBounds(map))
                {
                    yield return cell;
                }
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
