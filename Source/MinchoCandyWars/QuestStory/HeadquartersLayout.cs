#nullable disable
using RimWorld;
using Verse;

namespace MinchoCandyWars.QuestStory;

// Original ground-base layout. The reference is GD5's central objective and
// layered approaches, not its prefab coordinates, assets or orbital mechanics.
public static class HeadquartersLayout
{
    public const int Radius = 41;
    public static IntVec3 ReinforcementEntry(Map map, int slot)
    {
        var direction = GenAdj.CardinalDirections[slot % 4];
        return new IntVec3(direction.x == 0 ? map.Center.x : direction.x > 0 ? map.Size.x - 1 : 0,
            0, direction.z == 0 ? map.Center.z : direction.z > 0 ? map.Size.z - 1 : 0);
    }
    public static void Generate(Map map, Faction faction)
    {
        var blueprint = DefDatabase<HeadquartersBlueprintDef>.GetNamedSilentFail("MCW_UserHeadquarters");
        if (blueprint != null) { blueprint.Generate(map, faction); return; }
        var center = map.Center;
        IntVec3 At(int x, int z) => center + new IntVec3(x, 0, z);
        var bounds = CellRect.CenteredOn(center, Radius * 2 + 1, Radius * 2 + 1);
        if (!CellRect.CenteredOn(center, 123, 113).Cells.All(c => c.InBounds(map)))
            throw new InvalidOperationException("总部地图不足以放置基地与外侧附楼");
        void Clear(CellRect area)
        {
            foreach (var cell in area)
            {
                foreach (var thing in cell.GetThingList(map).ToList())
                    if (thing is Plant || thing is Building)
                    {
                        if (thing.def.destroyable) thing.Destroy(DestroyMode.Vanish);
                        else thing.DeSpawn(); // 地热喷口不可Destroy；基地清场仅移出地图。
                    }
                map.roofGrid.SetRoof(cell, null);
                map.terrainGrid.SetTerrain(cell, TerrainDefOf.Concrete);
                map.terrainGrid.RemoveTempTerrain(cell, preventDestroyEffects: true); // 原生薄冰独立于地板，铺地不会自动移除。
            }
        }
        Clear(bounds);
        void Place(string name, IntVec3 cell, ThingDef stuff = null, Rot4? rotation = null)
        {
            var def = DefDatabase<ThingDef>.GetNamed(name);
            var thing = ThingMaker.MakeThing(def, def.MadeFromStuff ? stuff ?? ThingDefOf.Steel : null);
            thing.SetFaction(faction);
            GenSpawn.Spawn(thing, cell, map, rotation ?? Rot4.North);
        }
        // Broad entry lanes keep the heavy guns useful and allow flanking.
        foreach (var cell in bounds.EdgeCells)
        {
            var delta = cell - center;
            bool open = delta.z < 0 ? Math.Abs(delta.x) <= 22 : Math.Abs(delta.x) <= 12;
            if (Math.Abs(delta.z) <= 8 || open) continue;
            Place("Wall", cell);
        }
        void Room(int x, int z, int width, int height, bool core = false)
        {
            var rect = CellRect.CenteredOn(At(x, z), width, height);
            foreach (var cell in rect)
            {
                foreach (var item in cell.GetThingList(map).Where(t => t.def.category == ThingCategory.Item).ToList())
                    item.Destroy(DestroyMode.Vanish); // 新建室内先清走天然碎石等散落物，家具在后面布置。
                map.terrainGrid.SetTerrain(cell, TerrainDefOf.MetalTile);
                map.roofGrid.SetRoof(cell, RoofDefOf.RoofConstructed);
            }
            foreach (var cell in rect.EdgeCells)
            {
                bool gate = (cell.x == At(x,z).x && (cell.z == rect.minZ || cell.z == rect.maxZ)) ||
                    (cell.z == At(x,z).z && (cell.x == rect.minX || cell.x == rect.maxX));
                Place(gate ? "Door" : "Wall", cell, core ? ThingDefOf.Plasteel : ThingDefOf.Steel);
            }
        }
        Room(0, 0, 17, 17, true);
        Room(0, 24, 27, 13); Room(0, -24, 27, 13);
        Room(24, 0, 13, 27); Room(-24, 0, 13, 27);
        foreach (int x in new[] { -30, 30 }) foreach (int z in new[] { -30, 30 })
        {
            // 每个开放炮位两门铀弹、两门加农；间距十格，避免同组重炮连爆。
            foreach (int side in new[] { -1, 1 })
            {
                Place("Turret_Sniper", At(x + side * 5, z + side * 5));
                Place("Turret_Autocannon", At(x + side * 5, z - side * 5));
                Place("Barricade", At(x + side * 5, z - 8));
            }
        }
        // 外圈走廊近防，补重炮的最小射程盲区。
        foreach (int x in new[] { -16, 16 }) foreach (int z in new[] { -32, 32 })
        {
            Place("Turret_MiniTurret", At(x, z));
            Place("Turret_MiniTurret", At(z, x));
        }
        int strength = MissionObjective.For(map)?.FinalScale ?? 4;
        if (strength >= 7)
        {
            foreach (var direction in GenAdj.CardinalDirections) Place("Turret_Sniper", center + direction * 35);
            foreach (int x in new[] { -16, 16 }) foreach (int z in new[] { -16, 16 }) Place("Turret_Autocannon", At(x, z));
        }
        if (strength >= 9)
            foreach (int x in new[] { -20, 20 }) foreach (int z in new[] { -34, 34 })
            {
                Place("Turret_MiniTurret", At(x, z));
                Place("Turret_MiniTurret", At(z, x));
            }
        // 核心四个入口及四角有独立近防，侧袭必须先压制火力。
        foreach (var direction in GenAdj.CardinalDirections) Place("Turret_MiniTurret", center + direction * 12);
        foreach (int x in new[] { -11, 11 }) foreach (int z in new[] { -11, 11 }) Place("Turret_MiniTurret", At(x, z));
        foreach (int x in new[] { -4, 4 }) foreach (int z in new[] { -4, 4 }) Place("Column", At(x, z));
        // 四座分散机房共24个能源核心，为最高规模炮塔和附楼提供余量；仍使用可破坏的真实电网。
        foreach (var direction in GenAdj.CardinalDirections)
            foreach (int x in new[] { -3, 3 }) foreach (int z in new[] { -3, 0, 3 })
                Place("UnstablePowerCell", center + direction * 24 + new IntVec3(x, 0, z));
        foreach (int x in new[] { -5, 5 }) foreach (int z in new[] { -5, 5 }) Place("StandingLamp", At(x, z));
        // 原炮位不挪动：外侧附楼承载生活/生产区，内圈保留交叉火力与侧袭通路。
        foreach (int x in new[] { -51, 51 }) foreach (int z in new[] { -23, 23 })
        {
            Clear(CellRect.CenteredOn(At(x, z), 21, 25));
            Room(x, z, 15, 19);
            Place("StandingLamp", At(x + 4, z));
        }
        foreach (int x in new[] { -51, 51 })
        {
            Clear(CellRect.CenteredOn(At(x, 0), 7, 27));
            foreach (var cell in CellRect.CenteredOn(At(x, 0), 3, 27))
                map.terrainGrid.SetTerrain(cell, TerrainDefOf.PavedTile);
        }
        // 长楼中央仍是分散能源间，左右/上下隔开设备与值班区；门对齐原通道。
        foreach (int z in new[] { -24, 24 }) foreach (int x in new[] { -5, 5 })
            for (int n = -5; n <= 5; n++) Place(n == 0 ? "Door" : "Wall", At(x, z + n));
        foreach (int x in new[] { -24, 24 }) foreach (int z in new[] { -5, 5 })
            for (int n = -5; n <= 5; n++) Place(n == 0 ? "Door" : "Wall", At(x + n, z));
        foreach (var direction in GenAdj.CardinalDirections) Place("StandingLamp", center + direction * 24 + new IntVec3(4, 0, 4));
        // 中央控制室：机架与显示台贴内墙，四门和守卫巡逻圈保持通畅。
        foreach (int x in new[] { -6, 6 }) Place("AncientSystemRack", At(x, 4));
        foreach (int x in new[] { -4, 4 }) Place("AncientDisplayBank", At(x, -6));
        foreach (int x in new[] { -9, 9 })
        {
            Place("AncientSystemRack", At(x, 22));
            Place("AncientDisplayBank", At(x, 28));
            Place("AncientLockerBank", At(x, -28));
            Place("Table1x2c", At(x, -23));
            Place("DiningChair", At(x - 1, -23), rotation: Rot4.East);
            Place("DiningChair", At(x + 1, -23), rotation: Rot4.West);
        }
        Place("AncientStove", At(-11, -20)); Place("AncientKitchenSink", At(-8, -20));
        Place("AncientRefrigerator", At(9, -20));
        foreach (int x in new[] { -24, 24 }) foreach (int z in new[] { -10, 10 })
        {
            Place("Shelf", At(x - 2, z)); Place("ToolCabinet", At(x + 2, z));
            Place("AncientLockerBank", At(x, z > 0 ? 7 : -7));
        }
        // 西北拆解车间；西南宿舍；东北仓库；东南医务室。
        Place("AncientMachine", At(-51, 27)); Place("TableMachining", At(-51, 18));
        Place("TableStonecutter", At(-55, 23)); Place("ToolCabinet", At(-47, 26));
        foreach (int x in new[] { -56, -53, -49, -46 }) foreach (int z in new[] { -28, -18 })
            Place("Bed", At(x, z));
        foreach (int x in new[] { 47, 50, 54 }) foreach (int z in new[] { 18, 22, 28 })
            Place("Shelf", At(x, z));
        foreach (int x in new[] { 47, 54 }) foreach (int z in new[] { -28, -18 })
            Place("HospitalBed", At(x, z));
        Place("Shelf", At(50, -29)); Place("AncientLockerBank", At(51, -16));
        Place("Table1x2c", At(51, -23)); Place("DiningChair", At(50, -23), rotation: Rot4.East);
        // 正面货场留中央宽进攻路；废料是可见的回收业务痕迹，不堆在炮口与核心旁。
        foreach (int side in new[] { -1, 1 })
        {
            Clear(CellRect.CenteredOn(At(side * 26, -49), 19, 13));
            Place("AncientLargeContainer", At(side * 30, -49));
            Place("AncientLargeCrate", At(side * 21, -47));
            Place("AncientLongCrate", At(side * 24, -51));
            Place("AncientBarrel", At(side * 20, -52));
            Place("AncientLargeRustedEngineBlock", At(side * 27, -45));
            foreach (int z in new[] { -8, -5, 5, 8 })
                Place("AncientSmallCrate", At(side * 39, z));
        }
        for (int n = -40; n <= 40; n++) for (int side = -1; side <= 1; side++)
            foreach (var cell in new[] { At(n, side), At(side, n) })
                if (!cell.Roofed(map)) map.terrainGrid.SetTerrain(cell, TerrainDefOf.PavedTile);
        foreach (var offset in new[] { new IntVec3(-54,0,25), new IntVec3(-48,0,20), new IntVec3(48,0,26), new IntVec3(54,0,19), new IntVec3(-23,0,-50), new IntVec3(22,0,-48) })
            GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.Filth_Dirt), center + offset, map);
        // Clear five-cell approaches all the way to the map edge, including water,
        // rock and temporary ice. Keep the interior defensive rooms intact.
        foreach (var direction in GenAdj.CardinalDirections)
            for (int n = Radius; ; n++)
            {
                var middle = center + direction * n;
                if (!middle.InBounds(map)) break;
                for (int side = -2; side <= 2; side++)
                {
                    var cell = middle + new IntVec3(direction.z * side, 0, direction.x * side);
                    if (cell.InBounds(map)) Clear(CellRect.SingleCell(cell));
                }
            }
        var wires = new HashSet<IntVec3>();
        // 绕过随后清场的传送点平台，避免生成时亮着、首轮电网重算后附楼断电。
        for (int x = -55; x <= 55; x++) foreach (int z in new[] { -8, 8 }) wires.Add(At(x, z));
        for (int n = -35; n <= 35; n++)
        {
            wires.Add(At(n, 0)); wires.Add(At(0, n));
            if (Math.Abs(n) <= 30)
            { wires.Add(At(n, -30)); wires.Add(At(n, 30)); wires.Add(At(-30, n)); wires.Add(At(30, n)); }
        }
        foreach (var building in map.listerBuildings.allBuildingsNonColonist.Where(b => b.TryGetComp<CompPowerTrader>() != null))
        {
            foreach (var cell in building.OccupiedRect()) wires.Add(cell);
            // 炮位与机房支线都连续接到干线，不能留下孤立发电机。
            for (int z = Math.Min(building.Position.z, center.z); z <= Math.Max(building.Position.z, center.z); z++)
                wires.Add(new IntVec3(building.Position.x, 0, z));
        }
        foreach (var cell in wires)
            if (!cell.GetThingList(map).Any(t => t.TryGetComp<CompPower>()?.Props.transmitsPower == true))
                Place("HiddenConduit", cell);
        map.powerNetManager.UpdatePowerNetsAndConnections_First();
        // 这是已经运作的敌军总部；按真实电网余量初始化开机，战斗中仍受断电和EMP影响。
        foreach (var plant in map.listerBuildings.allBuildingsNonColonist.Select(b => b.TryGetComp<CompPowerPlant>()).OfType<CompPowerPlant>())
            plant.UpdateDesiredPowerOutput();
        if (!map.gameConditionManager.ElectricityDisabled(map))
            foreach (var consumer in map.listerBuildings.allBuildingsNonColonist.Select(b => b.TryGetComp<CompPowerTrader>()).OfType<CompPowerTrader>())
                if (consumer.PowerNet?.CanPowerNow(consumer) == true) consumer.PowerOn = true;
    }
}

// Preserve the four native portal definitions, but avoid wiping the computer
// or a wall when the later generation step places portals and the allied beacon.
public class HeadquartersPortals : GenStep
{
    public override int SeedPart => 107107;
    public override void Generate(Map map, GenStepParams parms)
    {
        void Clear(IntVec3 cell)
        {
            if (!cell.InBounds(map)) return;
            foreach (var thing in cell.GetThingList(map).ToList())
                if (thing is Plant || thing is Building)
                {
                    if (thing.def.destroyable) thing.Destroy(DestroyMode.Vanish);
                    else thing.DeSpawn(); // 地热喷口不可Destroy；基地清场仅移出地图。
                }
            map.roofGrid.SetRoof(cell, null); map.terrainGrid.SetTerrain(cell, TerrainDefOf.Concrete);
            map.terrainGrid.RemoveTempTerrain(cell, preventDestroyEffects: true);
        }
        foreach (var direction in GenAdj.CardinalDirections)
        {
            // Keep the existing approach corridors; reinforcements arrive in drop pods.
            var origin = map.Center + direction * (HeadquartersLayout.Radius + 7);
            for (int n = HeadquartersLayout.Radius; n <= HeadquartersLayout.Radius + 7; n++)
                for (int side = -1; side <= 1; side++)
                    Clear(map.Center + direction * n + new IntVec3(direction.z * side, 0, direction.x * side));
            foreach (var cell in CellRect.CenteredOn(origin, 7, 7)) Clear(cell);
        }
    }
}




