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
            foreach (IntVec3 cell in parent.OccupiedRect())
            {
                map.GetComponent<MapComponent_CellTemperatureOverrideRegistry>().Register(cell, this);
                registeredCells.Add(cell);
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
