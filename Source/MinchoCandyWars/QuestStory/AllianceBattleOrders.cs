using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace MinchoCandyWars.QuestStory;

public class LordJob_AllianceBattle : LordJob_AssaultColony
{
    public LordJob_AllianceBattle() { }
    public LordJob_AllianceBattle(Faction faction) : base(faction, canKidnap:false, canTimeoutOrFlee:false,
        sappers:false, useAvoidGridSmart:false, canSteal:false, breachers:false, canPickUpOpportunisticWeapons:false) { }
    public override bool AddFleeToil => false;
    public override StateGraph CreateGraph()
    {
        var graph = new StateGraph();
        graph.AddToil(new LordToil_AssaultColony());
        return graph;
    }
}

public class AllianceBattleOrders : MapComponent
{
    public const string FighterTag = "MCW_AllianceBattle";
    public AllianceBattleOrders(Map map) : base(map) { }
    public static bool IsFighter(Pawn pawn) => pawn?.Faction?.def == MCW_DefOf.MCW_Alliance_Faction && !pawn.IsPrisoner && pawn.GetLord()?.LordJob is not LordJob_ExitMapBest
        && (pawn.questTags?.Contains(FighterTag) == true || pawn.GetLord()?.LordJob is LordJob_AllianceBattle or LordJob_AssistColony);
    public override void MapComponentTick() { if (Find.TickManager.TicksGame % 120 == 0) Enforce(); }
    public void Enforce()
    {
        var pawns = map.mapPawns.AllPawnsSpawned.Where(p => IsFighter(p) && !p.Dead && !p.Downed).ToList();
        foreach (var pawn in pawns)
        {
            pawn.questTags ??= new List<string>();
            if (!pawn.questTags.Contains(FighterTag)) pawn.questTags.Add(FighterTag);
            // Old generated support squads may still hold the previously assigned charge rifles.
            if (pawn.RaceProps.Humanlike && (pawn.equipment?.Primary == null || !pawn.equipment.Primary.def.defName.StartsWith("MCW_")))
                AllianceBattleLoadout.Apply(pawn);
            if (pawn.MentalStateDef == MentalStateDefOf.PanicFlee || pawn.MentalStateDef == MCW_DefOf.GiveUpExit) pawn.MentalState.RecoverFromState();
            if (pawn.GetLord()?.LordJob is LordJob_AllianceBattle) continue;
            pawn.GetLord()?.Notify_PawnLost(pawn, PawnLostCondition.ForcedToJoinOtherLord);
            pawn.jobs.StopAll();
            var lord = map.lordManager.lords.FirstOrDefault(l => l.faction == pawn.Faction && l.LordJob is LordJob_AllianceBattle);
            if (lord != null) lord.AddPawn(pawn);
            else LordMaker.MakeNewLord(pawn.Faction, new LordJob_AllianceBattle(pawn.Faction), map, new[] { pawn });
        }
    }
}

[HarmonyPatch(typeof(MentalStateHandler),nameof(MentalStateHandler.TryStartMentalState))]
static class AllianceBattleNoPanic
{
    static bool Prefix(MentalStateDef stateDef, Pawn ___pawn)
        => !AllianceBattleOrders.IsFighter(___pawn) || stateDef != MentalStateDefOf.PanicFlee && stateDef != MCW_DefOf.GiveUpExit;
}

// These two old ThingDefs must remain readable until old saves no longer contain them.
public class LegacyWarMarker : Building { }
public class LegacyWarMarkerCleanup : MapComponent
{
    public LegacyWarMarkerCleanup(Map map) : base(map) { }
    public override void FinalizeInit() { base.FinalizeInit(); RemoveMarkers(); }
    public override void MapComponentTick() { if (Find.TickManager.TicksGame % 120 == 0) RemoveMarkers(); }
    public void RemoveMarkers()
    {
        foreach (var name in new[] { "MCW_RecyclePortal", "MCW_MinchoBeacon" })
        {
            var markers = map.listerThings.ThingsOfDef(DefDatabase<ThingDef>.GetNamed(name));
            for (int i = markers.Count - 1; i >= 0; i--) markers[i].Destroy(DestroyMode.Vanish);
        }
    }
}
