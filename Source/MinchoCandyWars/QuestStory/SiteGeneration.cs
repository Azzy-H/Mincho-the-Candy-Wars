#nullable disable
using HarmonyLib;
using MinchoCandyWars;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace MinchoCandyWars.QuestStory;

public class MissionBuildings : GenStep
{
    public int kind;
    public override int SeedPart => 1010100 + kind;
    public override void Generate(Map map, GenStepParams parms)
    {
        var faction = Find.FactionManager.FirstFactionOfDef(DefDatabase<FactionDef>.GetNamed(kind == 5 ? "MCW_Alliance_Faction" : "MCW_RecycleCorp_Faction"));
        EnsureWar();
        if (kind == 6) HeadquartersLayout.Generate(map, faction);
        else new GenStep_Outpost { size = 20, settlementDontGeneratePawns = true, overrideFaction = faction, generateLoot = true, allowGeneratingFarms = false }.Generate(map, parms);
        if (kind == 5) return; // Residents and 38+2 attackers come from the reviewed generator.
        int n = MissionObjective.For(map)?.Scale ?? CampaignPolicy.GuardScale(Find.AnyPlayerHomeMap?.wealthWatcher.WealthTotal ?? 0);
        if (kind == 6)
        {
            int strength = MissionObjective.For(map).FinalScale;
            // 外圈8N、内圈两翼预备队4N，核心2N领队；总兵力仍为14N。
            foreach (int x in new[] { -24, 24 }) foreach (int z in new[] { -24, 24 })
                Spawn(map, faction, "MCW_RecycleCorp_Member", 2 * strength, false, map.Center + new IntVec3(x, 0, z));
            foreach (int x in new[] { -12, 12 })
                Spawn(map, faction, "MCW_RecycleCorp_Member", 2 * strength, false, map.Center + new IntVec3(x, 0, 0));
            Spawn(map, faction, "MCW_RecycleCorp_Leader", 2 * strength, false, map.Center,
                new LordJob_DefendPoint(map.Center, wanderRadius: 5, defendRadius: 8, addFleeToil: false));
        }
        else
        {
            Spawn(map, faction, "MCW_RecycleCorp_Member", kind == 3 ? 5 * n : kind == 9 ? 2 * n : n, false);
            Spawn(map, faction, "MCW_RecycleCorp_Leader", kind == 3 ? 3 : 1, false);
        }
        if (kind == 6)
        {
            var computer = ThingMaker.MakeThing(MCW_ThingDefOf.MCWQ101_ContactComputer);
            computer.SetFaction(faction);
            bool Fits(IntVec3 c) => GenAdj.OccupiedRect(c, Rot4.North, computer.def.size).Cells.All(p => p.InBounds(map) && p.Walkable(map) && p.GetEdifice(map) == null);
            var cell = map.Center;
            if (!Fits(cell) || !cell.Roofed(map)) throw new InvalidOperationException("总部中央控制室不能放置联络电脑");
            GenSpawn.Spawn(computer, cell, map, WipeMode.VanishOrMoveAside);
            var objective = MissionObjective.For(map); if (objective != null) objective.Computer = computer;
        }
    }
    public static void EnsureWar()
    {
        var faction = Find.FactionManager.FirstFactionOfDef(MCW_DefOf.MCW_RecycleCorp_Faction);
        var alliance = Find.FactionManager.FirstFactionOfDef(MCW_DefOf.MCW_Alliance_Faction);
        if (faction.HostileTo(alliance)) return;
        if (faction.HasGoodwill && alliance.HasGoodwill)
        {
            // 两个有好感度的派系不能调用SetRelationDirect；永久敌对状态也会拒绝TryAffectGoodwill。
            faction.RelationWith(alliance).baseGoodwill = -100;
            alliance.RelationWith(faction).baseGoodwill = -100;
            faction.Notify_GoodwillSituationsChanged(alliance, false, "MinchoCandyWars.Reason.AllianceCorpWar".Translate().RawText, null);
        }
        else faction.SetRelationDirect(alliance, FactionRelationKind.Hostile, false);
    }
    public static List<Pawn> Spawn(Map map, Faction faction, string kind, int count, bool edge, IntVec3? rally = null, LordJob orders = null)
    {
        var pawns = new List<Pawn>();
        var def = DefDatabase<PawnKindDef>.GetNamed(kind);
        bool battleAlly = edge && faction.def == MCW_DefOf.MCW_Alliance_Faction;
        IntVec3 origin = rally ?? (edge && CellFinder.TryFindRandomEdgeCellWith(c => c.Standable(map) && !c.Fogged(map), map, CellFinder.EdgeRoadChance_Neutral, out var cell) ? cell : map.Center);
        // Edge arrivals must remain on the actual border, including waves with a rally anchor.
        var entries = edge ? CellRect.WholeMap(map).EdgeCells
            .Where(c => c.Standable(map)).OrderBy(c => c.DistanceToSquared(origin))
            .Where(c => map.reachability.CanReach(c, map.Center, Verse.AI.PathEndMode.OnCell,
                TraverseParms.For(TraverseMode.PassDoors))).Take(12).ToList() : null;
        if (edge && entries.Count == 0) return pawns;
        for (int i = 0; i < count; i++)
        {
            var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(def, faction, forceGenerateNewPawn: true, canGeneratePawnRelations: faction.def != MCW_DefOf.MCW_RecycleCorp_Faction, mustBeCapableOfViolence: battleAlly, fixedBiologicalAge: battleAlly && def.race.race.Humanlike ? Rand.Range(20f,35f) : null));
            if (faction.def == MCW_DefOf.MCW_Alliance_Faction) AllianceBelief.Apply(pawn);
            if (battleAlly) { AllianceBattleLoadout.Apply(pawn); pawn.questTags ??= new List<string>(); pawn.questTags.Add(AllianceBattleOrders.FighterTag); }
            if (pawn.GetComp<CompMinchoCore>() is { Active: false } core) core.Active = true;
            GenSpawn.Spawn(pawn, edge ? entries[i % entries.Count] : CellFinder.RandomClosewalkCellNear(origin, map, rally.HasValue ? 6 : 12), map); pawns.Add(pawn);
        }
        if (pawns.Count > 0)
        {
            bool support = edge && faction.def == MCW_DefOf.MCW_Alliance_Faction;
            var lord = support ? map.lordManager.lords.FirstOrDefault(l => l.faction == faction && l.LordJob is LordJob_AllianceBattle) : null;
            if (lord != null) foreach (var pawn in pawns) lord.AddPawn(pawn);
            else LordMaker.MakeNewLord(faction, HeadquartersLifecycle.IsHeadquarters(map.Parent) && faction.def == MCW_DefOf.MCW_RecycleCorp_Faction ? new LordJob_CompanyGuard(origin) : orders ?? (support ? new LordJob_AllianceBattle(faction) : edge ? new LordJob_AssaultColony(faction) : new LordJob_DefendPoint(origin)), map, pawns);
        }
        return pawns;
    }
}
public class ContactComputer : Building
{
    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        var objective = Spawned ? MissionObjective.For(Map) : null;
        // Deconstruction, map removal and despawning are not combat destruction.
        if (objective?.Computer == this && mode == DestroyMode.KillFinalize) objective.ComputerKilled = true;
        base.Destroy(mode);
    }
}
[HarmonyPatch(typeof(Comp_WarBeacon), nameof(Comp_WarBeacon.CompTick))]
static class HeadquartersBeacon
{
    static bool Prefix(Comp_WarBeacon __instance)
    {
        // Deprecated physical markers are retained only to read existing saves.
        return false;
    }
}
[HarmonyPatch(typeof(GenStep_AncientComplex), nameof(GenStep_AncientComplex.Generate))]
static class BetaRuinLayoutFallback
{
    static void Prefix(Map map, out int __state)
    {
        __state = MissionObjective.For(map)?.Kind == 4 ? BuildingCount(map) : -1;
    }

    static void Postfix(Map map, int __state)
    {
        if (__state < 0 || BuildingCount(map) > __state) return;

        bool Fits(IntVec3 center) => CellRect.CenteredOn(center, 3).Cells.All(cell =>
            cell.InBounds(map) && cell.Standable(map) && cell.GetEdifice(map) == null &&
            cell.GetTerrain(map).affordances.Contains(TerrainAffordanceDefOf.Heavy));

        if (!CellFinder.TryFindRandomCellNear(map.Center, map, 24, Fits, out var site))
            site = map.AllCells.FirstOrDefault(Fits, IntVec3.Invalid);
        if (!site.IsValid) return;

        var wall = ThingDefOf.Wall;
        foreach (var cell in CellRect.CenteredOn(site, 2).EdgeCells)
        {
            // Openings keep the ruined chamber accessible without a door.
            if (cell.x == site.x || cell.z == site.z) continue;
            GenSpawn.Spawn(ThingMaker.MakeThing(wall, ThingDefOf.Steel), cell, map);
        }
        foreach (var cell in CellRect.CenteredOn(site, 2).Cells)
            map.terrainGrid.SetTerrain(cell, TerrainDefOf.Concrete);
    }

    static int BuildingCount(Map map) => map.listerThings.AllThings.OfType<Building>().Count();
}

public class BetaSampleStep : GenStep
{
    public override int SeedPart => 101048;
    public override void Generate(Map map, GenStepParams parms)
    {
        var objective = MissionObjective.For(map);
        if (objective?.Kind != 4) return;
        var sample = ThingMaker.MakeThing(MCW_ThingDefOf.MCW_Beta_NanoShapingFluid);
        sample.questTags = ["MCWQ101_Sample"];
        bool CanPlace(IntVec3 cell) => cell.Standable(map) && cell.GetEdifice(map) == null;
        if (!CellFinder.TryFindRandomCellNear(map.Center, map, 15, CanPlace, out var near) ||
            !GenPlace.TryPlaceThing(sample, near, map, ThingPlaceMode.Direct))
        {
            var fallback = map.AllCells.FirstOrDefault(CanPlace, IntVec3.Invalid);
            if (!fallback.IsValid ||
                !GenPlace.TryPlaceThing(sample, fallback, map, ThingPlaceMode.Direct))
            {
                sample.Destroy();
                Log.Error("[MCW] Unable to place the beta nanoshaping fluid sample on the ruin map.");
                objective.quest.End(QuestEndOutcome.Fail);
                return;
            }
        }
        objective.Sample = sample;
        if (Rand.Chance(0.3f) && ModsConfig.BiotechActive)
        {
            var pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.AncientSoldier);
            var brain = pawn.health.hediffSet.GetBrain();
            pawn.health.AddHediff(HediffDefOf.MechlinkImplant, brain); pawn.Kill(null);
            if (pawn.Corpse != null) GenPlace.TryPlaceThing(pawn.Corpse, map.Center, map, ThingPlaceMode.Near);
        }
    }
}
[HarmonyPatch(typeof(Thing), nameof(Thing.CanStackWith))]
static class KeepSampleIdentity
{
    static bool Prefix(Thing __instance, Thing other, ref bool __result)
    {
        if (__instance.questTags?.Contains("MCWQ101_Sample") != true && other.questTags?.Contains("MCWQ101_Sample") != true) return true;
        __result = false; return false;
    }
}
[HarmonyPatch(typeof(SitePartWorker_MCWAllianceOutpost), nameof(SitePartWorker_MCWAllianceOutpost.Notify_GeneratedByQuestGen))]
static class AllianceResidents
{
    static void Postfix(RimWorld.Planet.SitePart part)
    {
        var faction = Find.FactionManager.FirstFactionOfDef(MCW_DefOf.MCW_Alliance_Faction);
        foreach (var pawn in part.things.OfType<Pawn>()) pawn.SetFaction(faction);
    }
}
[HarmonyPatch(typeof(GenStep_MCWAllianceOutpost), nameof(GenStep_MCWAllianceOutpost.Generate))]
static class DefenseOrders
{
    static void Postfix(Map map)
    {
        if (MissionObjective.For(map)?.Kind != 5) return;
        MissionBuildings.EnsureWar();
        foreach (string def in new[] { "MCW_Alliance_Faction", "MCW_RecycleCorp_Faction" })
        {
            var faction = Find.FactionManager.FirstFactionOfDef(DefDatabase<FactionDef>.GetNamed(def));
            var pawns = map.mapPawns.AllPawnsSpawned.Where(p => p.Faction == faction && !p.IsPrisoner && p.GetLord() == null).ToList();
            if (pawns.Count > 0) LordMaker.MakeNewLord(faction, def == "MCW_Alliance_Faction" ? new LordJob_DefendPoint(map.Center) : new LordJob_AssaultColony(faction), map, pawns);
        }
    }
}





