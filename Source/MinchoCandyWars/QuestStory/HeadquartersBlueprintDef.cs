using RimWorld;
using Verse;
namespace MinchoCandyWars.QuestStory;

// Data exported from the user's headquarters save; only buildings, materials, floors and roofs.
public class HeadquartersBlueprintDef : Def
{
    public List<HeadquartersBuilding> buildings = new();
    public List<HeadquartersCell> cells = new();
    public void Generate(Map map, Faction faction)
    {
        var center = map.Center;
        IntVec3 At(int x, int z) => center + new IntVec3(x, 0, z);
        var clear = new HashSet<IntVec3>(cells.Select(c => At(c.x, c.z)));
        foreach (var entry in buildings)
        {
            var def = DefDatabase<ThingDef>.GetNamed(entry.thing);
            foreach (var cell in GenAdj.OccupiedRect(At(entry.x, entry.z), new Rot4(entry.rotation), def.size)) clear.Add(cell);
        }
        if (clear.Any(c => !c.InBounds(map))) throw new InvalidOperationException("Map is too small for the headquarters blueprint.");
        void Clear(IntVec3 cell)
        {
            foreach (var thing in cell.GetThingList(map).ToList())
                if (thing is Building || thing is Plant || thing.def.category == ThingCategory.Item)
                { if (thing.def.destroyable) thing.Destroy(DestroyMode.Vanish); else thing.DeSpawn(); }
            map.roofGrid.SetRoof(cell, null);
            map.terrainGrid.SetTerrain(cell, TerrainDefOf.Concrete);
            map.terrainGrid.RemoveTempTerrain(cell, preventDestroyEffects: true);
        }
        foreach (var cell in clear) Clear(cell);
        foreach (var entry in cells)
        {
            var cell = At(entry.x, entry.z);
            map.terrainGrid.SetTerrain(cell, DefDatabase<TerrainDef>.GetNamed(entry.terrain));
            if (entry.roof) map.roofGrid.SetRoof(cell, RoofDefOf.RoofConstructed);
        }
        foreach (var entry in buildings)
        {
            var def = DefDatabase<ThingDef>.GetNamed(entry.thing);
            var stuff = def == ThingDefOf.Wall ? ThingDefOf.Plasteel : def.MadeFromStuff ? DefDatabase<ThingDef>.GetNamed(entry.stuff) : null;
            var thing = ThingMaker.MakeThing(def, stuff);
            if (def.CanHaveFaction) thing.SetFaction(faction);
            GenSpawn.Spawn(thing, At(entry.x, entry.z), map, new Rot4(entry.rotation));
        }
        // The same five-cell approaches continue from the user's perimeter to every map edge.
        foreach (var direction in GenAdj.CardinalDirections)
            for (int distance = HeadquartersLayout.Radius + 1; ; distance++)
            {
                var middle = center + direction * distance;
                if (!middle.InBounds(map)) break;
                for (int side = -2; side <= 2; side++)
                {
                    var cell = middle + new IntVec3(direction.z * side, 0, direction.x * side);
                    if (cell.InBounds(map)) Clear(cell);
                }
            }
        // Connect all equipment to buried trunks, outside the later portal-clearing pads.
        var wires = new HashSet<IntVec3>();
        for (int x = -56; x <= 56; x++) foreach (int z in new[] { -8, 8 }) wires.Add(At(x,z));
        var equipment = map.listerThings.AllThings.OfType<Building>().Where(t => t.Faction == faction && t.TryGetComp<CompPowerTrader>() != null).ToList();
        foreach (var building in equipment)
        {
            int trunk = building.Position.z >= center.z ? center.z + 8 : center.z - 8;
            for (int z = Math.Min(building.Position.z, trunk); z <= Math.Max(building.Position.z, trunk); z++) wires.Add(new IntVec3(building.Position.x,0,z));
        }
        for (int z = -8; z <= 8; z++) wires.Add(At(0,z));
        var conduit = MCW_ThingDefOf.HiddenConduit;
        foreach (var cell in wires)
            if (cell.InBounds(map) && !cell.GetThingList(map).Any(t => t.TryGetComp<CompPower>()?.Props.transmitsPower == true))
            { var wire = ThingMaker.MakeThing(conduit); wire.SetFaction(faction); GenSpawn.Spawn(wire,cell,map); }
        map.powerNetManager.UpdatePowerNetsAndConnections_First();
        foreach (var plant in equipment.Select(b => b.TryGetComp<CompPowerPlant>()).OfType<CompPowerPlant>()) plant.UpdateDesiredPowerOutput();
        if (!map.gameConditionManager.ElectricityDisabled(map))
            foreach (var consumer in equipment.Select(b => b.TryGetComp<CompPowerTrader>()).OfType<CompPowerTrader>())
                if (consumer.PowerNet?.CanPowerNow(consumer) == true) consumer.PowerOn = true;
    }
}
public class HeadquartersBuilding { public string thing = "", stuff = ""; public int x, z, rotation; }
public class HeadquartersCell { public int x,z; public string terrain = ""; public bool roof; }
