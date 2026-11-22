using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;
using Verse.AI;

namespace MinchoCandyWars.QuestStory;

public static class HeadquartersVictory
{
    public static void Apply(Map headquarters)
    {
        var company = Find.FactionManager.FirstFactionOfDef(MCW_DefOf.MCW_RecycleCorp_Faction);
        if (company == null) return;
        company.defeated = true;
        // Retain the faction object: existing pawns and old saves still reference it.
        foreach (var other in Find.FactionManager.AllFactionsListForReading)
            if (other != company && (other.IsPlayer || other.def == MCW_DefOf.MCW_Alliance_Faction))
                {
                    // SetRelationDirect rejects goodwill-based factions; write both saved relations.
                    company.RelationWith(other).baseGoodwill = 0;
                    other.RelationWith(company).baseGoodwill = 0;
                    company.RelationWith(other).kind = FactionRelationKind.Neutral;
                    other.RelationWith(company).kind = FactionRelationKind.Neutral;
                }
        foreach (var battlefield in Find.Maps.ToList())
        {
            var leavers = battlefield.mapPawns.AllPawnsSpawned.Where(p => p.Faction == company && !p.Dead && !p.Downed && !p.IsPrisoner
                && p.GetLord()?.LordJob is not LordJob_ExitMapBest).ToList();
            foreach (var pawn in leavers)
            {
                pawn.GetLord()?.Notify_PawnLost(pawn, PawnLostCondition.ForcedToJoinOtherLord);
                pawn.jobs.StopAll();
                pawn.mindState.enemyTarget = null;
            }
            if (leavers.Count > 0)
                LordMaker.MakeNewLord(company, new LordJob_ExitMapBest(LocomotionUrgency.Sprint, canDig: false, canDefendSelf: false), battlefield, leavers);
        }
        foreach (var parent in Find.WorldObjects.AllWorldObjects.OfType<MapParent>().Where(p => p.Faction == company).ToList())
        {
            // A loaded battlefield may contain the player's party and loot. Keep it as neutral ruins.
            if (parent.HasMap) parent.SetFaction(null);
            else parent.Destroy();
        }
    }
}
