#nullable disable
using RimWorld;
using Verse;

namespace MinchoCandyWars.QuestStory;

public sealed partial class Story
{
    public List<int> GiftTicks = new(), GiftPoints = new();
    void GiftsExpose()
    {
        Scribe_Collections.Look(ref GiftTicks, "giftTicks101", LookMode.Value); Scribe_Collections.Look(ref GiftPoints, "giftPoints101", LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit) { GiftTicks ??= new(); GiftPoints ??= new(); }
    }
}
public static class Gifts
{
    // A single shared rolling window, not per-character goodwill or cooldowns.
    static readonly (string[] defs, int[] counts, int points)[] Menu = [
        (["Mincho_Mintchoco"], [200], 2),
        (["MCW_Delicious_Cookies"], [50], 2),
        (["Mincho_ChocoStick"], [100], 3),
        (["DevilstrandCloth"], [100], 3),
        (["MCW_NurseOutfit","MCW_NurseCap"], [1,1], 6),
        (["MCW_Qipao"], [1], 6)];
    static string GiftItems(string[] defs, int[] counts) =>
        string.Join(" + ", defs.Select((def, index) => $"{DefDatabase<ThingDef>.GetNamed(def).LabelCap} ×{counts[index]}"));

    public static void Open()
    {
        if (!MinchoCandyWars.StellarisEvent.StellarisEventFlagManager.HasFlag("MCW_QS1_AllianceJoined")) { StoryUtility.Letter("MinchoCandyWars.Gift.Title".Translate(), "MinchoCandyWars.Gift.Locked".Translate()); return; }
        if (Relations.Number >= 100) { StoryUtility.Letter("MinchoCandyWars.Gift.Title".Translate(), "MinchoCandyWars.Gift.Max".Translate()); return; }
        Find.WindowStack.Add(new FloatMenu(CharacterData.Names.Select((name,i) => new FloatMenuOption(CharacterData.DisplayName(i), StoryCampaign.CanTalk(i) ? () => Select(i) : null)).ToList()));
    }
    static void Select(int role) => Find.WindowStack.Add(new FloatMenu(Menu.Select((row,i) => new FloatMenuOption("MinchoCandyWars.Gift.Option".Translate(GiftItems(row.defs, row.counts), row.points).RawText, () => Preview(role,i))).ToList()));
    static bool Quota(int points)
    {
        var s=Story.Current; int now=Find.TickManager.TicksGame;
        for (int i=s.GiftTicks.Count-1;i>=0;i--) if(now-s.GiftTicks[i]>=60000) { s.GiftTicks.RemoveAt(i);s.GiftPoints.RemoveAt(i); }
        return s.GiftTicks.Count<2 && s.GiftPoints.Sum()+points<=6;
    }
    public static bool Eligible(Thing t) => !t.IsForbidden(Faction.OfPlayer) && !t.Position.Fogged(t.Map)
        && (t.TryGetComp<CompRottable>() is not { } rot || rot.Stage==RotStage.Fresh)
        && (!t.TryGetQuality(out var q) || q>=QualityCategory.Normal) && (!t.def.useHitPoints || t.HitPoints>=t.MaxHitPoints*0.8f);
    static List<(Thing thing,int count)> Collect(int selected)
    {
        var map=Find.AnyPlayerHomeMap;if(map==null)return null;
        var row=Menu[selected];var plan=new List<(Thing,int)>();
        for(int i=0;i<row.defs.Length;i++)
        {
            var def=DefDatabase<ThingDef>.GetNamed(row.defs[i]);int left=row.counts[i];
            foreach(var t in map.listerThings.ThingsOfDef(def).Where(Eligible))
            { int take=Math.Min(left,t.stackCount);plan.Add((t,take));left-=take;if(left==0)break; }
            if(left!=0)return null;
        }
        return plan;
    }
    static void Preview(int role,int selected)
    {
        var row=Menu[selected];var plan=Collect(selected);
        if(plan==null || !Quota(row.points)) { StoryUtility.Letter("MinchoCandyWars.Delivery.CannotDeliver".Translate(), "MinchoCandyWars.Gift.Unavailable".Translate());return; }
        bool used=false;
        string items = string.Join("\n", plan.Select(p => $"{p.thing.LabelCap} ×{p.count}"));
        string text = "MinchoCandyWars.Gift.Preview".Translate(CharacterData.DisplayName(role), items, Math.Min(row.points, 100 - Relations.Number));
        Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(text,()=>
        {
            if(used || !Quota(row.points) || Relations.Number>=100 || !StoryCampaign.CanTalk(role))return;
            // Recheck the shown inventory; do not silently substitute a different stack.
            if(plan.Any(p=>!p.thing.Spawned || !Eligible(p.thing) || p.thing.stackCount<p.count))return;
            used=true;
            foreach(var p in plan) p.thing.SplitOff(p.count).Destroy();
            var faction=Find.FactionManager.FirstFactionOfDef(MCW_DefOf.MCW_Alliance_Faction);
            faction.TryAffectGoodwillWith(Faction.OfPlayer,row.points);
            Story.Current.GiftTicks.Add(Find.TickManager.TicksGame);Story.Current.GiftPoints.Add(row.points);
            string name = CharacterData.DisplayName(role);
            string reply = $"MinchoCandyWars.Gift.Reply.{role}".Translate();
            StoryUtility.Letter("MinchoCandyWars.Gift.Accepted".Translate(name), $"{name}: {reply}\n" + "MinchoCandyWars.Gift.Goodwill".Translate(Relations.Value));
        }));
    }
}
