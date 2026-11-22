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
    public StoryState State = new();
    public Pawn First;
    public Quest Series;
    public List<Pawn> Guests = new();
    public int ArrivalAt = -1, DepartAt = -1;
    public bool Started, ContactReady, A1Viewed, CompletedBatch;
    public string Result = "尚未开始";
    public Story(Game game) { }
    public override void ExposeData()
    {
        CampaignExpose();
        Scribe_Values.Look(ref State.ActiveQuest, "guestQuest", -1);
        Scribe_Values.Look(ref State.LastSettled, "lastSettled", -1);
        Scribe_Values.Look(ref State.NextContactTick, "nextContactTick", -1);
        Scribe_References.Look(ref First, "firstMincho");
        Scribe_Values.Look(ref FirstTemporary, "firstTemporary139");
        Scribe_References.Look(ref Series, "allianceSeries100");
        Scribe_Collections.Look(ref Guests, "guestPawns", LookMode.Reference);
        Scribe_Values.Look(ref ArrivalAt, "arrivalAt", -1);
        Scribe_Values.Look(ref DepartAt, "departAt", -1);
        Scribe_Values.Look(ref HasAcceptedHospitality, "hasAcceptedHospitality");
        Scribe_Values.Look(ref Started, "started");
        Scribe_Values.Look(ref ContactReady, "contactReady");
        Scribe_Values.Look(ref A1Viewed, "a1Viewed");
        Scribe_Values.Look(ref CompletedBatch, "completedBatch");
        Scribe_Values.Look(ref Result, "lastResult", "尚未开始");
        if (Scribe.mode == LoadSaveMode.PostLoadInit) Guests ??= new();
    }
    public override void GameComponentTick()
    {
        int now = Find.TickManager.TicksGame;
        if (!Started && Find.AnyPlayerHomeMap != null) StartStory();
        if (now % 60 != 0) return;
        First ??= StellarisEventFlagManager.GetTrackedThing("MCW_QS1_HuntedMincho") as Pawn;
        if (ArrivalAt >= 0 && (First != null || Find.QuestManager.QuestsListForReading.Any(q => q.root == MCW_DefOf.MCW_QS1_HuntedMincho))) ArrivalAt = -1;
        if (ArrivalAt >= 0 && now >= ArrivalAt)
        {
            var def = MCW_DefOf.MCW_GiveQuest_QS1_MinchoMincho;
            var map = Find.AnyPlayerHomeMap;
            if (map != null) EnsureSeries();
            if (map != null && def.Worker.TryExecute(StorytellerUtility.DefaultParmsNow(def.category, map)))
            { ArrivalAt = -1; First ??= StellarisEventFlagManager.GetTrackedThing("MCW_QS1_HuntedMincho") as Pawn; }
        }
        if (!Appearance103)
        {
            for (int role = 0; role < Chiefs.Count; role++)
                if (Chiefs[role] is { Dead: false } chief) Appearance.Apply(chief, role);
            Appearance103 = true;
        }
        CampaignTick(now);
    }

    public void StartStory()
    {
        if (Started) return;
        Started = true;
        StellarisEventFlagManager.SetFlag("MCW_QuestLine_Accepted", true);
        StellarisEventFlagManager.RemoveFlag("MCW_QuestLine_Declined");
        ArrivalAt = Find.TickManager.TicksGame + 60000;
        Result = "主线已开启，受庇护的珉巧将在1天后抵达";
    }
    public void CorpResolved(bool mincho)
    {
        CompanyDeadline = -1;
        if (mincho) JoinFirst();
        Result = mincho ? "保护受庇护的珉巧，等待同盟请求" : "进入公司分支（本批不扩写）";
        if (mincho) State.Schedule(Find.TickManager.TicksGame, Rand.RangeInclusive(2, 3) * 60000);
    }
    public bool CanAccept => (!Victory || CampaignPolicy.AllowedAfterVictory(Offer)) && ContactReady && State.ActiveQuest < 0 && AvailableContact() >= 0 && BucketMissions.Available(Offer);
    public Quest EnsureSeries()
    {
        if (Series == null)
        {
            var definition = MCW_DefOf.MCWQ100_AllianceSeries;
            Series = QuestUtility.GenerateQuestAndMakeAvailable(definition, 0f);
        }
        if (Series != null)
        {
            Series.name = "MinchoCandyWars.Hospitality.SeriesTitle".Translate().RawText;
            Series.description = "MinchoCandyWars.Hospitality.SeriesDescription".Translate().RawText;
        }
        return Series;
    }
    public bool HasAcceptedHospitality;
    public bool IsFirstHospitality => !HasAcceptedHospitality && (Succeeded & (1 << 1)) == 0 &&
        !Find.QuestManager.QuestsListForReading.Any(q => q.root?.defName == GuestDef);
    public static string GuestDescription(RewardPlan plan) =>
        "MinchoCandyWars.Hospitality.Description".Translate(
            Math.Max(0, Current.DepartAt - Find.TickManager.TicksGame).ToStringTicksToPeriod(), plan.Summary).RawText;
    void RefreshHospitalityText(Quest quest, RewardPlan plan)
    {
        quest.name = "MinchoCandyWars.Hospitality.Title".Translate().RawText;
        quest.description = "MinchoCandyWars.Hospitality.Issuer".Translate(IssuerName, Guests.Count).RawText
            + "\n\n" + GuestDescription(plan);
    }

    public void AcceptGuests()
    {
        if (!CanAccept) return;
        var map = Find.AnyPlayerHomeMap;
        if (map == null) return;
        EnsureSeries();
        var slate = new Slate(); slate.Set("map", map); slate.Set("points", StorytellerUtility.DefaultThreatPointsNow(map));
        var quest = QuestUtility.GenerateQuestAndMakeAvailable(DefDatabase<QuestScriptDef>.GetNamed(GuestDef), slate);
        if (quest == null) { Log.Error("[MCWQ097] 庇护任务生成失败，请保留日志"); return; }
        if (!State.Begin(quest.id)) throw new InvalidOperationException("已有正在进行的庇护任务");
        if (quest.State == QuestState.NotYetAccepted) quest.Accept(null);
        ContactReady = false;
        HasAcceptedHospitality = true;
        RefreshHospitalityText(quest, quest.PartsListForReading.OfType<GuestSettlement>().Single().Plan);
        // QuestManager.Add initiates auto-accepted quests.
        Result = "B1庇护进行中";
        Letter("MinchoCandyWars.StoryLetter.GuestsArrivedTitle".Translate().RawText,
            "MinchoCandyWars.StoryLetter.GuestsArrivedBody".Translate().RawText);
    }
    public void RejectGuests()
    {
        RejectOffer(1);
    }
    public void EndGuests(bool success) => FinishMission(1, success);
    public static void Letter(string title, string text) => Find.LetterStack.ReceiveLetter(title, text, LetterDefOf.NeutralEvent);
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
            case "start": s.StartStory(); break;
            case "mincho": s.CorpResolved(true); break;
            case "corp": s.CorpResolved(false); break;
            case "accept": s.AcceptGuests(); break;
            case "reject": s.RejectGuests(); break;
            case "status": Find.WindowStack.Add(new Dialog_MessageBox(s.Status())); break;
            case "allianceDeclined": Story.Letter("MinchoCandyWars.StoryLetter.AllianceDeclinedTitle".Translate().RawText, "MinchoCandyWars.StoryLetter.AllianceDeclinedBody".Translate().RawText); break;
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
            int role = s.AvailableContact();
            if (role < 0) return;
            def.pictures = [new StellarisEventPictureDef { path = Story.Portraits[role] }];
            def.selectedPicture = ContentFinder<UnityEngine.Texture2D>.Get(Story.Portraits[role]);
            def.selectedPictureOffsetX = def.selectedPictureOffsetY = 0f;
            def.title = "MinchoCandyWars.AllianceContact.OfferTitle".Translate(Story.DisplayName(role), Relations.Value, s.Affinity(role));
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
            def.desc = $"{Story.DisplayName(chatRole)}: {greeting}";
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
        { def.desc = "MinchoCandyWars.AllianceContact.Greeting".Translate(Story.DisplayName(0)) + "\n\n" + Relations.Summary; return; }
        if ((def == MCW_EventDefOf.MCW_Event_AllianceCandyShop || def == MCW_EventDefOf.MCW_Event_AllianceBoosterShop))
        { def.desc = "MinchoCandyWars.AllianceContact.ShopGreeting".Translate(Story.DisplayName(0)) + "\n\n" + Relations.Summary; return; }
        if (def == MCW_EventDefOf.MCW_Event_QS1_CorpContact)
        {
            def.title = "MinchoCandyWars.CorpContact.Title";
            def.desc = "MinchoCandyWars.CorpContact.Demand".Translate() + "\n\n" +
                "MinchoCandyWars.CorpContact.Manifest".Translate();
            return;
        }
    }
}


