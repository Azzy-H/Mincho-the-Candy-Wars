using RimWorld;
using UnityEngine;
using Verse;

namespace MinchoCandyWars.Buildings;

public class Building_IceCreamConversionBeacon : Building_Storage
{
    private const float Radius = 5.9f;
    private const int Interval = GenDate.TicksPerHour;
    private int nextConversionTick;
    private Zone_Stockpile? feedStockpile;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref nextConversionTick, "nextConversionTick");
        Scribe_References.Look(ref feedStockpile, "feedStockpile");
    }

    public override void TickRare()
    {
        base.TickRare();
        int now = Find.TickManager.TicksGame;
        if (!Spawned || now < nextConversionTick || this.TryGetComp<CompFlickable>()?.SwitchIsOn != true)
            return;

        nextConversionTick = now + Interval;
        var map = Map;
        if (feedStockpile != null && !feedStockpile.Cells.Any(cell => map.zoneManager.ZoneAt(cell) == feedStockpile))
            feedStockpile = null;
        var input = GenRadial.RadialCellsAround(Position, Radius, true)
            .Where(cell => cell.InBounds(map) && (feedStockpile == null ||
                map.zoneManager.ZoneAt(cell) == feedStockpile))
            .SelectMany(cell => cell.GetThingList(map))
            .FirstOrDefault(thing => CanConvert(thing.def)
                && !thing.IsForbidden(Faction.OfPlayer)
                && !map.reservationManager.IsReserved(thing));

        if (input == null)
            return;

        var flavors = new[] { MCW_ThingDefOf.Mincho_Mintchoco, MCW_ThingDefOf.Mincho_BluePolarBear, MCW_ThingDefOf.Mincho_PinkFallInLove };
        var contents = Position.GetThingList(map).Where(thing => thing.def.category == ThingCategory.Item).ToList();
        var available = flavors.Where(flavor => flavor != null && GetStoreSettings().AllowedToAccept(flavor)
            && (contents.Count < def.building.maxItemsInCell || contents.Any(t => t.def == flavor && t.stackCount < flavor.stackLimit))).ToList();
        if (available.Count == 0)
            return;
        var outputDef = available.RandomElement();

        if (GenPlace.TryPlaceThing(ThingMaker.MakeThing(outputDef), Position, map, ThingPlaceMode.Direct))
            input.SplitOff(1).Destroy();
    }

    public override void DrawExtraSelectionOverlays()
    {
        base.DrawExtraSelectionOverlays();
        GenDraw.DrawRadiusRing(Position, Radius);
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var gizmo in base.GetGizmos())
            yield return gizmo;

        if (Spawned && Faction == Faction.OfPlayer)
            yield return new Command_Action
            {
                defaultLabel = "MinchoCandyWars.Beacon.Create".Translate(),
                defaultDesc = "MinchoCandyWars.Beacon.Description".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Designators/ZoneCreate_Stockpile"),
                action = CreateFeedStockpile
            };
    }

    private bool CanCreateFeedStockpileAt(IntVec3 cell)
    {
        return cell != Position && cell.InBounds(Map) && cell.Walkable(Map)
            && Map.zoneManager.ZoneAt(cell) == null
            && StoreUtility.GetSlotGroup(cell, Map) == null
            && !cell.GetThingList(Map).Any(thing => !thing.def.CanOverlapZones);
    }

    private static bool CanConvert(ThingDef thingDef)
    {
        return thingDef.category == ThingCategory.Item && !thingDef.destroyOnDrop
            && (thingDef.IsCorpse || thingDef.IsApparel || thingDef.IsWeapon);
    }

    private void CreateFeedStockpile()
    {
        if (feedStockpile != null && feedStockpile.Cells.Any(cell => Map.zoneManager.ZoneAt(cell) == feedStockpile))
        {
            Find.Selector.ClearSelection();
            Find.Selector.Select(feedStockpile);
            return;
        }
        feedStockpile = null;

        var cells = GenRadial.RadialCellsAround(Position, Radius, true)
            .Where(CanCreateFeedStockpileAt).ToList();
        if (cells.Count == 0)
        {
            Messages.Message("MinchoCandyWars.Beacon.NoSpace".Translate(), MessageTypeDefOf.RejectInput, false);
            return;
        }

        var stockpile = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, Map.zoneManager);
        stockpile.label = "MinchoCandyWars.Beacon.Stockpile".Translate();
        stockpile.settings.filter.SetDisallowAll();
        foreach (var thingDef in DefDatabase<ThingDef>.AllDefsListForReading)
            if (CanConvert(thingDef))
                stockpile.settings.filter.SetAllow(thingDef, true);
        stockpile.settings.Priority = StoragePriority.Low;
        Map.zoneManager.RegisterZone(stockpile);
        feedStockpile = stockpile;
        foreach (var cell in cells)
            stockpile.AddCell(cell);
        Find.Selector.ClearSelection();
        Find.Selector.Select(stockpile);
    }
}
