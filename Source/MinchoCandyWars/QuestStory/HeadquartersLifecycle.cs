using HarmonyLib;
using RimWorld.Planet;
using Verse;
namespace MinchoCandyWars.QuestStory;

public class HeadquartersLifecycle : MapComponent
{
    public bool PlayerEntered;
    public HeadquartersLifecycle(Map map) : base(map) { }
    public static bool IsHeadquarters(MapParent parent) => parent is Site site && site.parts.Any(p => p.def == MCW_DefOf.MCW_RecycleHQ);
    public void ObservePlayer() { if (!PlayerEntered && IsHeadquarters(map.Parent) && map.mapPawns.AnyPawnBlockingMapRemoval) PlayerEntered = true; }
    public override void MapComponentTick() => ObservePlayer();
    public override void ExposeData() { base.ExposeData(); Scribe_Values.Look(ref PlayerEntered,"headquartersPlayerEntered"); }
}
[HarmonyPatch(typeof(Site), nameof(Site.ShouldRemoveMapNow))]
static class HeadquartersDeparture
{
    static bool Prefix(Site __instance, ref bool __result, ref bool __0)
    {
        if (!HeadquartersLifecycle.IsHeadquarters(__instance) || __instance.Map == null) return true;
        var map = __instance.Map;
        var lifecycle = map.GetComponent<HeadquartersLifecycle>();
        lifecycle.ObservePlayer();
        if (!lifecycle.PlayerEntered) return true;
        // Use the native blocker check: colonists, player mechs and held/transported pawns must be safe.
        __result = !map.mapPawns.AnyPawnBlockingMapRemoval;
        __0 = __result;
        return false;
    }
}
