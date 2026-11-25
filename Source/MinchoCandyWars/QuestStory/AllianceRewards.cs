#nullable disable
using RimWorld;
using RimWorld.QuestGen;
using Verse;
using MinchoCandyWars.StellarisEvent;

namespace MinchoCandyWars.QuestStory;

public sealed class RewardPlan : IExposable
{
    public List<ThingDef> Defs = new();
    public List<int> Counts = new();
    public int Goodwill;
    public void ExposeData()
    {
        Scribe_Collections.Look(ref Defs, "defs", LookMode.Def);
        Scribe_Collections.Look(ref Counts, "counts", LookMode.Value);
        Scribe_Values.Look(ref Goodwill, "goodwill");
    }
    public void Add(string def, int count) { Defs.Add(DefDatabase<ThingDef>.GetNamed(def)); Counts.Add(count); }
    public void AddGuaranteed(bool first)
    {
        string[] candy = ["MCW_MechaCandyChangeItem", "MCW_NectarCandyChangeItem", "MCW_AmberonCandyChangeItem", "MCW_PrismCandyChangeItem", "MCW_LinglonCandyChangeItem"];
        Add(candy.RandomElement(), 1);
        int remaining = StoryState.BonusPackCount(first, Rand.RangeInclusive(1, 3));
        if (first || Rand.Bool)
        {
            Add("MCW_MinchoIntelBooster", 1);
            remaining--;
        }
        string[] upgrades = ["MCW_MinchoBodyBooster", "MCW_MinchoCoreBoosterBasic", "MCW_MinchoCoreBoosterAdvanced"];
        var selected = new Dictionary<string, int>();
        for (int i = 0; i < remaining; i++)
        {
            string upgrade = upgrades.RandomElement();
            selected[upgrade] = selected.GetValueOrDefault(upgrade) + 1;
        }
        foreach (var upgrade in selected) Add(upgrade.Key, upgrade.Value);
    }
    public List<Thing> MakeThings()
    {
        var things = new List<Thing>();
        for (int i = 0; i < Defs.Count; i++)
            for (int remaining = Counts[i]; remaining > 0;)
            {
                var thing = ThingMaker.MakeThing(Defs[i]);
                thing.stackCount = Math.Min(remaining, Defs[i].stackLimit);
                remaining -= thing.stackCount;
                things.Add(thing.def.Minifiable ? thing.MakeMinified() : thing);
            }
        return things;
    }
    public string Summary => string.Join("\n", Defs.Select((d, i) => $"{d.LabelCap} ×{Counts[i]}"));
    public void AttachPreview(Quest quest, bool replaceUnknown)
    {
        var choices = quest.PartsListForReading.OfType<QuestPart_Choice>().FirstOrDefault();
        if (choices == null) { choices = new QuestPart_Choice(); quest.AddPart(choices); }
        if (choices.choices.Count == 0) choices.choices.Add(new QuestPart_Choice.Choice());
        // Reward_Items只保存引用；预览物品必须由QuestPart深保存，读档才不会消失。
        var preview = new AllianceRewardPreview { Items = MakeThings() };
        quest.AddPart(preview);
        foreach (var choice in choices.choices)
        {
            if (replaceUnknown) choice.rewards.RemoveAll(r => r is Reward_Items || r is Reward_PossibleFutureReward);
            choice.rewards.Add(new Reward_Items { items = preview.Items });
        }
    }
    public void Pay(bool progress = true)
    {
        var alliance = Find.FactionManager.FirstFactionOfDef(MCW_DefOf.MCW_Alliance_Faction);
        if (alliance != null && alliance.HasGoodwill) alliance.TryAffectGoodwillWith(Faction.OfPlayer, Goodwill);
        float old = StellarisEventFlagManager.GetFlag("MCW_EndgameProgress")?.floatValue ?? 0f;
        if (progress && !Story.Current.Victory) StellarisEventFlagManager.SetFlag("MCW_EndgameProgress", old + 1);
        var map = Find.AnyPlayerHomeMap;
        if (map != null)
            PodDelivery.Send(MakeThings(), map);
        else
        {
            Story.Current.PendingRewards.Add(this);
            StoryUtility.Letter("MinchoCandyWars.StoryLetter.RewardPendingTitle".Translate().RawText, "MinchoCandyWars.StoryLetter.RewardPendingBody".Translate().RawText);
        }
    }
    public static RewardPlan Build(Quest quest, int goodwill, bool first, int pickCount = 4, bool bonus = true)
    {
        int count = quest.PartsListForReading.Count;
        new MinchoCandyWars.QuestNodes.QuestNode_GiveAllianceReward { pickCount = pickCount }.Run();
        var original = quest.PartsListForReading.Skip(count).OfType<MinchoCandyWars.QuestParts.QuestPart_GiveAllianceReward>().Single();
        quest.PartsListForReading.Remove(original);
        var plan = new RewardPlan { Defs = original.rewardDefs, Counts = original.rewardCounts, Goodwill = goodwill };
        if (quest.root == MCW_DefOf.MCW_QuestFF1_RecycleHQ) { for (int i = 0; i < plan.Counts.Count; i++) plan.Counts[i] *= 5; }
        if (bonus) plan.AddGuaranteed(first);
        return plan;
    }
}

public static class PodDelivery
{
    public static void Send(List<Thing> things, Map map)
    {
        if (map == null) throw new InvalidOperationException("同盟奖励没有可用主地图");
        var groups = things.GroupBy(t => t.def).Select(g => g.ToList()).ToList();
        DropPodUtility.DropThingGroupsNear(DropCellFinder.TradeDropSpot(map), map, groups,
            openDelay: 110, instaDrop: false, leaveSlag: false, canRoofPunch: false, forbid: false, allowFogged: false);
        Log.Message($"[MCWQ101] 同盟奖励空投已发出：{groups.Count}组");
    }
}

public class PodSupply : StellarisEventEffect
{
    public ThingDef thingDef, stuff;
    public int num = 1;
    public QualityCategory quality = QualityCategory.Normal;
    public override void Execute()
    {
        var items = new List<Thing>();
        for (int left = num; left > 0;)
        {
            var thing = ThingMaker.MakeThing(thingDef, stuff);
            thing.stackCount = Math.Min(left, thingDef.stackLimit); left -= thing.stackCount;
            thing.TryGetComp<CompQuality>()?.SetQuality(quality, ArtGenerationContext.Outsider);
            items.Add(thing.def.Minifiable ? thing.MakeMinified() : thing);
        }
        PodDelivery.Send(items, Find.AnyPlayerHomeMap);
    }
}

// Reusable for the existing B2-B5 roots; later story batches still control their availability.
public class QuestNode_AllianceReward : QuestNode
{
    public SlateRef<FactionDef> factionDef;
    public int kind;
    public SlateRef<int> goodwillChange;
    public SlateRef<int> endgameProgress = 1;
    protected override bool TestRunInt(Slate slate) => true;
    protected override void RunInt()
    {
        var quest = QuestGen.quest;
        var plan = RewardPlan.Build(quest, goodwillChange.GetValue(QuestGen.slate), false);
        quest.AddPart(new AllianceSettlement { Plan = plan, Kind = kind });
        plan.AttachPreview(quest, true);
        quest.parent = Story.Current.Series;
    }
}
public class AllianceSettlement : QuestPart
{
    public RewardPlan Plan;
    bool settled;
    public int Kind;
    public override void ExposeData() { base.ExposeData(); Scribe_Deep.Look(ref Plan, "plan"); Scribe_Values.Look(ref settled, "settled"); Scribe_Values.Look(ref Kind, "kind101"); }
    public override void Cleanup()
    {
        base.Cleanup();
        if (settled) return;
        settled = true;
        if (!Story.Current.State.Settle(quest.id)) return;
        bool success = quest.State == QuestState.EndedSuccess;
        if (success) Plan.Pay(Kind <= 5 || Kind >= 10);
        StoryCampaign.FinishMission(Kind, success);
    }
}

public class AllianceRewardPreview : QuestPart
{
    public List<Thing> Items = new();
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref Items, "previewItems", LookMode.Deep);
    }
}

