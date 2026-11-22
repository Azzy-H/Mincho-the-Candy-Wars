#nullable disable
using System.Reflection;
using HarmonyLib;
using MinchoCandyWars;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace MinchoCandyWars.QuestStory;

// Reuse the native refugee lifecycle rather than reproducing its arrest, death,
// recruitment and departure rules. The three substitutions apply only inside this root.
public class RefugeeRoot : QuestNode_Root_Hospitality_Refugee
{
    [ThreadStatic] internal static RefugeeRoot Generating;
    internal int Count, Generated;
    internal bool Smart;
    static readonly FieldInfo Duration = AccessTools.Field(typeof(QuestNode_Root_Hospitality_Refugee), "QuestDurationDaysRange");

    protected override void RunInt()
    {
        Count = Rand.RangeInclusive(3, 7);
        Smart = Rand.Chance(0.1f);
        Generated = 0;
        var previous = Duration.GetValue(null);
        Generating = this;
        try
        {
            if (Story.Current.IsFirstHospitality)
                Duration.SetValue(null, new IntRange(StoryState.GuestDays, StoryState.GuestDays));
            base.RunInt();
        }
        finally { Duration.SetValue(null, previous); Generating = null; }

        var quest = QuestGen.quest;
        var guests = QuestGen.slate.Get<List<Pawn>>("lodgers");
        if (guests.NullOrEmpty()) throw new InvalidOperationException("庇护任务未生成暂住者");
        Story.Current.Guests = guests.ToList();
        Story.Current.DepartAt = Find.TickManager.TicksGame + QuestGen.slate.Get<int>("questDurationTicks");
        // Keep the existing material budget; attach only a guarded settlement.
        int oldCount = quest.PartsListForReading.Count;
        new MinchoCandyWars.QuestNodes.QuestNode_GiveAllianceReward().Run();
        var reward = quest.PartsListForReading.Skip(oldCount)
            .OfType<MinchoCandyWars.QuestParts.QuestPart_GiveAllianceReward>().Single();
        reward.factionDef = MCW_DefOf.MCW_Alliance_Faction;
        reward.goodwillChange = StoryState.GoodwillReward(true);
        reward.endgameProgress = StoryState.ProgressReward(true);
        quest.PartsListForReading.Remove(reward);
        var settlement = new GuestSettlement { Reward = reward, quest = quest };
        settlement.Prepare(false);
        quest.parent = Story.Current.Series;
        quest.AddPart(settlement);
    }
}

[HarmonyPatch(typeof(QuestNode_Root_Beggars), nameof(QuestNode_Root_Beggars.LodgerCountFromPopulation))]
static class GuestCount
{
    static void Postfix(ref int __result)
    { if (RefugeeRoot.Generating is { } root) __result = root.Count + (root.Smart ? 1 : 0); }
}

[HarmonyPatch(typeof(QuestGen_Pawns), nameof(QuestGen_Pawns.GeneratePawn_NewTemp))]
static class GuestKind
{
    static void Prefix(ref PawnKindDef kindDef)
    {
        if (RefugeeRoot.Generating is not { } root || kindDef != PawnKindDefOf.Refugee) return;
        kindDef = DefDatabase<PawnKindDef>.GetNamed(root.Smart && root.Generated++ == root.Count ? "MCW_SmartMincho" : "Mincho_SpaceRefugee");
    }
    static void Postfix(Pawn __result)
    {
        if (RefugeeRoot.Generating != null) AllianceBelief.Apply(__result);
        if (RefugeeRoot.Generating == null || __result?.kindDef != MCW_DefOf.MCW_SmartMincho) return;
        if (__result.GetComp<CompMinchoCore>() is { Active: false } core) core.Active = true;
    }
}

public class GuestSettlement : QuestPart
{
    public MinchoCandyWars.QuestParts.QuestPart_GiveAllianceReward Reward;
    public ThingDef Bonus;
    public RewardPlan Plan;
    bool settled;
    bool checkedMutiny;
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref Bonus, "bonus");
        Scribe_Deep.Look(ref Plan, "rewardPlan100");
        // Old saves nested a detached QuestPart here. Read its data without
        // registering the detached part as a load-referenceable object.
        if (Scribe.mode == LoadSaveMode.LoadingVars && Plan == null && Scribe.EnterNode("materialReward"))
        {
            try
            {
                Reward = new MinchoCandyWars.QuestParts.QuestPart_GiveAllianceReward();
                Scribe_Collections.Look(ref Reward.rewardDefs, "rewardDefs", LookMode.Def);
                Scribe_Collections.Look(ref Reward.rewardCounts, "rewardCounts", LookMode.Value);
            }
            finally { Scribe.ExitNode(); }
        }
        Scribe_Values.Look(ref settled, "settled");
    }
    // Also upgrades an ongoing 097 playtest instance without generating a new group.
    public void Prepare(bool existing)
    {
        if (existing && quest.name == "收留珉巧")
            quest.name = "MinchoCandyWars.Hospitality.Title".Translate().RawText;

        if (!checkedMutiny)
        {
            // The native refugee root schedules random mutiny as a separate delay.
            quest.PartsListForReading.RemoveAll(p => p is QuestPart_Delay &&
                p.debugLabel?.StartsWith("Mutiny (", StringComparison.Ordinal) == true);
            checkedMutiny = true;
        }
        if (Plan != null) return;
        EnsurePlan();
        quest.PartsListForReading.RemoveAll(p => p is QuestPart_AddQuestRefugeeDelayedReward);
        Plan.AttachPreview(quest, true);
        if (existing)
        {
            var delay = quest.PartsListForReading.OfType<QuestPart_Delay>().FirstOrDefault(p => p.debugLabel == "QuestDelay");
            if (delay != null) { Story.Current.DepartAt = Find.TickManager.TicksGame + Math.Max(0, delay.TicksLeft); }
            quest.parent = Story.Current.EnsureSeries();
            quest.description = Story.GuestDescription(Plan);
        }
    }
    void EnsurePlan()
    {
        if (Plan != null) return;
        Plan = new RewardPlan { Goodwill = 40 };
        if (Reward?.rewardDefs != null && Reward.rewardCounts != null)
            for (int i = 0; i < Math.Min(Reward.rewardDefs.Count, Reward.rewardCounts.Count); i++)
                if (Reward.rewardDefs[i] != null && Reward.rewardCounts[i] > 0)
                    Plan.Add(Reward.rewardDefs[i].defName, Reward.rewardCounts[i]);
        Plan.AddGuaranteed(true);
        Reward = null;
    }
    public override void Cleanup()
    {
        base.Cleanup();
        if (settled || !Story.Current.State.Settle(quest.id)) return;
        settled = true;
        bool success = quest.State == QuestState.EndedSuccess;
        if (success)
        {
            EnsurePlan();
            Plan.Pay();
        }
        Story.Current.EndGuests(success);
    }
}

