#nullable disable
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;
namespace MinchoCandyWars.QuestStory;

public class LordJob_CompanyLastStand : LordJob_AssaultColony
{
    public LordJob_CompanyLastStand() { }
    public LordJob_CompanyLastStand(Faction faction) : base(faction, canKidnap: false, canTimeoutOrFlee: false,
        sappers: false, useAvoidGridSmart: false, canSteal: false, breachers: false, canPickUpOpportunisticWeapons: false) { }
    public override bool AddFleeToil => false; // Lord.SetJob otherwise injects its own casualty retreat transitions.
    public override StateGraph CreateGraph()
    {
        // The vanilla assault graph can still panic after major casualties. No retreat,
        // kidnapping or looting transitions exist here; victory explicitly assigns ExitMap.
        var graph = new StateGraph();
        graph.AddToil(new LordToil_AssaultColony());
        return graph;
    }

}
public class LordJob_CompanyGuard : LordJob_DefendPoint
{
    public IntVec3 Post;
    public LordJob_CompanyGuard() { }
    public LordJob_CompanyGuard(IntVec3 post) : base(post, wanderRadius: 5, defendRadius: 12, addFleeToil: false) { Post = post; }
    public override bool AddFleeToil => false;
    public override void ExposeData() { base.ExposeData(); Scribe_Values.Look(ref Post, "companyGuardPost"); }
}
public class HeadquartersCombat : MapComponent
{
    public const string FighterTag = "MCW_FinaleFighter";
    public int StartedAt = -1, NextHomeRaid = -1, NextHeadquartersDrop = -1;
    public HeadquartersCombat(Map map) : base(map) { }
    private MissionObjective cachedObjective;
    private MissionObjective Objective
    {
        get
        {
            if (cachedObjective == null || cachedObjective.Site != map.Parent || cachedObjective.quest.State != QuestState.Ongoing)
                cachedObjective = MissionObjective.For(map);
            return cachedObjective;
        }
    }
    public static bool IsActive(Map map) => HeadquartersLifecycle.IsHeadquarters(map.Parent) && !Story.Current.Victory
        && map.GetComponent<HeadquartersCombat>().Objective is { ComputerKilled: false };
    public static bool IsFighter(Pawn pawn) => pawn?.Faction?.def == MCW_DefOf.MCW_RecycleCorp_Faction
        && !pawn.Faction.defeated && !Story.Current.Victory && !pawn.IsPrisoner
        && (pawn.questTags?.Contains(FighterTag) == true || pawn.Map != null && IsActive(pawn.Map));
    public override void ExposeData()
    {
        base.ExposeData(); Scribe_Values.Look(ref StartedAt,"companyAttackStarted",-1);
        Scribe_Values.Look(ref NextHomeRaid,"companyNextHomeRaid",-1);
        Scribe_Values.Look(ref NextHeadquartersDrop,"companyNextHeadquartersDrop",-1);
    }
    public override void MapComponentTick()
    {
        int now = Find.TickManager.TicksGame;
        if (now % 60 == 0) EnforceOrders();
        if (!IsActive(map)) return;
        map.GetComponent<HeadquartersLifecycle>().ObservePlayer();
        if (!map.GetComponent<HeadquartersLifecycle>().PlayerEntered) return;
        if (NextHeadquartersDrop < 0) NextHeadquartersDrop = now + 2500;
        if (now >= NextHeadquartersDrop)
        {
            var defenders = Find.FactionManager.FirstFactionOfDef(MCW_DefOf.MCW_RecycleCorp_Faction);
            foreach (var direction in GenAdj.CardinalDirections)
                DropCompany(map, defenders, 4, map.Center + direction * (HeadquartersLayout.Radius + 7));
            NextHeadquartersDrop = now + 2500;
        }
        if (StartedAt < 0)
        {
            StartedAt = now; NextHomeRaid = now + 10000;
            Find.LetterStack.ReceiveLetter("MinchoCandyWars.Finale.CounterattackTitle".Translate(),
                "MinchoCandyWars.Finale.CounterattackWarning".Translate(), LetterDefOf.ThreatBig, new TargetInfo(map.Center,map));
        }
        if (now < NextHomeRaid) return;
        var home = Find.Maps.Where(m => m.IsPlayerHome && m != map).OrderBy(m => m.uniqueID).FirstOrDefault();
        if (home == null) return; // Keep a single due wave; do not accumulate a burst while no home exists.
        var company = Find.FactionManager.FirstFactionOfDef(MCW_DefOf.MCW_RecycleCorp_Faction);
        int count = Math.Max(4, (Objective?.FinalScale ?? 4) * 2);
        if (DropCompany(home, company, count) == 0) return;
        NextHomeRaid = now + 20000;
        Find.LetterStack.ReceiveLetter("MinchoCandyWars.Finale.CounterattackTitle".Translate(),
            "MinchoCandyWars.Finale.CounterattackArrived".Translate(), LetterDefOf.ThreatBig, new TargetInfo(home.Center,home));
    }
    public static int DropCompany(Map target, Faction faction, int count, IntVec3? anchor = null)
    {
        if (faction == null || faction.defeated || Story.Current.Victory) return 0;
        int sent = 0;
        for (int i=0;i<count;i++)
        {
            bool Valid(IntVec3 c) => c.Standable(target) && !c.Roofed(target) && c.GetEdifice(target) == null;
            IntVec3 cell;
            if (anchor.HasValue)
            {
                var candidates = CellRect.CenteredOn(anchor.Value,12).Cells.Where(c => c.InBounds(target) && Valid(c)).ToList();
                if (candidates.Count == 0) continue;
                cell = candidates.RandomElement();
            }
            else if (!CellFinderLoose.TryFindRandomNotEdgeCellWith(8, Valid, target, out cell)) continue;
            var kind = DefDatabase<PawnKindDef>.GetNamed(i % 4 == 3 ? "MCW_RecycleCorp_Leader" : "MCW_RecycleCorp_Member");
            var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind,faction,forceGenerateNewPawn:true,canGeneratePawnRelations:false));
            pawn.questTags ??= new List<string>(); pawn.questTags.Add(FighterTag);
            DropPodUtility.DropThingsNear(cell,target,new List<Thing>{pawn},openDelay:110,canInstaDropDuringInit:false,
                leaveSlag:false,canRoofPunch:false,forbid:true,allowFogged:true,faction:faction);
            sent++;
        }
        return sent;
    }
    void RestoreGarrison(List<Pawn> pawns)
    {
        var posts = new[] { new IntVec3(-24,0,-24), new IntVec3(-24,0,24), new IntVec3(24,0,-24),
            new IntVec3(24,0,24), new IntVec3(-12,0,0), new IntVec3(12,0,0), IntVec3.Zero };
        foreach(var pawn in pawns)
        {
            if(pawn.MentalStateDef==MentalStateDefOf.PanicFlee || pawn.MentalStateDef == MCW_DefOf.GiveUpExit) pawn.MentalState.RecoverFromState();
            if(pawn.GetLord()?.LordJob is LordJob_CompanyGuard) continue;
            var post = pawn.kindDef == MCW_DefOf.MCW_RecycleCorp_Leader ? map.Center
                : posts.Select(offset=>map.Center+offset).OrderBy(cell=>cell.DistanceToSquared(pawn.Position)).First();
            pawn.GetLord()?.Notify_PawnLost(pawn,PawnLostCondition.ForcedToJoinOtherLord);
            pawn.jobs.StopAll();
            if(pawn.carryTracker.CarriedThing!=null) pawn.carryTracker.TryDropCarriedThing(pawn.Position,ThingPlaceMode.Near,out _);
            var lord=map.lordManager.lords.FirstOrDefault(l=>l.faction==pawn.Faction&&l.LordJob is LordJob_CompanyGuard guard&&guard.Post==post);
            if(lord!=null) lord.AddPawn(pawn);
            else LordMaker.MakeNewLord(pawn.Faction,new LordJob_CompanyGuard(post),map,new[]{pawn});
        }
    }
    public void EnforceOrders()
    {
        var pawns=map.mapPawns.AllPawnsSpawned.Where(p=>IsFighter(p)&&!p.Dead&&!p.Downed).ToList();
        ApplyOrders(pawns);
    }
    public void EnforcePawnOrders(Pawn pawn)
    {
        if (IsFighter(pawn) && !pawn.Dead && !pawn.Downed) ApplyOrders(new List<Pawn> { pawn });
    }
    private void ApplyOrders(List<Pawn> pawns)
    {
        if(pawns.Count==0)return;
        if(IsActive(map)) { RestoreGarrison(pawns); return; }
        var faction=pawns[0].Faction;
        var lord=map.lordManager.lords.FirstOrDefault(l=>l.faction==faction&&l.LordJob is LordJob_CompanyLastStand);
        var join=new List<Pawn>();
        foreach(var pawn in pawns)
        {
            if(pawn.MentalStateDef==MentalStateDefOf.PanicFlee || pawn.MentalStateDef == MCW_DefOf.GiveUpExit) pawn.MentalState.RecoverFromState();
            if(pawn.GetLord()?.LordJob is LordJob_CompanyLastStand)continue;
            pawn.GetLord()?.Notify_PawnLost(pawn,PawnLostCondition.ForcedToJoinOtherLord);
            pawn.jobs.StopAll();
            if(pawn.carryTracker.CarriedThing!=null)pawn.carryTracker.TryDropCarriedThing(pawn.Position,ThingPlaceMode.Near,out _);
            if(lord!=null)lord.AddPawn(pawn);else join.Add(pawn);
        }
        if(join.Count>0)LordMaker.MakeNewLord(faction,new LordJob_CompanyLastStand(faction),map,join);
    }
}
[HarmonyPatch(typeof(Pawn),nameof(Pawn.SpawnSetup))]
static class CompanyDropOrders
{
    static void Postfix(Pawn __instance)
    {
        if(HeadquartersCombat.IsFighter(__instance) && (__instance.questTags?.Contains(HeadquartersCombat.FighterTag) == true || __instance.Map?.GetComponent<HeadquartersLifecycle>().PlayerEntered == true)) __instance.Map?.GetComponent<HeadquartersCombat>().EnforcePawnOrders(__instance);
        else if (__instance.Spawned && __instance.questTags?.Contains(HeadquartersCombat.FighterTag) == true && __instance.Faction?.defeated == true)
            LordMaker.MakeNewLord(__instance.Faction, new LordJob_ExitMapBest(LocomotionUrgency.Sprint, canDig:false, canDefendSelf:false), __instance.Map, new[]{__instance});
    }
}
[HarmonyPatch(typeof(MentalStateHandler),nameof(MentalStateHandler.TryStartMentalState))]
static class CompanyNoPanic
{
    static bool Prefix(MentalStateDef stateDef,Pawn ___pawn)
        => !HeadquartersCombat.IsFighter(___pawn) || stateDef != MentalStateDefOf.PanicFlee && stateDef != MCW_DefOf.GiveUpExit;
}

