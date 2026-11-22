#nullable disable
using HarmonyLib;
using MinchoCandyWars.StellarisEvent;
using RimWorld;
using Verse;

namespace MinchoCandyWars.QuestStory;

public sealed partial class Story
{
    public int Chatted;
    public bool Appearance103;
    public string SketchName;
    public List<Pawn> Chiefs = new();
    public List<bool> ChiefCreated = new();
    List<string> chiefDefaultNames = new();
    void CharactersExpose()
    {
        Scribe_Values.Look(ref Chatted, "chatted101");
        ChatExpose();
        AffinityExpose();
        Scribe_Values.Look(ref Appearance103, "appearance103");
        Scribe_Values.Look(ref SketchName, "sketchName101");
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
    internal void SetChiefDefaultName(Pawn pawn, int role)
    {
        while (chiefDefaultNames.Count <= role) chiefDefaultNames.Add(null);
        string name = DisplayName(role);
        pawn.Name = new NameSingle(name);
        chiefDefaultNames[role] = name;
    }

    void RefreshChiefDefaultNames()
    {
        for (int role = 0; role < Math.Min(Chiefs.Count, Names.Length); role++)
        {
            var pawn = Chiefs[role];
            string previousDefault = role < chiefDefaultNames.Count ? chiefDefaultNames[role] ?? Names[role] : Names[role];
            if (pawn?.Name is NameSingle name && name.ToStringShort == previousDefault && previousDefault != DisplayName(role))
                SetChiefDefaultName(pawn, role);
        }
    }

    public static string DisplayName(int role) => $"MinchoCandyWars.Alliance.Name.{role}".TryTranslate(out var name) ? name.RawText : Names[role];
    public static readonly string[] Names = ["沁心糖", "琥珀糖", "甘露糖", "千机糖", "琉璃糖", "玲珑糖"];
    public static readonly string[] Portraits = ["Events/Portraits/MCW_Qinxintang_Smile", "Events/Portraits/MCW_AmberonCandy", "MCWQ/Portraits/NectarClean100", "Events/Portraits/MCW_MechaCandy", "MCWQ/Portraits/Prism101", "Events/Portraits/MCW_LinglonCandy"];
    public int ContactRole => OfferIssuer < 0 ? 0 : OfferIssuer;
    public int AvailableContact()
    {
        if (OfferIssuer < 0) OfferIssuer = ContactRole;
        if (!CanTalk(OfferIssuer)) OfferIssuer = OfferPolicy.Issuers(Offer).FirstOrDefault(CanTalk, -1);
        return OfferIssuer;
    }
    public Pawn Chief(int role)
    {
        while (Chiefs.Count < Names.Length) Chiefs.Add(null);
        while (ChiefCreated.Count < Names.Length) ChiefCreated.Add(false);
        if (Chiefs[role] != null) return Chiefs[role];
        var candidates = PawnsFinder.AllMapsWorldAndTemporary_AliveOrDead.Where(p => p.GetComp<MinchoCandyWars.CompMinchoCore>() != null && p.Name?.ToStringShort == Names[role]).ToList();
        if (candidates.Count == 1) { ChiefCreated[role] = true; return Chiefs[role] = candidates[0]; }
        return null;
    }
    public bool CanTalk(int role)
    {
        var pawn = Chief(role);
        bool ambiguous = pawn == null && PawnsFinder.AllMapsWorldAndTemporary_AliveOrDead.Count(p => p.GetComp<MinchoCandyWars.CompMinchoCore>() != null && p.Name?.ToStringShort == Names[role]) > 1;
        return !ambiguous && (pawn == null ? !ChiefCreated[role] : !pawn.Dead && !pawn.Downed && !pawn.IsPrisoner && !pawn.Suspended && (pawn.Faction == Faction.OfPlayer || pawn.Faction?.def == MCW_DefOf.MCW_Alliance_Faction));
    }
    public bool CanKitchen => State.ActiveQuest >= 0 && ActiveKind == 1 && Guests.Any(p => p is { Dead: false, Spawned: true } && p.Map == Find.AnyPlayerHomeMap);
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
        if (!s.CanKitchen) { Story.Letter(Story.DisplayName(2), "MinchoCandyWars.Kitchen.Greeting".Translate().RawText); return; }
        string text = s.KitchenViewed ? "MinchoCandyWars.Kitchen.Again".Translate().RawText : "MinchoCandyWars.Kitchen.First".Translate().RawText;
        s.KitchenViewed = true;
        var root = new DiaNode(text);
        var yes = new DiaOption("MinchoCandyWars.Kitchen.Try".Translate().RawText) { resolveTree = true, action = () => Story.Letter("MinchoCandyWars.Kitchen.Title".Translate().RawText, "MinchoCandyWars.Kitchen.Reply".Translate().RawText) };
        root.options.Add(yes); root.options.Add(new DiaOption("MinchoCandyWars.Kitchen.Later".Translate().RawText) { resolveTree = true });
        Find.WindowStack.Add(new Dialog_NodeTree(root, true, false));
    }
}
