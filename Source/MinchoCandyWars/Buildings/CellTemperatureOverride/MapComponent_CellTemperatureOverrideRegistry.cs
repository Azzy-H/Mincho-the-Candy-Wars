using System.Collections.Generic;
using Verse;

namespace MinchoCandyWars.Buildings
{
    public class MapComponent_CellTemperatureOverrideRegistry : MapComponent
    {
        public static Dictionary<Map, MapComponent_CellTemperatureOverrideRegistry> Dic = new Dictionary<Map, MapComponent_CellTemperatureOverrideRegistry>();
        public static MapComponent_CellTemperatureOverrideRegistry Get(Map map)
        {
            if (!Dic.TryGetValue(map, out MapComponent_CellTemperatureOverrideRegistry? comp))
            {
                comp = map.GetComponent<MapComponent_CellTemperatureOverrideRegistry>();
                Dic[map] = comp;
            }
            return comp;
        }
        private readonly Dictionary<IntVec3, List<CompCellTemperatureOverride>> overridesByCell = new Dictionary<IntVec3, List<CompCellTemperatureOverride>>();

        public MapComponent_CellTemperatureOverrideRegistry(Map map) : base(map)
        {
        }
        public override void ExposeData()
        {
            base.ExposeData();
            if(Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                Dic.Clear();
            }
        }

        public void Register(IntVec3 cell, CompCellTemperatureOverride comp)
        {
            if (!overridesByCell.TryGetValue(cell, out List<CompCellTemperatureOverride> list))
            {
                list = new List<CompCellTemperatureOverride>();
                overridesByCell[cell] = list;
            }

            if (!list.Contains(comp))
            {
                list.Add(comp);
            }
        }

        public void Deregister(IntVec3 cell, CompCellTemperatureOverride comp)
        {
            if (!overridesByCell.TryGetValue(cell, out List<CompCellTemperatureOverride> list))
            {
                return;
            }

            list.Remove(comp);
            if (list.Count == 0)
            {
                overridesByCell.Remove(cell);
            }
        }

        public bool TryGetTemperatureOverride(IntVec3 cell, out float temperature)
        {
            temperature = 0f;
            if (!overridesByCell.TryGetValue(cell, out List<CompCellTemperatureOverride> list) || list.Count == 0)
            {
                return false;
            }

            for (int i = list.Count - 1; i >= 0; i--)
            {
                CompCellTemperatureOverride comp = list[i];
                if (comp?.parent == null || comp.parent.DestroyedOrNull() || !comp.parent.Spawned)
                {
                    list.RemoveAt(i);
                    continue;
                }

                temperature = comp.Props.temperature;
                return true;
            }

            overridesByCell.Remove(cell);
            return false;
        }
    }
}
