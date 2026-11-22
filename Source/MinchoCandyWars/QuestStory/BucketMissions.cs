#nullable disable
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;
using Verse.AI;

namespace MinchoCandyWars.QuestStory;

public static class BucketMissions
{
    public static bool Available(int kind) => kind switch
    {
        12 => ModsConfig.OdysseyActive && (ModsConfig.RoyaltyActive || ModsConfig.IdeologyActive)
            && Find.Storyteller.difficulty.allowViolentQuests
            && DefDatabase<ThingDef>.GetNamedSilentFail("MCW_BucketShuttle") != null,
        13 => ModsConfig.AnomalyActive && DefDatabase<ThingDef>.GetNamedSilentFail("MCW_EndlessIceCream") != null
            && (Story.Current.LastBucketCargo < 0 || Find.TickManager.TicksGame - Story.Current.LastBucketCargo >= 240 * 60000),
        _ => true
    };
    public static void Rewards(Quest quest, int kind)
    {
        var plan = RewardPlan.Build(quest, kind == 12 ? 40 : 20, false, kind == 12 ? 4 : 2);
        quest.AddPart(new AllianceSettlement { Kind = kind, Plan = plan });
        plan.AttachPreview(quest, true); quest.parent = Story.Current.Series;
    }
}

// Same native sequence as ShuttleCrash_Rescue: crash, escort, raid, rescue ship,
// boarding deadline, then success only when that ship actually leaves satisfied.
public class BucketCrashRoot : QuestNode
{
    protected override bool TestRunInt(Slate slate) => CanStart(QuestGen_Get.GetMap());

    internal static bool CanStart(Map map)
    {
        if (map == null || !BucketMissions.Available(12)) return false;
        var alliance = Find.FactionManager.FirstFactionOfDef(MCW_DefOf.MCW_Alliance_Faction);
        var wreck = MCW_ThingDefOf.MCWQ105_BucketWreck;
        var enemy = Find.FactionManager.FirstFactionOfDef(MCW_DefOf.MCW_RecycleCorp_Faction);
        return alliance != null && enemy != null && DropCellFinder.FindSafeLandingSpot(out _, alliance, map, 35, 15, 25, wreck.size, wreck.interactionCellOffset);
    }
    protected override void RunInt()
    {
        var quest = QuestGen.quest; var map = QuestGen_Get.GetMap();
        var alliance = Find.FactionManager.FirstFactionOfDef(MCW_DefOf.MCW_Alliance_Faction);
        var enemy = Find.FactionManager.FirstFactionOfDef(MCW_DefOf.MCW_RecycleCorp_Faction);
        var wreckDef = MCW_ThingDefOf.MCWQ105_BucketWreck;
        if (alliance == null || enemy == null || !DropCellFinder.FindSafeLandingSpot(out var cell, alliance, map, 35, 15, 25, wreckDef.size, wreckDef.interactionCellOffset))
            throw new InvalidOperationException("没有适合飞行器迫降的安全落点");
        var passengers = new List<Pawn>();
        for (int i = 0, count = Rand.RangeInclusive(2, 4); i < count; i++)
        {
            var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(MCW_DefOf.Mincho_SpaceRefugee, alliance, forceGenerateNewPawn: true, fixedGender: Gender.Female, canGeneratePawnRelations: false));
            Find.WorldPawns.PassToWorld(pawn); QuestGen.AddToGeneratedPawns(pawn);
            QuestUtility.AddQuestTag(ref pawn.questTags, QuestGenUtility.HardcodedTargetQuestTagWithQuestID("passengers")); passengers.Add(pawn);
        }
        var wreck = ThingMaker.MakeThing(wreckDef);
        var rescue = QuestGen_Shuttle.GenerateShuttle(alliance, passengers, shuttleDef: DefDatabase<ThingDef>.GetNamed("MCW_BucketShuttle"));
        rescue.TryGetComp<CompRefuelable>()?.Refuel(1000);
        QuestGen.slate.Set("map", map); QuestGen.slate.Set("passengers", passengers); QuestGen.slate.Set("rescueShuttle", rescue);
        string Signal(string suffix) => QuestGenUtility.HardcodedSignalWithQuestID(suffix);
        QuestUtility.AddQuestTag(ref rescue.questTags, QuestGenUtility.HardcodedTargetQuestTagWithQuestID("rescueShuttle"));
        quest.Delay(120, () =>
        {
            quest.SpawnSkyfaller(map, MCW_ThingDefOf.MCWQ105_BucketCrashing, new[] { wreck }, Faction.OfPlayer, cell);
            // The wreck explodes on impact (150-200 ticks). Drop the survivors only
            // after that impact, so their arrival cannot race the crash blast.
            quest.Delay(600, () =>
            {
            quest.DropPods(map.Parent, passengers, dropSpot: cell, sendStandardLetter: false);
            quest.WaitForEscort(map.Parent, passengers, alliance, cell + IntVec3.South, addFleeToil: false);
            quest.ExitOnShuttle(map.Parent, passengers, alliance, rescue, Signal("rescueShuttle.Spawned"), addFleeToil: false);
            quest.ShuttleDelay(20000, passengers, () =>
            {
                var ship = quest.GenerateTransportShip(DefDatabase<TransportShipDef>.GetNamed("MCW_BucketShuttle_Ship"), null, rescue).transportShip;
                quest.SendTransportShipAwayOnCleanup(ship);
                if (!DropCellFinder.TryFindDropSpotNear(cell, map, out var landing, allowFogged: false,
                        canRoofPunch: false, allowIndoors: false, rescue.def.size + new IntVec2(2, 2)) &&
                    !DropCellFinder.FindSafeLandingSpot(out landing, alliance, map, 35, 15, 25,
                        rescue.def.size, rescue.def.interactionCellOffset))
                {
                    landing = IntVec3.Invalid; // Let the native ship arrival job resolve its default landing cell.
                }
                quest.AddShipJob_Arrive(ship, map.Parent, null, landing, ShipJobStartMode.Instant, alliance);
                quest.AddShipJob_WaitTime(ship, 30000, true, passengers.Cast<Thing>().ToList());
                quest.ShuttleLeaveDelay(rescue, 30000);
                quest.AddShipJob_FlyAway(ship, null, null, TransportShipDropMode.None);
                quest.Letter(LetterDefOf.NeutralEvent, text: "MinchoCandyWars.Mission.ShuttleBody".Translate().RawText, label: "MinchoCandyWars.Mission.ShuttleTitle".Translate().RawText, lookTargets: new[] { rescue });
            }, alert: true);
            quest.Delay(10000, () =>
            {
                var parms = new PawnGroupMakerParms { faction = enemy, groupKind = PawnGroupKindDefOf.Combat, tile = map.Tile, points = Math.Max(150, QuestGen.slate.Get("points", 300f) * 0.37f) };
                parms.points = Math.Max(parms.points, enemy.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat, parms));
                var attackers = PawnGroupMakerUtility.GeneratePawns(parms).ToList();
                foreach (var pawn in attackers) { Find.WorldPawns.PassToWorld(pawn); QuestGen.AddToGeneratedPawns(pawn); }
                quest.AddPart(new QuestPart_PawnsArrive { pawns = attackers, arrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn, mapParent = map.Parent, inSignal = QuestGen.slate.Get<string>("inSignal"), sendStandardLetter = true });
                quest.AssaultThings(map.Parent, attackers, enemy, passengers);
            }, debugLabel: "MCWBucketRaid");
            }, debugLabel: "MCWBucketPassengers");
        });
        quest.End(QuestEndOutcome.Success, inSignal: Signal("rescueShuttle.SentSatisfied"));
        foreach (string suffix in new[] { "rescueShuttle.SentUnsatisfied", "rescueShuttle.Destroyed", "rescueShuttle.LeftBehind", "passengers.Destroyed", "map.MapRemoved" })
        {
            quest.Leave(passengers, Signal(suffix), sendStandardLetter: false);
            quest.End(QuestEndOutcome.Fail, inSignal: Signal(suffix));
        }
        BucketMissions.Rewards(quest, 12);
    }
}

// The bucket is an anomalous item, like the original cube cargo, not a pawn
// that can be bound to an entity holding platform. Its original effects remain.
public class BucketCargoRoot : QuestNode
{
    protected override bool TestRunInt(Slate slate) => BucketMissions.Available(13) && QuestGen_Get.GetMap() != null;
    protected override void RunInt()
    {
        var quest = QuestGen.quest; var map = QuestGen_Get.GetMap();
        var bucket = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("MCW_EndlessIceCream"));
        quest.Delay(120, () => quest.DropPods(map.Parent, new[] { bucket }, customLetterLabel: "MinchoCandyWars.Mission.BucketTitle".Translate().RawText, customLetterText: "MinchoCandyWars.Mission.BucketBody".Translate().RawText, useTradeDropSpot: true, destroyItemsOnCleanup: false, dropAllInSamePod: true));
        quest.AddPart(new BucketReceipt { Bucket = bucket, Destination = map.Parent, RewardAt = Find.TickManager.TicksGame + Rand.RangeInclusive(2500, 7500) });
        BucketMissions.Rewards(quest, 13);
    }
}
public class BucketReceipt : QuestPart
{
    public Thing Bucket;
    public MapParent Destination;
    public int RewardAt;
    public bool Received;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref Bucket, "bucket");
        Scribe_References.Look(ref Destination, "destination");
        Scribe_Values.Look(ref RewardAt, "rewardAt");
        Scribe_Values.Look(ref Received, "received");
    }

    public static void RecordArrival(Thing bucket, Map map)
    {
        foreach (var quest in Find.QuestManager.QuestsListForReading)
        {
            if (quest.State != QuestState.Ongoing) continue;
            foreach (var receipt in quest.PartsListForReading.OfType<BucketReceipt>())
                if (receipt.Bucket == bucket && receipt.Destination?.Map == map)
                    receipt.Received = true;
        }
    }

    public static void RecordReplacement(Thing original, Thing replacement, Map map)
    {
        foreach (var active in Find.QuestManager.QuestsListForReading)
        {
            if (active.State != QuestState.Ongoing) continue;
            foreach (var receipt in active.PartsListForReading.OfType<BucketReceipt>())
            {
                if (receipt.Bucket != original) continue;
                receipt.Received |= receipt.Destination?.Map == map;
                receipt.Bucket = replacement;
            }
        }
    }
    public void Check()
    {
        if (quest.State != QuestState.Ongoing || Find.TickManager.TicksGame % 60 != 0) return;
        if (!Received && Bucket is { Destroyed: false } && Destination?.HasMap == true && Bucket.MapHeld == Destination.Map)
        {
            // A held item counts once unloaded. An incoming pod still has to open.
            Received = Bucket.Spawned || Bucket.ParentHolder is Pawn_CarryTracker or Pawn_InventoryTracker;
        }
        if (!Received && (Bucket == null || Bucket.Destroyed || Destination?.HasMap != true))
        {
            quest.End(QuestEndOutcome.Fail);
            return;
        }
        if (Find.TickManager.TicksGame < RewardAt) return;
        if (Received) quest.End(QuestEndOutcome.Success);
        else if (Find.TickManager.TicksGame >= RewardAt + 2500) quest.End(QuestEndOutcome.Fail);
    }
}
