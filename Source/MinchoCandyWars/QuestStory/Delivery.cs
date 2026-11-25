#nullable disable
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace MinchoCandyWars.QuestStory;

public class DeliveryRoot : QuestNode
{
    public int kind;
    protected override bool TestRunInt(Slate slate) => true;
    protected override void RunInt()
    {
        var quest=QuestGen.quest;
        var plan=RewardPlan.Build(quest,kind >= 10 ? 20 : 5,false,kind >= 10 ? 4 : 2,kind >= 10);
        quest.AddPart(new AllianceSettlement { Kind=kind,Plan=plan });plan.AttachPreview(quest,true);quest.parent=Story.Current.Series;
        quest.AddPart(new DeliveryPart { Kind=kind,RequiredCount=kind==11 ? 24 : kind==7 ? 200 : 100,Deadline=Find.TickManager.TicksGame+5*60000 });
    }
}
public class DeliveryPart : QuestPart, ITickableQuestPart
{
    public int Kind,Deadline,RequiredCount;
    public string RequiredDef => Kind==11 ? "ComponentIndustrial" : Kind==7 ? "Mincho_ChocoStick" : "MCW_Delicious_Cookies";
    public int Count => RequiredCount > 0 ? RequiredCount : Kind==11 ? 12 : Kind==7 ? 100 : 50;
    public string RequiredLabel => $"{DefDatabase<ThingDef>.GetNamed(RequiredDef).LabelCap} ×{Count}";
    public override string DescriptionPart => "MinchoCandyWars.Delivery.Summary".Translate(RequiredLabel, Math.Max(0, (Deadline - Find.TickManager.TicksGame) / 2500f).ToString("0.#"));
    public override void ExposeData() { base.ExposeData();Scribe_Values.Look(ref Kind,"kind");Scribe_Values.Look(ref Deadline,"deadline");Scribe_Values.Look(ref RequiredCount,"requiredCount"); }
    public void Check() { if(quest.State==QuestState.Ongoing && Find.TickManager.TicksGame>=Deadline)quest.End(QuestEndOutcome.Fail); }
    public void Tick() => Check();
    public static void Open()
    {
        var quest=Find.QuestManager.QuestsListForReading.FirstOrDefault(q=>q.id==Story.Current.State.ActiveQuest && q.State==QuestState.Ongoing);
        var part=quest?.PartsListForReading.OfType<DeliveryPart>().FirstOrDefault();
        if(part==null) { StoryUtility.Letter("MinchoCandyWars.Delivery.Title".Translate(), "MinchoCandyWars.Delivery.NoQuest".Translate());return; }
        var map = Find.CurrentMap is { IsPlayerHome: true } current ? current : Find.AnyPlayerHomeMap;
        if (map == null) { StoryUtility.Letter("MinchoCandyWars.Delivery.CannotDeliver".Translate(), "MinchoCandyWars.Delivery.NoHome".Translate()); return; }
        int left=part.Count;var items=new List<(Thing thing,int count)>();
        foreach(var thing in map.listerThings.ThingsOfDef(DefDatabase<ThingDef>.GetNamed(part.RequiredDef)).Where(Gifts.Eligible))
        {int take=Math.Min(left,thing.stackCount);items.Add((thing,take));left-=take;if(left==0)break;}
        if(left>0) { StoryUtility.Letter("MinchoCandyWars.Delivery.ShortageTitle".Translate(), "MinchoCandyWars.Delivery.Shortage".Translate(part.RequiredLabel, part.Count - left, left));return; }
        bool delivered=false;
        Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("MinchoCandyWars.Delivery.Confirm".Translate(part.RequiredLabel),()=>
        {
            if(delivered || quest.State!=QuestState.Ongoing || Find.TickManager.TicksGame>=part.Deadline || items.Any(i=>!i.thing.Spawned || i.thing.Map != map || !Gifts.Eligible(i.thing) || i.thing.stackCount<i.count))return;
            delivered=true;foreach(var item in items)item.thing.SplitOff(item.count).Destroy();quest.End(QuestEndOutcome.Success);
        }));
    }
}


