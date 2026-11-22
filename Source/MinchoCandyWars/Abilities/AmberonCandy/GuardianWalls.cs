using HarmonyLib;
using Verse;
using RimWorld;
namespace MinchoCandyWars.Abilities.AmberonCandy;

// 只登记技能新生成的原版珉巧墙体；玩家建造的墙不登记、不计时。
public sealed class GuardianWalls : MapComponent
{
    private Dictionary<Thing,int> expiry = new();
    private List<Thing>? walls;
    private List<int>? times;
    public GuardianWalls(Map map) : base(map) { }
    public void Register(Thing wall) => expiry[wall] = Find.TickManager.TicksGame + 10000;
    public int Remaining(Thing wall) => expiry.TryGetValue(wall,out int end) ? Math.Max(0,end-Find.TickManager.TicksGame) : -1;
    public override void MapComponentTick()
    {
        if (expiry.Count == 0 || Find.TickManager.TicksGame % 60 != 0) return;
        foreach (var entry in expiry.ToArray())
        {
            if (entry.Key.Destroyed) expiry.Remove(entry.Key);
            else if (Find.TickManager.TicksGame >= entry.Value)
            {
                expiry.Remove(entry.Key);
                entry.Key.Destroy(DestroyMode.Vanish);
            }
        }
    }
    public override void ExposeData()
    {
        Scribe_Collections.Look(ref expiry,"guardianWallExpiry",LookMode.Reference,LookMode.Value,ref walls,ref times);
        if (Scribe.mode == LoadSaveMode.PostLoadInit) expiry ??= new();
    }
}
[HarmonyPatch(typeof(ThingWithComps),nameof(ThingWithComps.GetInspectString))]
static class GuardianWallInspect
{
    static void Postfix(ThingWithComps __instance,ref string __result)
    {
        if (__instance.def != MCW_ThingDefOf.Mincho_MintchocoWall || __instance.Map == null) return;
        int remaining=__instance.Map.GetComponent<GuardianWalls>().Remaining(__instance);
        if(remaining>=0) __result += (__result.NullOrEmpty()?"":"\n")+"MinchoCandyWars.UI.GuardianWall.Lifetime".Translate(remaining.ToStringTicksToPeriod()).RawText;
    }
}


