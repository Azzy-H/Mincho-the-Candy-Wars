#nullable disable
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace MinchoCandyWars.QuestStory;

public class MissionSiteWorker : SitePartWorker { }

// Native Site/Quest lifecycle remains responsible for travel, map ownership and expiry.
// Only success is narrowed to the actual objective, rather than a disappearing site.
public class MissionNode : QuestNode
{
    public int kind;
    protected override bool TestRunInt(Slate slate) => true;
    protected override void RunInt()
    {
        var site = QuestGen.slate.Get<Site>("site");
        float wealth = Find.AnyPlayerHomeMap?.wealthWatcher.WealthTotal ?? 0;
        var objective = new MissionObjective { Kind = kind, Site = site, Prisoner = QuestGen.slate.Get<Pawn>("prisoner"), Scale = CampaignPolicy.GuardScale(wealth), FinalScale = Math.Clamp((int)(wealth / 8000f) + 1, 4, 10) };
        site.SetFaction(Find.FactionManager.FirstFactionOfDef(DefDatabase<FactionDef>.GetNamed(kind == 5 ? "MCW_Alliance_Faction" : "MCW_RecycleCorp_Faction")));
        QuestGen.quest.AddPart(objective);
    }
}
public class MissionFactionNode : QuestNode
{
    public int kind;
    protected override bool TestRunInt(Slate slate) => true;
    protected override void RunInt() => QuestGen.slate.Set("siteFaction", Find.FactionManager.FirstFactionOfDef(DefDatabase<FactionDef>.GetNamed(kind == 5 ? "MCW_Alliance_Faction" : "MCW_RecycleCorp_Faction")));
}
public class MissionObjective : QuestPart
{
    public int Kind, EnteredAt = -1, Scale;
    public Site Site;
    public Pawn Prisoner;
    public Thing Sample, Computer;
    public List<Pawn> OutpostResidents = new();
    public bool OutpostResidentsRecorded;
    public bool Entered, ComputerKilled, ReinforcementsSent;
    public int NextWave = -1, FinalScale;
    public override string DescriptionPart => Kind switch
    {
        2 => "MinchoCandyWars.Mission.Rescue".Translate().RawText,
        3 or 5 or 9 => "MinchoCandyWars.Mission.Combat".Translate().RawText,
        4 => "MinchoCandyWars.Mission.Sample".Translate().RawText,
        6 => "MinchoCandyWars.Mission.Headquarters".Translate().RawText,
        _ => ""
    };
    public override IEnumerable<GlobalTargetInfo> QuestLookTargets { get { if (Site != null) yield return Site; } }
    public override void ExposeData()
    {
        base.ExposeData(); Scribe_Values.Look(ref Kind, "kind"); Scribe_Values.Look(ref Entered, "entered");
        Scribe_Values.Look(ref Scale, "scale");
        Scribe_Values.Look(ref EnteredAt, "enteredAt", -1); Scribe_Values.Look(ref ComputerKilled, "computerKilled");
        Scribe_Values.Look(ref WaveSlot, "waveSlot"); Scribe_Values.Look(ref ReinforcementsSent, "reinforcementsSent"); Scribe_Values.Look(ref NextWave, "nextWave", -1); Scribe_Values.Look(ref FinalScale, "finalScale");
        Scribe_References.Look(ref Site, "site"); Scribe_References.Look(ref Prisoner, "prisoner");
        Scribe_References.Look(ref Sample, "sample"); Scribe_References.Look(ref Computer, "computer");
        Scribe_Collections.Look(ref OutpostResidents, "outpostResidents", LookMode.Reference);
        Scribe_Values.Look(ref OutpostResidentsRecorded, "outpostResidentsRecorded");
        if (Scribe.mode == LoadSaveMode.PostLoadInit) OutpostResidents ??= new List<Pawn>();
    }
    public override void Cleanup()
    {
        base.Cleanup(); Sample?.questTags?.Remove("MCWQ101_Sample");
    }
    public void Check()
    {
        if (quest.State != QuestState.Ongoing) return;
        var map = Site?.Map;
        if (map != null && !Entered) { Entered = true; EnteredAt = Find.TickManager.TicksGame; Story.Letter("MinchoCandyWars.Mission.Title".Translate().RawText, DescriptionPart); }
        if (map != null && Entered) Reinforce(map);
        bool success = Kind switch
        {
            2 => Prisoner is { Dead: false, Faction.IsPlayer: true, IsPrisoner: false } &&
                Prisoner.MapHeld?.Parent != Site && (Prisoner.Spawned || Prisoner.IsCaravanMember() || IsRecovered(Prisoner)),
            4 => Sample != null && !Sample.Destroyed && IsRecovered(Sample),
            6 => ComputerKilled,
            3 or 5 or 9 => map != null && CampaignPolicy.ThreatDefeated(Entered, map.mapPawns.AllPawnsSpawned.Count(p => !p.Dead && !p.Downed && !p.IsPrisoner && p.Faction?.def == MCW_DefOf.MCW_RecycleCorp_Faction)),
            _ => false
        };
        if (Kind == 5 && map != null && !HasLivingOutpostResident(map))
        { quest.End(QuestEndOutcome.Fail); return; }
        if (success) { if (Kind == 6) HeadquartersVictory.Apply(map); quest.End(QuestEndOutcome.Success); return; }
        if (Kind == 2 && Prisoner?.Dead == true || Site == null || Site.Destroyed || Kind == 4 && Sample?.Destroyed == true)
            quest.End(QuestEndOutcome.Fail);
    }
    bool HasLivingOutpostResident(Map map)
    {
        if (OutpostResidentsRecorded || OutpostResidents.Count > 0)
            return OutpostResidents.Any(pawn => pawn is { Dead: false, Destroyed: false });

        // Older saves did not record which pawns originally lived at this site.
        return map.mapPawns.AllPawnsSpawned.Any(pawn => !pawn.Dead && pawn.Faction?.def == MCW_DefOf.MCW_Alliance_Faction);
    }

    void Reinforce(Map map)
    {
        int now = Find.TickManager.TicksGame;
        if (now < EnteredAt + (Kind == 6 ? 500 : 2500)) return;
        var faction = Find.FactionManager.FirstFactionOfDef(MCW_DefOf.MCW_Alliance_Faction);
        if (Kind == 3 && !ReinforcementsSent)
        {
            ReinforcementsSent = true;
            int n = Scale;
            MissionBuildings.Spawn(map, faction, "Mincho_Mercenary", 2 * n, true);
            MissionBuildings.Spawn(map, faction, "Mincho_Knight_Elite", n, true);
            MissionBuildings.Spawn(map, faction, "MCW_SmartMincho", 1, true);
        }
        if (Kind != 6 || !map.GetComponent<HeadquartersLifecycle>().PlayerEntered || ComputerKilled || NextWave >= 0 && now < NextWave) return;
        if (FinalScale == 0) FinalScale = Math.Clamp((int)((Find.AnyPlayerHomeMap?.wealthWatcher.WealthTotal ?? 0) / 8000f) + 1, 4, 10);
        // Continue rotating the reviewed human/mechanical formation until the computer is destroyed.
        int slot = WaveSlot % 16;
        string kind = slot switch
        {
            1 or 6 or 9 or 13 => "Mincho_Mercenary",
            2 or 11 => "Mincho_Defender", 4 => "Mincho_Knight_Elite", 7 => "MCW_SmartMincho",
            0 or 5 or 10 or 14 => "MCW_Mech_Battle", _ => "MCW_Mech_Defender"
        };
        var entry = HeadquartersLayout.ReinforcementEntry(map, slot);
        // One golem replaces a defender per rotation; do not increase wave size or cadence.
        int golems = slot == 15 ? 1 : 0;
        MissionBuildings.Spawn(map, faction, kind, FinalScale - golems, true, entry);
        if (golems > 0) MissionBuildings.Spawn(map, faction, "MinchoWitch_MinchoGolem", golems, true, entry);
        WaveSlot++; NextWave = now + 1250;
    }
    public int WaveSlot;
    bool IsRecovered(Thing item)
    {
        var holder = item.ParentHolder;
        while (holder != null)
        {
            if (holder is Pawn pawn && pawn.Faction == Faction.OfPlayer && pawn.MapHeld?.Parent != Site && (pawn.Spawned || pawn.IsCaravanMember())) return true;
            if (holder is TravellingTransporters transport && transport.Faction == Faction.OfPlayer) return true;
            holder = holder.ParentHolder;
        }
        return item.MapHeld?.IsPlayerHome == true ||
            Find.WorldObjects.AllWorldObjects.OfType<Gravship>().Any(ship =>
                ship.Faction == Faction.OfPlayer && IsInCargo(item, ship.Things.Concat(ship.Pawns)));
    }
    public static bool IsInCargo(Thing item, IEnumerable<Thing> cargo)
    {
        // Gravship stores cargo and passengers separately; search both collections
        // including inventories. Keep identity matching for the original quest sample.
        foreach (var carried in cargo)
        {
            if (carried == item) return true;
            if (carried is IThingHolder holder &&
                ThingOwnerUtility.GetAllThingsRecursively(holder).Contains(item)) return true;
        }
        return false;
    }
    public static MissionObjective For(Map map) => Find.QuestManager.QuestsListForReading.Where(q => q.State == QuestState.Ongoing)
        .SelectMany(q => q.PartsListForReading.OfType<MissionObjective>()).FirstOrDefault(p => p.Site == map.Parent);
}
