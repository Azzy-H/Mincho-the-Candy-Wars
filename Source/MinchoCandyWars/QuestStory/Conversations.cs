#nullable disable
using HarmonyLib;
using MinchoCandyWars.StellarisEvent;
using RimWorld;
using Verse;

namespace MinchoCandyWars.QuestStory;

public sealed partial class Story
{
    // 标量：flag 单源。
    public int Chatted
    {
        get => StoryFlags.GetInt("MCW_Story_Chatted", 0);
        set => StoryFlags.SetInt("MCW_Story_Chatted", value);
    }

    public bool Appearance103
    {
        get => StoryFlags.GetBool("MCW_Story_Appearance103");
        set => StoryFlags.SetBool("MCW_Story_Appearance103", value);
    }

    public string SketchName
    {
        get => StoryFlags.GetString("MCW_Story_SketchName");
        set => StoryFlags.SetString("MCW_Story_SketchName", value);
    }

    // 集合：保留字段（需要原地 Add/Remove）。
    internal List<Pawn> Chiefs = new();
    internal List<bool> ChiefCreated = new();
    internal List<string> chiefDefaultNames = new();
    void CharactersExpose()
    {
        ChatExpose();
        AffinityExpose();
        Scribe_Collections.Look(ref Chiefs, "chiefs101", LookMode.Reference);
        Scribe_Collections.Look(ref ChiefCreated, "chiefCreated101", LookMode.Value);
        Scribe_Collections.Look(ref chiefDefaultNames, "chiefDefaultNames", LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            Chiefs ??= new();
            ChiefCreated ??= new();
            chiefDefaultNames ??= new();
        }
    }
    // 角色/联系人的查询逻辑已迁到 StoryCampaign
    // （Chief / CanTalk / CanKitchen / AvailableContact / ContactRole / RefreshChiefDefaultNames）。
}

public class ConversationEffect : StellarisEventEffect
{
    public string operation;
    public override void Execute()
    {
        if (operation == "hub")
        {
            ConversationPages.Hub();
        }
        else if (operation == "invite") Invitations.Open();
        else if (operation == "gift") Gifts.Open();
        else if (operation == "deliver") DeliveryPart.Open();
        else if (operation == "kitchen") Kitchen();
        else if (operation.StartsWith("reply:"))
        {
            var id = operation.Split(':');
            ConversationPages.Reply(int.Parse(id[1]),int.Parse(id[2]));
        }
    }
    static void Kitchen()
    {
        var s = Story.Current;
        if (!StoryCampaign.CanKitchen) { StoryUtility.Letter(CharacterData.DisplayName(2), "MinchoCandyWars.Kitchen.Greeting".Translate().RawText); return; }
        string text = s.KitchenViewed ? "MinchoCandyWars.Kitchen.Again".Translate().RawText : "MinchoCandyWars.Kitchen.First".Translate().RawText;
        s.KitchenViewed = true;
        var root = new DiaNode(text);
        var yes = new DiaOption("MinchoCandyWars.Kitchen.Try".Translate().RawText) { resolveTree = true, action = () => StoryUtility.Letter("MinchoCandyWars.Kitchen.Title".Translate().RawText, "MinchoCandyWars.Kitchen.Reply".Translate().RawText) };
        root.options.Add(yes); root.options.Add(new DiaOption("MinchoCandyWars.Kitchen.Later".Translate().RawText) { resolveTree = true });
        Find.WindowStack.Add(new Dialog_NodeTree(root, true, false));
    }
}
