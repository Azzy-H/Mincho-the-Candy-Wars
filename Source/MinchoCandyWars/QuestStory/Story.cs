#nullable disable
using HarmonyLib;
using MinchoCandyWars.StellarisEvent;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace MinchoCandyWars.QuestStory;

public sealed partial class Story : GameComponent
{
    public const string GuestDef = "MCWQ097_Hospitality";
    public const string A1 = "MCW_Event_QuestA1_AllianceContact";
    public static Story Current => Verse.Current.Game.GetComponent<Story>();

    // ──────────────────── 状态（flag 单源）────────────────────
    // 标量与引用状态统一存在 StellarisEventFlagManager 里，随存档持久化；
    // Story 自身不再持有这些字段（Guests / State / 集合类仍保留，见下）。

    public StoryState State = new();
    public List<Pawn> Guests = new();

    public Pawn First
    {
        get => StoryFlags.GetPawn("MCW_Story_First");
        set => StoryFlags.SetPawn("MCW_Story_First", value);
    }

    public Quest Series
    {
        get => StoryFlags.GetQuest("MCW_Story_Series");
        set => StoryFlags.SetQuest("MCW_Story_Series", value);
    }

    public int ArrivalAt
    {
        get => StoryFlags.GetInt("MCW_Story_ArrivalAt", -1);
        set => StoryFlags.SetInt("MCW_Story_ArrivalAt", value);
    }

    public int DepartAt
    {
        get => StoryFlags.GetInt("MCW_Story_DepartAt", -1);
        set => StoryFlags.SetInt("MCW_Story_DepartAt", value);
    }

    public bool Started
    {
        get => StoryFlags.GetBool("MCW_Story_Started");
        set => StoryFlags.SetBool("MCW_Story_Started", value);
    }

    public bool ContactReady
    {
        get => StoryFlags.GetBool("MCW_Story_ContactReady");
        set => StoryFlags.SetBool("MCW_Story_ContactReady", value);
    }

    public bool A1Viewed
    {
        get => StoryFlags.GetBool("MCW_Story_A1Viewed");
        set => StoryFlags.SetBool("MCW_Story_A1Viewed", value);
    }

    public bool CompletedBatch
    {
        get => StoryFlags.GetBool("MCW_Story_CompletedBatch");
        set => StoryFlags.SetBool("MCW_Story_CompletedBatch", value);
    }

    public bool HasAcceptedHospitality
    {
        get => StoryFlags.GetBool("MCW_Story_HasAcceptedHospitality");
        set => StoryFlags.SetBool("MCW_Story_HasAcceptedHospitality", value);
    }

    public bool FirstTemporary
    {
        get => StoryFlags.GetBool("MCW_Story_FirstTemporary");
        set => StoryFlags.SetBool("MCW_Story_FirstTemporary", value);
    }

    public string Result
    {
        get => StoryFlags.GetString("MCW_Story_Result", "尚未开始");
        set => StoryFlags.SetString("MCW_Story_Result", value);
    }

    public Story(Game game) { }

    public override void ExposeData()
    {
        CampaignExpose();
        ReadLegacySave();
        // 标量与引用状态由 flag 自行持久化，这里只处理集合与 StoryState。
        Scribe_Values.Look(ref State.ActiveQuest, "guestQuest", -1);
        Scribe_Values.Look(ref State.LastSettled, "lastSettled", -1);
        Scribe_Values.Look(ref State.NextContactTick, "nextContactTick", -1);
        Scribe_Collections.Look(ref Guests, "guestPawns", LookMode.Reference);
        if (Scribe.mode == LoadSaveMode.PostLoadInit) Guests ??= new();
    }
    public override void GameComponentTick()
    {
        int now = Find.TickManager.TicksGame;
        if (!Started && Find.AnyPlayerHomeMap != null) StoryCampaign.StartStory();
        if (now % 60 != 0) return;

        // 核心职责：为没有 Tick 的 QuestPart 补周期驱动。
        // 每个任务部件自己实现 ITickableQuestPart.Tick()，Story 只负责遍历与触发。
        foreach (Quest quest in Find.QuestManager.QuestsListForReading)
        {
            if (quest.State != QuestState.Ongoing) continue;
            foreach (QuestPart part in quest.PartsListForReading)
            {
                (part as ITickableQuestPart)?.Tick();
            }
        }

        First ??= StellarisEventFlagManager.GetTrackedThing("MCW_QS1_HuntedMincho") as Pawn;
        if (ArrivalAt >= 0 && (First != null || Find.QuestManager.QuestsListForReading.Any(q => q.root == MCW_DefOf.MCW_QS1_HuntedMincho))) ArrivalAt = -1;
        if (ArrivalAt >= 0 && now >= ArrivalAt)
        {
            var def = MCW_DefOf.MCW_GiveQuest_QS1_MinchoMincho;
            var map = Find.AnyPlayerHomeMap;
            if (map != null) StoryCampaign.EnsureSeries();
            if (map != null && def.Worker.TryExecute(StorytellerUtility.DefaultParmsNow(def.category, map)))
            { ArrivalAt = -1; First ??= StellarisEventFlagManager.GetTrackedThing("MCW_QS1_HuntedMincho") as Pawn; }
        }
        if (!Appearance103)
        {
            for (int role = 0; role < Chiefs.Count; role++)
                if (Chiefs[role] is { Dead: false } chief) Appearance.Apply(chief, role);
            Appearance103 = true;
        }
        StoryCampaign.Tick(now);
    }

    public bool IsFirstHospitality => !HasAcceptedHospitality && (Succeeded & (1 << 1)) == 0 &&
        !Find.QuestManager.QuestsListForReading.Any(q => q.root?.defName == GuestDef);
    internal void RefreshHospitalityText(Quest quest, RewardPlan plan)
    {
        quest.name = "MinchoCandyWars.Hospitality.Title".Translate().RawText;
        quest.description = "MinchoCandyWars.Hospitality.Issuer".Translate(IssuerName, Guests.Count).RawText
            + "\n\n" + StoryUtility.GuestDescription(plan);
    }

    /// <summary>兼容旧调用点：转发到 StoryCampaign 的判断。</summary>
    public bool CanAccept => StoryCampaign.CanAccept;

    public void RejectGuests()
    {
        StoryCampaign.RejectOffer(1);
    }
    public void EndGuests(bool success) => StoryCampaign.FinishMission(1, success);
    public string Status()
    {
        const string prefix = "MinchoCandyWars.AllianceStatus.";
        string Label(string key) => (prefix + key).Translate().RawText;
        string Remaining(int tick) => tick < 0 ? Label("None") :
            Math.Max(0, tick - Find.TickManager.TicksGame).ToStringTicksToPeriod();
        string firstState = First == null ? "Unknown" : First.Dead ? "Dead" :
            !First.Spawned ? "OffMap" : FirstTemporary ? "Guest" : "OnMap";
        return (prefix + "Report").Translate(Relations.Summary, Label(firstState),
            Remaining(CompanyDeadline), Remaining(ArrivalAt), State.ActiveQuest,
            Guests.Count(p => p != null && !p.Dead), Remaining(DepartAt), Remaining(State.NextContactTick),
            StellarisEventFlagManager.GetFlag("MCW_EndgameProgress")?.floatValue ?? 0,
            Offer > 0 ? $"A{Offer}" : Label("None"), Label(Victory ? "Complete" : "Pending"), Remaining(FinalAt)).RawText;
    }
}

public class StoryEffect : StellarisEventEffect
{
    public string operation;
    public override void Execute()
    {
        var s = Story.Current;
        switch (operation)
        {
            case "start": StoryCampaign.StartStory(); break;
            case "mincho": StoryCampaign.CorpResolved(true); break;
            case "corp": StoryCampaign.CorpResolved(false); break;
            case "accept": StoryCampaign.AcceptGuests(); break;
            case "reject": StoryCampaign.RejectOffer(1); break;
            case "status": Find.WindowStack.Add(new Dialog_MessageBox(StoryCampaign.Status())); break;
            case "allianceDeclined": StoryUtility.Letter("MinchoCandyWars.StoryLetter.AllianceDeclinedTitle".Translate().RawText, "MinchoCandyWars.StoryLetter.AllianceDeclinedBody".Translate().RawText); break;
        }
    }
}
public class ReadyTrigger : StellarisEventTrigger
{
    public override bool CanTrigger() => Verse.Current.Game != null && Story.Current.CanAccept && Story.Current.Offer == 1;
}

// Apply text on every opening, including restored windows, using the actual first pawn.
[HarmonyPatch(typeof(StellarisEventWindow), MethodType.Constructor, typeof(StellarisEventDef))]
static class DialogText
{
    static void Prefix(StellarisEventDef def)
    {
        var s = Story.Current;
        if (def.defName == s.OfferEvent)
        {
            int role = StoryCampaign.AvailableContact();
            if (role < 0) return;
            def.pictures = [new StellarisEventPictureDef { path = CharacterData.Portraits[role] }];
            def.selectedPicture = ContentFinder<UnityEngine.Texture2D>.Get(CharacterData.Portraits[role]);
            def.selectedPictureOffsetX = def.selectedPictureOffsetY = 0f;
            def.title = "MinchoCandyWars.AllianceContact.OfferTitle".Translate(CharacterData.DisplayName(role), Relations.Value, StoryCampaign.Affinity(role));
            def.desc = OfferDialogue.Request(s.Offer, role, (s.Viewed & (1 << s.Offer)) != 0, s.Victory);
            return;
        }

        if ((def == MCW_EventDefOf.MCWQ101_Chat0 || def == MCW_EventDefOf.MCWQ101_Chat1 || def == MCW_EventDefOf.MCWQ101_Chat2 || def == MCW_EventDefOf.MCWQ101_Chat3 || def == MCW_EventDefOf.MCWQ101_Chat4 || def == MCW_EventDefOf.MCWQ101_Chat5))
        {
            int chatRole = def.defName[^1] - '0';
            def.title = $"MinchoCandyWars.Alliance.Name.{chatRole}";
            int chatBit = 1 << chatRole;
            string greetingKey = (s.Chatted & chatBit) != 0 ? "RepeatGreeting" : "Greeting";
            string greeting = $"MinchoCandyWars.Chat.{greetingKey}.{chatRole}".Translate();
            s.Chatted |= chatBit;
            def.desc = $"{CharacterData.DisplayName(chatRole)}: {greeting}";
            for (int index = 0; index < 2; index++)
            {
                string prefix = $"MinchoCandyWars.Chat.Legacy.{chatRole}.{index}.";
                if ((prefix + "Label").TryTranslate(out var label))
                    def.options[index].label = label.RawText;
                if ((prefix + "Reply").TryTranslate(out var reply))
                    def.options[index].desc = reply.RawText;
            }
            if (chatRole == 2 && "MinchoCandyWars.Chat.Legacy.Kitchen".TryTranslate(out var kitchen))
                def.options[2].label = kitchen.RawText;
            def.options[^1].label = "MinchoCandyWars.Chat.EndCall";
            return;
        }

        if (def == MCW_EventDefOf.MCW_Event_AllianceGreeting)
        { def.desc = "MinchoCandyWars.AllianceContact.Greeting".Translate(CharacterData.DisplayName(0)) + "\n\n" + Relations.Summary; return; }
        if ((def == MCW_EventDefOf.MCW_Event_AllianceCandyShop || def == MCW_EventDefOf.MCW_Event_AllianceBoosterShop))
        { def.desc = "MinchoCandyWars.AllianceContact.ShopGreeting".Translate(CharacterData.DisplayName(0)) + "\n\n" + Relations.Summary; return; }
        if (def == MCW_EventDefOf.MCW_Event_QS1_CorpContact)
        {
            def.title = "MinchoCandyWars.CorpContact.Title";
            def.desc = "MinchoCandyWars.CorpContact.Demand".Translate() + "\n\n" +
                "MinchoCandyWars.CorpContact.Manifest".Translate();
            return;
        }
    }
}


